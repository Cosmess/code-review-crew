using CodeReviewCrew.Core.Configuration;
using CodeReviewCrew.Core.Orchestration;
using Microsoft.Extensions.AI;

namespace CodeReviewCrew.Tests;

public class ToolCallingTests
{
    private const string Diff = """
        --- a/src/Api.cs
        +++ b/src/Api.cs
        @@ -0,0 +1 @@
        +var user = repository.Find(name);
        """;

    /// <summary>Asks for a tool on the first call and answers once the tool result is in the conversation.</summary>
    private sealed class ToolCallingChatClient : IChatClient
    {
        public List<IList<AITool>?> ToolsSeen { get; } = [];

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            lock (ToolsSeen) ToolsSeen.Add(options?.Tools);
            var list = messages.ToList();
            var isSecurity = (options?.Instructions ?? "").Contains("engenheiro sênior de segurança")
                || list.Any(m => m.Text.Contains("engenheiro sênior de segurança"));

            if (isSecurity && !list.SelectMany(m => m.Contents).OfType<FunctionResultContent>().Any())
            {
                var call = new FunctionCallContent("call-1", "read_file", new Dictionary<string, object?> { ["path"] = "src/Api.cs" });
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [call])));
            }

            var toolResult = list.SelectMany(m => m.Contents).OfType<FunctionResultContent>().FirstOrDefault()?.Result?.ToString();
            var json = toolResult is not null && toolResult.Contains("sem validação")
                ? """{"findings":[{"severity":"High","file":"src/Api.cs","line":1,"title":"Entrada sem validação","explanation":"Confirmado lendo o arquivo.","suggestion":null}]}"""
                : (options?.Instructions ?? "").Contains("revisor líder") || list.Any(m => m.Text.Contains("revisor líder"))
                    ? """{"summary":"s","verdict":"RequestChanges","findings":[]}"""
                    : """{"findings":[]}""";
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, json)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }

    [Fact]
    public async Task Specialists_can_call_tools_and_use_the_result()
    {
        var readPaths = new List<string>();
        var readFile = AIFunctionFactory.Create((string path) => { readPaths.Add(path); return "1| Find(name) sem validação"; }, "read_file");
        var client = new ToolCallingChatClient();

        var result = await new ReviewOrchestrator(client, CrewConfig.Default, _ => TimeSpan.Zero, [readFile]).ReviewAsync(Diff);

        Assert.Equal(["src/Api.cs"], readPaths);
        var security = result.SpecialistReports.Single(r => r.Agent == "AgenteSeguranca");
        Assert.Equal("Entrada sem validação", Assert.Single(security.Findings).Title);
    }

    [Fact]
    public async Task Only_specialists_receive_the_tools()
    {
        var tool = AIFunctionFactory.Create((string path) => "sem validação", "read_file");
        var client = new ToolCallingChatClient();

        await new ReviewOrchestrator(client, CrewConfig.Default, _ => TimeSpan.Zero, [tool]).ReviewAsync(Diff);

        // 4 specialists + 1 follow-up after the tool call get tools; the synthesizer call does not.
        Assert.Equal(5, client.ToolsSeen.Count(t => t?.Any(x => x.Name == "read_file") == true));
        Assert.Single(client.ToolsSeen, t => t is null || !t.Any(x => x.Name == "read_file"));
    }

    [Fact]
    public void Parses_mcp_servers_from_the_config()
    {
        var config = CrewConfig.Parse("""
            mcpServers:
              - name: docs
                command: npx
                args: ["-y", "@exemplo/mcp-docs"]
                env:
                  DOCS_TOKEN: abc
            """);

        var server = Assert.Single(config.McpServers);
        Assert.Equal(["-y", "@exemplo/mcp-docs"], server.Args);
        Assert.Equal("abc", server.Env["DOCS_TOKEN"]);
        Assert.Throws<InvalidOperationException>(() => CrewConfig.Parse("mcpServers: [{ name: x }]"));
    }
}
