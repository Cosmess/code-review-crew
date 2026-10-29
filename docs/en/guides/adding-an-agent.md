# Adding a specialist agent

**English** | [Português](../../pt-BR/guides/adding-an-agent.md) | [简体中文](../../zh-CN/guides/adding-an-agent.md)

Example: an accessibility agent for front-end code.

## 1. Define the agent in `src/CodeReviewCrew.Core/Agents/Crew.cs`

```csharp
public static readonly AgentDefinition Accessibility = new(
    "accessibility",                    // key used in .crc.yml
    "AccessibilityAgent",               // name shown in the review
    "Checks accessibility problems.",
    $"""
    You are an accessibility specialist (WCAG 2.2) reviewing a pull request.
    Look for: images without alternative text, ...
    {SharedRules}
    """);
```

- Write the instructions in English, listing concretely what to look for. The review language is added automatically.
- End with `{SharedRules}`.
- Say which severity to use if the agent tends to exaggerate.

## 2. Register it in the crew

Add the agent to `Crew.Specialists`. It then runs by default and can be selected in `.crc.yml` by its key.

## 3. Update the tests

- `ReviewOrchestratorTests` counts specialists and prompts (`4` specialists, `5` prompts with the synthesizer).
- `CrewConfigTests` counts the specialists in the empty configuration test.
- If the agent has specific behavior, add a test with `FakeChatClient` answering it.

## 4. Update the documentation

- The agent table and diagram in the three READMEs.
- The `agents` values in `.crc.example.yml`.
- The diagram in the three versions of the architecture document.

## 5. Measure the impact

Add a case to `evals/cases/` with the kind of issue the agent must find (see [`evals/README.md`](../../../evals/README.md))
and run the evaluation before and after the change. The new agent must not reduce the precision of the other cases.

```bash
dotnet run --project src/CodeReviewCrew.Evals
```

## 6. Validate and commit

```bash
dotnet test
git commit -m "feat(agentes): adiciona agente de acessibilidade"
```
