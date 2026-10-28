using System.Text;
using CodeReviewCrew.Core.Models;

namespace CodeReviewCrew.Core.Rendering;

/// <summary>Renders a review as Markdown, ready to be posted as a pull request comment.</summary>
public static class MarkdownRenderer
{
    public static string Render(CrewResult result)
    {
        var report = result.Report;
        var md = new StringBuilder();

        md.AppendLine("## 🤖 Code Review Crew").AppendLine();
        md.AppendLine($"**Verdict:** {VerdictLabel(report.Verdict)}").AppendLine();
        md.AppendLine(report.Summary).AppendLine();

        if (report.Findings.Count == 0)
        {
            md.AppendLine("No issues found. ✅");
        }
        else
        {
            md.AppendLine("| Severity | Location | Issue |").AppendLine("|---|---|---|");
            foreach (var f in report.Findings.OrderByDescending(f => f.Severity))
            {
                var location = f.Line is { } line ? $"`{f.File}:{line}`" : $"`{f.File}`";
                md.AppendLine($"| {SeverityLabel(f.Severity)} | {location} | {Escape(f.Title)} |");
            }

            md.AppendLine();
            foreach (var f in report.Findings.OrderByDescending(f => f.Severity))
            {
                md.AppendLine($"### {SeverityLabel(f.Severity)} {f.Title}").AppendLine();
                md.AppendLine(f.Explanation).AppendLine();
                if (!string.IsNullOrWhiteSpace(f.Suggestion))
                {
                    md.AppendLine($"**Suggestion:** {f.Suggestion}").AppendLine();
                }
            }
        }

        md.AppendLine("<details><summary>Raw findings per agent</summary>").AppendLine();
        foreach (var r in result.SpecialistReports)
        {
            md.AppendLine($"- **{r.Agent}**: {r.Findings.Count} finding(s)");
        }

        md.AppendLine().AppendLine("</details>");
        return md.ToString();
    }

    private static string VerdictLabel(Verdict verdict) => verdict switch
    {
        Verdict.Approve => "✅ Approve",
        Verdict.Comment => "💬 Comment",
        Verdict.RequestChanges => "❌ Request changes",
        _ => verdict.ToString()
    };

    private static string SeverityLabel(Severity severity) => severity switch
    {
        Severity.Critical => "🔴 Critical",
        Severity.High => "🟠 High",
        Severity.Medium => "🟡 Medium",
        Severity.Low => "🔵 Low",
        _ => "⚪ Info"
    };

    private static string Escape(string text) => text.Replace("|", "\\|");
}
