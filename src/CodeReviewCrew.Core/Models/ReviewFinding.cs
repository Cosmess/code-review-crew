using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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

public sealed partial record ReviewFinding(
    [property: Description("Severity of the issue.")] Severity Severity,
    [property: Description("Path of the file where the issue was found.")] string File,
    [property: Description("Line number in the new version of the file, if known.")] int? Line,
    [property: Description("Short title of the issue.")] string Title,
    [property: Description("Why this is a problem.")] string Explanation,
    [property: Description("Concrete suggestion to fix it, if any.")] string? Suggestion)
{
    /// <summary>
    /// Models sometimes repeat the severity in the title ("High: ...") or emit runs of whitespace.
    /// The severity is shown separately, so it is removed here.
    /// </summary>
    public ReviewFinding Normalized() => this with
    {
        Title = Whitespace().Replace(SeverityPrefix().Replace(Title.Trim(), ""), " ").Trim()
    };

    [GeneratedRegex(@"^(?:(?:critical|high|medium|low|info|cr[ií]tic[oa]|alt[oa]|m[eé]di[oa]|baix[oa])\s*[:\-–]\s*)+", RegexOptions.IgnoreCase)]
    private static partial Regex SeverityPrefix();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex Whitespace();
}

/// <summary>Structured output returned by each specialist agent.</summary>
public sealed record SpecialistFindings(List<ReviewFinding> Findings);

/// <param name="Error">Set when the agent failed on at least one part of the diff after every retry.</param>
public sealed record SpecialistReport(string Agent, IReadOnlyList<ReviewFinding> Findings, string? Error = null);
