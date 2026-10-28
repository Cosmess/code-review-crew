using CodeReviewCrew.Core.Configuration;
using CodeReviewCrew.Core.Models;
using Microsoft.Extensions.AI;

namespace CodeReviewCrew.Core.Orchestration;

/// <summary>Thread-safe accumulator of the tokens spent by every agent call.</summary>
internal sealed class UsageTracker(PricingConfig? pricing)
{
    private long _input;
    private long _output;

    public void Add(UsageDetails? usage)
    {
        Interlocked.Add(ref _input, usage?.InputTokenCount ?? 0);
        Interlocked.Add(ref _output, usage?.OutputTokenCount ?? 0);
    }

    public UsageSummary ToSummary()
    {
        decimal? cost = pricing is null
            ? null
            : (_input * pricing.InputPerMillion + _output * pricing.OutputPerMillion) / 1_000_000m;

        return new UsageSummary(_input, _output, cost);
    }
}
