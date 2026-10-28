# Testes

## Projetos

| Projeto | O que cobre | Rede/LLM |
|---|---|---|
| `tests/CodeReviewCrew.Tests` | Toda a lógica do Core: parser, config, orquestração, retry, renderers, GitHub | Nunca |
| `tests/CodeReviewCrew.IntegrationTests` | Revisão de ponta a ponta com a API real da OpenAI | Sim, só com `OPENAI_API_KEY` |

No CI, os unitários rodam em todo push e PR. Os de integração rodam só em push na `main`, porque custam dinheiro.

## Fakes

- **`FakeChatClient`** implementa `IChatClient`. Recebe uma função `prompt → resposta` (o prompt inclui
  as instruções do agente, então dá para responder conforme o agente). Registra os prompts em `Prompts` e
  devolve 1000 tokens de entrada e 100 de saída por chamada. Para simular falhas, lance exceções na função;
  para simular lentidão, use o construtor assíncrono e `Task.Delay(..., token)`.
- **`FakeHttpHandler`** é um `HttpMessageHandler` que registra cada requisição (`Requests`) e responde com a
  função informada. Use com `new HttpClient(handler) { BaseAddress = ... }`.

## Convenções

- Nome do teste descreve o comportamento, em inglês com `_`: `Retries_a_transient_failure`.
- Estrutura arrange / act / assert separada por linhas em branco.
- Nos testes do orquestrador, crie com `retryBackoff: _ => TimeSpan.Zero` para não esperar o backoff real.
- Teste comportamento observável (saídas, requisições feitas, eventos), não detalhes internos.
- Todo bug corrigido ganha um teste que falharia antes da correção.

## Rodando

```bash
dotnet test tests/CodeReviewCrew.Tests                                  # unitários
OPENAI_API_KEY=sk-... dotnet test tests/CodeReviewCrew.IntegrationTests  # integração
```
