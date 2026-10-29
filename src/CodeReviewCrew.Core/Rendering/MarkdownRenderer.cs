using System.Text;
using CodeReviewCrew.Core.Localization;
using CodeReviewCrew.Core.Models;

namespace CodeReviewCrew.Core.Rendering;

/// <summary>Renders a review as Markdown, in the review's language, ready to be posted as a pull request comment.</summary>
public static class MarkdownRenderer
{
    /// <param name="postedInline">Findings already posted as inline comments; their details are not repeated in the body.</param>
    public static string Render(CrewResult result, IReadOnlySet<ReviewFinding>? postedInline = null)
    {
        var language = ReviewLanguage.Parse(result.Language);
        var s = language.Strings;
        var report = result.Report;
        var findings = report.Findings.OrderByDescending(f => f.Severity).ToList();
        var md = new StringBuilder();

        md.AppendLine("## 🤖 Code Review Crew").AppendLine();
        md.AppendLine($"**{s.Verdict}:** {VerdictLabel(report.Verdict, s)}").AppendLine();
        md.AppendLine(report.Summary).AppendLine();

        if (findings.Count == 0)
        {
            md.AppendLine(s.NoIssues).AppendLine();
        }
        else
        {
            md.AppendLine($"| {s.SeverityHeader} | {s.LocationHeader} | {s.IssueHeader} |").AppendLine("|---|---|---|");
            foreach (var f in findings)
            {
                var inline = postedInline?.Contains(f) == true ? " 💬" : "";
                md.AppendLine($"| {SeverityLabel(f.Severity, s)} | {Location(f)}{inline} | {Escape(f.Title)} |");
            }

            md.AppendLine();
            if (postedInline is { Count: > 0 })
            {
                md.AppendLine(s.InlineLegend).AppendLine();
            }

            foreach (var f in findings.Where(f => postedInline?.Contains(f) != true))
            {
                md.AppendLine($"### {SeverityLabel(f.Severity, s)} {f.Title}").AppendLine();
                md.AppendLine($"{Location(f)}").AppendLine();
                AppendDetails(md, f, s);
            }
        }

        var failed = result.SpecialistReports.Where(r => r.Error is not null).ToList();
        if (failed.Count > 0)
        {
            md.AppendLine("> [!WARNING]");
            md.AppendLine($"> {s.FailedAgentsWarning}");
            foreach (var r in failed)
            {
                md.AppendLine($"> - **{r.Agent}**: {r.Error}");
            }

            md.AppendLine();
        }

        md.AppendLine($"<details><summary>{s.FindingsPerAgent}</summary>").AppendLine();
        foreach (var r in result.SpecialistReports)
        {
            var status = r.Error is null ? "" : $" {s.AgentFailed}";
            md.AppendLine($"- **{r.Agent}**: {string.Format(s.FindingsCount, r.Findings.Count)}{status}");
        }

        md.AppendLine().AppendLine("</details>").AppendLine();
        md.AppendLine($"<sub>{UsageLine(result.Usage, result.Language)}</sub>");
        return md.ToString();
    }

    /// <summary>Renders a single finding as an inline comment on the line of code.</summary>
    public static string RenderInlineComment(ReviewFinding finding, string language = "en")
    {
        var s = ReviewLanguage.Parse(language).Strings;
        var md = new StringBuilder();
        md.AppendLine($"**{SeverityLabel(finding.Severity, s)}: {finding.Title}**").AppendLine();
        AppendDetails(md, finding, s);
        md.Append("<sub>Code Review Crew</sub>");
        return md.ToString();
    }

    public static string UsageLine(UsageSummary usage, string language = "en")
    {
        var lang = ReviewLanguage.Parse(language);
        var tokens = string.Format(lang.Strings.TokenUsage, usage.InputTokens.ToString("N0", lang.Culture), usage.OutputTokens.ToString("N0", lang.Culture));
        return usage.EstimatedCostUsd is { } cost
            ? $"{tokens} · {string.Format(lang.Strings.EstimatedCost, cost.ToString("0.0000", lang.Culture))}"
            : tokens;
    }

    private static void AppendDetails(StringBuilder md, ReviewFinding f, ReviewStrings s)
    {
        md.AppendLine(f.Explanation).AppendLine();
        if (!string.IsNullOrWhiteSpace(f.Suggestion))
        {
            md.AppendLine($"**{s.Suggestion}:** {f.Suggestion}").AppendLine();
        }
    }

    private static string Location(ReviewFinding f) => f.Line is { } line ? $"`{f.File}:{line}`" : $"`{f.File}`";

    private static string VerdictLabel(Verdict verdict, ReviewStrings s) => verdict switch
    {
        Verdict.Approve => s.Approve,
        Verdict.Comment => s.Comment,
        Verdict.RequestChanges => s.RequestChanges,
        _ => verdict.ToString()
    };

    private static string SeverityLabel(Severity severity, ReviewStrings s) => severity switch
    {
        Severity.Critical => s.Critical,
        Severity.High => s.High,
        Severity.Medium => s.Medium,
        Severity.Low => s.Low,
        _ => s.Info
    };

    private static string Escape(string text) => text.Replace("|", "\\|");
}
