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
	/// <summary>
	/// Implementation of user settings service using a VS dialog.
	/// </summary>
	/// <remarks>
	/// Steps for adding a new user setting: 
	/// 1. Add 'ShouldShowCats' property to <see cref="PersistedUserSettings"/>
	/// 2. Add 'ShouldShowCats' to <see cref="UserOptionsDialog"/> with appropriate category, name, and description.
	/// 3. Update <see cref="GetSettings"/> to instantiate PersistedUserSettings with _dialogPage.ShouldShowCats.
	/// 4. Update <see cref="ApplySettings(PersistedUserSettings)"/> to propagate PersistedUserSettings.ShouldShowCats to _dialogPage.
	/// 
	/// Consuming new setting from view model:
	/// 
	/// 5. Add, eg, a 'ShouldShowCats' property to <see cref="DependencyGraphToolWindowViewModel"/>.
	/// 6. Update <see cref="DependencyGraphToolWindowViewModel.ApplyUserSettings"/> to update DependencyGraphToolWindowViewModel.ShouldShowCats
	///    when the settings change or are loaded.
	/// 7. If 'ShouldShowCats' can be changed from the main UI, then in the setter of DependencyGraphToolWindowViewModel.ShouldShowCats, call
	///    _userSettingsService.ApplySettings(_userSettingsService.GetSettings() with { ShouldShowCats = value });
	/// </remarks>
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
				_dialogPage.OutputLevel,
				_dialogPage.EnableDebugFeatures
			);
		}

		public void ApplySettings(PersistedUserSettings settings)
		{
			_dialogPage.MaxAutomaticallyLoadedNodes = settings.MaxAutomaticallyLoadedNodes;
			_dialogPage.LayoutMode = settings.LayoutMode;
			_dialogPage.IsActiveAlwaysIncluded = settings.IsActiveAlwaysIncluded;
			_dialogPage.IncludeActiveMode = settings.IncludeActiveMode;
			_dialogPage.OutputLevel = settings.OutputLevel;
			_dialogPage.EnableDebugFeatures = settings.EnableDebugFeatures;

			_dialogPage.SaveSettingsToStorage();
			SettingsChanged?.Invoke();
		}
	}
}
