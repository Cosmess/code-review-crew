using CodeReviewCrew.Core.Models;
using CodeReviewCrew.Core.Rendering;

namespace CodeReviewCrew.Tests;

public class MarkdownRendererTests
{
    [Fact]
    public void Renders_verdict_findings_and_agent_counts()
    {
        var finding = new ReviewFinding(Severity.High, "src/A.cs", 42, "Blocking call | sync", "Uses .Result.", "Await the task.");
        var result = new CrewResult(
            [new SpecialistReport("PerformanceAgent", [finding])],
            new ReviewReport("One blocking call.", Verdict.RequestChanges, [finding]));

        var md = MarkdownRenderer.Render(result);

        Assert.Contains("❌ Request changes", md);
        Assert.Contains("`src/A.cs:42`", md);
        Assert.Contains("Blocking call \\| sync", md);
        Assert.Contains("**Suggestion:** Await the task.", md);
        Assert.Contains("**PerformanceAgent**: 1 finding(s)", md);
    }

    [Fact]
    public void Renders_an_approval_without_findings()
    {
        var result = new CrewResult([], new ReviewReport("Looks good.", Verdict.Approve, []));

        var md = MarkdownRenderer.Render(result);

        Assert.Contains("✅ Approve", md);
        Assert.Contains("No issues found.", md);
    }
}
