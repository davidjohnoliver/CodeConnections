﻿// https://github.com/dotnet/roslyn/blob/version-3.2.0/src/Compilers/Core/Portable/InternalUtilities/PlatformInformation.cs

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Roslyn.Utilities
{
	/// <summary>
	/// This class provides simple properties for determining whether the current platform is Windows or Unix-based.
	/// We intentionally do not use System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(...) because
	/// it incorrectly reports 'true' for 'Windows' in desktop builds running on Unix-based platforms via Mono.
	/// </summary>
	internal static class PlatformInformation
	{
		public static bool IsWindows => Path.DirectorySeparatorChar == '\\';
		public static bool IsUnix => Path.DirectorySeparatorChar == '/';
		public static bool IsRunningOnMono
		{
			get
			{
				try
				{
					return !(Type.GetType("Mono.Runtime") is null);
				}
				catch
				{
					// Arbitrarily assume we're not running on Mono.
					return false;
				}
			}
		}
	}
}
