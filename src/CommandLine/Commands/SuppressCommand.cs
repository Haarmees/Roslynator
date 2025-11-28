using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslynator.Suppress;
using static Roslynator.Logger;

namespace Roslynator.CommandLine
{
    internal class SuppressCommand : MSBuildWorkspaceCommand<SuppressCommandResult>
    {
        private SuppressCommandLineOptions Options { get; }
        private DiagnosticSeverity SeverityLevel { get; }
        
        public SuppressCommand(SuppressCommandLineOptions options, DiagnosticSeverity severityLevel, in ProjectFilter projectFilter, FileSystemFilter fileSystemFilter) : base(in projectFilter, fileSystemFilter)
        {
            Options = options;
            SeverityLevel = severityLevel;
        }

        public override async Task<SuppressCommandResult> ExecuteAsync(ProjectOrSolution projectOrSolution, CancellationToken cancellationToken = default)
        {
            var codeWarningSuppressorOptions = new CodeWarningSuppressorOptions(
                ignoreAnalyzerReferences: Options.IgnoreAnalyzerReferences,
                severityLevel: SeverityLevel,
                supportedDiagnosticIds: Options.SupportedDiagnostics,
                ignoredDiagnosticIds: Options.IgnoredDiagnostics);

            IEnumerable<AnalyzerAssembly> analyzerAssemblies = Options.AnalyzerAssemblies
                .SelectMany(path => AnalyzerAssemblyLoader.LoadFrom(path, loadFixers: false).Select(info => info.AnalyzerAssembly));

            CultureInfo culture = (Options.Culture != null) ? CultureInfo.GetCultureInfo(Options.Culture) : null;

            var analyzerLoader = new AnalyzerLoader(analyzerAssemblies, codeWarningSuppressorOptions);

            analyzerLoader.AnalyzerAssemblyAdded += (sender, args) =>
            {
                AnalyzerAssembly analyzerAssembly = args.AnalyzerAssembly;

                if (analyzerAssembly.Name.EndsWith(".Analyzers")
                    || analyzerAssembly.HasAnalyzers
                    || analyzerAssembly.HasFixers)
                {
                    WriteLine($"Add analyzer assembly '{analyzerAssembly.FullName}'", ConsoleColors.DarkGray, Verbosity.Detailed);
                }
            };

            ImmutableArray<ProjectWarningSuppressorResult> results;

            if (projectOrSolution.IsProject)
            {
                Project project = projectOrSolution.AsProject();
                
                var suppressor = new CodeWarningPragmaSuppressor(
                    project.Solution,
                    analyzerLoader: analyzerLoader,
                    formatProvider: culture,
                    options: codeWarningSuppressorOptions);

                ProjectWarningSuppressorResult result = await suppressor.SuppressDiagnosticsProjectAsync(project, cancellationToken);

                results = ImmutableArray.Create(result);
            }
            else
            {
                Solution solution = projectOrSolution.AsSolution();
                
                var suppressor = new CodeWarningPragmaSuppressor(
                    solution,
                    analyzerLoader: analyzerLoader,
                    formatProvider: culture,
                    options: codeWarningSuppressorOptions);

                // var projectFilter = new ProjectFilter(Options.Projects, Options.IgnoredProjects, Language);

                results = await suppressor.SuppressDiagnosticsSolutionAsync(solution, f => ProjectFilter.IsMatch(f), cancellationToken);
            }

            return new SuppressCommandResult(CommandStatus.Success);
        }
    }
}