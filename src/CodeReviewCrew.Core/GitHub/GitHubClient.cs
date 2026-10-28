using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeReviewCrew.Core.GitHub;

public sealed record PullRequestInfo(string Title, string HeadSha);

public sealed record ReviewComment(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("line")] int Line,
    [property: JsonPropertyName("body")] string Body)
{
    [JsonPropertyName("side")]
    public string Side => "RIGHT";
}

public sealed class GitHubApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

/// <summary>Minimal client for the GitHub REST endpoints used to review a pull request.</summary>
public sealed class GitHubClient
{
    private readonly HttpClient _http;

    /// <param name="http">An <see cref="HttpClient"/> whose base address is the GitHub API (e.g. https://api.github.com/).</param>
    public GitHubClient(HttpClient http, string token)
    {
        _http = http;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("code-review-crew");
        _http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<PullRequestInfo> GetPullRequestAsync(string repository, int number, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, PullUrl(repository, number));
        request.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var response = await SendAsync(request, cancellationToken);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = json.RootElement;
        return new PullRequestInfo(root.GetProperty("title").GetString() ?? "", root.GetProperty("head").GetProperty("sha").GetString()!);
    }

    public async Task<string> GetPullRequestDiffAsync(string repository, int number, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, PullUrl(repository, number));
        request.Headers.Accept.ParseAdd("application/vnd.github.diff");

        using var response = await SendAsync(request, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <returns>The URL of the created review.</returns>
    public async Task<string> CreateReviewAsync(
        string repository, int number, string commitId, string body, IReadOnlyList<ReviewComment> comments, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{PullUrl(repository, number)}/reviews")
        {
            // COMMENT never blocks the merge and works with the default GITHUB_TOKEN of GitHub Actions.
            Content = JsonContent.Create(new { commit_id = commitId, body, @event = "COMMENT", comments })
        };
        request.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var response = await SendAsync(request, cancellationToken);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return json.RootElement.TryGetProperty("html_url", out var url) ? url.GetString() ?? "" : "";
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            response.Dispose();
            throw new GitHubApiException(response.StatusCode, $"GitHub respondeu {(int)response.StatusCode} em {request.Method} {request.RequestUri}: {error}");
        }

        return response;
    }

    private static string PullUrl(string repository, int number)
    {
        if (repository.Split('/') is not [{ Length: > 0 }, { Length: > 0 }])
        {
            throw new ArgumentException($"Repositório inválido '{repository}'. Use o formato dono/nome.", nameof(repository));
        }

        return $"repos/{repository}/pulls/{number}";
    }
}
