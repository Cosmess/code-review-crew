# Code Review Crew

[![CI](https://github.com/Cosmess/code-review-crew/actions/workflows/ci.yml/badge.svg)](https://github.com/Cosmess/code-review-crew/actions/workflows/ci.yml)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![License: MIT](https://img.shields.io/badge/license-MIT-green)

A **multi-agent code reviewer** built with .NET 10 and the
[Microsoft Agent Framework](https://github.com/microsoft/agent-framework).
Specialist AI agents review a pull request diff in parallel, and a lead agent consolidates
their findings into a single, deduplicated review, ready to post as a PR comment.

## How it works

```mermaid
flowchart LR
    D[PR diff] --> S1[🔐 AgenteSeguranca]
    D --> S4[🛡️ AgenteOwasp]
    D --> S2[⚡ AgentePerformance]
    D --> S3[🧹 AgenteEstilo]
    S1 --> Y[🧠 AgenteSintetizador]
    S2 --> Y
    S4 --> Y
    S3 --> Y
    D --> Y
    Y --> R[Markdown review<br/>verdict + findings]
```

| Agent | Responsibility |
|---|---|
| **AgenteSeguranca** | Injection, hardcoded secrets, auth flaws, sensitive data exposure |
| **AgenteOwasp** | Audits the diff against each OWASP Top 10:2025 category (A01–A10) and tags findings with the category |
| **AgentePerformance** | N+1 queries, sync-over-async, allocations, missing cancellation |
| **AgenteEstilo** | Naming, swallowed exceptions, dead code, maintainability |
| **AgenteSintetizador** | Merges duplicates, discards false positives, ranks by severity, decides the verdict |

Design highlights:

- **OWASP-aware**: a dedicated agent maps every vulnerability to its OWASP Top 10:2025 category and suggests the OWASP-recommended mitigation.

- **Fan-out / fan-in orchestration**: specialists run concurrently; the synthesizer runs once all are done.
- **Structured output**: every agent returns typed JSON (`ReviewFinding`, `ReviewReport`) instead of free text.
- **OpenAI by default, provider-agnostic by design**: built on `Microsoft.Extensions.AI.IChatClient`, so it can also run on Ollama (local) or Anthropic Claude.
- **Reviews in Brazilian Portuguese**: agent roles and instructions are written in Portuguese, and so is the generated review.
- **Deterministic tests**: the orchestration is tested with a fake `IChatClient`, without calling any LLM.

## Getting started

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download) and an [OpenAI API key](https://platform.openai.com/api-keys).

```bash
git clone https://github.com/Cosmess/code-review-crew.git
cd code-review-crew

# OpenAI (default)
export OPENAI_API_KEY=sk-...
dotnet run --project src/CodeReviewCrew.Cli -- samples/vulnerable.diff

# Alternative: local model with Ollama (no API key)
CRC_PROVIDER=ollama dotnet run --project src/CodeReviewCrew.Cli -- samples/vulnerable.diff

# Alternative: Anthropic Claude
CRC_PROVIDER=anthropic ANTHROPIC_API_KEY=sk-ant-... \
  dotnet run --project src/CodeReviewCrew.Cli -- samples/vulnerable.diff
```

You can also pipe any diff into it:

```bash
git diff main | dotnet run --project src/CodeReviewCrew.Cli
```

### Configuration

| Variable | Default | Description |
|---|---|---|
| `CRC_PROVIDER` | `openai` | `openai`, `ollama` or `anthropic` |
| `CRC_MODEL` | `gpt-5-mini` (OpenAI) | Model id (e.g. `gpt-5-mini`, `qwen3:8b`, `claude-sonnet-5-5`) |
| `OLLAMA_ENDPOINT` | `http://localhost:11434` | Ollama server URL |
| `OPENAI_API_KEY` | | OpenAI API key (required for the default provider) |
| `ANTHROPIC_API_KEY` | | Anthropic API key, when `CRC_PROVIDER=anthropic` |

## Project structure

```
src/
  CodeReviewCrew.Core/     agents, orchestration, models, Markdown rendering
  CodeReviewCrew.Cli/      command-line entry point and LLM provider selection
tests/
  CodeReviewCrew.Tests/    xUnit tests with a fake chat client
samples/
  vulnerable.diff          example PR with security, performance and style issues
```

## Running the tests

```bash
dotnet test
```

## Roadmap

- [ ] GitHub integration: fetch a PR by number and post the review as a comment
- [ ] GitHub Action to run the crew on every pull request
- [ ] .NET Aspire AppHost with OpenTelemetry traces of each agent
- [ ] MCP tools so agents can read full files and repository conventions
- [ ] Evaluation suite measuring precision/recall on labeled diffs

## License

[MIT](LICENSE)
