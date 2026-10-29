using System.Globalization;

namespace CodeReviewCrew.Core.Localization;

/// <summary>
/// A language the review can be written in. The agents receive their instructions in English
/// and are asked to write the findings in this language; the Markdown labels are translated here.
/// </summary>
public sealed record ReviewLanguage(string Code, string EnglishName, CultureInfo Culture, ReviewStrings Strings)
{
    public static readonly ReviewLanguage English = new("en", "English", CultureInfo.GetCultureInfo("en-US"), ReviewStrings.English);
    public static readonly ReviewLanguage PortugueseBrazil = new("pt-BR", "Brazilian Portuguese", CultureInfo.GetCultureInfo("pt-BR"), ReviewStrings.PortugueseBrazil);
    public static readonly ReviewLanguage ChineseSimplified = new("zh-CN", "Simplified Chinese", CultureInfo.GetCultureInfo("zh-CN"), ReviewStrings.ChineseSimplified);

    public static IReadOnlyList<ReviewLanguage> All { get; } = [English, PortugueseBrazil, ChineseSimplified];

    /// <summary>Appended to every agent's instructions.</summary>
    public string Instruction =>
        $"\nWrite the summary, titles, explanations and suggestions in {EnglishName}. "
        + "Keep code, identifiers and OWASP category names (e.g. \"[A05:2025 Injection]\") unchanged.";

    /// <summary>Accepts the exact codes and the short forms "pt" and "zh".</summary>
    public static ReviewLanguage Parse(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return English;
        }

        var normalized = code.Trim().ToLowerInvariant();
        return All.FirstOrDefault(l => l.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase) || l.Code[..2] == normalized)
            ?? throw new InvalidOperationException(
                $"Unsupported language '{code}'. Supported languages: {string.Join(", ", All.Select(l => l.Code))}.");
    }
}

/// <summary>Every piece of text the crew writes itself (the rest comes from the model).</summary>
public sealed record ReviewStrings
{
    public required string Verdict { get; init; }
    public required string Approve { get; init; }
    public required string Comment { get; init; }
    public required string RequestChanges { get; init; }
    public required string Critical { get; init; }
    public required string High { get; init; }
    public required string Medium { get; init; }
    public required string Low { get; init; }
    public required string Info { get; init; }
    public required string NoIssues { get; init; }
    public required string SeverityHeader { get; init; }
    public required string LocationHeader { get; init; }
    public required string IssueHeader { get; init; }
    public required string InlineLegend { get; init; }
    public required string Suggestion { get; init; }
    public required string FailedAgentsWarning { get; init; }
    public required string FindingsPerAgent { get; init; }
    public required string FindingsCount { get; init; }
    public required string AgentFailed { get; init; }
    public required string TokenUsage { get; init; }
    public required string EstimatedCost { get; init; }
    public required string ReviewTruncated { get; init; }
    public required string NothingToReview { get; init; }
    public required string AllAgentsFailed { get; init; }
    public required string NoFindings { get; init; }
    public required string SynthesizerFailed { get; init; }

    public static readonly ReviewStrings English = new()
    {
        Verdict = "Verdict",
        Approve = "✅ Approve",
        Comment = "💬 Comment",
        RequestChanges = "❌ Request changes",
        Critical = "🔴 Critical",
        High = "🟠 High",
        Medium = "🟡 Medium",
        Low = "🔵 Low",
        Info = "⚪ Info",
        NoIssues = "No issues found. ✅",
        SeverityHeader = "Severity",
        LocationHeader = "Location",
        IssueHeader = "Issue",
        InlineLegend = "💬 = details posted as a comment on the line of code.",
        Suggestion = "Suggestion",
        FailedAgentsWarning = "Some agents failed and the review may be incomplete:",
        FindingsPerAgent = "Findings per agent",
        FindingsCount = "{0} finding(s)",
        AgentFailed = "⚠️ failed",
        TokenUsage = "Tokens: {0} input / {1} output",
        EstimatedCost = "estimated cost: US$ {0}",
        ReviewTruncated = "[review truncated by GitHub's size limit]",
        NothingToReview = "No files left to review after applying the filters in .crc.yml.",
        AllAgentsFailed = "None of the specialist agents could finish the analysis. Check the connection to the LLM provider.",
        NoFindings = "The specialist agents found no issues.",
        SynthesizerFailed = "The synthesizer agent failed ({0}). The issues below combine the specialists' findings without consolidation."
    };

    public static readonly ReviewStrings PortugueseBrazil = new()
    {
        Verdict = "Veredito",
        Approve = "✅ Aprovar",
        Comment = "💬 Comentar",
        RequestChanges = "❌ Solicitar alterações",
        Critical = "🔴 Crítico",
        High = "🟠 Alto",
        Medium = "🟡 Médio",
        Low = "🔵 Baixo",
        Info = "⚪ Info",
        NoIssues = "Nenhum problema encontrado. ✅",
        SeverityHeader = "Severidade",
        LocationHeader = "Local",
        IssueHeader = "Problema",
        InlineLegend = "💬 = detalhes comentados na linha do código.",
        Suggestion = "Sugestão",
        FailedAgentsWarning = "Alguns agentes falharam e a revisão pode estar incompleta:",
        FindingsPerAgent = "Análises por agente",
        FindingsCount = "{0} problema(s)",
        AgentFailed = "⚠️ falhou",
        TokenUsage = "Tokens: {0} de entrada / {1} de saída",
        EstimatedCost = "custo estimado: US$ {0}",
        ReviewTruncated = "[revisão truncada pelo limite de tamanho do GitHub]",
        NothingToReview = "Nenhum arquivo para revisar depois de aplicar os filtros do .crc.yml.",
        AllAgentsFailed = "Nenhum agente especialista conseguiu concluir a análise. Verifique a conexão com o provedor de LLM.",
        NoFindings = "Nenhum problema encontrado pelos agentes especialistas.",
        SynthesizerFailed = "O agente sintetizador falhou ({0}). Os problemas abaixo reúnem as análises dos especialistas sem consolidação."
    };

    public static readonly ReviewStrings ChineseSimplified = new()
    {
        Verdict = "结论",
        Approve = "✅ 批准",
        Comment = "💬 评论",
        RequestChanges = "❌ 需要修改",
        Critical = "🔴 严重",
        High = "🟠 高",
        Medium = "🟡 中",
        Low = "🔵 低",
        Info = "⚪ 信息",
        NoIssues = "未发现问题。✅",
        SeverityHeader = "严重程度",
        LocationHeader = "位置",
        IssueHeader = "问题",
        InlineLegend = "💬 = 详细说明已作为行内评论发布在对应代码行。",
        Suggestion = "建议",
        FailedAgentsWarning = "部分智能体执行失败，审查结果可能不完整：",
        FindingsPerAgent = "各智能体的发现",
        FindingsCount = "{0} 个问题",
        AgentFailed = "⚠️ 失败",
        TokenUsage = "Token：输入 {0} / 输出 {1}",
        EstimatedCost = "预估费用：US$ {0}",
        ReviewTruncated = "[审查内容因 GitHub 的长度限制被截断]",
        NothingToReview = "应用 .crc.yml 中的过滤规则后，没有需要审查的文件。",
        AllAgentsFailed = "所有专家智能体均未能完成分析。请检查与 LLM 提供商的连接。",
        NoFindings = "专家智能体未发现任何问题。",
        SynthesizerFailed = "汇总智能体执行失败（{0}）。以下问题直接合并自各专家智能体的分析，未经汇总。"
    };
}
