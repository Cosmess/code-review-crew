using System.ComponentModel;
using System.Text.Json.Serialization;

namespace CodeReviewCrew.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter<Severity>))]
public enum Severity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

public sealed record ReviewFinding(
    [property: Description("Severity of the issue.")] Severity Severity,
    [property: Description("Path of the file where the issue was found.")] string File,
    [property: Description("Line number in the new version of the file, if known.")] int? Line,
    [property: Description("Short title of the issue.")] string Title,
    [property: Description("Why this is a problem.")] string Explanation,
    [property: Description("Concrete suggestion to fix it, if any.")] string? Suggestion);

/// <summary>Structured output returned by each specialist agent.</summary>
public sealed record SpecialistFindings(List<ReviewFinding> Findings);

/// <param name="Error">Set when the agent failed on at least one part of the diff after every retry.</param>
public sealed record SpecialistReport(string Agent, IReadOnlyList<ReviewFinding> Findings, string? Error = null);
