using ConsoleTool;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using var traceProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService("ConsoleTool")
    )
    .AddSource(ApplicationDiagnostics.ActivitySourceName)
    .AddConsoleExporter()
    .Build();

await DoWork();

Console.WriteLine("Done!");

static async Task DoWork()
{
    using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("Do Work");
    await StepOne();
    await StepTwo();
}

static async Task StepOne()
{
    await Task.Delay(500);
}

static async Task StepTwo()
{
    await Task.Delay(1000);
}