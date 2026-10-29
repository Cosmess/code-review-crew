# 测试

[English](../en/testing.md) | [Português](../pt-BR/testing.md) | **简体中文**

## 项目

| 项目 | 覆盖范围 | 网络/LLM |
|---|---|---|
| `tests/CodeReviewCrew.Tests` | Core 的全部逻辑：解析器、配置、编排、重试、多语言、渲染器、GitHub、MCP | 从不 |
| `tests/CodeReviewCrew.IntegrationTests` | 使用真实 OpenAI API 的端到端审查 | 是，仅在设置 `OPENAI_API_KEY` 时 |
| `src/CodeReviewCrew.Evals`（不是测试） | 审查质量：在 `evals/cases` 上的精确率、召回率和 F1 | 是，按需运行 |

在 CI 中，单元测试会在每次 push 和 Pull Request 时运行。集成测试只在推送到 `main` 时运行，因为它们会产生费用。

单元测试还覆盖：

- **真实的 MCP 协议**（`McpServerProtocolTests`）：启动服务器进程并通过 stdio 调用其工具，
  包括尝试读取 `.env` 和访问根目录之外的内容。
- **`RepositoryReader` 的安全性**：路径穿越、绝对路径、符号链接、`.git` 和密钥类文件。
- **遥测**（`TelemetryTests`）：span 和事件；由于其他测试类并行运行，测试通过一个测试 span 进行隔离。
- **多语言**：语言指令会传递给每个智能体，Markdown 标签会被翻译。
- **数据集一致性**：每个 `expected.json` 都指向其 diff 中存在的文件和行。

## 测试替身

- **`FakeChatClient`** 实现了 `IChatClient`。它接收一个 `提示词 → 响应` 的函数（提示词包含智能体的指令，
  因此可以按智能体返回不同的响应），将提示词记录在 `Prompts` 中，并且每次调用报告 1000 个输入 token 和
  100 个输出 token。在函数中抛出异常即可模拟失败；使用异步构造函数和 `Task.Delay(..., token)` 可模拟慢响应。
- **`FakeHttpHandler`** 是一个 `HttpMessageHandler`，它记录每个请求（`Requests`）并用给定的函数返回响应。
  配合 `new HttpClient(handler) { BaseAddress = ... }` 使用。

## 约定

- 测试名称描述行为，使用英文并以 `_` 分隔：`Retries_a_transient_failure`。
- Arrange / Act / Assert 之间用空行分隔。
- 在编排器测试中，使用 `retryBackoff: _ => TimeSpan.Zero` 创建编排器，以跳过真实的退避等待。
- 测试可观察的行为（输出、发出的请求、事件），而不是内部细节。
- 每个修复的缺陷都要有一个在修复前会失败的测试。

## 运行

```bash
dotnet test tests/CodeReviewCrew.Tests                                  # 单元测试
OPENAI_API_KEY=sk-... dotnet test tests/CodeReviewCrew.IntegrationTests  # 集成测试
```
