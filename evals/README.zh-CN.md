# 评估

[English](README.md) | [Português](README.pt-BR.md) | **简体中文**

一组带标注的 Pull Request，用于衡量审查质量。每个用例都包含一个 diff，以及一次好的审查必须发现的问题。

## 运行

```bash
export OPENAI_API_KEY=sk-...
dotnet run --project src/CodeReviewCrew.Evals                              # 所有用例，英文审查
dotnet run --project src/CodeReviewCrew.Evals -- --filter sql              # 仅运行名称包含 "sql" 的用例
dotnet run --project src/CodeReviewCrew.Evals -- --language zh-CN          # 中文审查
dotnet run --project src/CodeReviewCrew.Evals -- --output report.md --min-recall 0.8
```

在 GitHub 上运行 **Evaluation** 工作流（*Actions → Evaluation → Run workflow*）。报告会显示在运行摘要中，
并作为构件（artifact）保存。

## 指标

| 指标 | 计算方式 |
|---|---|
| **召回率** | 发现的预期问题数 / 预期问题总数 |
| **精确率** | 命中数 / (命中数 + 误报数) |
| **F1** | 精确率和召回率的调和平均数 |
| **结论正确率** | 应阻止的用例给出 `RequestChanges`，不应阻止的用例给出其他结论 |
| **严重程度被低估** | 发现了问题，但严重程度低于预期最低值 |

当某个发现与预期问题位于同一文件、相距不超过 `tolerance` 行，并且提到了 `keywords` 中的某个关键词（不区分大小写
和重音）或在标题中包含 `owasp` 中的某个类别（如 `[A05:2025 ...]`）时，即视为**命中**。关键词同时提供英文、
葡萄牙文和中文，因此评估适用于所有审查语言。每个发现最多只能对应一个预期问题。

未命中任何预期问题的发现，只有在严重程度为 `Medium` 或更高时才计为**误报**。
额外的 `Low`/`Info` 级别发现（风格、命名）被视为可接受的噪声。

## 用例

| 用例 | 测试内容 |
|---|---|
| `sql-injection` | 通过字符串拼接构造 SQL |
| `hardcoded-secret` | 代码中的生产环境 API 密钥 |
| `sync-over-async` | 在控制器中使用 `.Result` |
| `n-plus-one-query` | 在循环中查询数据库 |
| `path-traversal` | 未经校验直接使用 URL 中的文件名 |
| `insecure-deserialization` | `TypeNameHandling.All` |
| `weak-password-hash` | 使用 MD5 存储密码 |
| `idor-missing-ownership-check` | 未检查资源归属就返回资源 |
| `swallowed-exception` | 空的 `catch` |
| `sql-injection-and-password-logging` | 同一文件中的两个问题 |
| `clean-parameterized-query` | 安全的改动：不应产生告警 |
| `clean-documentation` | 重命名和文档：不应产生告警 |

## 添加用例

1. 创建 `evals/cases/<用例名称>/`。
2. 使用 git 生成 `diff.patch`，以确保 hunk 头部正确：
   ```bash
   git diff main -- path/to/File.cs > evals/cases/<用例名称>/diff.patch
   ```
3. 编写 `expected.json`：
   ```json
   {
     "description": "该用例测试的内容。",
     "shouldBlock": true,
     "findings": [
       {
         "file": "src/Api/Controller.cs",
         "line": 14,
         "tolerance": 2,
         "keywords": ["traversal", "path", "caminho", "路径"],
         "owasp": ["A01"],
         "minSeverity": "High"
       }
     ]
   }
   ```
   `line` 是文件**新**版本中的行号。当且仅当某个问题的 `minSeverity` 为 `High` 或 `Critical` 时，
   `shouldBlock` 才应为 `true`。
4. 运行 `dotnet test`：`Every_labeled_case_in_the_dataset_is_consistent` 会检查文件和行号是否存在于 diff 中。
