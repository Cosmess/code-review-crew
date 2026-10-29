using System.CommandLine;
using CodeReviewCrew.Cli;
using CodeReviewCrew.Core.Models;

var diffArgument = new Argument<FileInfo?>("diff")
{
    Description = "Diff file to review. Without it and without --pr, the diff is read from standard input.",
    Arity = ArgumentArity.ZeroOrOne
};
var prOption = new Option<int?>("--pr") { Description = "Number of the GitHub pull request to review." };
var repoOption = new Option<string?>("--repo")
{
    Description = "Repository in owner/name format (default: the GITHUB_REPOSITORY variable).",
    DefaultValueFactory = _ => Environment.GetEnvironmentVariable("GITHUB_REPOSITORY")
};
var postOption = new Option<bool>("--post") { Description = "Posts the review on the pull request, with inline comments. Requires --pr and GITHUB_TOKEN." };
var configOption = new Option<FileInfo?>("--config") { Description = "Configuration file (default: .crc.yml in the current directory, if it exists)." };
var outputOption = new Option<FileInfo?>("--output") { Description = "Also saves the review as Markdown to this file." };
var sarifOption = new Option<FileInfo?>("--sarif") { Description = "Saves the findings as SARIF 2.1.0 for GitHub code scanning." };
var repoPathOption = new Option<DirectoryInfo?>("--repo-path")
{
    Description = "Repository directory: gives the agents MCP tools to read whole files, search code and read the conventions."
};
var failOnOption = new Option<Severity?>("--fail-on") { Description = "Exits with code 2 if there is a finding of this severity or higher (Low, Medium, High, Critical)." };

var languageOption = new Option<string?>("--language")
{
    Description = "Language of the review: en, pt-BR or zh-CN (default: the language in .crc.yml, or en)."
};

var root = new RootCommand("Code Review Crew: code review by a crew of AI agents.")
{
    diffArgument, prOption, repoOption, postOption, configOption, outputOption, sarifOption, failOnOption, repoPathOption, languageOption
};

root.SetAction((parseResult, cancellationToken) => new ReviewCommand(new ReviewCommandOptions(
    parseResult.GetValue(diffArgument),
    parseResult.GetValue(prOption),
    parseResult.GetValue(repoOption),
    parseResult.GetValue(postOption),
    parseResult.GetValue(configOption),
    parseResult.GetValue(outputOption),
    parseResult.GetValue(sarifOption),
    parseResult.GetValue(failOnOption),
    parseResult.GetValue(repoPathOption),
    parseResult.GetValue(languageOption))).RunAsync(cancellationToken));

using var telemetry = TelemetrySetup.StartIfConfigured();
return await root.Parse(args).InvokeAsync();
