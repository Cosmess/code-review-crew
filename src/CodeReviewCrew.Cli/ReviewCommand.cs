using CodeReviewCrew.Core.Configuration;
using CodeReviewCrew.Core.GitHub;
using CodeReviewCrew.Core.Models;
using CodeReviewCrew.Core.Orchestration;
using CodeReviewCrew.Core.Rendering;
using CodeReviewCrew.Providers;

namespace CodeReviewCrew.Cli;

internal sealed record ReviewCommandOptions(
    FileInfo? DiffFile,
    int? PullRequest,
    string? Repository,
    bool Post,
    FileInfo? ConfigFile,
    FileInfo? OutputFile,
    FileInfo? SarifFile,
    Severity? FailOn,
    DirectoryInfo? RepositoryPath);

internal sealed class ReviewCommand(ReviewCommandOptions options)
{
    public const int Success = 0;
    public const int Error = 1;
    public const int FindingsAboveThreshold = 2;

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or GitHubApiException or IOException or HttpRequestException)
        {
            Console.Error.WriteLine($"erro: {ex.Message}");
            return Error;
        }
    }

    private async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        if (options.Post && options.PullRequest is null)
        {
            throw new ArgumentException("--post requer --pr.");
        }

        var config = LoadConfig();
        using var http = new HttpClient { BaseAddress = GitHubApiUrl() };
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (options.Post && string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("A variável GITHUB_TOKEN é necessária para publicar a revisão.");
        }

        var gitHub = new GitHubClient(http, token);
        string diff;
        PullRequestInfo? pullRequest = null;

        if (options.PullRequest is { } number)
        {
            var repository = RequireRepository();
            pullRequest = await gitHub.GetPullRequestAsync(repository, number, cancellationToken);
            diff = await gitHub.GetPullRequestDiffAsync(repository, number, cancellationToken);
            Console.Error.WriteLine($"Revisando {repository}#{number}: {pullRequest.Title}");
        }
        else if (options.DiffFile is not null)
        {
            diff = await File.ReadAllTextAsync(options.DiffFile.FullName, cancellationToken);
        }
        else if (Console.IsInputRedirected)
        {
            diff = await Console.In.ReadToEndAsync(cancellationToken);
        }
        else
        {
            throw new ArgumentException("Informe um arquivo de diff, --pr ou envie o diff pela entrada padrão. Use --help para ver as opções.");
        }

        if (string.IsNullOrWhiteSpace(diff))
        {
            Console.Error.WriteLine("O diff está vazio: nada para revisar.");
            return Success;
        }

        if (options.RepositoryPath is { Exists: false } missing)
        {
            throw new ArgumentException($"Diretório do repositório não encontrado: {missing.FullName}");
        }

        await using var mcp = await McpToolHost.StartAsync(options.RepositoryPath?.FullName, config.McpServers, cancellationToken);
        var orchestrator = new ReviewOrchestrator(ChatClientFactory.Create(), config, tools: mcp.Tools);
        orchestrator.AgentStarted += name => Console.Error.WriteLine($"▶ {name} iniciou");
        orchestrator.AgentRetrying += (name, attempt, ex) => Console.Error.WriteLine($"↻ {name}: tentativa {attempt} falhou ({ex.Message}), tentando de novo");
        orchestrator.AgentCompleted += report => Console.Error.WriteLine(report.Error is null
            ? $"✔ {report.Agent} terminou com {report.Findings.Count} problema(s)"
            : $"⚠ {report.Agent} falhou: {report.Error}");

        var result = await orchestrator.ReviewAsync(diff, cancellationToken);
        var markdown = MarkdownRenderer.Render(result);
        Console.WriteLine(markdown);
        Console.Error.WriteLine(MarkdownRenderer.UsageLine(result.Usage));

        await WriteOutputsAsync(result, markdown, cancellationToken);

        if (options.Post && pullRequest is not null)
        {
            var url = await new ReviewPublisher(gitHub).PublishAsync(RequireRepository(), options.PullRequest!.Value, pullRequest.HeadSha, diff, result, cancellationToken);
            Console.Error.WriteLine($"Revisão publicada: {url}");
        }

        if (options.FailOn is { } threshold && result.Report.Findings.Any(f => f.Severity >= threshold))
        {
            Console.Error.WriteLine($"Há problemas com severidade {threshold} ou maior.");
            return FindingsAboveThreshold;
        }

        return Success;
    }

    private async Task WriteOutputsAsync(CrewResult result, string markdown, CancellationToken cancellationToken)
    {
        if (options.OutputFile is not null)
        {
            await File.WriteAllTextAsync(options.OutputFile.FullName, markdown, cancellationToken);
        }

        if (options.SarifFile is not null)
        {
            await File.WriteAllTextAsync(options.SarifFile.FullName, SarifRenderer.Render(result.Report), cancellationToken);
        }

        // Shows the review on the workflow run page when running inside GitHub Actions.
        if (Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is { Length: > 0 } summaryPath)
        {
            await File.AppendAllTextAsync(summaryPath, markdown, cancellationToken);
        }
    }

    private CrewConfig LoadConfig()
    {
        var path = options.ConfigFile?.FullName
            ?? (File.Exists(CrewConfig.DefaultFileName) ? CrewConfig.DefaultFileName : null);

        if (path is null)
        {
            return CrewConfig.Default;
        }

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Arquivo de configuração não encontrado: {path}");
        }

        Console.Error.WriteLine($"Usando a configuração de {path}");
        return CrewConfig.Load(path);
    }

    private string RequireRepository() =>
        options.Repository ?? throw new ArgumentException("Informe o repositório com --repo dono/nome ou pela variável GITHUB_REPOSITORY.");

    private static Uri GitHubApiUrl()
    {
        var url = Environment.GetEnvironmentVariable("GITHUB_API_URL") ?? "https://api.github.com";
        return new Uri(url.EndsWith('/') ? url : url + "/");
    }
}
