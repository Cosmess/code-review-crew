using System.Globalization;
using System.Text;
using CodeReviewCrew.Core.Models;

namespace CodeReviewCrew.Evals;

public sealed record CaseScore(
    string Name,
    int TruePositives,
    int FalseNegatives,
    int FalsePositives,
    int IgnoredLowFindings,
    int UnderratedSeverities,
    bool VerdictCorrect,
    IReadOnlyList<ExpectedFinding> Missed,
    IReadOnlyList<ReviewFinding> FalseAlarms);

public sealed record EvalSummary(IReadOnlyList<CaseScore> Cases)
{
    public int TruePositives => Cases.Sum(c => c.TruePositives);
    public int FalsePositives => Cases.Sum(c => c.FalsePositives);
    public int FalseNegatives => Cases.Sum(c => c.FalseNegatives);

    public double Precision => Ratio(TruePositives, TruePositives + FalsePositives);
    public double Recall => Ratio(TruePositives, TruePositives + FalseNegatives);
    public double F1 => Precision + Recall == 0 ? 0 : 2 * Precision * Recall / (Precision + Recall);
    public double VerdictAccuracy => Ratio(Cases.Count(c => c.VerdictCorrect), Cases.Count);

    private static double Ratio(int part, int total) => total == 0 ? 1 : (double)part / total;
}

/// <summary>Compares a review with the labeled expectations of a case.</summary>
public static class EvalScorer
{
    /// <summary>Findings below this severity that match nothing are style noise, not false positives.</summary>
    public const Severity FalsePositiveThreshold = Severity.Medium;

    public static CaseScore Score(EvalCase evalCase, ReviewReport report)
    {
        var unmatched = report.Findings.ToList();
        var missed = new List<ExpectedFinding>();
        var underrated = 0;

        foreach (var expected in evalCase.Expected.Findings)
        {
            var match = unmatched
                .Where(f => Matches(expected, f))
                .OrderBy(f => Math.Abs(f.Line!.Value - expected.Line))
                .FirstOrDefault();

            if (match is null)
            {
                missed.Add(expected);
                continue;
            }

            unmatched.Remove(match);
            if (match.Severity < expected.MinimumSeverity)
            {
                underrated++;
            }
        }

        var falseAlarms = unmatched.Where(f => f.Severity >= FalsePositiveThreshold).ToList();
        var verdictCorrect = evalCase.Expected.ShouldBlock == (report.Verdict == Verdict.RequestChanges);

        return new CaseScore(
            evalCase.Name,
            evalCase.Expected.Findings.Count - missed.Count,
            missed.Count,
            falseAlarms.Count,
            unmatched.Count - falseAlarms.Count,
            underrated,
            verdictCorrect,
            missed,
            falseAlarms);
    }

    public static bool Matches(ExpectedFinding expected, ReviewFinding finding)
    {
        if (finding.File != expected.File || finding.Line is not { } line || Math.Abs(line - expected.Line) > expected.Tolerance)
        {
            return false;
        }

        var text = Normalize($"{finding.Title} {finding.Explanation} {finding.Suggestion}");
        return expected.Keywords.Any(k => text.Contains(Normalize(k), StringComparison.Ordinal))
            || expected.Owasp.Any(category => finding.Title.Contains($"[{category}:", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Lower case without accents, so "injeção" matches the keyword "injecao".</summary>
    public static string Normalize(string text)
    {
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}
