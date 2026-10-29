using System.CommandLine;
using CodeReviewCrew.Core.Models;
using CodeReviewCrew.Core.Orchestration;
using CodeReviewCrew.Evals;
using CodeReviewCrew.Providers;

// Runs the crew over every labeled case and reports precision, recall and verdict accuracy.
var casesOption = new Option<DirectoryInfo>("--casos")
{
    Description = "Pasta com os casos rotulados.",
    DefaultValueFactory = _ => new DirectoryInfo(Path.Combine("evals", "casos"))
};
var filterOption = new Option<string?>("--filtro") { Description = "Roda só os casos cujo nome contém este texto." };
var outputOption = new Option<FileInfo?>("--output") { Description = "Salva o relatório em Markdown neste arquivo." };
var minRecallOption = new Option<double?>("--min-recall") { Description = "Termina com código 2 se o recall ficar abaixo deste valor (0 a 1)." };

var root = new RootCommand("Avaliação do Code Review Crew em diffs rotulados.") { casesOption, filterOption, outputOption, minRecallOption };
root.SetAction(async (parseResult, cancellationToken) =>
{
    var directory = parseResult.GetValue(casesOption)!;
    if (!directory.Exists)
    {
        Console.Error.WriteLine($"erro: pasta de casos não encontrada: {directory.FullName}");
        return 1;
    }

    var filter = parseResult.GetValue(filterOption);
    var cases = EvalCase.LoadAll(directory.FullName)
        .Where(c => filter is null || c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
        .ToList();

    ReviewOrchestrator orchestrator;
    try
    {
        orchestrator = new ReviewOrchestrator(ChatClientFactory.Create());
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine($"erro: {ex.Message}");
        return 1;
    }

    var scores = new List<CaseScore>();
    long inputTokens = 0, outputTokens = 0;
    decimal? cost = null;

    // Sequential on purpose: keeps the load on the provider predictable and avoids rate limits.
    foreach (var evalCase in cases)
    {
        Console.Error.Write($"▶ {evalCase.Name}... ");
        var result = await orchestrator.ReviewAsync(evalCase.Diff, cancellationToken);
        var score = EvalScorer.Score(evalCase, result.Report);
        scores.Add(score);

        inputTokens += result.Usage.InputTokens;
        outputTokens += result.Usage.OutputTokens;
        cost = result.Usage.EstimatedCostUsd is { } c ? (cost ?? 0) + c : cost;
        Console.Error.WriteLine($"{score.TruePositives} acerto(s), {score.FalseNegatives} perdido(s), {score.FalsePositives} falso(s) positivo(s)");
    }

    var summary = new EvalSummary(scores);
    var (provider, modelName) = ChatClientFactory.Selection;
    var model = $"{provider}/{modelName}";
    var report = EvalReportRenderer.Render(summary, model, new UsageSummary(inputTokens, outputTokens, cost));
    Console.WriteLine(report);

    if (parseResult.GetValue(outputOption) is { } output)
    {
        await File.WriteAllTextAsync(output.FullName, report, cancellationToken);
    }

    if (Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is { Length: > 0 } stepSummary)
    {
        await File.AppendAllTextAsync(stepSummary, report, cancellationToken);
    }

    return parseResult.GetValue(minRecallOption) is { } minRecall && summary.Recall < minRecall ? 2 : 0;
});

return await root.Parse(args).InvokeAsync();
