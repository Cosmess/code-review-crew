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
    Console.Error.WriteLine("Uso: crc <arquivo-diff>  |  git diff | crc");
    return 1;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var orchestrator = new ReviewOrchestrator(ChatClientFactory.Create());
orchestrator.AgentStarted += name => Console.Error.WriteLine($"▶ {name} iniciou");
orchestrator.AgentCompleted += report => Console.Error.WriteLine($"✔ {report.Agent} terminou com {report.Findings.Count} problema(s)");

var result = await orchestrator.ReviewAsync(diff, cts.Token);
Console.WriteLine(MarkdownRenderer.Render(result));
return 0;
