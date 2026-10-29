using System.Net;
using System.Text.Json;
using CodeReviewCrew.Core.GitHub;
using CodeReviewCrew.Core.Models;

namespace CodeReviewCrew.Tests;

public class GitHubTests
{
    private const string Diff = """
        diff --git a/src/A.cs b/src/A.cs
        --- a/src/A.cs
        +++ b/src/A.cs
        @@ -1,2 +1,3 @@
         class A
        +{ var sql = $"SELECT {x}"; }
         // fim
        """;

    private static readonly ReviewFinding OnDiffLine = new(Severity.Critical, "src/A.cs", 2, "SQL injection", "Explanation.", "Use parameters.");
    private static readonly ReviewFinding OutsideDiff = new(Severity.Medium, "src/A.cs", 40, "Outside the diff", "Explanation.", null);
    private static readonly ReviewFinding OtherFile = new(Severity.Low, "src/B.cs", 1, "Other file", "Explanation.", null);

    private static CrewResult Result => new([], new ReviewReport("Resumo.", Verdict.RequestChanges, [OnDiffLine, OutsideDiff, OtherFile]), UsageSummary.Empty);

    private static (GitHubClient Client, FakeHttpHandler Handler) Create(Func<RecordedRequest, HttpResponseMessage> respond)
    {
        var handler = new FakeHttpHandler(respond);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        return (new GitHubClient(http, "token-de-teste"), handler);
    }

    [Fact]
    public async Task Fetches_pull_request_metadata_and_diff()
    {
        var (client, handler) = Create(r => r.Accept!.Contains("diff")
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Diff) }
            : FakeHttpHandler.Json("""{"title":"Adiciona busca","head":{"sha":"abc123"}}"""));

        var pr = await client.GetPullRequestAsync("Cosmess/demo", 7);
        var diff = await client.GetPullRequestDiffAsync("Cosmess/demo", 7);

        Assert.Equal(new PullRequestInfo("Adiciona busca", "abc123"), pr);
        Assert.Equal(Diff, diff);
        Assert.All(handler.Requests, r => Assert.Equal("https://api.github.com/repos/Cosmess/demo/pulls/7", r.Url));
    }

    [Fact]
    public async Task Posts_findings_on_diff_lines_as_inline_comments()
    {
        var (client, handler) = Create(_ => FakeHttpHandler.Json("""{"html_url":"https://github.com/r/pull/7#review"}"""));

        var url = await new ReviewPublisher(client).PublishAsync("Cosmess/demo", 7, "abc123", Diff, Result);

        Assert.Equal("https://github.com/r/pull/7#review", url);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://api.github.com/repos/Cosmess/demo/pulls/7/reviews", request.Url);

        var body = JsonDocument.Parse(request.Body!).RootElement;
        Assert.Equal("abc123", body.GetProperty("commit_id").GetString());
        Assert.Equal("COMMENT", body.GetProperty("event").GetString());
        var comment = Assert.Single(body.GetProperty("comments").EnumerateArray());
        Assert.Equal("src/A.cs", comment.GetProperty("path").GetString());
        Assert.Equal(2, comment.GetProperty("line").GetInt32());
        Assert.Equal("RIGHT", comment.GetProperty("side").GetString());
        Assert.Contains("SQL injection", comment.GetProperty("body").GetString());
        Assert.Contains("Outside the diff", body.GetProperty("body").GetString());
    }

    [Fact]
    public async Task Falls_back_to_a_body_only_review_when_github_rejects_a_comment()
    {
        var (client, handler) = Create(r => r.Body!.Contains("\"line\"")
            ? FakeHttpHandler.Json("""{"message":"Validation Failed"}""", HttpStatusCode.UnprocessableEntity)
            : FakeHttpHandler.Json("""{"html_url":"ok"}"""));

        await new ReviewPublisher(client).PublishAsync("Cosmess/demo", 7, "abc123", Diff, Result);

        Assert.Equal(2, handler.Requests.Count);
        var retry = JsonDocument.Parse(handler.Requests[1].Body!).RootElement;
        Assert.Equal(0, retry.GetProperty("comments").GetArrayLength());
        Assert.Contains("Use parameters.", retry.GetProperty("body").GetString());
    }

    [Fact]
    public async Task Reports_api_errors_with_the_status_code()
    {
        var (client, _) = Create(_ => FakeHttpHandler.Json("""{"message":"Not Found"}""", HttpStatusCode.NotFound));

        var ex = await Assert.ThrowsAsync<GitHubApiException>(() => client.GetPullRequestAsync("Cosmess/demo", 999));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Theory]
    [InlineData("sem-barra")]
    [InlineData("dono/")]
    public async Task Rejects_invalid_repository_names(string repository)
    {
        var (client, _) = Create(_ => throw new InvalidOperationException("should not call the API"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetPullRequestDiffAsync(repository, 1));
    }

    [Fact]
    public void Limits_the_number_of_inline_comments_keeping_the_most_severe()
    {
        var (client, _) = Create(_ => FakeHttpHandler.Json("{}"));
        var low = OnDiffLine with { Severity = Severity.Low, Title = "baixo" };

        var selected = new ReviewPublisher(client, maxInlineComments: 1).SelectInlineFindings(Diff, [low, OnDiffLine]);

        Assert.Equal(OnDiffLine, Assert.Single(selected));
    }
}
