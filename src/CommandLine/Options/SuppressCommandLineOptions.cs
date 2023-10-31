using CommandLine;

namespace Roslynator.CommandLine
{
    [Verb("suppress", HelpText = "Suppresses diagnostics in the specified project or solution.")]
    internal class SuppressCommandLineOptions : AbstractAnalyzeCommandLineOptions
    {
    }
}