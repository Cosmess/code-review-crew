using CodeReviewCrew.McpServer;

namespace CodeReviewCrew.Tests;

public sealed class RepositoryReaderTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("crc-repo-").FullName;
    private readonly string _outside = Directory.CreateTempSubdirectory("crc-outside-").FullName;
    private readonly RepositoryReader _reader;

    public RepositoryReaderTests()
    {
        Write("src/UserService.cs", string.Join('\n', Enumerable.Range(1, 10).Select(i => $"// linha {i}")) + "\npublic void Validate(string name) { }");
        Write("AGENTS.md", "Use async em todo I/O.");
        Write(".env", "OPENAI_API_KEY=sk-segredo");
        Write("config/prod.pem", "-----BEGIN PRIVATE KEY-----");
        Write(".git/config", "[remote \"origin\"]");
        Write("bin/Debug/app.cs", "public void Validate() { }");
        File.WriteAllText(Path.Combine(_outside, "segredo.txt"), "fora do repositório");
        _reader = new RepositoryReader(_root);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        Directory.Delete(_outside, recursive: true);
    }

    private void Write(string relative, string content)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    [Fact]
    public void Reads_a_line_range_with_line_numbers()
    {
        var text = _reader.ReadFile("src/UserService.cs", startLine: 10, endLine: 11);

        Assert.Equal("   10| // linha 10\n   11| public void Validate(string name) { }\n", text.ReplaceLineEndings("\n"));
    }

    [Theory]
    [InlineData("../crc-outside/segredo.txt")]
    [InlineData("src/../../segredo.txt")]
    [InlineData("/etc/passwd")]
    public void Rejects_paths_outside_the_repository(string path)
    {
        Assert.Throws<UnauthorizedAccessException>(() => _reader.ReadFile(path));
    }

    [Theory]
    [InlineData(".env")]
    [InlineData("config/prod.pem")]
    [InlineData(".git/config")]
    public void Rejects_secret_files_and_the_git_directory(string path)
    {
        var ex = Assert.Throws<UnauthorizedAccessException>(() => _reader.ReadFile(path));
        Assert.Contains("protegido", ex.Message);
    }

    [Fact]
    public void Rejects_symbolic_links_that_point_outside()
    {
        File.CreateSymbolicLink(Path.Combine(_root, "src", "atalho.txt"), Path.Combine(_outside, "segredo.txt"));
        Directory.CreateSymbolicLink(Path.Combine(_root, "externo"), _outside);

        Assert.Throws<UnauthorizedAccessException>(() => _reader.ReadFile("src/atalho.txt"));
        Assert.Throws<UnauthorizedAccessException>(() => _reader.ReadFile("externo/segredo.txt"));
        Assert.StartsWith("Nenhum resultado", _reader.Search("fora do repositório"));
    }

    [Fact]
    public void Search_skips_secrets_and_build_output()
    {
        var results = _reader.Search("validate");

        Assert.Equal("src/UserService.cs:11: public void Validate(string name) { }", results);
        Assert.DoesNotContain("sk-segredo", _reader.Search("OPENAI_API_KEY"));
    }

    [Fact]
    public void Lists_a_directory_without_hidden_entries()
    {
        var listing = _reader.ListDirectory().Split('\n');

        Assert.Contains("src/", listing);
        Assert.Contains("AGENTS.md", listing);
        Assert.DoesNotContain(".env", listing);
        Assert.DoesNotContain(".git/", listing);
    }

    [Fact]
    public void Reads_project_conventions()
    {
        Assert.Contains("Use async em todo I/O.", _reader.ReadConventions());
    }
}
