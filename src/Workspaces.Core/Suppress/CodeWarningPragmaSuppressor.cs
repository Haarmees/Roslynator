using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslynator.Suppress
{
    internal class CodeWarningPragmaSuppressor
    {
        private readonly AnalyzerLoader _analyzerLoader;
        private readonly Workspace _workspace;

        public CodeWarningPragmaSuppressor(
            Solution solution,
            AnalyzerLoader analyzerLoader,
            IFormatProvider formatProvider = null,
            CodeWarningSuppressorOptions options = null)
        {
            _analyzerLoader = analyzerLoader;
            _workspace = solution.Workspace;

            FormatProvider = formatProvider;
            Options = options ?? CodeWarningSuppressorOptions.Default;
        }

        public IFormatProvider FormatProvider { get; }

        public CodeWarningSuppressorOptions Options { get; }

        public async Task<ImmutableArray<ProjectWarningSuppressorResult>> SuppressDiagnosticsSolutionAsync(
            Solution solution,
            Func<Project, bool> predicate,
            CancellationToken cancellationToken = default)
        {
            return new ImmutableArray<ProjectWarningSuppressorResult>();
        }

        public async Task<ProjectWarningSuppressorResult> SuppressDiagnosticsProjectAsync(Project project,
            CancellationToken cancellationToken = default)
        {
            var analyzers = _analyzerLoader.GetAnalyzers(project: project);

            var diagnostics = (await GetDiagnostics(project, analyzers, cancellationToken))
                .Where(d => d.IsEffective(Options, project.CompilationOptions));

            project = project.Solution.GetProject(project.Id);

            var groupedDiagnostics = diagnostics.GroupBy(diagnostic =>
            {
                var document = project.GetDocument(diagnostic.Location.SourceTree);
                return document.Id;
            });

            foreach (var groupedDiagnostic in groupedDiagnostics)
            {
                var document = project.GetDocument(groupedDiagnostic.Key);
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                var disableLines = groupedDiagnostic
                    .GroupBy(diagnostic => root.SyntaxTree.GetLineSpan(diagnostic.Location.SourceSpan).StartLine())
                    .ToDictionary(g => g.Key, g => g.Select(CreateDisablePragma));
                var restoreLines = groupedDiagnostic
                    .GroupBy(diagnostic => root.SyntaxTree.GetLineSpan(diagnostic.Location.SourceSpan).EndLine() + 1)
                    .ToDictionary(g => g.Key, g => g.Select(CreateRestorePragma));

                var lineNumbers = disableLines.Keys.Union(restoreLines.Keys).OrderBy(lineNumber => lineNumber);

                var offset = 0;
                foreach (var lineNumber in lineNumbers)
                {
                    var pragmas = new List<PragmaWarningDirectiveTriviaSyntax>();
                    if (restoreLines.ContainsKey(lineNumber))
                    {
                        pragmas = pragmas.Concat(restoreLines.GetValueOrDefault(lineNumber)).ToList();
                    }

                    if (disableLines.ContainsKey(lineNumber))
                    {
                        pragmas = pragmas.Concat(disableLines.GetValueOrDefault(lineNumber)).ToList();
                    }

                    if (pragmas.Any())
                    {
                        var newLineNumber = lineNumber - 1 + offset;

                        if (newLineNumber < 0)
                        {
                            root = root.PrependToLeadingTrivia(pragmas.Select(SyntaxFactory.Trivia));
                        }
                        else if (newLineNumber > root.GetText().Lines.Count)
                        {
                            var trivia = pragmas.Select(SyntaxFactory.Trivia).ToList();
                            trivia.Insert(0, SyntaxFactory.CarriageReturnLineFeed);
                            root = root.PrependToTrailingTrivia(trivia);
                            root = root.PrependToTrailingTrivia(pragmas.Select(SyntaxFactory.Trivia));
                        }
                        else
                        {
                            var startLineSpan = root.GetText().Lines[newLineNumber].Span;
                            var lineEndTrivia = root.FindTrivia(startLineSpan.End);

                            root = root.InsertTriviaAfter(lineEndTrivia, pragmas.Select(SyntaxFactory.Trivia));
                        }

                        offset += pragmas.Count();
                    }
                }
                
                project = document.WithSyntaxRoot(root).Project;
            }

            _workspace.TryApplyChanges(project.Solution);

            return null;
        }

        private async Task<ImmutableArray<Diagnostic>> GetDiagnostics(Project project,
            ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken = default)
        {
            LogHelpers.WriteUsedAnalyzers(analyzers, null, Options, ConsoleColors.DarkGray, Verbosity.Diagnostic);

            cancellationToken.ThrowIfCancellationRequested();

            Compilation compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            ImmutableArray<Diagnostic> diagnostics = ImmutableArray<Diagnostic>.Empty;

            if (analyzers.Any())
            {
                var compilationWithAnalyzersOptions = new CompilationWithAnalyzersOptions(
                    options: project.AnalyzerOptions,
                    onAnalyzerException: default(Action<Exception, DiagnosticAnalyzer, Diagnostic>),
                    concurrentAnalysis: Options.ConcurrentAnalysis,
                    logAnalyzerExecutionTime: false,
                    reportSuppressedDiagnostics: false);

                var compilationWithAnalyzers =
                    new CompilationWithAnalyzers(compilation, analyzers, compilationWithAnalyzersOptions);

                diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            return diagnostics;
        }
        
        private static PragmaWarningDirectiveTriviaSyntax CreateRestorePragma(Diagnostic diagnostic)
        {
            return CreateDisablePragma(diagnostic).WithDisableOrRestoreKeyword(SyntaxFactory.Token(
                SyntaxFactory.TriviaList(),
                SyntaxKind.RestoreKeyword,
                SyntaxFactory.TriviaList(
                    SyntaxFactory.Space)));
        }

        private static PragmaWarningDirectiveTriviaSyntax CreateDisablePragma(Diagnostic diagnostic)
        {
            return SyntaxFactory.PragmaWarningDirectiveTrivia(
                    SyntaxFactory.Token(
                        SyntaxFactory.TriviaList(),
                        SyntaxKind.DisableKeyword,
                        SyntaxFactory.TriviaList(
                            SyntaxFactory.Space)),
                    true)
                .WithPragmaKeyword(
                    SyntaxFactory.Token(
                        SyntaxFactory.TriviaList(),
                        SyntaxKind.PragmaKeyword,
                        SyntaxFactory.TriviaList(
                            SyntaxFactory.Space)))
                .WithWarningKeyword(
                    SyntaxFactory.Token(
                        SyntaxFactory.TriviaList(),
                        SyntaxKind.WarningKeyword,
                        SyntaxFactory.TriviaList(
                            SyntaxFactory.Space)))
                .WithErrorCodes(
                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                        SyntaxFactory.IdentifierName(
                            SyntaxFactory.Identifier(
                                SyntaxFactory.TriviaList(),
                                diagnostic.Id,
                                SyntaxFactory.TriviaList(
                                    new[]
                                    {
                                        SyntaxFactory.Space,
                                        SyntaxFactory.Comment($"// {diagnostic.Descriptor.Title}")
                                    }
                                )
                            )
                        )
                    )
                )
                .WithEndOfDirectiveToken(
                    SyntaxFactory.Token(
                        SyntaxFactory.TriviaList(),
                        SyntaxKind.EndOfDirectiveToken,
                        SyntaxFactory.TriviaList(
                            SyntaxFactory.CarriageReturnLineFeed)));
        }
    }
}