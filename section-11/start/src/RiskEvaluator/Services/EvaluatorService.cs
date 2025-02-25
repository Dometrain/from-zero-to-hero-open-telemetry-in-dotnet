using System.Diagnostics;
using Grpc.Core;
using OpenTelemetry;
using RiskEvaluator.Services.Rules;

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
        var clientId = Baggage.Current.GetBaggage("client.id");
        _logger.LogInformation("Evaluating risk for {Email} {id}", request.Email,
            clientId);
        Activity.Current?.SetTag("client.id", clientId);

        var score = _rules.Sum(rule => rule.Evaluate(request));

        var level = score switch
        {
            <= 5 => RiskLevel.Low,
            <= 20 => RiskLevel.Medium,
            _ => RiskLevel.High
        };

        _logger.LogInformation("Risk level for {Email} is {Level}", request.Email, level);

        return Task.FromResult(new RiskEvaluationReply()
        {
            RiskLevel = level,
        });
    }
}