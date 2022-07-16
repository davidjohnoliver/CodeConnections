#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace CodeConnections.Services;

public class ActiveDocumentChangedEventArgs : EventArgs
{
	public static new ActiveDocumentChangedEventArgs Empty { get; } = new();
}
