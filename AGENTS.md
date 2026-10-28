# AGENTS.md

Instruções para agentes de IA (Claude Code, Codex, Copilot, Cursor etc.) que trabalham neste repositório.
Leia este arquivo inteiro antes de alterar qualquer coisa. Os detalhes estão em [`docs/`](docs/README.md).

## O que é o projeto

O **Code Review Crew** é um revisor de pull requests feito com uma equipe de agentes de IA em .NET 10.
Agentes especialistas (segurança, OWASP Top 10, performance, estilo) analisam o diff em paralelo, e um
agente sintetizador consolida tudo em uma revisão, publicada no PR com comentários inline e em SARIF.

## Comandos

```bash
dotnet build                                   # compila a solução
dotnet test tests/CodeReviewCrew.Tests         # testes unitários (rápidos, sem rede)
dotnet test                                    # tudo; os de integração são ignorados sem OPENAI_API_KEY
dotnet run --project src/CodeReviewCrew.Cli -- samples/vulnerable.diff   # revisão real (precisa de OPENAI_API_KEY)
dotnet run --project src/CodeReviewCrew.Cli -- --help
```

## Regras obrigatórias

1. **Commits em Conventional Commits, em português.** Ex.: `feat(agentes): adiciona agente de acessibilidade`.
   Tipos: `feat`, `fix`, `docs`, `test`, `refactor`, `perf`, `ci`, `chore`. Corpo explica o porquê.
2. **Nenhuma atribuição a IA** em commits ou PRs: sem `Co-Authored-By` de assistentes, sem "Generated with ...".
   Os commits saem com a identidade git configurada na máquina.
3. **Idioma:** identificadores e comentários de código em inglês; tudo que o usuário final lê (prompts dos
   agentes, mensagens da CLI, revisão em Markdown, erros) em português do Brasil. Veja [padrões de código](docs/padroes-de-codigo.md).
4. **Testes acompanham toda mudança de comportamento.** Testes unitários nunca chamam LLM nem rede:
   use `FakeChatClient` e `FakeHttpHandler`. Rode `dotnet test` antes de cada commit; todos devem passar.
5. **O Core não depende de provedor de LLM.** `CodeReviewCrew.Core` só conhece `IChatClient`
   (`Microsoft.Extensions.AI`). SDKs de OpenAI, Anthropic e Ollama ficam na CLI.
6. **Uma falha de agente nunca derruba a revisão.** Toda chamada a modelo passa pelo `RetryPolicy`; erros
   viram `SpecialistReport.Error` ou o fallback do sintetizador. Não propague exceções de LLM para o usuário.
7. **Segredos só por variável de ambiente.** Nunca escreva chaves em código, testes, exemplos ou logs.
8. **Mudou comportamento visível? Atualize o README** e, se for decisão de arquitetura, crie um ADR em `docs/adr/`.

## Mapa do código

| Caminho | Responsabilidade |
|---|---|
| `src/CodeReviewCrew.Core/Agents/` | Definição dos agentes (`Crew.cs`): papel, descrição e instruções |
| `src/CodeReviewCrew.Core/Orchestration/` | `ReviewOrchestrator` (fan-out/fan-in), `RetryPolicy`, contagem de tokens |
| `src/CodeReviewCrew.Core/Diff/` | Parser de diff unificado e divisão em partes |
| `src/CodeReviewCrew.Core/Configuration/` | `.crc.yml` (`CrewConfig`) |
| `src/CodeReviewCrew.Core/GitHub/` | Cliente REST do GitHub e publicação da revisão com comentários inline |
| `src/CodeReviewCrew.Core/Rendering/` | Saídas em Markdown e SARIF |
| `src/CodeReviewCrew.Cli/` | Linha de comando (System.CommandLine) e escolha do provedor de LLM |
| `tests/CodeReviewCrew.Tests/` | Testes unitários |
| `tests/CodeReviewCrew.IntegrationTests/` | Testes com a API real da OpenAI |
| `action.yml` | GitHub Action composta |

## Guias para tarefas comuns

- [Arquitetura](docs/arquitetura.md): fluxo completo e por que cada peça existe
- [Padrões de código](docs/padroes-de-codigo.md)
- [Testes](docs/testes.md)
- [Como adicionar um novo agente](docs/guias/adicionar-agente.md)
- [Usando a GitHub Action](docs/guias/github-action.md)
- [Decisões de arquitetura (ADRs)](docs/adr/)

## Antes de encerrar uma tarefa

- [ ] `dotnet build` sem erros nem novos warnings
- [ ] `dotnet test` passando
- [ ] README/docs atualizados se o comportamento mudou
- [ ] Commit no padrão Conventional Commits, em português, sem atribuição a IA
