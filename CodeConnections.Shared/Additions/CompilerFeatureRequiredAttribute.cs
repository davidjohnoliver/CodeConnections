﻿// https://github.com/dotnet/runtime/blob/f08d13f7f912dd3d89508b3ccd48f47c85c56ddd/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/CompilerFeatureRequiredAttribute.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
	/// <summary>
	/// Indicates that compiler support for a particular feature is required for the location where this attribute is applied.
	/// </summary>
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
	public sealed class CompilerFeatureRequiredAttribute : Attribute
	{
		public CompilerFeatureRequiredAttribute(string featureName)
		{
			FeatureName = featureName;
		}

		/// <summary>
		/// The name of the compiler feature.
		/// </summary>
		public string FeatureName { get; }

		/// <summary>
		/// If true, the compiler can choose to allow access to the location where this attribute is applied if it does not understand <see cref="FeatureName"/>.
		/// </summary>
		public bool IsOptional { get; init; }

		/// <summary>
		/// The <see cref="FeatureName"/> used for the ref structs C# feature.
		/// </summary>
		public const string RefStructs = nameof(RefStructs);

		/// <summary>
		/// The <see cref="FeatureName"/> used for the required members C# feature.
		/// </summary>
		public const string RequiredMembers = nameof(RequiredMembers);
	}
}
