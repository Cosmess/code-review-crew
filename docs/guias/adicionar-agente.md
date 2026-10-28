# Como adicionar um novo agente especialista

Exemplo: um agente de acessibilidade para código front-end.

## 1. Defina o agente em `src/CodeReviewCrew.Core/Agents/Crew.cs`

```csharp
public static readonly AgentDefinition Accessibility = new(
    "acessibilidade",                       // chave usada no .crc.yml
    "AgenteAcessibilidade",                 // nome exibido na revisão
    "Verifica problemas de acessibilidade.",
    $"""
    Você é um especialista em acessibilidade (WCAG 2.2) revisando um pull request.
    Procure por: imagens sem texto alternativo, ...
    {SharedRules}
    """);
```

- Escreva as instruções em português, listando o que procurar de forma concreta.
- Termine com `{SharedRules}`.
- Diga qual severidade usar, se o agente tender a exagerar.

## 2. Registre na equipe

Adicione o agente em `Crew.Specialists`. Ele passa a rodar por padrão e pode ser escolhido no `.crc.yml`
pela chave.

## 3. Atualize os testes

- `ReviewOrchestratorTests`: os testes contam especialistas e prompts (`4` especialistas, `5` prompts com o
  sintetizador). Ajuste os números.
- `CrewConfigTests`: o teste de configuração vazia conta os especialistas.
- Se o agente tiver um comportamento específico, crie um teste com o `FakeChatClient` respondendo a ele.

## 4. Atualize a documentação

- Tabela de agentes e diagrama no `README.md`.
- Lista de valores de `agents` no `.crc.example.yml`.
- Diagrama em `docs/arquitetura.md`.

## 5. Valide e faça o commit

```bash
dotnet test
git commit -m "feat(agentes): adiciona agente de acessibilidade"
```
