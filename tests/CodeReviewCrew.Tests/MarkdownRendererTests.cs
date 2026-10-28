using CodeReviewCrew.Core.Models;
using CodeReviewCrew.Core.Rendering;

namespace CodeReviewCrew.Tests;

public class MarkdownRendererTests
{
    private static readonly ReviewFinding Blocking = new(Severity.High, "src/A.cs", 42, "Chamada bloqueante | sync", "Usa .Result.", "Use await.");
    private static readonly ReviewFinding Naming = new(Severity.Low, "src/B.cs", null, "Nome ruim", "Nome não diz nada.", null);

    private static CrewResult Result(params SpecialistReport[] reports) =>
        new(reports, new ReviewReport("Uma chamada bloqueante.", Verdict.RequestChanges, [Blocking, Naming]), new UsageSummary(12345, 678, 0.0123m));

    [Fact]
    public void Renders_verdict_findings_usage_and_agent_counts()
    {
        var md = MarkdownRenderer.Render(Result(new SpecialistReport("AgentePerformance", [Blocking])));

        Assert.Contains("❌ Solicitar alterações", md);
        Assert.Contains("`src/A.cs:42`", md);
        Assert.Contains("Chamada bloqueante \\| sync", md);
        Assert.Contains("**Sugestão:** Use await.", md);
        Assert.Contains("**AgentePerformance**: 1 problema(s)", md);
        Assert.Contains("Tokens: 12.345 de entrada / 678 de saída · custo estimado: US$ 0,0123", md);
    }

    [Fact]
    public void Does_not_repeat_details_of_findings_posted_inline()
    {
        var md = MarkdownRenderer.Render(Result(), new HashSet<ReviewFinding> { Blocking });

        Assert.Contains("`src/A.cs:42` 💬", md);
        Assert.DoesNotContain("Usa .Result.", md);
        Assert.Contains("Nome não diz nada.", md);
    }

    [Fact]
    public void Warns_about_failed_agents()
    {
        var md = MarkdownRenderer.Render(Result(new SpecialistReport("AgenteOwasp", [], "Tempo limite excedido")));

        Assert.Contains("[!WARNING]", md);
        Assert.Contains("**AgenteOwasp**: Tempo limite excedido", md);
    }

    [Fact]
    public void Renders_an_approval_without_findings()
    {
        var result = new CrewResult([], new ReviewReport("Tudo certo.", Verdict.Approve, []), UsageSummary.Empty);

        var md = MarkdownRenderer.Render(result);

        Assert.Contains("✅ Aprovar", md);
        Assert.Contains("Nenhum problema encontrado.", md);
        Assert.DoesNotContain("custo estimado", md);
    }

    [Fact]
    public void Renders_an_inline_comment()
    {
        var comment = MarkdownRenderer.RenderInlineComment(Blocking);

        Assert.StartsWith("**🟠 Alto: Chamada bloqueante | sync**", comment);
        Assert.Contains("**Sugestão:** Use await.", comment);
    }
}
