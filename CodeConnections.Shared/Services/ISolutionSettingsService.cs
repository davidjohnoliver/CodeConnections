﻿#nullable enable

using System;
using CodeConnections.Presentation;

namespace CodeConnections.Services
{
	/// <summary>
	/// Save and load persisted solution-level settings.
	/// </summary>
	internal interface ISolutionSettingsService
	{
		/// <summary>
		/// Raised when solution-level settings should be saved.
		/// </summary>
		event Action? SolutionSettingsSaving;

		/// <summary>
		/// Load solution-level settings from persisted storage.
		/// </summary>
		PersistedSolutionSettings? LoadSolutionSettings();

		/// <summary>
		/// Save solution-level settings to persisted storage. This should generally be called from <see cref="SolutionSettingsSaving"/>.
		/// </summary>
		void SaveSolutionSettings(PersistedSolutionSettings solutionSettings);
	}
}