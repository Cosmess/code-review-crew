using CodeReviewCrew.Core.Configuration;
using CodeReviewCrew.Core.Models;

namespace CodeReviewCrew.Tests;

public class CrewConfigTests
{
    [Fact]
    public void Parses_every_setting()
    {
        var config = CrewConfig.Parse("""
            agents: [owasp, Performance]
            minSeverity: Medium
            ignore: ["**/*.Designer.cs", "docs/**"]
            maxChunkChars: 5000
            agentTimeoutSeconds: 30
            maxRetries: 4
            pricing:
              inputPerMillion: 0.25
              outputPerMillion: 2
            """);

        Assert.Equal(["AgenteOwasp", "AgentePerformance"], config.ResolveSpecialists().Select(a => a.Name));
        Assert.Equal(Severity.Medium, config.MinSeverity);
        Assert.Equal(5000, config.MaxChunkChars);
        Assert.Equal(30, config.AgentTimeoutSeconds);
        Assert.Equal(4, config.MaxRetries);
        Assert.Equal(0.25m, config.Pricing!.InputPerMillion);
    }

    [Fact]
    public void Empty_file_uses_defaults_with_every_agent()
    {
        var config = CrewConfig.Parse("");

        Assert.Equal(4, config.ResolveSpecialists().Count);
        Assert.Equal(Severity.Info, config.MinSeverity);
        Assert.Null(config.Pricing);
    }

    [Fact]
    public void Example_file_in_the_repository_is_valid()
    {
        var path = Path.Combine(AppContext.BaseDirectory, ".crc.example.yml");

        var config = CrewConfig.Load(path);

        Assert.Equal(Severity.Low, config.MinSeverity);
    }

    [Theory]
    [InlineData("src/Form1.Designer.cs", true)]
    [InlineData("docs/guia.md", true)]
    [InlineData("src/Program.cs", false)]
    public void Matches_ignored_paths(string path, bool ignored)
    {
        var config = CrewConfig.Parse("""ignore: ["**/*.Designer.cs", "docs/**"]""");

        Assert.Equal(ignored, config.IsIgnored(path));
    }

    [Theory]
    [InlineData("agents: [inexistente]", "Agente desconhecido")]
    [InlineData("maxChunkChars: 10", "maxChunkChars")]
    [InlineData("maxRetries: -1", "maxRetries")]
    public void Rejects_invalid_settings(string yaml, string message)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CrewConfig.Parse(yaml));
        Assert.Contains(message, ex.Message);
    }
}
