#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnections.Tests.Utilities
{
	public static partial class WorkspaceUtils
	{
		/// <summary>
		/// Simple routine to create a Roslyn <see cref="Workspace"/> from a target solution.
		/// </summary>
		/// <param name="rootFolder">The root solution folder</param>
		/// <param name="outputKinds">An optional set of <see cref="OutputKind"/> values to use for each project. If not supplied, a default of <see cref="OutputKind.DynamicallyLinkedLibrary"/> (class library) will be used.</param>
		/// <remarks>
		/// This is designed to load simple contrived solutions for testing, without the hassle of using MSBuild, and as such doesn't support many 
		/// key features of real projects (such as NuGet package references for example).
		/// </remarks>
		public static Workspace GetWorkspace(string rootFolder, params (string ProjectName, OutputKind OutputKind)[] outputKinds)
		{
			var workspace = new AdhocWorkspace();
			try
			{
				var solutionPath = Directory.GetFiles(rootFolder, "*.sln", SearchOption.AllDirectories).Single();
				var solution = workspace.AddSolution(SolutionInfo.Create(
					SolutionId.CreateNewId(),
					VersionStamp.Create(),
					filePath: solutionPath
				));

				var projectIds = new Dictionary<string, ProjectId>();
				var projectReferences = new Dictionary<string, IEnumerable<string>>();
				foreach (var csproj in Directory.GetFiles(Path.GetDirectoryName(solutionPath), "*.csproj", SearchOption.AllDirectories))
				{
					var projectId = ProjectId.CreateNewId();
					var projectFileName = Path.GetFileName(csproj);
					projectIds[projectFileName] = projectId;
					var documents = Directory.GetFiles(Path.GetDirectoryName(csproj), "*.cs", SearchOption.AllDirectories)
						.Select(f =>
							DocumentInfo.Create(
								DocumentId.CreateNewId(projectId),
								Path.GetFileName(f),
								filePath: f,
								loader: TextLoader.From(
									TextAndVersion.Create(
										SourceText.From(File.ReadAllText(f)),
										VersionStamp.Create()
									)
								)
							)
						);

					var outputKind = OutputKind.DynamicallyLinkedLibrary;
					foreach (var tpl in outputKinds)
					{
						if (tpl.ProjectName == projectFileName)
						{
							outputKind = tpl.OutputKind;
							break;
						}
					}

					var projectName = Path.GetFileNameWithoutExtension(csproj);
					solution = solution.AddProject(ProjectInfo.Create(
							projectId,
							VersionStamp.Create(),
							projectName,
							assemblyName: projectName,
							LanguageNames.CSharp,
							filePath: csproj,
							documents: documents
						))
						.AddMetadataReferences(projectId, GetStandardReferences());

					projectReferences[projectFileName] = ProjectUtils.GetProjectReferences(csproj);

					solution = solution.WithProjectCompilationOptions(projectId, solution.GetProject(projectId)?.CompilationOptions?.WithOutputKind(outputKind) ?? throw new InvalidOperationException());
				}

				foreach (var kvp in projectReferences)
				{
					foreach (var reference in kvp.Value)
					{
						var referencingProjectId = projectIds[kvp.Key];
						var referencedProjectId = projectIds[Path.GetFileName(reference)];
						solution = solution.AddProjectReference(referencingProjectId, new ProjectReference(referencedProjectId));
					}
				}

				workspace.TryApplyChanges(solution);

				return workspace;
			}
			catch (Exception)
			{
				workspace?.Dispose();
				throw;
			}
		}

		private static IEnumerable<MetadataReference> GetStandardReferences()
			=> _standardAssemblies.Select(a => MetadataReference.CreateFromFile(new Uri(a.CodeBase).LocalPath));

		private static readonly HashSet<Assembly> _standardAssemblies = new HashSet<Assembly>(new[] {
				typeof(object).Assembly,
				typeof(Enumerable).Assembly
		});
	}
}
