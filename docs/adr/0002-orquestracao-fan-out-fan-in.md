# ADR 0002: Orquestração fan-out / fan-in com sintetizador

**Status:** aceita

## Contexto

Um único prompt "revise tudo" mistura preocupações e perde problemas. Agentes que conversam livremente
entre si são caros e difíceis de testar.

## Decisão

Especialistas independentes rodam em paralelo sobre o mesmo diff (**fan-out**) e um sintetizador consolida
o resultado (**fan-in**). A orquestração é código C# explícito no `ReviewOrchestrator`, não um chat entre agentes.

## Consequências

- Cada especialista tem um prompt focado; é simples adicionar ou desligar agentes.
- Latência ≈ especialista mais lento + sintetizador.
- O fluxo é determinístico e testável com fakes.
- Duplicatas entre especialistas são esperadas; resolvê-las é papel do sintetizador.
