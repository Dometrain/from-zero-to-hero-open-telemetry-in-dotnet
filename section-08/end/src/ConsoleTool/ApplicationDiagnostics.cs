using System.Diagnostics;

namespace ConsoleTool;

public static class ApplicationDiagnostics
{
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public const string ActivitySourceName = "Console.Tool.Diagnostics";
}