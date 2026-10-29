using System.CommandLine;
using CodeReviewCrew.Core.Configuration;
using CodeReviewCrew.Core.Localization;
using CodeReviewCrew.Core.Models;
using CodeReviewCrew.Core.Orchestration;
using CodeReviewCrew.Evals;
using CodeReviewCrew.Providers;

// Runs the crew over every labeled case and reports precision, recall and verdict accuracy.
var casesOption = new Option<DirectoryInfo>("--cases")
{
    Description = "Folder with the labeled cases.",
    DefaultValueFactory = _ => new DirectoryInfo(Path.Combine("evals", "cases"))
};
var filterOption = new Option<string?>("--filter") { Description = "Only runs the cases whose name contains this text." };
var outputOption = new Option<FileInfo?>("--output") { Description = "Saves the Markdown report to this file." };
var minRecallOption = new Option<double?>("--min-recall") { Description = "Exits with code 2 if the recall is below this value (0 to 1)." };
var languageOption = new Option<string>("--language")
{
    Description = "Language of the reviews: en, pt-BR or zh-CN.",
    DefaultValueFactory = _ => ReviewLanguage.English.Code
};

var root = new RootCommand("Evaluates the Code Review Crew on labeled diffs.") { casesOption, filterOption, outputOption, minRecallOption, languageOption };
root.SetAction(async (parseResult, cancellationToken) =>
{
    var directory = parseResult.GetValue(casesOption)!;
    if (!directory.Exists)
    {
        Console.Error.WriteLine($"error: cases folder not found: {directory.FullName}");
        return 1;
    }

    var filter = parseResult.GetValue(filterOption);
    var cases = EvalCase.LoadAll(directory.FullName)
        .Where(c => filter is null || c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
        .ToList();

    ReviewOrchestrator orchestrator;
    ReviewLanguage language;
    try
    {
        language = ReviewLanguage.Parse(parseResult.GetValue(languageOption));
        orchestrator = new ReviewOrchestrator(ChatClientFactory.Create(), new CrewConfig { Language = language.Code });
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine($"error: {ex.Message}");
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
        Console.Error.WriteLine($"{score.TruePositives} found, {score.FalseNegatives} missed, {score.FalsePositives} false positive(s)");
    }

    var summary = new EvalSummary(scores);
    var (provider, modelName) = ChatClientFactory.Selection;
    var report = EvalReportRenderer.Render(summary, $"{provider}/{modelName}", language.Code, new UsageSummary(inputTokens, outputTokens, cost));
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
