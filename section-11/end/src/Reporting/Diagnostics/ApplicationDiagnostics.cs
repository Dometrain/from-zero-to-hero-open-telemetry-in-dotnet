using System.Diagnostics;

namespace Reporting.Diagnostics;

public static class ApplicationDiagnostics
{
    public const string ActivitySourceName = "Reporting";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

}