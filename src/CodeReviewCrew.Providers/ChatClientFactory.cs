using Anthropic;
using CodeReviewCrew.Core.Telemetry;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

namespace CodeReviewCrew.Providers;

/// <summary>Creates the <see cref="IChatClient"/> for the provider selected through environment variables.</summary>
public static class ChatClientFactory
{
    public static IChatClient Create()
    {
        var provider = (Environment.GetEnvironmentVariable("CRC_PROVIDER") ?? "openai").ToLowerInvariant();
        var model = Environment.GetEnvironmentVariable("CRC_MODEL");

        IChatClient client = provider switch
        {
            "ollama" => new OllamaApiClient(
                new Uri(Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434"),
                model ?? "qwen3:8b"),

            "openai" => new OpenAIClient(RequireEnv("OPENAI_API_KEY"))
                .GetChatClient(model ?? "gpt-5-mini")
                .AsIChatClient(),

            "anthropic" => new AnthropicClient { ApiKey = RequireEnv("ANTHROPIC_API_KEY") }
                .AsIChatClient(model ?? "claude-sonnet-5-5"),

            _ => throw new InvalidOperationException($"Provedor desconhecido '{provider}'. Use openai, ollama ou anthropic.")
        };

        // One gen_ai "chat" span per model call, nested under the agent span. Prompts and code are not recorded.
        return client.AsBuilder()
            .UseOpenTelemetry(sourceName: CrewTelemetry.SourceName)
            .Build();
    }

    private static string RequireEnv(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"A variável de ambiente {name} não está definida.");
}
