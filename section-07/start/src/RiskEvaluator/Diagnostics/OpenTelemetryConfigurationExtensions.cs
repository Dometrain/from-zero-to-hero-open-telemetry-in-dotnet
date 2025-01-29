using System.Reflection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace RiskEvaluator.Diagnostics;

public static class OpenTelemetryConfigurationExtensions
{
    public static WebApplicationBuilder AddOpenTelemetry(this WebApplicationBuilder builder)
    {
        const string serviceName = "RiskEvaluator";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource
                    .AddService(serviceName)
                    .AddAttributes(new[]
                    {
                        new KeyValuePair<string, object>("service.version",
                            Assembly.GetExecutingAssembly().GetName().Version!.ToString())
                    });
            })
            .WithTracing(tracing =>
                tracing
                    .AddAspNetCoreInstrumentation()
                    // .AddConsoleExporter()
                    .AddOtlpExporter(options => 
                        options.Endpoint = new Uri(builder.Configuration.GetValue<string>("Jaeger")!))
            );

        return builder;
    }
}