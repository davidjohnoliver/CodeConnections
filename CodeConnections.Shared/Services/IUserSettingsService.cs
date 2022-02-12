using System;
using CodeConnections.Presentation;

namespace CodeConnections.Services
{
	/// <summary>
	/// Service for retrieving and applying user settings.
	/// </summary>
	internal interface IUserSettingsService
	{
		event Action SettingsChanged;

		void ApplySettings(PersistedUserSettings settings);
		PersistedUserSettings GetSettings();
	}
}