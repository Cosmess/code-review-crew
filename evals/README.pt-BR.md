# Avaliação

[English](README.md) | **Português** | [简体中文](README.zh-CN.md)

Conjunto de pull requests rotulados para medir a qualidade das revisões. Cada caso tem o diff e os problemas que
uma boa revisão precisa encontrar.

## Rodando

```bash
export OPENAI_API_KEY=sk-...
dotnet run --project src/CodeReviewCrew.Evals                              # todos os casos, revisões em inglês
dotnet run --project src/CodeReviewCrew.Evals -- --filter sql              # só os casos com "sql" no nome
dotnet run --project src/CodeReviewCrew.Evals -- --language pt-BR          # revisões em português
dotnet run --project src/CodeReviewCrew.Evals -- --output relatorio.md --min-recall 0.8
```

No GitHub, rode o workflow **Evaluation** (*Actions → Evaluation → Run workflow*). O relatório aparece no resumo
da execução e como artefato.

## Métricas

| Métrica | Como é calculada |
|---|---|
| **Recall** | Problemas esperados encontrados / problemas esperados |
| **Precisão** | Acertos / (acertos + falsos positivos) |
| **F1** | Média harmônica de precisão e recall |
| **Vereditos corretos** | `RequestChanges` quando o caso deve bloquear, outro veredito quando não deve |
| **Severidade subestimada** | Problemas encontrados com severidade abaixo da mínima esperada |

Um problema da revisão **corresponde** a um esperado quando está no mesmo arquivo, a até `tolerance` linhas de
distância, e cita uma das `keywords` (sem diferenciar maiúsculas nem acentos) ou uma das categorias `owasp` no
título (ex.: `[A05:2025 ...]`). As palavras-chave estão em inglês, português e chinês, para que a avaliação funcione
em qualquer idioma de revisão. Cada problema conta para no máximo um esperado.

Problemas que não correspondem a nada só contam como **falso positivo** se forem `Medium` ou mais graves.
Achados `Low`/`Info` extras (estilo, nomes) são tratados como ruído aceitável.

## Casos

| Caso | O que testa |
|---|---|
| `sql-injection` | SQL montado por concatenação |
| `hardcoded-secret` | Chave de API de produção no código |
| `sync-over-async` | `.Result` em controller |
| `n-plus-one-query` | Consulta ao banco dentro de laço |
| `path-traversal` | Nome de arquivo da URL usado sem validação |
| `insecure-deserialization` | `TypeNameHandling.All` |
| `weak-password-hash` | MD5 para senhas |
| `idor-missing-ownership-check` | Recurso retornado sem checar o dono |
| `swallowed-exception` | `catch` vazio |
| `sql-injection-and-password-logging` | Dois problemas no mesmo arquivo |
| `clean-parameterized-query` | Mudança segura: não deve gerar alarmes |
| `clean-documentation` | Renomeações e docs: não deve gerar alarmes |

## Adicionando um caso

1. Crie `evals/cases/<nome-do-caso>/`.
2. Gere o `diff.patch` com o git, para que os cabeçalhos dos hunks fiquem corretos:
   ```bash
   git diff main -- caminho/do/Arquivo.cs > evals/cases/<nome-do-caso>/diff.patch
   ```
3. Escreva o `expected.json`:
   ```json
   {
     "description": "O que o caso testa.",
     "shouldBlock": true,
     "findings": [
       {
         "file": "src/Api/Controller.cs",
         "line": 14,
         "tolerance": 2,
         "keywords": ["traversal", "path", "caminho", "路径"],
         "owasp": ["A01"],
         "minSeverity": "High"
       }
     ]
   }
   ```
   `line` é a linha na **nova** versão do arquivo. `shouldBlock` deve ser `true` se e somente se algum problema
   tiver `minSeverity` `High` ou `Critical`.
4. Rode `dotnet test`: `Every_labeled_case_in_the_dataset_is_consistent` confere se o arquivo e a linha existem no diff.
