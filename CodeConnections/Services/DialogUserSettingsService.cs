#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Presentation;
using CodeConnections.VSIX;

namespace CodeConnections.Services
{
	internal class DialogUserSettingsService : IUserSettingsService
	{
		private readonly UserOptionsDialog _dialogPage;

		public event Action? SettingsChanged;

		public DialogUserSettingsService(UserOptionsDialog dialogPage)
		{
			_dialogPage = dialogPage;
			_dialogPage.OptionsApplied += () => SettingsChanged?.Invoke();
		}

		public PersistedUserSettings GetSettings()
		{
			return new PersistedUserSettings(
				_dialogPage.MaxAutomaticallyLoadedNodes,
				_dialogPage.LayoutMode,
				_dialogPage.IsActiveAlwaysIncluded,
				_dialogPage.IncludeActiveMode,
				_dialogPage.OutputLevel
			);
		}

		public void ApplySettings(PersistedUserSettings settings)
		{
			_dialogPage.MaxAutomaticallyLoadedNodes = settings.MaxAutomaticallyLoadedNodes;
			_dialogPage.LayoutMode = settings.LayoutMode;
			_dialogPage.IsActiveAlwaysIncluded = settings.IsActiveAlwaysIncluded;
			_dialogPage.IncludeActiveMode = settings.IncludeActiveMode;
			_dialogPage.OutputLevel = settings.OutputLevel;

			_dialogPage.SaveSettingsToStorage();
			SettingsChanged?.Invoke();
		}
	}
}
