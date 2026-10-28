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
        md.AppendLine($"**Veredito:** {VerdictLabel(report.Verdict)}").AppendLine();
        md.AppendLine(report.Summary).AppendLine();

        if (report.Findings.Count == 0)
        {
            md.AppendLine("Nenhum problema encontrado. ✅");
        }
        else
        {
            md.AppendLine("| Severidade | Local | Problema |").AppendLine("|---|---|---|");
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
                    md.AppendLine($"**Sugestão:** {f.Suggestion}").AppendLine();
                }
            }
        }

        md.AppendLine("<details><summary>Análises por agente</summary>").AppendLine();
        foreach (var r in result.SpecialistReports)
        {
            md.AppendLine($"- **{r.Agent}**: {r.Findings.Count} problema(s)");
        }

        md.AppendLine().AppendLine("</details>");
        return md.ToString();
    }

    private static string VerdictLabel(Verdict verdict) => verdict switch
    {
        Verdict.Approve => "✅ Aprovar",
        Verdict.Comment => "💬 Comentar",
        Verdict.RequestChanges => "❌ Solicitar alterações",
        _ => verdict.ToString()
    };

    private static string SeverityLabel(Severity severity) => severity switch
    {
        Severity.Critical => "🔴 Crítico",
        Severity.High => "🟠 Alto",
        Severity.Medium => "🟡 Médio",
        Severity.Low => "🔵 Baixo",
        _ => "⚪ Info"
    };

    private static string Escape(string text) => text.Replace("|", "\\|");
}
