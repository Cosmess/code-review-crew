# ADR 0001: Microsoft Agent Framework e Microsoft.Extensions.AI

**Status:** aceita

## Contexto

O projeto precisa de agentes com instruções próprias, saída estruturada e liberdade para trocar de provedor de LLM.

## Decisão

Usar o **Microsoft Agent Framework** (`ChatClientAgent`, `RunAsync<T>`) sobre a abstração `IChatClient` do
**Microsoft.Extensions.AI**. O Core só conhece `IChatClient`; a CLI cria o cliente do provedor.

## Consequências

- Trocar de provedor (OpenAI, Anthropic, Ollama) não toca no Core.
- Testes usam um `IChatClient` fake, sem rede.
- `RunAsync<T>` gera o JSON schema a partir dos tipos C#, eliminando parsing manual.
- Ficamos acoplados à evolução da API do Agent Framework; as versões ficam fixas nos `.csproj`.
