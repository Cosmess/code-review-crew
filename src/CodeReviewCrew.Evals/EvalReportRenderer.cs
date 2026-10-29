using System.Globalization;
using System.Text;
using CodeReviewCrew.Core.Models;
using CodeReviewCrew.Core.Rendering;

namespace CodeReviewCrew.Evals;

public static class EvalReportRenderer
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string Render(EvalSummary summary, string model, UsageSummary usage)
    {
        var md = new StringBuilder();
        md.AppendLine("# Avaliação do Code Review Crew").AppendLine();
        md.AppendLine($"Modelo: `{model}` · {summary.Cases.Count} casos · {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").AppendLine();

        md.AppendLine("| Métrica | Valor |").AppendLine("|---|---|");
        md.AppendLine($"| Precisão | {Percent(summary.Precision)} |");
        md.AppendLine($"| Recall | {Percent(summary.Recall)} |");
        md.AppendLine($"| F1 | {Percent(summary.F1)} |");
        md.AppendLine($"| Vereditos corretos | {Percent(summary.VerdictAccuracy)} |");
        md.AppendLine($"| Acertos / esperados | {summary.TruePositives} / {summary.TruePositives + summary.FalseNegatives} |");
        md.AppendLine($"| Falsos positivos (≥ {EvalScorer.FalsePositiveThreshold}) | {summary.FalsePositives} |").AppendLine();
        md.AppendLine($"<sub>{MarkdownRenderer.UsageLine(usage)}</sub>").AppendLine();

        md.AppendLine("## Por caso").AppendLine();
        md.AppendLine("| Caso | Acertos | Perdidos | Falsos positivos | Severidade subestimada | Veredito |");
        md.AppendLine("|---|---|---|---|---|---|");
        foreach (var c in summary.Cases)
        {
            md.AppendLine($"| {c.Name} | {c.TruePositives} | {c.FalseNegatives} | {c.FalsePositives} | {c.UnderratedSeverities} | {(c.VerdictCorrect ? "✅" : "❌")} |");
        }

        var details = summary.Cases.Where(c => c.Missed.Count > 0 || c.FalseAlarms.Count > 0).ToList();
        if (details.Count > 0)
        {
            md.AppendLine().AppendLine("## Detalhes dos erros").AppendLine();
            foreach (var c in details)
            {
                md.AppendLine($"### {c.Name}").AppendLine();
                foreach (var m in c.Missed)
                {
                    md.AppendLine($"- Não encontrado: `{m.File}:{m.Line}` ({string.Join(", ", m.Keywords.Take(3))})");
                }

                foreach (var f in c.FalseAlarms)
                {
                    md.AppendLine($"- Falso positivo: `{f.File}:{f.Line}` {f.Severity}: {f.Title}");
                }

                md.AppendLine();
            }
        }

        return md.ToString();
    }

    private static string Percent(double value) => value.ToString("P0", PtBr);
}
