using CodeReviewCrew.Core.Configuration;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace CodeReviewCrew.Cli;

/// <summary>
/// Starts the MCP servers available to the agents (the built-in repository server and the ones in
/// .crc.yml) and exposes their tools. A server that fails to start is reported and skipped.
/// </summary>
internal sealed class McpToolHost : IAsyncDisposable
{
    private const string RepositoryServerDll = "CodeReviewCrew.McpServer.dll";

    // The only variables a server inherits: API keys and tokens of this process never reach it.
    private static readonly string[] SafeEnvironmentVariables = ["PATH", "HOME", "USERPROFILE", "TMPDIR", "TEMP", "DOTNET_ROOT", "SystemRoot"];

    private readonly List<McpClient> _clients = [];

    public List<AITool> Tools { get; } = [];

    public static async Task<McpToolHost> StartAsync(string? repositoryPath, IReadOnlyList<McpServerConfig> servers, CancellationToken cancellationToken)
    {
        var host = new McpToolHost();

        if (repositoryPath is not null)
        {
            var repositoryServer = new McpServerConfig
            {
                Name = "repository",
                Command = "dotnet",
                Args = [Path.Combine(AppContext.BaseDirectory, RepositoryServerDll), "--root", Path.GetFullPath(repositoryPath)]
            };
            await host.ConnectAsync(repositoryServer, cancellationToken);
        }

        foreach (var server in servers)
        {
            await host.ConnectAsync(server, cancellationToken);
        }

        return host;
    }

    private async Task ConnectAsync(McpServerConfig server, CancellationToken cancellationToken)
    {
        var environment = SafeEnvironmentVariables
            .Select(name => (name, value: Environment.GetEnvironmentVariable(name)))
            .Where(v => v.value is not null)
            .ToDictionary(v => v.name, v => v.value);
        foreach (var (name, value) in server.Env)
        {
            environment[name] = value;
        }

        try
        {
            var transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = server.Name,
                Command = server.Command,
                Arguments = server.Args,
                InheritEnvironmentVariables = false,
                EnvironmentVariables = environment
            });

            var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
            _clients.Add(client);

            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            Tools.AddRange(tools);
            Console.Error.WriteLine($"MCP: server '{server.Name}' connected with {tools.Count} tool(s): {string.Join(", ", tools.Select(t => t.Name))}");
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            Console.Error.WriteLine($"⚠ MCP: could not start server '{server.Name}': {ex.Message}. The review continues without it.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
        {
            await client.DisposeAsync();
        }
    }
}
