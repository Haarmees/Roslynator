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
    internal class CodeWarningGlobalSuppressor
    {
        private readonly AnalyzerLoader _analyzerLoader;
        private readonly Workspace _workspace;

        public CodeWarningGlobalSuppressor(
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

        private static AttributeListSyntax DiagnosticToGlobalSuppressMessageAttribute(Diagnostic diagnostic,
            SyntaxNode node)
        {
            return SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                        SyntaxFactory.Attribute(
                                SyntaxFactory.IdentifierName("SuppressMessage")
                            )
                            .WithArgumentList(
                                SyntaxFactory.AttributeArgumentList(
                                    SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(
                                        new SyntaxNodeOrToken[]
                                        {
                                            SyntaxFactory.AttributeArgument(
                                                SyntaxFactory.LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    SyntaxFactory.Literal(diagnostic.Descriptor.Category)
                                                )
                                            ),
                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            SyntaxFactory.AttributeArgument(
                                                SyntaxFactory.LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    SyntaxFactory.Literal(
                                                        $"{diagnostic.Id}: {diagnostic.Descriptor.Title}")
                                                )
                                            ),
                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            SyntaxFactory.AttributeArgument(
                                                    SyntaxFactory.LiteralExpression(
                                                        SyntaxKind.StringLiteralExpression,
                                                        SyntaxFactory.Literal("Autogenerated")
                                                    )
                                                )
                                                .WithNameEquals(
                                                    SyntaxFactory.NameEquals(
                                                        SyntaxFactory.IdentifierName("Justification")
                                                    )
                                                ),
                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            SyntaxFactory.AttributeArgument(
                                                    SyntaxFactory.LiteralExpression(
                                                        SyntaxKind.StringLiteralExpression,
                                                        SyntaxFactory.Literal("member")
                                                    )
                                                )
                                                .WithNameEquals(
                                                    SyntaxFactory.NameEquals(
                                                        SyntaxFactory.IdentifierName("Scope")
                                                    )
                                                ),
                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            SyntaxFactory.AttributeArgument(
                                                    SyntaxFactory.LiteralExpression(
                                                        SyntaxKind.StringLiteralExpression,
                                                        SyntaxFactory.Literal(
                                                            "~P:ChipSoft.Publics.Consult.Units.ConsultResources.RS_CONSULT_MAINTENANCE_RIGHTSGROUP")
                                                    )
                                                )
                                                .WithNameEquals(
                                                    SyntaxFactory.NameEquals(
                                                        SyntaxFactory.IdentifierName("Target")
                                                    )
                                                )
                                        }
                                    )
                                )
                            )
                    )
                )
                .WithTarget(
                    SyntaxFactory.AttributeTargetSpecifier(
                        SyntaxFactory.Token(SyntaxKind.AssemblyKeyword)
                    )
                ).NormalizeWhitespace();
        }

        private static string GetSuppressionScope(SyntaxNode node)
        {
            return string.Empty;
        }
    }
}