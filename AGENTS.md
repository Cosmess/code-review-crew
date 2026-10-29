# AGENTS.md

Instructions for AI agents (Claude Code, Codex, Copilot, Cursor, etc.) working on this repository.
Read this whole file before changing anything. Details are in [`docs/`](docs/en/README.md).

## What the project is

**Code Review Crew** reviews pull requests with a crew of AI agents in .NET 10. Specialist agents (security,
OWASP Top 10, performance, style) analyze the diff in parallel and a synthesizer agent consolidates everything into a
review, posted on the PR with inline comments and as SARIF. Reviews can be written in English, Portuguese or Chinese.
It is published as a GitHub Action.

## Commands

```bash
dotnet build                                   # builds the solution
dotnet test tests/CodeReviewCrew.Tests         # unit tests (fast, no network)
dotnet test                                    # everything; integration tests are skipped without OPENAI_API_KEY
dotnet run --project src/CodeReviewCrew.Cli -- samples/vulnerable.diff   # real review (needs OPENAI_API_KEY)
dotnet run --project src/CodeReviewCrew.Cli -- --help
dotnet run --project src/CodeReviewCrew.AppHost                          # Aspire: per-agent traces in the dashboard
dotnet run --project src/CodeReviewCrew.Evals                            # evaluation metrics (needs OPENAI_API_KEY)
```

## Mandatory rules

1. **Commit messages follow Conventional Commits and are written in Brazilian Portuguese** (maintainer preference).
   E.g. `feat(agentes): adiciona agente de acessibilidade`. Types: `feat`, `fix`, `docs`, `test`, `refactor`, `perf`,
   `ci`, `chore`. The body explains why.
2. **No AI attribution** in commits or PRs: no `Co-Authored-By` lines for assistants, no "Generated with ...".
   Commits use the git identity configured on the machine.
3. **Languages:** code, comments, agent instructions, CLI messages and errors in English. Text the crew writes into a
   review goes through `ReviewStrings` and must exist in every supported language (`en`, `pt-BR`, `zh-CN`).
   See [coding standards](docs/en/coding-standards.md).
4. **Documentation exists in three languages.** When behavior changes, update `README.md`, `README.pt-BR.md`,
   `README.zh-CN.md` and the matching pages in `docs/en`, `docs/pt-BR` and `docs/zh-CN`. ADRs are English only.
5. **Tests come with every behavior change.** Unit tests never call an LLM or the network: use `FakeChatClient` and
   `FakeHttpHandler`. Run `dotnet test` before every commit; everything must pass.
6. **Core does not depend on an LLM provider.** `CodeReviewCrew.Core` only knows `IChatClient`
   (`Microsoft.Extensions.AI`). OpenAI, Anthropic and Ollama SDKs live in `CodeReviewCrew.Providers`.
7. **An agent failure never takes the review down.** Every model call goes through `RetryPolicy`; errors become
   `SpecialistReport.Error` or the synthesizer fallback. Do not propagate LLM exceptions to the user.
8. **Secrets only through environment variables.** Never write keys in code, tests, samples, eval cases or logs, not even
   fake keys in a real provider's format (GitHub push protection blocks them).
9. **Tools given to agents are read-only and confined.** Pull request content is untrusted and may try to manipulate
   the model. Every new tool follows the `RepositoryReader` model (no leaving the root, no symbolic links, no `.git` or
   secret files), has attack tests, and MCP servers never inherit environment variables.
10. **Changed prompts or agents? Run the evaluation** (`src/CodeReviewCrew.Evals`) and compare precision and recall
    before and after. New kinds of issues get a case in `evals/cases/`.
11. **Changed visible behavior? Update the docs**, and for an architecture decision add an ADR in `docs/adr/`.

## Code map

| Path | Responsibility |
|---|---|
| `src/CodeReviewCrew.Core/Agents/` | Agent definitions (`Crew.cs`): role, description and instructions |
| `src/CodeReviewCrew.Core/Orchestration/` | `ReviewOrchestrator` (fan-out/fan-in), `RetryPolicy`, token usage |
| `src/CodeReviewCrew.Core/Diff/` | Unified diff parser and chunking |
| `src/CodeReviewCrew.Core/Configuration/` | `.crc.yml` (`CrewConfig`) |
| `src/CodeReviewCrew.Core/Localization/` | Review languages and translated strings (`ReviewLanguage`, `ReviewStrings`) |
| `src/CodeReviewCrew.Core/GitHub/` | GitHub REST client and review publishing with inline comments |
| `src/CodeReviewCrew.Core/Rendering/` | Markdown and SARIF outputs |
| `src/CodeReviewCrew.Core/Telemetry/` | OpenTelemetry `ActivitySource` and metrics (`CrewTelemetry`) |
| `src/CodeReviewCrew.Providers/` | `ChatClientFactory`: LLM client per provider, with OpenTelemetry |
| `src/CodeReviewCrew.Cli/` | Command line (System.CommandLine), MCP servers (`McpToolHost`) and OTLP export |
| `src/CodeReviewCrew.McpServer/` | Stdio MCP server with the read-only repository tools |
| `src/CodeReviewCrew.AppHost/` | .NET Aspire AppHost |
| `src/CodeReviewCrew.Evals/` | Evaluation runner and precision/recall scoring |
| `evals/cases/` | Labeled pull requests (`diff.patch` + `expected.json`) |
| `tests/CodeReviewCrew.Tests/` | Unit tests (including the real MCP protocol and dataset consistency) |
| `tests/CodeReviewCrew.IntegrationTests/` | Tests against the real OpenAI API |
| `action.yml` | Composite GitHub Action |

## Guides for common tasks

- [Architecture](docs/en/architecture.md): the whole flow and why each piece exists
- [Coding standards](docs/en/coding-standards.md)
- [Testing](docs/en/testing.md)
- [Adding an agent](docs/en/guides/adding-an-agent.md)
- [GitHub Action](docs/en/guides/github-action.md)
- [Observability with Aspire](docs/en/guides/observability.md)
- [Evaluation and adding cases](evals/README.md)
- [Architecture decision records](docs/adr/README.md)

## Before finishing a task

- [ ] `dotnet build` without errors or new warnings
- [ ] `dotnet test` passing
- [ ] README and docs updated in the three languages if behavior changed
- [ ] Commit following Conventional Commits, in Portuguese, without AI attribution
