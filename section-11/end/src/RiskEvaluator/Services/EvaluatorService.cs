using System.Diagnostics;
using Grpc.Core;
using OpenTelemetry;
using OpenTelemetry.Trace;
using RiskEvaluator.Services.Rules;
using Status = OpenTelemetry.Trace.Status;

namespace RiskEvaluator.Services;

public class EvaluatorService : Evaluator.EvaluatorBase
{
    private readonly ILogger<EvaluatorService> _logger;
    private readonly IEnumerable<IRule> _rules;

    public EvaluatorService(ILogger<EvaluatorService> logger, IEnumerable<IRule> rules)
    {
        _logger = logger;
        _rules = rules;
    }

    public override Task<RiskEvaluationReply> Evaluate(RiskEvaluationRequest request, ServerCallContext context)
    {
        try
        {
            var score = _rules.Sum(rule => rule.Evaluate(request));

            var level = score switch
            {
                <= 5 => RiskLevel.Low,
                <= 20 => RiskLevel.Medium,
                _ => RiskLevel.High
            };

            Activity.Current?.SetTag("evaluation.email",
                request.Email);

            Activity.Current?.AddEvent(
                new ActivityEvent(
                    "RiskResult",
                    tags: new ActivityTagsCollection(
                        new KeyValuePair<string, object?>[]
                        {
                            new("risk.score", score),
                            new("risk.level", level)
                        }
                    )
                )
            );

            return Task.FromResult(new RiskEvaluationReply()
            {
                RiskLevel = level,
            });
        }
        catch (Exception exception)
        {
            Activity.Current?.SetStatus(
                Status.Error
                );
            Activity.Current?
                .RecordException(
                    exception
                );
            return Task.FromResult(new RiskEvaluationReply()
            {
                RiskLevel = RiskLevel.High,
            });
        }
    }
}