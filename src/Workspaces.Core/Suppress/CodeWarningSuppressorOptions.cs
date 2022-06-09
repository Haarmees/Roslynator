using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Roslynator.Suppress
{
    internal class CodeWarningSuppressorOptions : CodeAnalysisOptions
    {
        public static CodeWarningSuppressorOptions Default { get; } = new();
        
        public CodeWarningSuppressorOptions(DiagnosticSeverity severityLevel = DiagnosticSeverity.Info, bool ignoreAnalyzerReferences = false, bool concurrentAnalysis = true, IEnumerable<string> supportedDiagnosticIds = null, IEnumerable<string> ignoredDiagnosticIds = null) : base(severityLevel, ignoreAnalyzerReferences, concurrentAnalysis, supportedDiagnosticIds, ignoredDiagnosticIds)
        {
        }
    }
}