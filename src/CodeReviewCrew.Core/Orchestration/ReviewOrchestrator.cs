using System.Text;
using System.Text.Json;
using CodeReviewCrew.Core.Agents;
using CodeReviewCrew.Core.Configuration;
using CodeReviewCrew.Core.Diff;
using CodeReviewCrew.Core.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CodeReviewCrew.Core.Orchestration;

/// <summary>
/// Runs the specialist agents concurrently over a diff (fan-out) and asks the
/// synthesizer agent to consolidate their findings into a single review (fan-in).
/// </summary>
/// <remarks>
/// Large diffs are split into parts, every agent call has a timeout and retries,
/// and a failing agent never takes the whole review down: the specialist is reported
/// as failed, and if the synthesizer fails the raw findings are merged instead.
/// </remarks>
public sealed class ReviewOrchestrator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly CrewConfig _config;
    private readonly RetryPolicy _retry;
    private readonly IReadOnlyList<(AgentDefinition Definition, AIAgent Agent)> _specialists;
    private readonly AIAgent _synthesizer;

    public ReviewOrchestrator(IChatClient chatClient, CrewConfig? config = null, Func<int, TimeSpan>? retryBackoff = null)
    {
        ArgumentNullException.ThrowIfNull(chatClient);

        _config = config ?? CrewConfig.Default;
        _retry = new RetryPolicy(_config.MaxRetries, TimeSpan.FromSeconds(_config.AgentTimeoutSeconds), retryBackoff);
        _specialists = _config.ResolveSpecialists()
            .Select(definition => (definition, CreateAgent(chatClient, definition)))
            .ToList();
        _synthesizer = CreateAgent(chatClient, Crew.Synthesizer);
    }

    public event Action<string>? AgentStarted;
    public event Action<SpecialistReport>? AgentCompleted;
    public event Action<string, int, Exception>? AgentRetrying;

    public async Task<CrewResult> ReviewAsync(string diff, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(diff))
        {
            throw new ArgumentException("O diff está vazio.", nameof(diff));
        }

        var parsed = DiffParser.Parse(diff);
        var files = parsed.Where(f => !_config.IsIgnored(f.Path)).ToList();
        if (parsed.Count > 0 && files.Count == 0)
        {
            return new CrewResult([], new ReviewReport("Nenhum arquivo para revisar depois de aplicar os filtros do .crc.yml.", Verdict.Approve, []), UsageSummary.Empty);
        }

        // A diff without file headers is still reviewed, as a single part.
        var chunks = files.Count > 0 ? DiffChunker.Chunk(files, _config.MaxChunkChars) : [diff];
        var usage = new UsageTracker(_config.Pricing);

        var reports = await Task.WhenAll(_specialists.Select(s => RunSpecialistAsync(s.Definition, s.Agent, chunks, usage, cancellationToken)));
        var report = await SynthesizeAsync(files, chunks, reports, usage, cancellationToken);

        report = report with { Findings = report.Findings.Where(f => f.Severity >= _config.MinSeverity).ToList() };
        return new CrewResult(reports, report, usage.ToSummary());
    }

    private async Task<SpecialistReport> RunSpecialistAsync(
        AgentDefinition definition, AIAgent agent, IReadOnlyList<string> chunks, UsageTracker usage, CancellationToken cancellationToken)
    {
        AgentStarted?.Invoke(definition.Name);

        var findings = new List<ReviewFinding>();
        var errors = new List<string>();

        for (var i = 0; i < chunks.Count; i++)
        {
            var part = chunks.Count > 1 ? $" (parte {i + 1} de {chunks.Count})" : "";
            try
            {
                var response = await _retry.ExecuteAsync(
                    token => agent.RunAsync<SpecialistFindings>(
                        $"Revise o diff deste pull request{part}:\n\n```diff\n{chunks[i]}\n```",
                        serializerOptions: JsonOptions,
                        cancellationToken: token),
                    (attempt, ex) => AgentRetrying?.Invoke(definition.Name, attempt, ex),
                    cancellationToken);

                usage.Add(response.Usage);
                findings.AddRange(response.Result.Findings ?? []);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                errors.Add($"{ex.Message}{part}");
            }
        }

        var report = new SpecialistReport(definition.Name, findings, errors.Count > 0 ? string.Join("; ", errors) : null);
        AgentCompleted?.Invoke(report);
        return report;
    }

    private async Task<ReviewReport> SynthesizeAsync(
        IReadOnlyList<DiffFile> files, IReadOnlyList<string> chunks, SpecialistReport[] reports, UsageTracker usage, CancellationToken cancellationToken)
    {
        if (reports.All(r => r.Error is not null))
        {
            return new ReviewReport("Nenhum agente especialista conseguiu concluir a análise. Verifique a conexão com o provedor de LLM.", Verdict.Comment, []);
        }

        // Nothing to consolidate: skip the synthesizer call and save tokens.
        if (reports.All(r => r.Findings.Count == 0))
        {
            return new ReviewReport("Nenhum problema encontrado pelos agentes especialistas.", Verdict.Approve, []);
        }

        AgentStarted?.Invoke(Crew.Synthesizer.Name);
        try
        {
            var response = await _retry.ExecuteAsync(
                token => _synthesizer.RunAsync<ReviewReport>(
                    BuildSynthesizerPrompt(files, chunks, reports),
                    serializerOptions: JsonOptions,
                    cancellationToken: token),
                (attempt, ex) => AgentRetrying?.Invoke(Crew.Synthesizer.Name, attempt, ex),
                cancellationToken);

            usage.Add(response.Usage);
            return response.Result;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            var merged = reports
                .SelectMany(r => r.Findings)
                .DistinctBy(f => (f.File, f.Line, f.Title))
                .OrderByDescending(f => f.Severity)
                .ToList();

            return new ReviewReport(
                $"O agente sintetizador falhou ({ex.Message}). Os problemas abaixo reúnem as análises dos especialistas sem consolidação.",
                ReviewReport.VerdictFor(merged),
                merged);
        }
    }

    private static string BuildSynthesizerPrompt(IReadOnlyList<DiffFile> files, IReadOnlyList<string> chunks, IEnumerable<SpecialistReport> reports)
    {
        var prompt = new StringBuilder();
        if (chunks.Count == 1)
        {
            prompt.AppendLine("Diff do pull request:").AppendLine("```diff").AppendLine(chunks[0]).AppendLine("```").AppendLine();
        }
        else
        {
            prompt.AppendLine("O diff é grande demais para ser enviado inteiro. Arquivos alterados:");
            foreach (var file in files)
            {
                prompt.AppendLine($"- {file.Path}");
            }

            prompt.AppendLine();
        }

        prompt.AppendLine("Análises dos especialistas:");
        foreach (var report in reports.Where(r => r.Findings.Count > 0))
        {
            prompt.AppendLine($"## {report.Agent}").AppendLine(JsonSerializer.Serialize(report.Findings, JsonOptions));
        }

        return prompt.ToString();
    }

    private static AIAgent CreateAgent(IChatClient chatClient, AgentDefinition definition) =>
        new ChatClientAgent(chatClient, instructions: definition.Instructions, name: definition.Name, description: definition.Description);
}
