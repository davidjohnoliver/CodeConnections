using System;
using CodeConnections.Presentation;

namespace CodeConnections.Services
{
	internal interface IUserSettingsService
	{
		event Action SettingsChanged;

		void ApplySettings(PersistedUserSettings settings);
		PersistedUserSettings GetSettings();
	}
}