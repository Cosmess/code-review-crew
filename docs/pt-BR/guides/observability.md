# Observabilidade com OpenTelemetry e .NET Aspire

[English](../../en/guides/observability.md) | **Português** | [简体中文](../../zh-CN/guides/observability.md)

## Rodando com o dashboard do Aspire

```bash
dotnet run --project src/CodeReviewCrew.AppHost
```

1. O dashboard abre no navegador. Na primeira execução ele pede o parâmetro `openai-api-key`.
   Para não digitar sempre:
   ```bash
   dotnet user-secrets set Parameters:openai-api-key sk-... --project src/CodeReviewCrew.AppHost
   ```
2. O recurso `code-review-crew` revisa `samples/vulnerable.diff` e termina.
3. Em **Traces**, abra o trace `code_review`.

Para revisar outro diff ou trocar o modelo:

```bash
dotnet run --project src/CodeReviewCrew.AppHost -- --Review:Diff=/caminho/do/arquivo.diff --Review:Model=gpt-5
```

## O que aparece

```
code_review                              veredito, problemas, tokens
├── invoke_agent SecurityAgent           problemas encontrados
│   └── chat gpt-5-mini                  tokens da chamada
├── invoke_agent OwaspAgent
│   ├── chat gpt-5-mini                  pede a ferramenta read_file
│   └── chat gpt-5-mini                  responde com o arquivo lido
├── invoke_agent PerformanceAgent        evento "retry" se uma tentativa falhou
├── invoke_agent StyleAgent
└── invoke_agent SynthesizerAgent
```

Os quatro especialistas aparecem em paralelo na linha do tempo; o sintetizador começa quando todos terminam.
Agentes que falharam ficam com status de erro e a exceção registrada.

Em **Metrics**, o meter `CodeReviewCrew` traz `crc.tokens.input`, `crc.tokens.output`, `crc.findings`
(por agente e severidade), `crc.agent.failures` e `crc.agent.duration`.

Prompts e código **não** são registrados nos spans.

## Usando outro coletor

A CLI exporta via OTLP sempre que `OTEL_EXPORTER_OTLP_ENDPOINT` está definida, então funciona com Jaeger, Grafana,
Honeycomb ou qualquer coletor OpenTelemetry:

```bash
docker run -d -p 16686:16686 -p 4317:4317 jaegertracing/all-in-one
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 \
  dotnet run --project src/CodeReviewCrew.Cli -- samples/vulnerable.diff
```
