# ADR 0003: Uma falha de agente nunca derruba a revisão

**Status:** aceita

## Contexto

Chamadas a LLM falham por timeout, limite de taxa ou JSON inválido. Em um pipeline de PR, falhar a revisão
inteira por causa de um agente é pior do que entregar uma revisão parcial e avisar.

## Decisão

- Toda chamada passa por `RetryPolicy` (timeout por tentativa + backoff exponencial).
- Esgotadas as tentativas, o especialista é marcado com `Error` e os demais seguem.
- Se o sintetizador falhar, as análises são unidas sem ele.
- A revisão mostra quais agentes falharam.

## Consequências

- Revisões podem ser parciais; isso fica explícito no Markdown.
- Sem dependência de Polly: a política é pequena e testada diretamente.
