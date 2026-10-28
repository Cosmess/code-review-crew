using System.Collections.Concurrent;
using Microsoft.Extensions.AI;

namespace CodeReviewCrew.Tests;

/// <summary>Deterministic <see cref="IChatClient"/> that answers based on which agent is calling.</summary>
internal sealed class FakeChatClient(Func<string, string> respond) : IChatClient
{
    public ConcurrentBag<string> Prompts { get; } = [];

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var prompt = string.Join("\n", new[] { options?.Instructions }.Concat(messages.Select(m => m.Text)));
        Prompts.Add(prompt);
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, respond(prompt))));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
