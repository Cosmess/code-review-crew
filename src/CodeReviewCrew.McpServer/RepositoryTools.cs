using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace CodeReviewCrew.McpServer;

/// <summary>MCP tools that let review agents read the repository beyond the diff.</summary>
[McpServerToolType]
public sealed class RepositoryTools(RepositoryReader reader)
{
    [McpServerTool(Name = "read_file", ReadOnly = true), Description("Lê um arquivo do repositório, com números de linha. Use para ver o contexto completo de um arquivo alterado no diff.")]
    public string ReadFile(
        [Description("Caminho relativo à raiz do repositório, ex.: src/Api/Program.cs")] string path,
        [Description("Primeira linha a ler (padrão: 1).")] int startLine = 1,
        [Description("Última linha a ler (padrão: fim do arquivo). No máximo 400 linhas por chamada.")] int? endLine = null) =>
        Run(() => reader.ReadFile(path, startLine, endLine));

    [McpServerTool(Name = "search_code", ReadOnly = true), Description("Busca um texto em todos os arquivos do repositório (sem diferenciar maiúsculas). Use para achar definições, chamadas ou validações de um método.")]
    public string SearchCode(
        [Description("Texto a buscar, ex.: nome de um método ou classe.")] string text,
        [Description("Padrão glob opcional para filtrar arquivos, ex.: **/*.cs")] string? filePattern = null) =>
        Run(() => reader.Search(text, filePattern));

    [McpServerTool(Name = "list_directory", ReadOnly = true), Description("Lista arquivos e pastas de um diretório do repositório.")]
    public string ListDirectory([Description("Caminho relativo à raiz (padrão: raiz).")] string path = ".") =>
        Run(() => reader.ListDirectory(path));

    [McpServerTool(Name = "read_conventions", ReadOnly = true), Description("Lê as convenções do projeto (AGENTS.md, CONTRIBUTING.md, .editorconfig e similares). Use para saber os padrões que o código deve seguir.")]
    public string ReadConventions() => Run(reader.ReadConventions);

    // Only these messages are safe and useful to return to the model; anything else is a bug.
    private static string Run(Func<string> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or ArgumentException or InvalidOperationException)
        {
            throw new McpException(ex.Message);
        }
    }
}
