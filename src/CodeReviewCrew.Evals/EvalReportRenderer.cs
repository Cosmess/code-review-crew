using System.Globalization;
using System.Text;
using CodeReviewCrew.Core.Models;
using CodeReviewCrew.Core.Rendering;

namespace CodeReviewCrew.Evals;

public static class EvalReportRenderer
{
    public static string Render(EvalSummary summary, string model, string language, UsageSummary usage)
    {
        var md = new StringBuilder();
        md.AppendLine("# Code Review Crew evaluation").AppendLine();
        md.AppendLine($"Model: `{model}` · review language: `{language}` · {summary.Cases.Count} cases · {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").AppendLine();

        md.AppendLine("| Metric | Value |").AppendLine("|---|---|");
        md.AppendLine($"| Precision | {Percent(summary.Precision)} |");
        md.AppendLine($"| Recall | {Percent(summary.Recall)} |");
        md.AppendLine($"| F1 | {Percent(summary.F1)} |");
        md.AppendLine($"| Correct verdicts | {Percent(summary.VerdictAccuracy)} |");
        md.AppendLine($"| Found / expected | {summary.TruePositives} / {summary.TruePositives + summary.FalseNegatives} |");
        md.AppendLine($"| False positives (≥ {EvalScorer.FalsePositiveThreshold}) | {summary.FalsePositives} |").AppendLine();
        md.AppendLine($"<sub>{MarkdownRenderer.UsageLine(usage)}</sub>").AppendLine();

        md.AppendLine("## Per case").AppendLine();
        md.AppendLine("| Case | Found | Missed | False positives | Underrated severity | Verdict |");
        md.AppendLine("|---|---|---|---|---|---|");
        foreach (var c in summary.Cases)
        {
            md.AppendLine($"| {c.Name} | {c.TruePositives} | {c.FalseNegatives} | {c.FalsePositives} | {c.UnderratedSeverities} | {(c.VerdictCorrect ? "✅" : "❌")} |");
        }

        var details = summary.Cases.Where(c => c.Missed.Count > 0 || c.FalseAlarms.Count > 0).ToList();
        if (details.Count > 0)
        {
            md.AppendLine().AppendLine("## Error details").AppendLine();
            foreach (var c in details)
            {
                md.AppendLine($"### {c.Name}").AppendLine();
                foreach (var m in c.Missed)
                {
                    md.AppendLine($"- Missed: `{m.File}:{m.Line}` ({string.Join(", ", m.Keywords.Take(3))})");
                }

                foreach (var f in c.FalseAlarms)
                {
                    md.AppendLine($"- False positive: `{f.File}:{f.Line}` {f.Severity}: {f.Title}");
                }

                md.AppendLine();
            }
        }

        return md.ToString();
    }

    private static string Percent(double value) => $"{(value * 100).ToString("0", CultureInfo.InvariantCulture)}%";
}
