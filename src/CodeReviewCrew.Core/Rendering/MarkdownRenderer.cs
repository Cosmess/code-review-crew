using System.Globalization;
using System.Text;
using CodeReviewCrew.Core.Models;

namespace CodeReviewCrew.Core.Rendering;

/// <summary>Renders a review as Markdown, ready to be posted as a pull request comment.</summary>
public static class MarkdownRenderer
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <param name="postedInline">Findings already posted as inline comments; their details are not repeated in the body.</param>
    public static string Render(CrewResult result, IReadOnlySet<ReviewFinding>? postedInline = null)
    {
        var report = result.Report;
        var findings = report.Findings.OrderByDescending(f => f.Severity).ToList();
        var md = new StringBuilder();

        md.AppendLine("## 🤖 Code Review Crew").AppendLine();
        md.AppendLine($"**Veredito:** {VerdictLabel(report.Verdict)}").AppendLine();
        md.AppendLine(report.Summary).AppendLine();

        if (findings.Count == 0)
        {
            md.AppendLine("Nenhum problema encontrado. ✅").AppendLine();
        }
        else
        {
            md.AppendLine("| Severidade | Local | Problema |").AppendLine("|---|---|---|");
            foreach (var f in findings)
            {
                var inline = postedInline?.Contains(f) == true ? " 💬" : "";
                md.AppendLine($"| {SeverityLabel(f.Severity)} | {Location(f)}{inline} | {Escape(f.Title)} |");
            }

            md.AppendLine();
            if (postedInline is { Count: > 0 })
            {
                md.AppendLine("💬 = detalhes comentados na linha do código.").AppendLine();
            }

            foreach (var f in findings.Where(f => postedInline?.Contains(f) != true))
            {
                md.AppendLine($"### {SeverityLabel(f.Severity)} {f.Title}").AppendLine();
                md.AppendLine($"{Location(f)}").AppendLine();
                AppendDetails(md, f);
            }
        }

        var failed = result.SpecialistReports.Where(r => r.Error is not null).ToList();
        if (failed.Count > 0)
        {
            md.AppendLine("> [!WARNING]");
            md.AppendLine("> Alguns agentes falharam e a revisão pode estar incompleta:");
            foreach (var r in failed)
            {
                md.AppendLine($"> - **{r.Agent}**: {r.Error}");
            }

            md.AppendLine();
        }

        md.AppendLine("<details><summary>Análises por agente</summary>").AppendLine();
        foreach (var r in result.SpecialistReports)
        {
            var status = r.Error is null ? "" : " ⚠️ falhou";
            md.AppendLine($"- **{r.Agent}**: {r.Findings.Count} problema(s){status}");
        }

        md.AppendLine().AppendLine("</details>").AppendLine();
        md.AppendLine($"<sub>{UsageLine(result.Usage)}</sub>");
        return md.ToString();
    }

    /// <summary>Renders a single finding as an inline comment on the line of code.</summary>
    public static string RenderInlineComment(ReviewFinding finding)
    {
        var md = new StringBuilder();
        md.AppendLine($"**{SeverityLabel(finding.Severity)}: {finding.Title}**").AppendLine();
        AppendDetails(md, finding);
        md.Append("<sub>Code Review Crew</sub>");
        return md.ToString();
    }

    public static string UsageLine(UsageSummary usage)
    {
        var tokens = $"Tokens: {usage.InputTokens.ToString("N0", PtBr)} de entrada / {usage.OutputTokens.ToString("N0", PtBr)} de saída";
        return usage.EstimatedCostUsd is { } cost
            ? $"{tokens} · custo estimado: US$ {cost.ToString("0.0000", PtBr)}"
            : tokens;
    }

    private static void AppendDetails(StringBuilder md, ReviewFinding f)
    {
        md.AppendLine(f.Explanation).AppendLine();
        if (!string.IsNullOrWhiteSpace(f.Suggestion))
        {
            md.AppendLine($"**Sugestão:** {f.Suggestion}").AppendLine();
        }
    }

    private static string Location(ReviewFinding f) => f.Line is { } line ? $"`{f.File}:{line}`" : $"`{f.File}`";

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
