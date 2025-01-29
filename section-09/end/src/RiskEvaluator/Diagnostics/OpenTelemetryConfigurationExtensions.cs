using System.Reflection;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace RiskEvaluator.Diagnostics;

public static class OpenTelemetryConfigurationExtensions
{
    public static WebApplicationBuilder AddOpenTelemetry(this WebApplicationBuilder builder)
    {
        const string serviceName = "RiskEvaluator";

        var otlpEndpoint = new Uri(builder.Configuration.GetValue<string>("OTLP_Endpoint")!);
        
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
                        options.Endpoint = otlpEndpoint)
            )
            .WithLogging(
                logging=>
                    logging
                        // .AddConsoleExporter()
                        .AddOtlpExporter(options => 
                            options.Endpoint = otlpEndpoint)
                );

        return builder;
    }
}