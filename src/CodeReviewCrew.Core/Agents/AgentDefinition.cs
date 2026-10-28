namespace CodeReviewCrew.Core.Agents;

/// <param name="Key">Short identifier used in <c>.crc.yml</c> to enable the agent.</param>
public sealed record AgentDefinition(string Key, string Name, string Description, string Instructions);
