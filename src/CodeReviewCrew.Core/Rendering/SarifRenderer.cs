using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CodeReviewCrew.Core.Models;

namespace CodeReviewCrew.Core.Rendering;

/// <summary>
/// Renders a review as SARIF 2.1.0, the format GitHub code scanning uses to show
/// findings in the Security tab and as annotations on the pull request.
/// </summary>
public static partial class SarifRenderer
{
    public const string ToolName = "Code Review Crew";
    private const string GenericRuleId = "CRC0001";

    public static string Render(ReviewReport report)
    {
        var rules = new Dictionary<string, (string Name, Severity MaxSeverity)>();
        var results = new JsonArray();

        foreach (var finding in report.Findings)
        {
            var (ruleId, ruleName) = RuleFor(finding);
            rules[ruleId] = rules.TryGetValue(ruleId, out var rule) && rule.MaxSeverity > finding.Severity
                ? rule
                : (ruleName, finding.Severity);

            var physicalLocation = new JsonObject { ["artifactLocation"] = new JsonObject { ["uri"] = finding.File } };
            if (finding.Line is > 0 and var line)
            {
                physicalLocation["region"] = new JsonObject { ["startLine"] = line };
            }

            var message = string.IsNullOrWhiteSpace(finding.Suggestion)
                ? $"{finding.Title}: {finding.Explanation}"
                : $"{finding.Title}: {finding.Explanation} Sugestão: {finding.Suggestion}";

            results.Add(new JsonObject
            {
                ["ruleId"] = ruleId,
                ["level"] = Level(finding.Severity),
                ["message"] = new JsonObject { ["text"] = message },
                ["locations"] = new JsonArray(new JsonObject { ["physicalLocation"] = physicalLocation })
            });
        }

        var sarif = new JsonObject
        {
            ["$schema"] = "https://json.schemastore.org/sarif-2.1.0.json",
            ["version"] = "2.1.0",
            ["runs"] = new JsonArray(new JsonObject
            {
                ["tool"] = new JsonObject
                {
                    ["driver"] = new JsonObject
                    {
                        ["name"] = ToolName,
                        ["informationUri"] = "https://github.com/Cosmess/code-review-crew",
                        ["rules"] = new JsonArray(rules.Select(r => (JsonNode)RuleNode(r.Key, r.Value.Name, r.Value.MaxSeverity)).ToArray())
                    }
                },
                ["results"] = results
            })
        };

        return sarif.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static (string Id, string Name) RuleFor(ReviewFinding finding) =>
        OwaspTag().Match(finding.Title) is { Success: true } match
            ? ($"OWASP-{match.Groups["id"].Value}", $"OWASP {match.Groups["id"].Value}: {match.Groups["name"].Value.Trim()}")
            : (GenericRuleId, "Problema encontrado na revisão de código");

    private static JsonObject RuleNode(string id, string name, Severity severity)
    {
        var rule = new JsonObject
        {
            ["id"] = id,
            ["name"] = id.Replace("-", ""),
            ["shortDescription"] = new JsonObject { ["text"] = name },
            ["defaultConfiguration"] = new JsonObject { ["level"] = Level(severity) },
            ["properties"] = new JsonObject
            {
                // Read by GitHub code scanning to rank security alerts.
                ["security-severity"] = SecuritySeverity(severity).ToString("0.0", CultureInfo.InvariantCulture),
                ["tags"] = new JsonArray("security", "code-review")
            }
        };

        if (id.StartsWith("OWASP-", StringComparison.Ordinal))
        {
            rule["helpUri"] = "https://owasp.org/Top10/";
        }

        return rule;
    }

    private static string Level(Severity severity) => severity switch
    {
        >= Severity.High => "error",
        Severity.Medium => "warning",
        _ => "note"
    };

    private static double SecuritySeverity(Severity severity) => severity switch
    {
        Severity.Critical => 9.5,
        Severity.High => 8.0,
        Severity.Medium => 5.5,
        Severity.Low => 3.0,
        _ => 1.0
    };

    [GeneratedRegex(@"\[(?<id>A\d{2}):\d{4}\s*(?<name>[^\]]*)\]")]
    private static partial Regex OwaspTag();
}
