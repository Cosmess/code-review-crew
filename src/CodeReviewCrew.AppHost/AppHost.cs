// Runs a review under Aspire: the dashboard shows one trace per review, with a span per agent
// and per model call, plus token and finding metrics.
var builder = DistributedApplication.CreateBuilder(args);

// Asked for in the dashboard on first run, or set with:
// dotnet user-secrets set Parameters:openai-api-key sk-... --project src/CodeReviewCrew.AppHost
var openAiKey = builder.AddParameter("openai-api-key", secret: true);

var diff = builder.Configuration["Review:Diff"]
    ?? Path.Combine(builder.AppHostDirectory, "..", "..", "samples", "vulnerable.diff");

builder.AddProject<Projects.CodeReviewCrew_Cli>("code-review-crew")
    .WithEnvironment("OPENAI_API_KEY", openAiKey)
    .WithEnvironment("CRC_MODEL", builder.Configuration["Review:Model"] ?? "gpt-5-mini")
    .WithArgs(Path.GetFullPath(diff));

builder.Build().Run();
