using System.Diagnostics;
using Clients.Contracts.Events;

namespace Clients.Api.Diagnostics.Extensions;

public static class ActivityExtensions
{
    public static Activity? EnrichWithClient(this Activity? activity, Client client)
    {
        activity?.SetTag(TagNames.ClientId, client.Id);
        activity?.SetTag(TagNames.ClientMembership, client.Membership.ToString());
        return activity;
    }
}