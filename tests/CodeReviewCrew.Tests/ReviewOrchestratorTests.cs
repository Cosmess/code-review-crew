using CodeReviewCrew.Core.Models;
using CodeReviewCrew.Core.Orchestration;

namespace CodeReviewCrew.Tests;

public class ReviewOrchestratorTests
{
    private const string Diff = """
        --- a/src/UserRepository.cs
        +++ b/src/UserRepository.cs
        @@ -10,3 +10,3 @@
        -    var sql = "SELECT * FROM Users WHERE Id = @id";
        +    var sql = $"SELECT * FROM Users WHERE Name = '{name}'";
        """;

    private const string SqlInjection = """
        {"findings":[{"severity":"Critical","file":"src/UserRepository.cs","line":11,"title":"SQL injection","explanation":"User input is concatenated into SQL.","suggestion":"Use parameters."}]}
        """;

    private const string Empty = """{"findings":[]}""";

    private const string Synthesized = """
        {"summary":"Introduces a SQL injection.","verdict":"RequestChanges","findings":[{"severity":"Critical","file":"src/UserRepository.cs","line":11,"title":"SQL injection","explanation":"User input is concatenated into SQL.","suggestion":"Use parameters."}]}
        """;

    private static FakeChatClient CreateFake() => new(prompt => prompt switch
    {
        _ when prompt.Contains("lead reviewer") => Synthesized,
        _ when prompt.Contains("security engineer") => SqlInjection,
        _ => Empty
    });

    [Fact]
    public async Task Runs_every_specialist_then_the_synthesizer()
    {
        var fake = CreateFake();
        var orchestrator = new ReviewOrchestrator(fake);
        var started = new List<string>();
        orchestrator.AgentStarted += name => { lock (started) started.Add(name); };

        var result = await orchestrator.ReviewAsync(Diff);

        Assert.Equal(3, result.SpecialistReports.Count);
        Assert.Equal(4, fake.Prompts.Count);
        Assert.Equal("SynthesizerAgent", started[^1]);
        Assert.Single(result.SpecialistReports.Single(r => r.Agent == "SecurityAgent").Findings);
    }

    [Fact]
    public async Task Synthesizer_receives_specialist_findings_and_the_diff()
    {
        var fake = CreateFake();

        await new ReviewOrchestrator(fake).ReviewAsync(Diff);

        var synthesizerPrompt = fake.Prompts.Single(p => p.Contains("lead reviewer"));
        Assert.Contains("SQL injection", synthesizerPrompt);
        Assert.Contains("SecurityAgent", synthesizerPrompt);
        Assert.Contains("UserRepository.cs", synthesizerPrompt);
    }

    [Fact]
    public async Task Returns_the_synthesized_report()
    {
        var result = await new ReviewOrchestrator(CreateFake()).ReviewAsync(Diff);

        Assert.Equal(Verdict.RequestChanges, result.Report.Verdict);
        var finding = Assert.Single(result.Report.Findings);
        Assert.Equal(Severity.Critical, finding.Severity);
        Assert.Equal(11, finding.Line);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Rejects_an_empty_diff(string diff)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => new ReviewOrchestrator(CreateFake()).ReviewAsync(diff));
    }
}
