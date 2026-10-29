using CodeReviewCrew.Core.Diff;
using CodeReviewCrew.Core.Models;
using CodeReviewCrew.Evals;

namespace CodeReviewCrew.Tests;

public class EvalTests
{
    private static readonly ExpectedFinding SqlInjection = new("src/Repo.cs", 15, 2, ["sql", "injecao"], ["A05"], Severity.High);

    private static EvalCase Case(bool shouldBlock, params ExpectedFinding[] expected) =>
        new("caso", "", new ExpectedReview("descrição", shouldBlock, [.. expected]));

    private static ReviewFinding Finding(int? line, string title, Severity severity = Severity.High, string file = "src/Repo.cs") =>
        new(severity, file, line, title, "explicação", null);

    [Theory]
    [InlineData(15, "Injeção de SQL", true)]
    [InlineData(17, "[A05:2025 Injection] Consulta montada", true)]
    [InlineData(18, "Injeção de SQL", false)]
    [InlineData(null, "Injeção de SQL", false)]
    [InlineData(15, "Variável mal nomeada", false)]
    public void Matches_by_file_line_tolerance_and_keyword_or_owasp_tag(int? line, string title, bool matches)
    {
        Assert.Equal(matches, EvalScorer.Matches(SqlInjection, Finding(line, title)));
    }

    [Fact]
    public void Does_not_match_another_file()
    {
        Assert.False(EvalScorer.Matches(SqlInjection, Finding(15, "Injeção de SQL", file: "src/Outro.cs")));
    }

    [Fact]
    public void Scores_hits_misses_false_positives_and_verdict()
    {
        var logLeak = new ExpectedFinding("src/Repo.cs", 10, 2, ["senha", "log"], [], Severity.Medium);
        var report = new ReviewReport("s", Verdict.RequestChanges,
        [
            Finding(15, "Injeção de SQL", Severity.Medium),
            Finding(40, "Problema inventado", Severity.High),
            Finding(41, "Nome de variável", Severity.Low)
        ]);

        var score = EvalScorer.Score(Case(true, SqlInjection, logLeak), report);

        Assert.Equal(1, score.TruePositives);
        Assert.Equal(1, score.FalseNegatives);
        Assert.Equal(1, score.FalsePositives);
        Assert.Equal(1, score.IgnoredLowFindings);
        Assert.Equal(1, score.UnderratedSeverities);
        Assert.True(score.VerdictCorrect);
        Assert.Equal(logLeak, Assert.Single(score.Missed));
    }

    [Fact]
    public void A_finding_counts_for_only_one_expected_issue()
    {
        var duplicate = SqlInjection with { Line = 16 };

        var score = EvalScorer.Score(Case(true, SqlInjection, duplicate), new ReviewReport("s", Verdict.RequestChanges, [Finding(15, "SQL injection")]));

        Assert.Equal(1, score.TruePositives);
        Assert.Equal(1, score.FalseNegatives);
    }

    [Fact]
    public void Blocking_a_clean_change_is_a_wrong_verdict()
    {
        var score = EvalScorer.Score(Case(false), new ReviewReport("s", Verdict.RequestChanges, [Finding(1, "Falso alarme")]));

        Assert.False(score.VerdictCorrect);
        Assert.Equal(1, score.FalsePositives);
    }

    [Fact]
    public void Summary_computes_precision_recall_and_f1()
    {
        var summary = new EvalSummary(
        [
            new CaseScore("a", 3, 1, 1, 0, 0, true, [], []),
            new CaseScore("b", 1, 1, 0, 2, 0, false, [], [])
        ]);

        Assert.Equal(0.8, summary.Precision, 3);
        Assert.Equal(4.0 / 6, summary.Recall, 3);
        Assert.Equal(2 * 0.8 * (4.0 / 6) / (0.8 + 4.0 / 6), summary.F1, 3);
        Assert.Equal(0.5, summary.VerdictAccuracy, 3);
    }

    [Fact]
    public void Every_labeled_case_in_the_dataset_is_consistent()
    {
        var directory = Path.Combine(RepositoryRoot(), "evals", "casos");

        var cases = EvalCase.LoadAll(directory);

        Assert.True(cases.Count >= 10, "o dataset deve ter pelo menos 10 casos");
        Assert.Contains(cases, c => c.Expected.Findings.Count == 0);
        foreach (var evalCase in cases)
        {
            var files = DiffParser.Parse(evalCase.Diff).ToDictionary(f => f.Path);
            Assert.NotEmpty(files);
            Assert.False(string.IsNullOrWhiteSpace(evalCase.Expected.Description), evalCase.Name);
            foreach (var expected in evalCase.Expected.Findings)
            {
                Assert.True(files.TryGetValue(expected.File, out var file), $"{evalCase.Name}: {expected.File} não está no diff");
                Assert.True(file!.CommentableLines.Contains(expected.Line), $"{evalCase.Name}: linha {expected.Line} não está no diff");
                Assert.NotEmpty(expected.Keywords);
            }

            Assert.Equal(evalCase.Expected.ShouldBlock, evalCase.Expected.Findings.Any(f => f.MinimumSeverity >= Severity.High));
        }
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CodeReviewCrew.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }
}
