using Anthropic;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

namespace CodeReviewCrew.Cli;

/// <summary>Creates the <see cref="IChatClient"/> for the provider selected through environment variables.</summary>
internal static class ChatClientFactory
{
    public static IChatClient Create()
    {
        var provider = (Environment.GetEnvironmentVariable("CRC_PROVIDER") ?? "openai").ToLowerInvariant();
        var model = Environment.GetEnvironmentVariable("CRC_MODEL");

        return provider switch
        {
            "ollama" => new OllamaApiClient(
                new Uri(Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434"),
                model ?? "qwen3:8b"),

            "openai" => new OpenAIClient(RequireEnv("OPENAI_API_KEY"))
                .GetChatClient(model ?? "gpt-5-mini")
                .AsIChatClient(),

            "anthropic" => new AnthropicClient { ApiKey = RequireEnv("ANTHROPIC_API_KEY") }
                .AsIChatClient(model ?? "claude-sonnet-5-5"),

            _ => throw new InvalidOperationException($"Unknown provider '{provider}'. Use ollama, openai or anthropic.")
        };
    }

    private static string RequireEnv(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Environment variable {name} is not set.");
}
