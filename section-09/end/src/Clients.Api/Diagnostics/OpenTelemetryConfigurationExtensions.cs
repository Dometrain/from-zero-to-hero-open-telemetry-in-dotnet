using System.Reflection;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Clients.Api.Diagnostics;

public static class OpenTelemetryConfigurationExtensions
{
    public static WebApplicationBuilder AddOpenTelemetry(this WebApplicationBuilder builder)
    {
        const string serviceName = "Clients.Api";

        var otlpEndpoint = new Uri(builder.Configuration.GetValue<string>("OTLP_Endpoint")!);

        // builder.Logging
        //     .Configure(options =>
        //     {
        //         options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId
        //                                           | ActivityTrackingOptions.TraceId
        //                                           | ActivityTrackingOptions.ParentId
        //                                           | ActivityTrackingOptions.Baggage
        //                                           | ActivityTrackingOptions.Tags;
        //     })
        //     .AddOpenTelemetry(
        //     options =>
        //     {
        //         options.IncludeScopes = true;
        //         options.IncludeFormattedMessage = true;
        //         options.ParseStateValues = true;
        //
        //         options
        //             .SetResourceBuilder(ResourceBuilder.CreateDefault()
        //                 .AddService(serviceName));
        //
        //         options.AddConsoleExporter();
        //     }
        // );

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
                    .AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddNpgsql()
                    .AddRedisInstrumentation()
                    // .AddConsoleExporter()
                    .AddOtlpExporter(options =>
                        options.Endpoint = otlpEndpoint)
            )
            .WithMetrics(metrics =>
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    // Metrics provides by ASP.NET
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                    .AddMeter(ApplicationDiagnostics.Meter.Name)
                    .AddConsoleExporter()
                    .AddOtlpExporter(options =>
                        options.Endpoint = otlpEndpoint)
            )
            .WithLogging(
                logging =>
                    logging
                        // .AddConsoleExporter()
                        .AddOtlpExporter(options =>
                            options.Endpoint = otlpEndpoint)
                // ,
                // options =>
                // {
                //     options.IncludeFormattedMessage = true;
                // }
            );

        return builder;
    }
}