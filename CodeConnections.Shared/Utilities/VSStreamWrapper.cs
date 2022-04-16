// Adapted from https://www.codeproject.com/Articles/32906/Use-Visual-Studio-Extensibility-to-Make-a-Solution
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.OLE.Interop;

namespace CodeConnections.Utilities
{
	/// <summary>
	/// Wraps a VS stream object as a .NET <see cref="Stream"/>, so as not to have to deal with a VS stream object.
	/// </summary>
	public class VSStreamWrapper : Stream
	{
		private readonly IStream _iStream;

		public VSStreamWrapper(IStream vsStream)
		{
			_iStream = vsStream;
		}

		public override bool CanRead
		{
			get { return _iStream != null; }
		}

		public override bool CanSeek
		{
			get { return true; }
		}

		public override bool CanWrite
		{
			get { return true; }
		}

		public override void Flush()
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			_iStream.Commit(0);
		}

		public override long Length
		{
			get
			{
				Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
				STATSTG[] stat = new STATSTG[1];
				_iStream.Stat(stat, (uint)STATFLAG.STATFLAG_DEFAULT);

				return (long)stat[0].cbSize.QuadPart;
			}
		}

		public override long Position
		{
			get
			{
				Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
				return Seek(0, SeekOrigin.Current);
			}

			set
			{
				Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
				Seek(value, SeekOrigin.Begin);
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			if (buffer == null)
				throw new ArgumentNullException("Buffer cannot be null.");

			uint byteCounter;
			byte[] b = buffer;

			if (offset != 0)
			{
				b = new byte[buffer.Length - offset];
				buffer.CopyTo(b, 0);
			}

			_iStream.Read(b, (uint)count, out byteCounter);

			if (offset != 0)
			{
				b.CopyTo(buffer, offset);
			}

			return (int)byteCounter;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			LARGE_INTEGER l = new LARGE_INTEGER();
			ULARGE_INTEGER[] ul = new ULARGE_INTEGER[1] { new ULARGE_INTEGER() };
			l.QuadPart = offset;
			_iStream.Seek(l, (uint)origin, ul);
			return (long)ul[0].QuadPart;
		}

		public override void SetLength(long value)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			if (!CanWrite)
				throw new InvalidOperationException();

			ULARGE_INTEGER ul = new ULARGE_INTEGER();
			ul.QuadPart = (ulong)value;
			_iStream.SetSize(ul);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			if (buffer == null)
				throw new ArgumentNullException("Buffer cannot be null.");
			else if (!CanWrite)
				throw new InvalidOperationException();

			uint byteCounter;

			if (count > 0)
			{

				byte[] b = buffer;

				if (offset != 0)
				{
					b = new byte[buffer.Length - offset];
					buffer.CopyTo(b, 0);
				}

				_iStream.Write(b, (uint)count, out byteCounter);
				if (byteCounter != count)
					throw new IOException("Failed to write the total number of bytes to IStream!");

				if (offset != 0)
				{
					b.CopyTo(buffer, offset);
				}
			}
		}
	}

}
