# Testes

[English](../en/testing.md) | **Português** | [简体中文](../zh-CN/testing.md)

## Projetos

| Projeto | O que cobre | Rede/LLM |
|---|---|---|
| `tests/CodeReviewCrew.Tests` | Toda a lógica do Core: parser, config, orquestração, retry, idiomas, renderers, GitHub, MCP | Nunca |
| `tests/CodeReviewCrew.IntegrationTests` | Revisão de ponta a ponta com a API real da OpenAI | Sim, só com `OPENAI_API_KEY` |
| `src/CodeReviewCrew.Evals` (não é teste) | Qualidade das revisões: precisão, recall e F1 em `evals/cases` | Sim, sob demanda |

No CI, os testes unitários rodam em todo push e PR. Os de integração rodam só em push na `main`, porque custam dinheiro.

Os testes unitários também cobrem:

- **O protocolo MCP real** (`McpServerProtocolTests`): sobe o processo do servidor e chama as ferramentas por stdio,
  inclusive tentativas de ler `.env` e sair da raiz.
- **A segurança do `RepositoryReader`**: path traversal, caminhos absolutos, links simbólicos, `.git` e segredos.
- **Telemetria** (`TelemetryTests`): spans e eventos, isolados por um span de teste porque outras classes rodam em paralelo.
- **Idiomas**: a instrução de idioma chega a todos os agentes e os rótulos do Markdown são traduzidos.
- **Consistência do dataset**: todo `expected.json` aponta para arquivo e linha que existem no diff.

## Fakes

- **`FakeChatClient`** implementa `IChatClient`. Recebe uma função `prompt → resposta` (o prompt inclui as
  instruções do agente, então dá para responder conforme o agente), registra os prompts em `Prompts` e devolve
  1000 tokens de entrada e 100 de saída por chamada. Para simular falhas, lance exceções na função; para simular
  lentidão, use o construtor assíncrono e `Task.Delay(..., token)`.
- **`FakeHttpHandler`** é um `HttpMessageHandler` que registra cada requisição (`Requests`) e responde com a função
  informada. Use com `new HttpClient(handler) { BaseAddress = ... }`.

## Convenções

- O nome do teste descreve o comportamento, em inglês com `_`: `Retries_a_transient_failure`.
- Arrange / act / assert separados por linhas em branco.
- Nos testes do orquestrador, crie com `retryBackoff: _ => TimeSpan.Zero` para não esperar o backoff real.
- Teste comportamento observável (saídas, requisições feitas, eventos), não detalhes internos.
- Todo bug corrigido ganha um teste que falharia antes da correção.

## Rodando

```bash
dotnet test tests/CodeReviewCrew.Tests                                  # unitários
OPENAI_API_KEY=sk-... dotnet test tests/CodeReviewCrew.IntegrationTests  # integração
```
