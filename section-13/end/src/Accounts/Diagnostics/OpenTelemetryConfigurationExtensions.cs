using System.Reflection;
using Infrastructure.RabbitMQ;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Accounts.Diagnostics;

public static class OpenTelemetryConfigurationExtensions
{
    public static WebApplicationBuilder AddOpenTelemetry(this WebApplicationBuilder builder)
    {
        const string serviceName = "Accounts";

        var otlpEndpoint = new Uri(builder.Configuration.GetValue<string>("OTLP_Endpoint")!);

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
                    .AddNpgsql()
                    .AddSource(RabbitMqDiagnostics.ActivitySourceName)
                    .AddOtlpExporter(options =>
                        options.Endpoint = otlpEndpoint)
            )
            .WithMetrics(metrics =>
                metrics
                    .AddAspNetCoreInstrumentation()
                    // Metrics provides by ASP.NET
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                    .AddOtlpExporter(options =>
                        options.Endpoint = otlpEndpoint)
            )
            .WithLogging(
                logging=>
                    logging
                        .AddOtlpExporter(options => 
                            options.Endpoint = otlpEndpoint)
            );

        return builder;
    }
}