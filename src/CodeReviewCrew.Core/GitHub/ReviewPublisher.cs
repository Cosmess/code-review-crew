using System.Net;
using CodeReviewCrew.Core.Diff;
using CodeReviewCrew.Core.Localization;
using CodeReviewCrew.Core.Models;
using CodeReviewCrew.Core.Rendering;

namespace CodeReviewCrew.Core.GitHub;

/// <summary>
/// Posts a review on a pull request: findings on lines that are part of the diff become
/// inline comments, and the summary with every finding goes into the review body.
/// </summary>
public sealed class ReviewPublisher(GitHubClient gitHub, int maxInlineComments = 30)
{
    // GitHub rejects review bodies longer than 65536 characters.
    private const int MaxBodyLength = 65_000;

    public async Task<string> PublishAsync(
        string repository, int number, string headSha, string diff, CrewResult result, CancellationToken cancellationToken = default)
    {
        var inline = SelectInlineFindings(diff, result.Report.Findings);
        var comments = inline
            .Select(f => new ReviewComment(f.File, f.Line!.Value, MarkdownRenderer.RenderInlineComment(f, result.Language)))
            .ToList();

        try
        {
            return await gitHub.CreateReviewAsync(repository, number, headSha, Body(result, inline), comments, cancellationToken);
        }
        catch (GitHubApiException ex) when (ex.StatusCode == HttpStatusCode.UnprocessableEntity && comments.Count > 0)
        {
            // A line GitHub does not accept invalidates the whole review: post everything in the body instead.
            return await gitHub.CreateReviewAsync(repository, number, headSha, Body(result, new HashSet<ReviewFinding>()), [], cancellationToken);
        }
    }

    public HashSet<ReviewFinding> SelectInlineFindings(string diff, IEnumerable<ReviewFinding> findings)
    {
        var commentable = DiffParser.Parse(diff).ToDictionary(f => f.Path, f => f.CommentableLines);

        return findings
            .Where(f => f.Line is { } line && commentable.TryGetValue(f.File, out var lines) && lines.Contains(line))
            .OrderByDescending(f => f.Severity)
            .Take(maxInlineComments)
            .ToHashSet();
    }

    private static string Body(CrewResult result, IReadOnlySet<ReviewFinding> inline)
    {
        var body = MarkdownRenderer.Render(result, inline);
        return body.Length <= MaxBodyLength
            ? body
            : body[..MaxBodyLength] + $"\n\n_{ReviewLanguage.Parse(result.Language).Strings.ReviewTruncated}_";
    }
}
