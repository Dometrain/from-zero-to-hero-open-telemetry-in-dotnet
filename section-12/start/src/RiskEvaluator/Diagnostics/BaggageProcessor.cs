using System.Diagnostics;
using OpenTelemetry;

namespace RiskEvaluator.Diagnostics;

public class BaggageProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity data)
    {
        base.OnStart(data);
    }

    public override void OnEnd(Activity data)
    {
        foreach (var item in Baggage.Current)
        {
            if(item.Key.StartsWith("client."))
                data.SetTag(item.Key, item.Value);
        }
        
        base.OnEnd(data);
    }
}