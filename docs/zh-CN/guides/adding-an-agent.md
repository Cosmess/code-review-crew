# 添加专家智能体

[English](../../en/guides/adding-an-agent.md) | [Português](../../pt-BR/guides/adding-an-agent.md) | **简体中文**

示例：一个面向前端代码的无障碍（accessibility）智能体。

## 1. 在 `src/CodeReviewCrew.Core/Agents/Crew.cs` 中定义智能体

```csharp
public static readonly AgentDefinition Accessibility = new(
    "accessibility",                    // 在 .crc.yml 中使用的键
    "AccessibilityAgent",               // 在审查中显示的名称
    "Checks accessibility problems.",
    $"""
    You are an accessibility specialist (WCAG 2.2) reviewing a pull request.
    Look for: images without alternative text, ...
    {SharedRules}
    """);
```

- 用英文编写指令，具体列出需要检查的内容。审查语言会自动追加。
- 以 `{SharedRules}` 结尾。
- 如果智能体容易夸大问题，请说明应使用的严重程度。

## 2. 将其注册到智能体组

把智能体添加到 `Crew.Specialists` 中。之后它会默认运行，也可以在 `.crc.yml` 中通过键来选择。

## 3. 更新测试

- `ReviewOrchestratorTests` 会统计专家智能体和提示词的数量（`4` 个专家智能体，加上汇总智能体共 `5` 个提示词）。
- `CrewConfigTests` 在空配置测试中统计专家智能体的数量。
- 如果该智能体有特定行为，请用 `FakeChatClient` 为它编写一个测试。

## 4. 更新文档

- 三个 README 中的智能体表格和图表。
- `.crc.example.yml` 中 `agents` 的取值。
- 三个语言版本的架构文档中的图表。

## 5. 衡量影响

在 `evals/cases/` 中添加一个包含该智能体应发现的问题类型的用例（参见
[`evals/README.zh-CN.md`](../../../evals/README.zh-CN.md)），并在修改前后分别运行评估。
新智能体不应降低其他用例的精确率。

```bash
dotnet run --project src/CodeReviewCrew.Evals
```

## 6. 验证并提交

```bash
dotnet test
git commit -m "feat(agentes): adiciona agente de acessibilidade"
```
