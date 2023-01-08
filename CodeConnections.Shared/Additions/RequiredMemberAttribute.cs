// https://github.com/dotnet/runtime/blob/f08d13f7f912dd3d89508b3ccd48f47c85c56ddd/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/RequiredMemberAttribute.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
	/// <summary>Specifies that a type has required members or that a member is required.</summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
	internal
#endif
		sealed class RequiredMemberAttribute : Attribute
	{ }
}
