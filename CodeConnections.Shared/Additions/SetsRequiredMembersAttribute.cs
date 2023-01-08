// https://github.com/dotnet/runtime/blob/f08d13f7f912dd3d89508b3ccd48f47c85c56ddd/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/SetsRequiredMembersAttribute.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis
{
	/// <summary>
	/// Specifies that this constructor sets all required members for the current type, and callers
	/// do not need to set any required members themselves.
	/// </summary>
	[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
	internal
#endif
		sealed class SetsRequiredMembersAttribute : Attribute
	{ }
}
