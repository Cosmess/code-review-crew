using System.CommandLine;
using CodeReviewCrew.Cli;
using CodeReviewCrew.Core.Models;

var diffArgument = new Argument<FileInfo?>("diff")
{
    Description = "Arquivo com o diff a revisar. Sem ele e sem --pr, o diff é lido da entrada padrão.",
    Arity = ArgumentArity.ZeroOrOne
};
var prOption = new Option<int?>("--pr") { Description = "Número do pull request a revisar no GitHub." };
var repoOption = new Option<string?>("--repo")
{
    Description = "Repositório no formato dono/nome (padrão: variável GITHUB_REPOSITORY).",
    DefaultValueFactory = _ => Environment.GetEnvironmentVariable("GITHUB_REPOSITORY")
};
var postOption = new Option<bool>("--post") { Description = "Publica a revisão no pull request, com comentários inline. Requer --pr e GITHUB_TOKEN." };
var configOption = new Option<FileInfo?>("--config") { Description = "Arquivo de configuração (padrão: .crc.yml no diretório atual, se existir)." };
var outputOption = new Option<FileInfo?>("--output") { Description = "Também salva a revisão em Markdown neste arquivo." };
var sarifOption = new Option<FileInfo?>("--sarif") { Description = "Salva os problemas em SARIF 2.1.0 para o GitHub code scanning." };
var failOnOption = new Option<Severity?>("--fail-on") { Description = "Termina com código 2 se houver problema desta severidade ou maior (Low, Medium, High, Critical)." };

var root = new RootCommand("Code Review Crew: revisão de código com uma equipe de agentes de IA.")
{
    diffArgument, prOption, repoOption, postOption, configOption, outputOption, sarifOption, failOnOption
};

root.SetAction((parseResult, cancellationToken) => new ReviewCommand(new ReviewCommandOptions(
    parseResult.GetValue(diffArgument),
    parseResult.GetValue(prOption),
    parseResult.GetValue(repoOption),
    parseResult.GetValue(postOption),
    parseResult.GetValue(configOption),
    parseResult.GetValue(outputOption),
    parseResult.GetValue(sarifOption),
    parseResult.GetValue(failOnOption))).RunAsync(cancellationToken));

return await root.Parse(args).InvokeAsync();
