# Coding standards

**English** | [Português](../pt-BR/coding-standards.md) | [简体中文](../zh-CN/coding-standards.md)

## Languages

| What | Language |
|---|---|
| Identifiers, comments, XML docs, agent instructions, CLI messages, exceptions, tool descriptions | English |
| Text the crew writes into a review (labels, verdicts, fallback summaries) | The review language, through `ReviewStrings` |
| Test names | English, describing the behavior (`Falls_back_to_merged_findings_when_the_synthesizer_fails`) |
| Documentation | English, Brazilian Portuguese and Simplified Chinese, kept in sync |
| Commit messages and PRs | Conventional Commits, written in Brazilian Portuguese (maintainer preference) |

## C#

- .NET 10 with `Nullable` and `ImplicitUsings` enabled. Do not introduce warnings.
- File-scoped namespaces (`namespace X;`), one main public type per file.
- `record` for immutable models (`ReviewFinding`, `CrewResult`); classes are `sealed` by default.
- Primary constructors when a class only stores dependencies (`ReviewPublisher(GitHubClient gitHub, ...)`).
- Collection expressions (`[]`, `[a, b]`) and pattern matching (`is { Length: > 0 }`, switch expressions).
- `async` all the way; every public async method takes `CancellationToken cancellationToken = default`.
  Never use `.Result` or `.Wait()`.
- Regexes with `[GeneratedRegex]`.
- Comments explain **why**, not what. Use `<summary>` on public types.
- Argument validation with `ArgumentNullException.ThrowIfNull` and `ArgumentOutOfRangeException.ThrowIf...`.

## Organization

- New logic goes to Core, in the folder of its subject (`Agents`, `Orchestration`, `Diff`, `GitHub`, `Rendering`,
  `Configuration`, `Localization`, `Telemetry`).
- The CLI only does composition and I/O. If something needs a test, it probably belongs in Core.
- No new NuGet packages without a clear need; prefer what .NET already offers (e.g. `HttpClient` instead of Octokit).
- Package versions are pinned in the `.csproj` files.

## Agent prompts

- They live in `Agents/Crew.cs`, as `AgentDefinition(Key, Name, Description, Instructions)`.
- Specialists end with `{SharedRules}`, which standardizes: only what is in the diff, new-file line numbers,
  no generic advice, how to avoid false positives and how to calibrate severity.
- Be specific: list what to look for, with concrete .NET examples.
- Tests identify agents by snippets of their instructions (e.g. `"lead reviewer"`). If you change those snippets,
  update the helpers in `ReviewOrchestratorTests`.
- After changing a prompt, run the evaluation and compare precision and recall (see [AGENTS.md](../../AGENTS.md)).
