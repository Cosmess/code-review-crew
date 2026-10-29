# 编码规范

[English](../en/coding-standards.md) | [Português](../pt-BR/coding-standards.md) | **简体中文**

## 语言

| 内容 | 语言 |
|---|---|
| 标识符、注释、XML 文档、智能体指令、CLI 消息、异常、工具描述 | 英文 |
| 智能体组写入审查中的文本（标签、结论、回退摘要） | 审查语言，通过 `ReviewStrings` 提供 |
| 测试名称 | 英文，描述行为（`Falls_back_to_merged_findings_when_the_synthesizer_fails`） |
| 文档 | 英文、巴西葡萄牙语和简体中文，保持同步 |
| 提交信息和 PR | Conventional Commits，使用巴西葡萄牙语（维护者偏好） |

## C#

- .NET 10，启用 `Nullable` 和 `ImplicitUsings`。不要引入警告。
- 使用文件范围命名空间（`namespace X;`），每个文件一个主要的公共类型。
- 不可变模型使用 `record`（`ReviewFinding`、`CrewResult`）；类默认声明为 `sealed`。
- 当类只保存依赖时使用主构造函数（`ReviewPublisher(GitHubClient gitHub, ...)`）。
- 使用集合表达式（`[]`、`[a, b]`）和模式匹配（`is { Length: > 0 }`、switch 表达式）。
- 全程使用 `async`；所有公共异步方法都接收 `CancellationToken cancellationToken = default`。
  切勿使用 `.Result` 或 `.Wait()`。
- 正则表达式使用 `[GeneratedRegex]`。
- 注释说明**为什么**，而不是做了什么。公共类型使用 `<summary>`。
- 使用 `ArgumentNullException.ThrowIfNull` 和 `ArgumentOutOfRangeException.ThrowIf...` 校验参数。

## 代码组织

- 新逻辑放在 Core 中对应主题的目录下（`Agents`、`Orchestration`、`Diff`、`GitHub`、`Rendering`、
  `Configuration`、`Localization`、`Telemetry`）。
- CLI 只负责组合和 I/O。如果某段代码需要测试，它很可能应该放在 Core 中。
- 没有明确需要时不要引入新的 NuGet 包；优先使用 .NET 自带的能力（例如用 `HttpClient` 代替 Octokit）。
- 包版本固定在 `.csproj` 文件中。

## 智能体提示词

- 位于 `Agents/Crew.cs`，形式为 `AgentDefinition(Key, Name, Description, Instructions)`。
- 专家智能体的指令以 `{SharedRules}` 结尾，统一规定：只关注 diff 中的内容、使用新文件的行号、不给出泛泛的建议、
  如何避免误报以及如何评定严重程度。
- 要具体：列出需要检查的内容，并给出具体的 .NET 示例。
- 测试通过指令中的片段识别智能体（例如 `"lead reviewer"`）。如果修改了这些片段，请同步更新
  `ReviewOrchestratorTests` 中的辅助方法。
- 修改提示词后，运行评估并比较精确率和召回率（参见 [AGENTS.md](../../AGENTS.md)）。
