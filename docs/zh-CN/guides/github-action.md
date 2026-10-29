# 使用 GitHub Action

[English](../../en/guides/github-action.md) | [Português](../../pt-BR/guides/github-action.md) | **简体中文**

## 最小配置

1. 在仓库中添加 `OPENAI_API_KEY` secret（*Settings → Secrets and variables → Actions*）。
2. 创建 `.github/workflows/code-review.yml`：

```yaml
name: Code Review Crew

on:
  pull_request:
    types: [opened, synchronize, reopened, ready_for_review]

permissions:
  contents: read
  pull-requests: write

jobs:
  review:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: Cosmess/code-review-crew@v1
        with:
          openai-api-key: ${{ secrets.OPENAI_API_KEY }}
          language: zh-CN
```

需要 `actions/checkout`，这样 Action 才能找到你的 `.crc.yml`，智能体也才能读取仓库内容。

## 输入

| 输入 | 默认值 | 说明 |
|---|---|---|
| `openai-api-key` | 必填 | OpenAI API 密钥 |
| `github-token` | `github.token` | 用于读取 PR 并发布审查的令牌 |
| `model` | `gpt-5-mini` | OpenAI 模型 |
| `language` | 空（使用 `.crc.yml`，否则为 `en`） | 审查语言：`en`、`pt-BR` 或 `zh-CN` |
| `config` | `.crc.yml` | 配置文件（不存在时忽略） |
| `post-review` | `true` | 发布带行内评论的审查 |
| `read-repository` | `true` | 为智能体提供只读的仓库 MCP 工具 |
| `sarif-file` | 空 | 保存 SARIF 文件的路径 |
| `fail-on` | 空 | 存在该严重程度及以上的问题时使任务失败 |

## 在 Security 标签页中显示告警（SARIF）

```yaml
permissions:
  contents: read
  pull-requests: write
  security-events: write

    steps:
      - uses: actions/checkout@v4
      - uses: Cosmess/code-review-crew@v1
        with:
          openai-api-key: ${{ secrets.OPENAI_API_KEY }}
          sarif-file: code-review-crew.sarif
      - uses: github/codeql-action/upload-sarif@v4
        if: always() && hashFiles('code-review-crew.sarif') != ''
        with:
          sarif_file: code-review-crew.sarif
          category: code-review-crew
```

## 阻止合并

设置 `fail-on: High` 后，只要存在 High 或 Critical 级别的问题，任务就会失败。在分支保护规则中将该任务设为必需，
即可阻止合并。审查会在任务失败之前发布。

## 注意事项

- 审查以 `COMMENT` 形式发布，使用默认的 `GITHUB_TOKEN` 即可，并且本身不会阻止 PR。
- 来自 fork 的 Pull Request 无法获取 secret；在这种情况下应跳过审查，可参考本仓库的
  [工作流](../../../.github/workflows/code-review.yml)。
- 请固定版本（`@v1`、某个 tag 或提交 SHA），不要使用 `@main`。
- Action 每次运行都会从源码构建工具，大约需要一分钟。
