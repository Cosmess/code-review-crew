# ADR 0006: Observability with OpenTelemetry and .NET Aspire

**Status:** accepted

## Context

In a multi-agent system it is hard to know which agent was slow, failed, retried or spent the most tokens. Loose logs
do not show the parallel structure of a review.

## Decision

- Core instruments with the native .NET APIs (`ActivitySource` and `Meter`), without depending on the OpenTelemetry SDK.
- Spans follow the GenAI semantic conventions (`invoke_agent`, `gen_ai.agent.name`, `gen_ai.usage.*`).
- Model calls are instrumented by `UseOpenTelemetry` from Microsoft.Extensions.AI, without recording prompts or code.
- Only the CLI references the OpenTelemetry SDK, exporting through OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set.
- A .NET Aspire AppHost offers the local dashboard with no configuration.

## Consequences

- Without an endpoint, telemetry has no relevant cost (no listener).
- Any OpenTelemetry backend works, not only Aspire.
- The reviewed code never leaves the process through telemetry.
