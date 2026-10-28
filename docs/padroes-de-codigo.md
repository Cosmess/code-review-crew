# Padrões de código

## Idioma

| O quê | Idioma |
|---|---|
| Identificadores (classes, métodos, variáveis), comentários e XML docs | Inglês |
| Nomes e instruções dos agentes, mensagens da CLI, Markdown da revisão, mensagens de exceção vistas pelo usuário | Português do Brasil |
| Nomes de testes | Inglês, descrevendo o comportamento (`Falls_back_to_merged_findings_when_the_synthesizer_fails`) |
| Commits e PRs | Português, no padrão Conventional Commits |
| Documentação (`docs/`, README, AGENTS.md) | Português |

## C#

- .NET 10, `Nullable` e `ImplicitUsings` habilitados. Não introduza warnings.
- Namespaces com escopo de arquivo (`namespace X;`), um tipo público principal por arquivo.
- `record` para modelos imutáveis (`ReviewFinding`, `CrewResult`); `sealed` por padrão em classes.
- Construtores primários quando a classe só guarda dependências (`ReviewPublisher(GitHubClient gitHub, ...)`).
- Collection expressions (`[]`, `[a, b]`) e pattern matching (`is { Length: > 0 }`, `switch` de expressão).
- `async` de ponta a ponta; todo método assíncrono público recebe `CancellationToken cancellationToken = default`.
  Nunca use `.Result` ou `.Wait()`.
- Regex com `[GeneratedRegex]`.
- Comentários explicam o **porquê**, não o quê. Use `<summary>` em tipos públicos.
- Validação de argumentos com `ArgumentNullException.ThrowIfNull`, `ArgumentOutOfRangeException.ThrowIf...`.

## Organização

- Lógica nova vai para o Core, na pasta do assunto (`Agents`, `Orchestration`, `Diff`, `GitHub`, `Rendering`, `Configuration`).
- A CLI só faz composição e I/O. Se algo precisa de teste, provavelmente pertence ao Core.
- Sem novos pacotes NuGet sem necessidade clara; prefira o que o .NET já oferece (ex.: `HttpClient` em vez de Octokit).
- Versões de pacotes ficam fixas nos `.csproj`.

## Prompts dos agentes

- Ficam em `Agents/Crew.cs`, como `AgentDefinition(Key, Name, Description, Instructions)`.
- Especialistas terminam com `{SharedRules}`, que padroniza: só o que está no diff, linhas da nova versão,
  nada genérico, lista vazia quando não há problemas, texto em português.
- Seja específico: liste o que procurar, com exemplos .NET concretos.
- Os testes identificam agentes por trechos das instruções (ex.: `"revisor líder"`). Se mudar esses trechos,
  ajuste os helpers em `ReviewOrchestratorTests`.
