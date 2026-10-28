using CodeReviewCrew.Cli;
using CodeReviewCrew.Core.Orchestration;
using CodeReviewCrew.Core.Rendering;

// Usage: crc <diff-file>   or   git diff | crc
string diff;
if (args.Length > 0)
{
    diff = await File.ReadAllTextAsync(args[0]);
}
else if (Console.IsInputRedirected)
{
    diff = await Console.In.ReadToEndAsync();
}
else
{
    Console.Error.WriteLine("Usage: crc <diff-file>  |  git diff | crc");
    return 1;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var orchestrator = new ReviewOrchestrator(ChatClientFactory.Create());
orchestrator.AgentStarted += name => Console.Error.WriteLine($"▶ {name} started");
orchestrator.AgentCompleted += report => Console.Error.WriteLine($"✔ {report.Agent} finished with {report.Findings.Count} finding(s)");

var result = await orchestrator.ReviewAsync(diff, cts.Token);
Console.WriteLine(MarkdownRenderer.Render(result));
return 0;
