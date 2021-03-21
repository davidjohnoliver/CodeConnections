#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeConnections.Presentation;
using CodeConnections.Utilities;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;

namespace CodeConnections.Services
{
	internal class SettingsService : IVsPersistSolutionOpts, ISettingsService
	{
		private const string SolutionSettingsString = "DependsOnThat.Settings";
		private readonly IVsSolutionPersistence _vsSolutionPersistence;

		private PersistedSolutionSettings? _solutionSettingsToSave;
		private PersistedSolutionSettings? _solutionSettingsToLoad;

		public event Action? SolutionSettingsSaving
		{
			add
			{
				CodeConnections.VSIX.DependsOnThatPackage.SaveUserOptions += value;
			}
			remove
			{
				CodeConnections.VSIX.DependsOnThatPackage.SaveUserOptions -= value;
			}
		}

		public SettingsService(IVsSolutionPersistence vsSolutionPersistence)
		{
			_vsSolutionPersistence = vsSolutionPersistence ?? throw new ArgumentNullException(nameof(vsSolutionPersistence));
		}

		public PersistedSolutionSettings? LoadSolutionSettings()
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			try
			{
				_solutionSettingsToLoad = null;
				_vsSolutionPersistence.LoadPackageUserOpts(this, SolutionSettingsString);
				return _solutionSettingsToLoad;
			}
			finally
			{
				_solutionSettingsToLoad = null;
			}
		}

		public void SaveSolutionSettings(PersistedSolutionSettings solutionSettings)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			try
			{
				_solutionSettingsToSave = solutionSettings;
				_vsSolutionPersistence.SavePackageUserOpts(this, SolutionSettingsString);
			}
			finally
			{
				_solutionSettingsToSave = null;
			}
		}

		int IVsPersistSolutionOpts.SaveUserOptions(IVsSolutionPersistence pPersistence) => VSConstants.S_OK;

		int IVsPersistSolutionOpts.LoadUserOptions(IVsSolutionPersistence pPersistence, uint grfLoadOpts) => VSConstants.S_OK;

		int IVsPersistSolutionOpts.WriteUserOptions(IStream pOptionsStream, string pszKey)
		{
			using var stream = new VSStreamWrapper(pOptionsStream);
			using var sw = new StreamWriter(stream);
			var json = JsonConvert.SerializeObject(_solutionSettingsToSave);
			sw.Write(json);
			return VSConstants.S_OK;
		}

		int IVsPersistSolutionOpts.ReadUserOptions(IStream pOptionsStream, string pszKey)
		{
			try
			{
				using var stream = new VSStreamWrapper(pOptionsStream);
				using var sr = new StreamReader(stream);
				var json = sr.ReadToEnd();
				_solutionSettingsToLoad = JsonConvert.DeserializeObject<PersistedSolutionSettings>(json);
			}
			catch (Exception)
			{
				// TODO: log
			}
			return VSConstants.S_OK;
		}
	}
}
