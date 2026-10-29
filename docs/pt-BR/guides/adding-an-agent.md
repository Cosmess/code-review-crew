# Como adicionar um agente especialista

[English](../../en/guides/adding-an-agent.md) | **Português** | [简体中文](../../zh-CN/guides/adding-an-agent.md)

Exemplo: um agente de acessibilidade para código front-end.

## 1. Defina o agente em `src/CodeReviewCrew.Core/Agents/Crew.cs`

```csharp
public static readonly AgentDefinition Accessibility = new(
    "accessibility",                    // chave usada no .crc.yml
    "AccessibilityAgent",               // nome exibido na revisão
    "Checks accessibility problems.",
    $"""
    You are an accessibility specialist (WCAG 2.2) reviewing a pull request.
    Look for: images without alternative text, ...
    {SharedRules}
    """);
```

- Escreva as instruções em inglês, listando de forma concreta o que procurar. O idioma da revisão é acrescentado automaticamente.
- Termine com `{SharedRules}`.
- Diga qual severidade usar, se o agente tender a exagerar.

## 2. Registre na equipe

Adicione o agente em `Crew.Specialists`. Ele passa a rodar por padrão e pode ser escolhido no `.crc.yml` pela chave.

## 3. Atualize os testes

- `ReviewOrchestratorTests` conta especialistas e prompts (`4` especialistas, `5` prompts com o sintetizador).
- `CrewConfigTests` conta os especialistas no teste de configuração vazia.
- Se o agente tiver um comportamento específico, crie um teste com o `FakeChatClient` respondendo a ele.

## 4. Atualize a documentação

- A tabela de agentes e o diagrama nos três READMEs.
- Os valores de `agents` no `.crc.example.yml`.
- O diagrama nas três versões do documento de arquitetura.

## 5. Meça o impacto

Crie um caso em `evals/cases/` com o tipo de problema que o agente deve achar (veja
[`evals/README.pt-BR.md`](../../../evals/README.pt-BR.md)) e rode a avaliação antes e depois da mudança.
O novo agente não deve derrubar a precisão dos outros casos.

```bash
dotnet run --project src/CodeReviewCrew.Evals
```

## 6. Valide e faça o commit

```bash
dotnet test
git commit -m "feat(agentes): adiciona agente de acessibilidade"
```
