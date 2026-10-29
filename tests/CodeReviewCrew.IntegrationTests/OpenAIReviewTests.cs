using CodeReviewCrew.Core.Models;
using CodeReviewCrew.Core.Orchestration;
using Microsoft.Extensions.AI;
using OpenAI;

namespace CodeReviewCrew.IntegrationTests;

/// <summary>
/// End-to-end tests against the real OpenAI API. They are skipped when OPENAI_API_KEY is not set,
/// so they never run (or cost anything) by accident.
/// </summary>
public class OpenAIReviewTests
{
    private static readonly string? ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    private static readonly string Model = Environment.GetEnvironmentVariable("CRC_MODEL") ?? "gpt-5-mini";

    private static IChatClient CreateClient() => new OpenAIClient(ApiKey).GetChatClient(Model).AsIChatClient();

    [SkippableFact]
    public async Task Finds_the_critical_issues_in_the_vulnerable_sample()
    {
        Skip.If(string.IsNullOrEmpty(ApiKey), "OPENAI_API_KEY is not set.");
        var diff = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "samples", "vulnerable.diff"));

        var result = await new ReviewOrchestrator(CreateClient()).ReviewAsync(diff);

        Assert.All(result.SpecialistReports, r => Assert.Null(r.Error));
        Assert.Equal(Verdict.RequestChanges, result.Report.Verdict);
        Assert.Contains(result.Report.Findings, f => f.Severity >= Severity.High);
        Assert.Contains(result.Report.Findings, f => f.Title.Contains("[A0"));
        Assert.All(result.Report.Findings, f => Assert.Equal("src/Api/Controllers/UsersController.cs", f.File));
        Assert.True(result.Usage.InputTokens > 0);
    }

    [SkippableFact]
    public async Task Approves_a_harmless_change()
    {
        Skip.If(string.IsNullOrEmpty(ApiKey), "OPENAI_API_KEY is not set.");
        const string diff = """
            diff --git a/README.md b/README.md
            --- a/README.md
            +++ b/README.md
            @@ -1,3 +1,3 @@
             # Project
            -A sampel project.
            +A sample project.
            """;

        var result = await new ReviewOrchestrator(CreateClient()).ReviewAsync(diff);

        Assert.NotEqual(Verdict.RequestChanges, result.Report.Verdict);
        Assert.DoesNotContain(result.Report.Findings, f => f.Severity >= Severity.High);
    }
}
