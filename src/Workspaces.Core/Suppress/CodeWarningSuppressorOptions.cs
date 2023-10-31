using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Roslynator.Suppress;

internal class CodeWarningSuppressorOptions : CodeAnalysisOptions
{
    public static CodeWarningSuppressorOptions Default { get; } = new();

    public CodeWarningSuppressorOptions(FileSystemFilter? fileSystemFilter = null,
        DiagnosticSeverity severityLevel = DiagnosticSeverity.Info, bool ignoreAnalyzerReferences = false,
        bool concurrentAnalysis = true, IEnumerable<string>? supportedDiagnosticIds = null,
        IEnumerable<string>? ignoredDiagnosticIds = null) : base(fileSystemFilter, severityLevel,
        ignoreAnalyzerReferences, concurrentAnalysis, supportedDiagnosticIds, ignoredDiagnosticIds)
    {
    }
}