using System.Text.Json.Serialization;

namespace CodeReviewCrew.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter<Verdict>))]
public enum Verdict
{
    Approve,
    Comment,
    RequestChanges
}

/// <summary>Final consolidated review produced by the synthesizer agent.</summary>
public sealed record ReviewReport(string Summary, Verdict Verdict, List<ReviewFinding> Findings)
{
    /// <summary>Verdict rule used when the synthesizer is not available.</summary>
    public static Verdict VerdictFor(IEnumerable<ReviewFinding> findings) =>
        findings.Select(f => f.Severity).DefaultIfEmpty(Severity.Info).Max() switch
        {
            >= Severity.High => Verdict.RequestChanges,
            >= Severity.Low => Verdict.Comment,
            _ => Verdict.Approve
        };
}

/// <param name="EstimatedCostUsd">Null when no pricing is configured.</param>
public sealed record UsageSummary(long InputTokens, long OutputTokens, decimal? EstimatedCostUsd)
{
    public static UsageSummary Empty { get; } = new(0, 0, null);
}

/// <param name="Language">Code of the language the review is written in (see <c>ReviewLanguage</c>).</param>
public sealed record CrewResult(IReadOnlyList<SpecialistReport> SpecialistReports, ReviewReport Report, UsageSummary Usage, string Language = "en");
