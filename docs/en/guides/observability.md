# Observability with OpenTelemetry and .NET Aspire

**English** | [Português](../../pt-BR/guides/observability.md) | [简体中文](../../zh-CN/guides/observability.md)

## Running with the Aspire dashboard

```bash
dotnet run --project src/CodeReviewCrew.AppHost
```

1. The dashboard opens in the browser. On the first run it asks for the `openai-api-key` parameter.
   To avoid typing it every time:
   ```bash
   dotnet user-secrets set Parameters:openai-api-key sk-... --project src/CodeReviewCrew.AppHost
   ```
2. The `code-review-crew` resource reviews `samples/vulnerable.diff` and exits.
3. In **Traces**, open the `code_review` trace.

To review another diff or change the model:

```bash
dotnet run --project src/CodeReviewCrew.AppHost -- --Review:Diff=/path/to/file.diff --Review:Model=gpt-5
```

## What you see

```
code_review                              verdict, findings, tokens
├── invoke_agent SecurityAgent           findings
│   └── chat gpt-5-mini                  tokens of the call
├── invoke_agent OwaspAgent
│   ├── chat gpt-5-mini                  asks for the read_file tool
│   └── chat gpt-5-mini                  answers with the file content
├── invoke_agent PerformanceAgent        "retry" event if an attempt failed
├── invoke_agent StyleAgent
└── invoke_agent SynthesizerAgent
```

The four specialists appear in parallel on the timeline; the synthesizer starts when they all finish.
Agents that failed have an error status and the recorded exception.

Under **Metrics**, the `CodeReviewCrew` meter has `crc.tokens.input`, `crc.tokens.output`, `crc.findings`
(by agent and severity), `crc.agent.failures` and `crc.agent.duration`.

Prompts and code are **not** recorded in the spans.

## Using another collector

The CLI exports through OTLP whenever `OTEL_EXPORTER_OTLP_ENDPOINT` is set, so it works with Jaeger, Grafana,
Honeycomb or any OpenTelemetry collector:

```bash
docker run -d -p 16686:16686 -p 4317:4317 jaegertracing/all-in-one
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 \
  dotnet run --project src/CodeReviewCrew.Cli -- samples/vulnerable.diff
```
