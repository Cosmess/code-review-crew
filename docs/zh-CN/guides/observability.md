# 使用 OpenTelemetry 和 .NET Aspire 实现可观测性

[English](../../en/guides/observability.md) | [Português](../../pt-BR/guides/observability.md) | **简体中文**

## 使用 Aspire 仪表板运行

```bash
dotnet run --project src/CodeReviewCrew.AppHost
```

1. 仪表板会在浏览器中打开。首次运行时会要求填写 `openai-api-key` 参数。
   如果不想每次都输入：
   ```bash
   dotnet user-secrets set Parameters:openai-api-key sk-... --project src/CodeReviewCrew.AppHost
   ```
2. `code-review-crew` 资源会审查 `samples/vulnerable.diff`，然后退出。
3. 在 **Traces** 中打开 `code_review` 追踪。

如需审查其他 diff 或更换模型：

```bash
dotnet run --project src/CodeReviewCrew.AppHost -- --Review:Diff=/path/to/file.diff --Review:Model=gpt-5
```

## 你会看到什么

```
code_review                              结论、问题、token
├── invoke_agent SecurityAgent           发现的问题
│   └── chat gpt-5-mini                  本次调用的 token
├── invoke_agent OwaspAgent
│   ├── chat gpt-5-mini                  请求 read_file 工具
│   └── chat gpt-5-mini                  根据读取的文件作答
├── invoke_agent PerformanceAgent        某次尝试失败时会有 "retry" 事件
├── invoke_agent StyleAgent
└── invoke_agent SynthesizerAgent
```

四个专家智能体在时间线上并行显示；汇总智能体会在它们全部完成后开始。
失败的智能体会带有错误状态和记录下来的异常。

在 **Metrics** 中，`CodeReviewCrew` meter 提供 `crc.tokens.input`、`crc.tokens.output`、`crc.findings`
（按智能体和严重程度）、`crc.agent.failures` 和 `crc.agent.duration`。

提示词和代码**不会**被记录在 span 中。

## 使用其他收集器

只要设置了 `OTEL_EXPORTER_OTLP_ENDPOINT`，CLI 就会通过 OTLP 导出数据，因此可以配合 Jaeger、Grafana、
Honeycomb 或任何 OpenTelemetry 收集器使用：

```bash
docker run -d -p 16686:16686 -p 4317:4317 jaegertracing/all-in-one
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 \
  dotnet run --project src/CodeReviewCrew.Cli -- samples/vulnerable.diff
```
