using CodeReviewCrew.Core.Configuration;
using CodeReviewCrew.Core.Models;
using CodeReviewCrew.Core.Orchestration;

namespace CodeReviewCrew.Tests;

public class ReviewOrchestratorTests
{
    private const string Diff = """
        --- a/src/UserRepository.cs
        +++ b/src/UserRepository.cs
        @@ -10,3 +10,3 @@
             public User? Find(string name)
        -    var sql = "SELECT * FROM Users WHERE Id = @id";
        +    var sql = $"SELECT * FROM Users WHERE Name = '{name}'";
             return Query(sql);
        """;

    private const string SqlInjection = """
        {"findings":[{"severity":"Critical","file":"src/UserRepository.cs","line":11,"title":"SQL injection","explanation":"User input is concatenated into SQL.","suggestion":"Use parameters."}]}
        """;

    private const string MagicNumber = """
        {"findings":[{"severity":"Low","file":"src/UserRepository.cs","line":11,"title":"Magic number","explanation":"Use a constant.","suggestion":null}]}
        """;

    private const string Empty = """{"findings":[]}""";

    private const string Synthesized = """
        {"summary":"Introduces a SQL injection.","verdict":"RequestChanges","findings":[
          {"severity":"Critical","file":"src/UserRepository.cs","line":11,"title":"SQL injection","explanation":"User input is concatenated into SQL.","suggestion":"Use parameters."},
          {"severity":"Low","file":"src/UserRepository.cs","line":11,"title":"Magic number","explanation":"Use a constant.","suggestion":null}]}
        """;

    private static bool IsSynthesizer(string prompt) => prompt.Contains("lead reviewer");
    private static bool IsSecurity(string prompt) => prompt.Contains("senior application security engineer");
    private static bool IsStyle(string prompt) => prompt.Contains("focused on maintainability");

    private static FakeChatClient CreateFake() => new(prompt => prompt switch
    {
        _ when IsSynthesizer(prompt) => Synthesized,
        _ when IsSecurity(prompt) => SqlInjection,
        _ when IsStyle(prompt) => MagicNumber,
        _ => Empty
    });

    private static ReviewOrchestrator Create(FakeChatClient fake, CrewConfig? config = null) =>
        new(fake, config, retryBackoff: _ => TimeSpan.Zero);

    [Fact]
    public async Task Runs_every_specialist_then_the_synthesizer()
    {
        var fake = CreateFake();
        var orchestrator = Create(fake);
        var started = new List<string>();
        orchestrator.AgentStarted += name => { lock (started) started.Add(name); };

        var result = await orchestrator.ReviewAsync(Diff);

        Assert.Equal(4, result.SpecialistReports.Count);
        Assert.Equal(5, fake.Prompts.Count);
        Assert.Equal("SynthesizerAgent", started[^1]);
        Assert.Single(result.SpecialistReports.Single(r => r.Agent == "SecurityAgent").Findings);
    }

    [Fact]
    public async Task Synthesizer_receives_specialist_findings_and_the_diff()
    {
        var fake = CreateFake();

        await Create(fake).ReviewAsync(Diff);

        var synthesizerPrompt = fake.Prompts.Single(IsSynthesizer);
        Assert.Contains("SQL injection", synthesizerPrompt);
        Assert.Contains("SecurityAgent", synthesizerPrompt);
        Assert.Contains("UserRepository.cs", synthesizerPrompt);
    }

    [Fact]
    public async Task Returns_the_synthesized_report_and_token_usage()
    {
        var config = new CrewConfig { Pricing = new PricingConfig { InputPerMillion = 1m, OutputPerMillion = 10m } };

        var result = await Create(CreateFake(), config).ReviewAsync(Diff);

        Assert.Equal(Verdict.RequestChanges, result.Report.Verdict);
        Assert.Equal(2, result.Report.Findings.Count);
        Assert.Equal(5000, result.Usage.InputTokens);
        Assert.Equal(500, result.Usage.OutputTokens);
        Assert.Equal(0.01m, result.Usage.EstimatedCostUsd);
    }

    [Fact]
    public async Task Drops_findings_below_the_minimum_severity()
    {
        var result = await Create(CreateFake(), new CrewConfig { MinSeverity = Severity.Medium }).ReviewAsync(Diff);

        Assert.Equal("SQL injection", Assert.Single(result.Report.Findings).Title);
    }

    [Fact]
    public async Task Retries_a_transient_failure()
    {
        var securityCalls = 0;
        var fake = new FakeChatClient(prompt =>
        {
            if (IsSecurity(prompt) && Interlocked.Increment(ref securityCalls) == 1)
            {
                throw new HttpRequestException("503 Service Unavailable");
            }

            return IsSynthesizer(prompt) ? Synthesized : IsSecurity(prompt) ? SqlInjection : Empty;
        });
        var orchestrator = Create(fake);
        var retries = new List<string>();
        orchestrator.AgentRetrying += (agent, _, _) => retries.Add(agent);

        var result = await orchestrator.ReviewAsync(Diff);

        Assert.Equal(["SecurityAgent"], retries);
        Assert.All(result.SpecialistReports, r => Assert.Null(r.Error));
        Assert.Single(result.SpecialistReports.Single(r => r.Agent == "SecurityAgent").Findings);
    }

    [Fact]
    public async Task A_failing_specialist_does_not_stop_the_review()
    {
        var fake = new FakeChatClient(prompt =>
            IsSecurity(prompt) ? throw new HttpRequestException("falha permanente")
            : IsSynthesizer(prompt) ? Synthesized
            : IsStyle(prompt) ? MagicNumber
            : Empty);

        var result = await Create(fake, new CrewConfig { MaxRetries = 1 }).ReviewAsync(Diff);

        var security = result.SpecialistReports.Single(r => r.Agent == "SecurityAgent");
        Assert.Contains("falha permanente", security.Error);
        Assert.Equal(2, fake.Prompts.Count(IsSecurity));
        Assert.Equal(Verdict.RequestChanges, result.Report.Verdict);
    }

    [Fact]
    public async Task Falls_back_to_merged_findings_when_the_synthesizer_fails()
    {
        var fake = new FakeChatClient(prompt =>
            IsSynthesizer(prompt) ? "this is not JSON"
            : IsSecurity(prompt) ? SqlInjection
            : IsStyle(prompt) ? MagicNumber
            : Empty);

        var result = await Create(fake, new CrewConfig { MaxRetries = 0 }).ReviewAsync(Diff);

        Assert.StartsWith("The synthesizer agent failed", result.Report.Summary);
        Assert.Equal(Verdict.RequestChanges, result.Report.Verdict);
        Assert.Equal(["SQL injection", "Magic number"], result.Report.Findings.Select(f => f.Title));
    }

    [Fact]
    public async Task Skips_the_synthesizer_when_no_specialist_finds_anything()
    {
        var fake = new FakeChatClient(_ => Empty);

        var result = await Create(fake).ReviewAsync(Diff);

        Assert.Equal(Verdict.Approve, result.Report.Verdict);
        Assert.Equal(4, fake.Prompts.Count);
    }

    [Fact]
    public async Task Does_not_call_the_model_when_every_file_is_ignored()
    {
        var fake = CreateFake();

        var result = await Create(fake, new CrewConfig { Ignore = ["src/**"] }).ReviewAsync(Diff);

        Assert.Empty(fake.Prompts);
        Assert.Equal(Verdict.Approve, result.Report.Verdict);
    }

    [Fact]
    public async Task Splits_large_diffs_into_parts()
    {
        var bigLine = "+" + new string('x', 900);
        var diff = string.Join('\n', Enumerable.Range(1, 3).Select(i => $"--- a/F{i}.cs\n+++ b/F{i}.cs\n@@ -0,0 +1 @@\n{bigLine}"));
        var fake = CreateFake();

        await Create(fake, new CrewConfig { MaxChunkChars = 1000 }).ReviewAsync(diff);

        Assert.Equal(3, fake.Prompts.Count(IsSecurity));
        Assert.Contains(fake.Prompts, p => p.Contains("(part 2 of 3)"));
        var synthesizerPrompt = fake.Prompts.Single(IsSynthesizer);
        Assert.Contains("- F3.cs", synthesizerPrompt);
        Assert.DoesNotContain(bigLine, synthesizerPrompt);
    }

    [Fact]
    public async Task Asks_every_agent_to_write_in_the_configured_language()
    {
        var fake = CreateFake();

        await Create(fake, new CrewConfig { Language = "zh-CN" }).ReviewAsync(Diff);

        Assert.All(fake.Prompts, p => Assert.Contains("in Simplified Chinese", p));
    }

    [Fact]
    public async Task Writes_its_own_messages_in_the_configured_language()
    {
        var result = await Create(new FakeChatClient(_ => Empty), new CrewConfig { Language = "pt-BR" }).ReviewAsync(Diff);

        Assert.Equal("pt-BR", result.Language);
        Assert.Equal("Nenhum problema encontrado pelos agentes especialistas.", result.Report.Summary);
    }

    [Theory]
    [InlineData("High: Bloqueio sync-over-async", "Bloqueio sync-over-async")]
    [InlineData("Médio - Medium: Falta de validação", "Falta de validação")]
    [InlineData("[A05:2025 Injection] SQL por   interpolação", "[A05:2025 Injection] SQL por interpolação")]
    [InlineData("Alta disponibilidade comprometida", "Alta disponibilidade comprometida")]
    public void Removes_repeated_severity_and_extra_spaces_from_titles(string title, string expected)
    {
        var finding = new ReviewFinding(Severity.High, "a.cs", 1, title, "x", null);

        Assert.Equal(expected, finding.Normalized().Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Rejects_an_empty_diff(string diff)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Create(CreateFake()).ReviewAsync(diff));
    }
}
