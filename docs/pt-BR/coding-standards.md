# Padrões de código

[English](../en/coding-standards.md) | **Português** | [简体中文](../zh-CN/coding-standards.md)

## Idiomas

| O quê | Idioma |
|---|---|
| Identificadores, comentários, XML docs, instruções dos agentes, mensagens da CLI, exceções, descrições de ferramentas | Inglês |
| Texto que a equipe escreve na revisão (rótulos, vereditos, resumos de fallback) | O idioma da revisão, via `ReviewStrings` |
| Nomes de testes | Inglês, descrevendo o comportamento (`Falls_back_to_merged_findings_when_the_synthesizer_fails`) |
| Documentação | Inglês, português do Brasil e chinês simplificado, mantidos em sincronia |
| Commits e PRs | Conventional Commits, em português do Brasil (preferência do mantenedor) |

## C#

- .NET 10 com `Nullable` e `ImplicitUsings` habilitados. Não introduza warnings.
- Namespaces com escopo de arquivo (`namespace X;`), um tipo público principal por arquivo.
- `record` para modelos imutáveis (`ReviewFinding`, `CrewResult`); classes `sealed` por padrão.
- Construtores primários quando a classe só guarda dependências (`ReviewPublisher(GitHubClient gitHub, ...)`).
- Collection expressions (`[]`, `[a, b]`) e pattern matching (`is { Length: > 0 }`, `switch` de expressão).
- `async` de ponta a ponta; todo método assíncrono público recebe `CancellationToken cancellationToken = default`.
  Nunca use `.Result` ou `.Wait()`.
- Regex com `[GeneratedRegex]`.
- Comentários explicam o **porquê**, não o quê. Use `<summary>` em tipos públicos.
- Validação de argumentos com `ArgumentNullException.ThrowIfNull` e `ArgumentOutOfRangeException.ThrowIf...`.

## Organização

- Lógica nova vai para o Core, na pasta do assunto (`Agents`, `Orchestration`, `Diff`, `GitHub`, `Rendering`,
  `Configuration`, `Localization`, `Telemetry`).
- A CLI só faz composição e I/O. Se algo precisa de teste, provavelmente pertence ao Core.
- Sem novos pacotes NuGet sem necessidade clara; prefira o que o .NET já oferece (ex.: `HttpClient` em vez de Octokit).
- Versões de pacotes ficam fixas nos `.csproj`.

## Prompts dos agentes

- Ficam em `Agents/Crew.cs`, como `AgentDefinition(Key, Name, Description, Instructions)`.
- Especialistas terminam com `{SharedRules}`, que padroniza: só o que está no diff, linhas da nova versão, nada
  genérico, como evitar falsos positivos e como calibrar a severidade.
- Seja específico: liste o que procurar, com exemplos .NET concretos.
- Os testes identificam agentes por trechos das instruções (ex.: `"lead reviewer"`). Se mudar esses trechos,
  ajuste os helpers em `ReviewOrchestratorTests`.
- Depois de mudar um prompt, rode a avaliação e compare precisão e recall (veja o [AGENTS.md](../../AGENTS.md)).
