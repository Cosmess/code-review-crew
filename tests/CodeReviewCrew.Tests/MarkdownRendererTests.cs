using CodeReviewCrew.Core.Localization;
using CodeReviewCrew.Core.Models;
using CodeReviewCrew.Core.Rendering;

namespace CodeReviewCrew.Tests;

public class MarkdownRendererTests
{
    private static readonly ReviewFinding Blocking = new(Severity.High, "src/A.cs", 42, "Blocking call | sync", "Uses .Result.", "Use await.");
    private static readonly ReviewFinding Naming = new(Severity.Low, "src/B.cs", null, "Bad name", "The name says nothing.", null);

    private static CrewResult Result(string language = "en", params SpecialistReport[] reports) =>
        new(reports, new ReviewReport("One blocking call.", Verdict.RequestChanges, [Blocking, Naming]), new UsageSummary(12345, 678, 0.0123m), language);

    [Fact]
    public void Renders_verdict_findings_usage_and_agent_counts()
    {
        var md = MarkdownRenderer.Render(Result("en", new SpecialistReport("PerformanceAgent", [Blocking])));

        Assert.Contains("**Verdict:** ❌ Request changes", md);
        Assert.Contains("`src/A.cs:42`", md);
        Assert.Contains("Blocking call \\| sync", md);
        Assert.Contains("**Suggestion:** Use await.", md);
        Assert.Contains("**PerformanceAgent**: 1 finding(s)", md);
        Assert.Contains("Tokens: 12,345 input / 678 output · estimated cost: US$ 0.0123", md);
    }

    [Theory]
    [InlineData("pt-BR", "**Veredito:** ❌ Solicitar alterações", "**Sugestão:** Use await.", "Tokens: 12.345 de entrada / 678 de saída · custo estimado: US$ 0,0123")]
    [InlineData("zh-CN", "**结论:** ❌ 需要修改", "**建议:** Use await.", "Token：输入 12,345 / 输出 678 · 预估费用：US$ 0.0123")]
    public void Translates_the_labels_to_the_review_language(string language, string verdict, string suggestion, string usage)
    {
        var md = MarkdownRenderer.Render(Result(language));

        Assert.Contains(verdict, md);
        Assert.Contains(suggestion, md);
        Assert.Contains(usage, md);
    }

    [Fact]
    public void Does_not_repeat_details_of_findings_posted_inline()
    {
        var md = MarkdownRenderer.Render(Result(), new HashSet<ReviewFinding> { Blocking });

        Assert.Contains("`src/A.cs:42` 💬", md);
        Assert.DoesNotContain("Uses .Result.", md);
        Assert.Contains("The name says nothing.", md);
    }

    [Fact]
    public void Warns_about_failed_agents()
    {
        var md = MarkdownRenderer.Render(Result("en", new SpecialistReport("OwaspAgent", [], "Timed out")));

        Assert.Contains("[!WARNING]", md);
        Assert.Contains("**OwaspAgent**: Timed out", md);
        Assert.Contains("⚠️ failed", md);
    }

    [Fact]
    public void Renders_an_approval_without_findings()
    {
        var result = new CrewResult([], new ReviewReport("All good.", Verdict.Approve, []), UsageSummary.Empty);

        var md = MarkdownRenderer.Render(result);

        Assert.Contains("✅ Approve", md);
        Assert.Contains("No issues found.", md);
        Assert.DoesNotContain("estimated cost", md);
    }

    [Fact]
    public void Renders_an_inline_comment_in_the_review_language()
    {
        Assert.StartsWith("**🟠 High: Blocking call | sync**", MarkdownRenderer.RenderInlineComment(Blocking));
        Assert.StartsWith("**🟠 Alto: Blocking call | sync**", MarkdownRenderer.RenderInlineComment(Blocking, "pt-BR"));
    }

    [Theory]
    [InlineData(null, "en")]
    [InlineData("PT-br", "pt-BR")]
    [InlineData("pt", "pt-BR")]
    [InlineData("zh", "zh-CN")]
    public void Parses_language_codes(string? code, string expected)
    {
        Assert.Equal(expected, ReviewLanguage.Parse(code).Code);
    }

    [Fact]
    public void Rejects_unsupported_languages()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ReviewLanguage.Parse("fr"));
        Assert.Contains("en, pt-BR, zh-CN", ex.Message);
    }
}
