using System.Text.Json;
using System.Text.Json.Serialization;
using CodeReviewCrew.Core.Models;

namespace CodeReviewCrew.Evals;

/// <summary>A labeled pull request: the diff and the issues a good review must find.</summary>
public sealed record EvalCase(string Name, string Diff, ExpectedReview Expected)
{
    public const string DiffFileName = "diff.patch";
    public const string ExpectedFileName = "expected.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static IReadOnlyList<EvalCase> LoadAll(string directory) =>
        Directory.EnumerateDirectories(directory)
            .Order(StringComparer.Ordinal)
            .Select(Load)
            .ToList();

    public static EvalCase Load(string caseDirectory)
    {
        var expected = JsonSerializer.Deserialize<ExpectedReview>(File.ReadAllText(Path.Combine(caseDirectory, ExpectedFileName)), JsonOptions)
            ?? throw new InvalidOperationException($"Empty {ExpectedFileName} in {caseDirectory}");
        return new EvalCase(Path.GetFileName(caseDirectory), File.ReadAllText(Path.Combine(caseDirectory, DiffFileName)), expected);
    }
}

/// <param name="ShouldBlock">Whether the right verdict is RequestChanges.</param>
public sealed record ExpectedReview(
    string Description,
    bool ShouldBlock,
    List<ExpectedFinding> Findings);

/// <summary>
/// An issue the review must report. A finding matches it when it is in the same file, within
/// <see cref="Tolerance"/> lines, and mentions one of the keywords or OWASP categories.
/// </summary>
public sealed record ExpectedFinding(
    string File,
    int Line,
    int Tolerance,
    List<string> Keywords,
    List<string> Owasp,
    [property: JsonPropertyName("minSeverity")] Severity MinimumSeverity);
