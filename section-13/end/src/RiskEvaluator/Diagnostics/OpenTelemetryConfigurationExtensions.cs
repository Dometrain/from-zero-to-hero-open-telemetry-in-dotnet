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

        builder.Services
            .ConfigureOpenTelemetryTracerProvider((provider, providerBuilder) =>
            providerBuilder.AddProcessor(new BaggageProcessor()));
        
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource
                    .AddService(serviceName,
                        "Dometrain.Courses.OpenTelemetry",
                        Assembly.GetExecutingAssembly().GetName().Version!.ToString())
                    .AddAttributes(new[]
                    {
                        new KeyValuePair<string, object>("team", "Dometrain")
                    });
            })
            .WithTracing(tracing =>
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddSource(ApplicationDiagnostics.ActivitySourceName)
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