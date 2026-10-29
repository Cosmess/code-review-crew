# Testing

**English** | [Português](../pt-BR/testing.md) | [简体中文](../zh-CN/testing.md)

## Projects

| Project | What it covers | Network/LLM |
|---|---|---|
| `tests/CodeReviewCrew.Tests` | All Core logic: parser, config, orchestration, retry, languages, renderers, GitHub, MCP | Never |
| `tests/CodeReviewCrew.IntegrationTests` | End-to-end review with the real OpenAI API | Yes, only with `OPENAI_API_KEY` |
| `src/CodeReviewCrew.Evals` (not a test) | Review quality: precision, recall and F1 on `evals/cases` | Yes, on demand |

In CI, unit tests run on every push and pull request. Integration tests only run on pushes to `main`, because they cost money.

The unit tests also cover:

- **The real MCP protocol** (`McpServerProtocolTests`): starts the server process and calls its tools over stdio,
  including attempts to read `.env` and to leave the root.
- **`RepositoryReader` security**: path traversal, absolute paths, symbolic links, `.git` and secret files.
- **Telemetry** (`TelemetryTests`): spans and events, isolated by a test span because other classes run in parallel.
- **Languages**: the language instruction reaches every agent and the Markdown labels are translated.
- **Dataset consistency**: every `expected.json` points to a file and line that exist in its diff.

## Fakes

- **`FakeChatClient`** implements `IChatClient`. It takes a `prompt → response` function (the prompt includes the
  agent's instructions, so you can answer per agent), records the prompts in `Prompts` and reports 1000 input and
  100 output tokens per call. Throw from the function to simulate failures; use the async constructor and
  `Task.Delay(..., token)` to simulate slowness.
- **`FakeHttpHandler`** is an `HttpMessageHandler` that records every request (`Requests`) and answers with the
  given function. Use it with `new HttpClient(handler) { BaseAddress = ... }`.

## Conventions

- The test name describes the behavior, in English with `_`: `Retries_a_transient_failure`.
- Arrange / act / assert separated by blank lines.
- In orchestrator tests, create it with `retryBackoff: _ => TimeSpan.Zero` to skip the real backoff.
- Test observable behavior (outputs, requests made, events), not internal details.
- Every fixed bug gets a test that would have failed before the fix.

## Running

```bash
dotnet test tests/CodeReviewCrew.Tests                                  # unit tests
OPENAI_API_KEY=sk-... dotnet test tests/CodeReviewCrew.IntegrationTests  # integration tests
```
