using System.Text.Json;
using CodeReviewCrew.Core.Models;
using CodeReviewCrew.Core.Rendering;

namespace CodeReviewCrew.Tests;

public class SarifRendererTests
{
    private static JsonElement Render(params ReviewFinding[] findings) =>
        JsonDocument.Parse(SarifRenderer.Render(new ReviewReport("x", Verdict.Comment, [.. findings]))).RootElement;

    [Fact]
    public void Produces_a_sarif_2_1_0_log()
    {
        var sarif = Render();

        Assert.Equal("2.1.0", sarif.GetProperty("version").GetString());
        var driver = sarif.GetProperty("runs")[0].GetProperty("tool").GetProperty("driver");
        Assert.Equal("Code Review Crew", driver.GetProperty("name").GetString());
        Assert.Equal(0, sarif.GetProperty("runs")[0].GetProperty("results").GetArrayLength());
    }

    [Fact]
    public void Maps_owasp_findings_to_one_rule_per_category()
    {
        var sarif = Render(
            new ReviewFinding(Severity.Critical, "src/A.cs", 10, "[A05:2025 Injection] SQL montado com interpolação", "Entrada concatenada.", "Use parâmetros."),
            new ReviewFinding(Severity.Medium, "src/B.cs", 3, "[A05:2025 Injection] Comando montado com string", "Entrada concatenada.", null));

        var run = sarif.GetProperty("runs")[0];
        var rule = Assert.Single(run.GetProperty("tool").GetProperty("driver").GetProperty("rules").EnumerateArray());
        Assert.Equal("OWASP-A05", rule.GetProperty("id").GetString());
        Assert.Equal("OWASP A05: Injection", rule.GetProperty("shortDescription").GetProperty("text").GetString());
        Assert.Equal("9.5", rule.GetProperty("properties").GetProperty("security-severity").GetString());

        var result = run.GetProperty("results")[0];
        Assert.Equal("error", result.GetProperty("level").GetString());
        Assert.EndsWith("Sugestão: Use parâmetros.", result.GetProperty("message").GetProperty("text").GetString());
        var location = result.GetProperty("locations")[0].GetProperty("physicalLocation");
        Assert.Equal("src/A.cs", location.GetProperty("artifactLocation").GetProperty("uri").GetString());
        Assert.Equal(10, location.GetProperty("region").GetProperty("startLine").GetInt32());
        Assert.Equal("warning", run.GetProperty("results")[1].GetProperty("level").GetString());
    }

    [Fact]
    public void Uses_a_generic_rule_and_omits_the_region_without_a_line()
    {
        var sarif = Render(new ReviewFinding(Severity.Low, "src/C.cs", null, "Nome ruim", "Explicação.", null));

        var run = sarif.GetProperty("runs")[0];
        Assert.Equal("CRC0001", run.GetProperty("results")[0].GetProperty("ruleId").GetString());
        Assert.Equal("note", run.GetProperty("results")[0].GetProperty("level").GetString());
        Assert.False(run.GetProperty("results")[0].GetProperty("locations")[0].GetProperty("physicalLocation").TryGetProperty("region", out _));
    }
}
