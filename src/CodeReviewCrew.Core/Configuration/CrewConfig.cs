using CodeReviewCrew.Core.Agents;
using CodeReviewCrew.Core.Localization;
using CodeReviewCrew.Core.Models;
using Microsoft.Extensions.FileSystemGlobbing;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CodeReviewCrew.Core.Configuration;

/// <summary>Per-repository settings, usually loaded from a <c>.crc.yml</c> file.</summary>
public sealed class CrewConfig
{
    public const string DefaultFileName = ".crc.yml";

    /// <summary>Language of the review: en (default), pt-BR or zh-CN.</summary>
    public string Language { get; set; } = ReviewLanguage.English.Code;

    /// <summary>Keys of the specialist agents to run. Empty means all of them.</summary>
    public List<string> Agents { get; set; } = [];

    /// <summary>Findings below this severity are dropped from the final review.</summary>
    public Severity MinSeverity { get; set; } = Severity.Info;

    /// <summary>Glob patterns of files that are never reviewed.</summary>
    public List<string> Ignore { get; set; } = [];

    /// <summary>Maximum diff size, in characters, sent to an agent in a single request.</summary>
    public int MaxChunkChars { get; set; } = 60_000;

    public int AgentTimeoutSeconds { get; set; } = 180;

    public int MaxRetries { get; set; } = 2;

    /// <summary>Optional model prices, used to estimate the cost of a review.</summary>
    public PricingConfig? Pricing { get; set; }

    /// <summary>Extra MCP servers whose tools are given to the specialist agents.</summary>
    public List<McpServerConfig> McpServers { get; set; } = [];

    public static CrewConfig Default => new();

    public static CrewConfig Parse(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithEnumNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        var config = deserializer.Deserialize<CrewConfig?>(yaml) ?? new CrewConfig();
        config.Validate();
        return config;
    }

    public static CrewConfig Load(string path) => Parse(File.ReadAllText(path));

    public IReadOnlyList<AgentDefinition> ResolveSpecialists()
    {
        if (Agents.Count == 0)
        {
            return Crew.Specialists;
        }

        return Agents.Select(key => Crew.Specialists.FirstOrDefault(a => a.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException(
                    $"Unknown agent '{key}' in {DefaultFileName}. Valid values: {string.Join(", ", Crew.Specialists.Select(a => a.Key))}."))
            .Distinct()
            .ToList();
    }

    public ReviewLanguage ResolveLanguage() => ReviewLanguage.Parse(Language);

    public bool IsIgnored(string path)
    {
        if (Ignore.Count == 0)
        {
            return false;
        }

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddIncludePatterns(Ignore);
        return matcher.Match(path).HasMatches;
    }

    private void Validate()
    {
        if (MaxChunkChars < 1000)
        {
            throw new InvalidOperationException("maxChunkChars must be at least 1000.");
        }

        if (AgentTimeoutSeconds <= 0 || MaxRetries < 0)
        {
            throw new InvalidOperationException("agentTimeoutSeconds must be positive and maxRetries cannot be negative.");
        }

        _ = ResolveSpecialists();
        _ = ResolveLanguage();

        if (McpServers.Any(s => string.IsNullOrWhiteSpace(s.Name) || string.IsNullOrWhiteSpace(s.Command)))
        {
            throw new InvalidOperationException("Every item in mcpServers needs a name and a command.");
        }
    }
}

/// <summary>An MCP server started over stdio. It does not inherit the environment (API keys stay private): pass what it needs in <see cref="Env"/>.</summary>
public sealed class McpServerConfig
{
    public string Name { get; set; } = "";

    public string Command { get; set; } = "";

    public List<string> Args { get; set; } = [];

    public Dictionary<string, string> Env { get; set; } = [];
}

/// <summary>Model prices in US dollars per one million tokens.</summary>
public sealed class PricingConfig
{
    public decimal InputPerMillion { get; set; }

    public decimal OutputPerMillion { get; set; }
}
