using System.Diagnostics;
using RiskEvaluator.Diagnostics;

namespace RiskEvaluator.Services.Rules;

public class EmailRule : IRule
{
    private readonly List<string> _suspiciousDomains =
    [
        "bugmenot.com",
        "mailinator.com"
    ];

    public int Evaluate(RiskEvaluationRequest request)
    {
        using var activity = ApplicationDiagnostics.ActivitySource
            .StartActivity("Evaluate Email Rule");

        activity?.SetTag("request.email", request.Email);
        
        var emailDomain = GetEmailDomain(request.Email);
        return IsSuspiciousDomain(emailDomain) ? 20 : 0;
    }

    private bool IsSuspiciousDomain(string emailDomain) 
        => _suspiciousDomains.Contains(emailDomain);

    private string GetEmailDomain(string email)
    {
        var atIndex = email.LastIndexOf('@');
        return atIndex == -1 ? string.Empty : email[(atIndex + 1)..];
    }
}