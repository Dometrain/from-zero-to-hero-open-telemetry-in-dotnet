using System.Diagnostics.Metrics;

namespace Clients.Api.Diagnostics;

public static class ApplicationDiagnostics
{
    private const string ServiceName = "Clients.Api";
    public static readonly Meter Meter = new(ServiceName);

    public static readonly Counter<long> ClientsCreatedCounter = Meter.CreateCounter<long>("clients.created");
}