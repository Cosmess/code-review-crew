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
public sealed record ReviewReport(string Summary, Verdict Verdict, List<ReviewFinding> Findings);

public sealed record CrewResult(IReadOnlyList<SpecialistReport> SpecialistReports, ReviewReport Report);
