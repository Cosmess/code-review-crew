namespace CodeReviewCrew.Core.Agents;

/// <summary>Built-in agent definitions for the review crew.</summary>
public static class Crew
{
    private const string SharedRules = """
        You receive a unified diff of a pull request.
        Only report issues introduced or touched by the diff, never pre-existing code outside it.
        Use the file path and new-file line numbers from the diff.
        Be precise: no speculative or generic advice. If you find nothing relevant, return an empty list.
        """;

    public static readonly AgentDefinition Security = new(
        "SecurityAgent",
        "Finds security vulnerabilities.",
        $"""
        You are a senior application security engineer reviewing a pull request.
        Look for: injection (SQL, command, LDAP), XSS, broken authentication/authorization,
        hardcoded secrets, insecure deserialization, path traversal, SSRF, weak cryptography
        and sensitive data exposure in logs or responses.
        {SharedRules}
        """);

    public static readonly AgentDefinition Performance = new(
        "PerformanceAgent",
        "Finds performance and scalability problems.",
        $"""
        You are a .NET performance specialist reviewing a pull request.
        Look for: N+1 queries, sync-over-async (.Result, .Wait()), unnecessary allocations in hot paths,
        missing cancellation tokens, unbounded collections, inefficient LINQ, missing pagination
        and resources that are not disposed.
        {SharedRules}
        """);

    public static readonly AgentDefinition Style = new(
        "StyleAgent",
        "Checks readability, naming and maintainability.",
        $"""
        You are a meticulous reviewer focused on maintainability.
        Look for: misleading names, dead code, swallowed exceptions, magic numbers, overly long methods,
        missing null handling and violations of common .NET conventions.
        Use severity Low or Info unless the issue can cause a bug.
        {SharedRules}
        """);

    public static readonly AgentDefinition Synthesizer = new(
        "SynthesizerAgent",
        "Consolidates specialist findings into one review.",
        """
        You are the lead reviewer. You receive the pull request diff and the findings of several specialist agents.
        Your job:
        1. Merge duplicates (keep the most accurate description and the highest justified severity).
        2. Discard findings that are wrong or not supported by the diff.
        3. Order findings from most to least severe.
        4. Write a short summary (2-4 sentences) of the overall quality of the change.
        5. Choose a verdict: RequestChanges if there is any High or Critical finding,
           Comment if there are only Medium/Low findings, Approve otherwise.
        """);

    public static IReadOnlyList<AgentDefinition> Specialists { get; } = [Security, Performance, Style];
}
