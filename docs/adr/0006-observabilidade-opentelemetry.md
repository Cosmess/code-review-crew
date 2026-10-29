# ADR 0006: Observabilidade com OpenTelemetry e .NET Aspire

**Status:** aceita

## Contexto

Em um sistema multi-agentes é difícil saber qual agente demorou, falhou, repetiu chamadas ou gastou mais
tokens. Logs soltos não mostram a estrutura paralela da revisão.

## Decisão

- O Core instrumenta com as APIs nativas do .NET (`ActivitySource` e `Meter`), sem depender do SDK do OpenTelemetry.
- Os spans seguem as convenções semânticas de GenAI (`invoke_agent`, `gen_ai.agent.name`, `gen_ai.usage.*`).
- As chamadas ao modelo são instrumentadas pelo `UseOpenTelemetry` do Microsoft.Extensions.AI, sem registrar
  prompts nem código.
- Só a CLI referencia o SDK do OpenTelemetry e exporta via OTLP quando `OTEL_EXPORTER_OTLP_ENDPOINT` existe.
- Um AppHost do .NET Aspire oferece o dashboard local sem configuração.

## Consequências

- Sem endpoint configurado, a telemetria não tem custo relevante (nenhum listener).
- Qualquer backend OpenTelemetry funciona, não só o Aspire.
- O código revisado não sai do processo pela telemetria.
