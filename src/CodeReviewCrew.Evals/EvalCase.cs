using System.Text.Json;
using System.Text.Json.Serialization;
using CodeReviewCrew.Core.Models;

namespace CodeReviewCrew.Evals;

/// <summary>A labeled pull request: the diff and the issues a good review must find.</summary>
public sealed record EvalCase(string Name, string Diff, ExpectedReview Expected)
{
    public const string DiffFileName = "diff.patch";
    public const string ExpectedFileName = "esperado.json";

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
            ?? throw new InvalidOperationException($"{ExpectedFileName} vazio em {caseDirectory}");
        return new EvalCase(Path.GetFileName(caseDirectory), File.ReadAllText(Path.Combine(caseDirectory, DiffFileName)), expected);
    }
}

/// <param name="ShouldBlock">Whether the right verdict is RequestChanges.</param>
public sealed record ExpectedReview(
    [property: JsonPropertyName("descricao")] string Description,
    [property: JsonPropertyName("deveBloquear")] bool ShouldBlock,
    [property: JsonPropertyName("problemas")] List<ExpectedFinding> Findings);

/// <summary>
/// An issue the review must report. A finding matches it when it is in the same file, within
/// <see cref="Tolerance"/> lines, and mentions one of the keywords or OWASP categories.
/// </summary>
public sealed record ExpectedFinding(
    [property: JsonPropertyName("arquivo")] string File,
    [property: JsonPropertyName("linha")] int Line,
    [property: JsonPropertyName("tolerancia")] int Tolerance,
    [property: JsonPropertyName("palavrasChave")] List<string> Keywords,
    [property: JsonPropertyName("owasp")] List<string> Owasp,
    [property: JsonPropertyName("severidadeMinima")] Severity MinimumSeverity);
