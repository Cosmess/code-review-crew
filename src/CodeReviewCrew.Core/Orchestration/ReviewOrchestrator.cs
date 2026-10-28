using System.Text;
using System.Text.Json;
using CodeReviewCrew.Core.Agents;
using CodeReviewCrew.Core.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CodeReviewCrew.Core.Orchestration;

/// <summary>
/// Runs the specialist agents concurrently over a diff (fan-out) and asks the
/// synthesizer agent to consolidate their findings into a single review (fan-in).
/// </summary>
public sealed class ReviewOrchestrator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly IReadOnlyList<(AgentDefinition Definition, AIAgent Agent)> _specialists;
    private readonly AIAgent _synthesizer;

    public ReviewOrchestrator(IChatClient chatClient, IReadOnlyList<AgentDefinition>? specialists = null)
    {
        ArgumentNullException.ThrowIfNull(chatClient);

        _specialists = (specialists ?? Crew.Specialists)
            .Select(definition => (definition, CreateAgent(chatClient, definition)))
            .ToList();
        _synthesizer = CreateAgent(chatClient, Crew.Synthesizer);
    }

    public event Action<string>? AgentStarted;
    public event Action<SpecialistReport>? AgentCompleted;

    public async Task<CrewResult> ReviewAsync(string diff, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(diff))
        {
            throw new ArgumentException("The diff is empty.", nameof(diff));
        }

        var reports = await Task.WhenAll(_specialists.Select(s => RunSpecialistAsync(s.Definition, s.Agent, diff, cancellationToken)));

        AgentStarted?.Invoke(Crew.Synthesizer.Name);
        var response = await _synthesizer.RunAsync<ReviewReport>(
            BuildSynthesizerPrompt(diff, reports),
            serializerOptions: JsonOptions,
            cancellationToken: cancellationToken);

        return new CrewResult(reports, response.Result);
    }

    private async Task<SpecialistReport> RunSpecialistAsync(AgentDefinition definition, AIAgent agent, string diff, CancellationToken cancellationToken)
    {
        AgentStarted?.Invoke(definition.Name);

        var response = await agent.RunAsync<SpecialistFindings>(
            $"Review this pull request diff:\n\n```diff\n{diff}\n```",
            serializerOptions: JsonOptions,
            cancellationToken: cancellationToken);

        var report = new SpecialistReport(definition.Name, response.Result.Findings ?? []);
        AgentCompleted?.Invoke(report);
        return report;
    }

    private static string BuildSynthesizerPrompt(string diff, IEnumerable<SpecialistReport> reports)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Pull request diff:").AppendLine("```diff").AppendLine(diff).AppendLine("```").AppendLine();
        prompt.AppendLine("Specialist findings:");
        foreach (var report in reports)
        {
            prompt.AppendLine($"## {report.Agent}").AppendLine(JsonSerializer.Serialize(report.Findings, JsonOptions));
        }

        return prompt.ToString();
    }

    private static AIAgent CreateAgent(IChatClient chatClient, AgentDefinition definition) =>
        new ChatClientAgent(chatClient, instructions: definition.Instructions, name: definition.Name, description: definition.Description);
}
