# ADR 0001: Microsoft Agent Framework and Microsoft.Extensions.AI

**Status:** accepted

## Context

The project needs agents with their own instructions, structured output and freedom to switch LLM providers.

## Decision

Use the **Microsoft Agent Framework** (`ChatClientAgent`, `RunAsync<T>`) on top of the `IChatClient` abstraction
from **Microsoft.Extensions.AI**. Core only knows `IChatClient`; `CodeReviewCrew.Providers` creates the provider client.

## Consequences

- Switching providers (OpenAI, Anthropic, Ollama) does not touch Core.
- Tests use a fake `IChatClient`, without network access.
- `RunAsync<T>` generates the JSON schema from the C# types, removing manual parsing.
- We are coupled to the evolution of the Agent Framework API; versions are pinned in the `.csproj` files.
