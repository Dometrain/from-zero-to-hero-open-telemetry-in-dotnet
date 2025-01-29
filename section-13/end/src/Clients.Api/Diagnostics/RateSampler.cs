using OpenTelemetry.Trace;

namespace Clients.Api.Diagnostics;

public class RateSampler : Sampler
{
    private readonly double _samplingRate;
    private readonly Random _random;

    public RateSampler(double samplingRate)
    {
        _samplingRate = samplingRate;
        _random = new Random();
    }

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        var shouldBeSample =
            _random.NextDouble() < _samplingRate;

        if (shouldBeSample)
            return new SamplingResult(SamplingDecision.RecordAndSample);

        return new SamplingResult(SamplingDecision.Drop);
    }
}