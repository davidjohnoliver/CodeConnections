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
				_dialogPage.MaxAutomaticallyLoadedNodes
			);
		}

		public void ApplySettings(PersistedUserSettings settings)
		{
			_dialogPage.MaxAutomaticallyLoadedNodes = settings.MaxAutomaticallyLoadedNodes;

			_dialogPage.SaveSettingsToStorage();
		}
	}
}
