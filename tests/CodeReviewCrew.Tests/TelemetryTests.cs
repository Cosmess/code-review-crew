using System.Collections.Concurrent;
using System.Diagnostics;
using CodeReviewCrew.Core.Configuration;
using CodeReviewCrew.Core.Orchestration;
using CodeReviewCrew.Core.Telemetry;

namespace CodeReviewCrew.Tests;

public sealed class TelemetryTests : IDisposable
{
    private static readonly ActivitySource TestSource = new("CodeReviewCrew.Tests");

    private readonly ConcurrentQueue<Activity> _activities = [];
    private readonly ActivityListener _listener;

    public TelemetryTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is CrewTelemetry.SourceName or "CodeReviewCrew.Tests",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _activities.Enqueue
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    private const string Diff = """
        --- a/src/A.cs
        +++ b/src/A.cs
        @@ -0,0 +1 @@
        +var sql = $"SELECT {x}";
        """;

    private const string Finding = """
        {"findings":[{"severity":"High","file":"src/A.cs","line":1,"title":"SQL injection","explanation":"x","suggestion":null}]}
        """;

    [Fact]
    public async Task Creates_a_review_span_with_one_child_span_per_agent()
    {
        var fake = new FakeChatClient(p => p.Contains("revisor líder")
            ? """{"summary":"s","verdict":"RequestChanges","findings":[]}"""
            : Finding);

        using var test = TestSource.StartActivity("test")!;
        await new ReviewOrchestrator(fake, retryBackoff: _ => TimeSpan.Zero).ReviewAsync(Diff);

        var activities = Recorded(test);
        var review = Assert.Single(activities, a => a.OperationName == "code_review");
        var agents = activities.Where(a => a.OperationName.StartsWith("invoke_agent")).ToList();
        Assert.Equal(5, agents.Count);
        Assert.All(agents, a => Assert.Equal(review.SpanId, a.ParentSpanId));
        Assert.Contains(agents, a => (string?)a.GetTagItem("gen_ai.agent.name") == "AgenteOwasp");
        Assert.Equal("RequestChanges", review.GetTagItem("crc.verdict"));
        Assert.Equal(5000L, review.GetTagItem("gen_ai.usage.input_tokens"));
    }

    [Fact]
    public async Task Marks_failed_agents_with_error_status_and_retry_events()
    {
        var fake = new FakeChatClient(p => p.Contains("engenheiro sênior de segurança") ? throw new HttpRequestException("503") : """{"findings":[]}""");

        using var test = TestSource.StartActivity("test")!;
        await new ReviewOrchestrator(fake, new CrewConfig { MaxRetries = 1 }, _ => TimeSpan.Zero).ReviewAsync(Diff);

        var security = Assert.Single(Recorded(test), a => a.OperationName == "invoke_agent AgenteSeguranca");
        Assert.Equal(ActivityStatusCode.Error, security.Status);
        Assert.Contains(security.Events, e => e.Name == "retry");
    }

    // Other tests run in parallel and also emit spans: keep only the trace started by this test.
    private List<Activity> Recorded(Activity test) => _activities.Where(a => a.TraceId == test.TraceId).ToList();
}
