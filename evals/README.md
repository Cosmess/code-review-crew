# Avaliação

Conjunto de pull requests rotulados para medir a qualidade das revisões.
Cada caso tem o diff e os problemas que uma boa revisão precisa encontrar.

## Rodando

```bash
export OPENAI_API_KEY=sk-...
dotnet run --project src/CodeReviewCrew.Evals                         # todos os casos
dotnet run --project src/CodeReviewCrew.Evals -- --filtro sql         # só os casos com "sql" no nome
dotnet run --project src/CodeReviewCrew.Evals -- --output relatorio.md --min-recall 0.8
```

No GitHub, rode o workflow **Avaliação** em Actions → Avaliação → Run workflow. O relatório aparece no
resumo da execução e como artefato.

## Métricas

| Métrica | Como é calculada |
|---|---|
| **Recall** | Problemas esperados encontrados / problemas esperados |
| **Precisão** | Acertos / (acertos + falsos positivos) |
| **F1** | Média harmônica de precisão e recall |
| **Vereditos corretos** | `RequestChanges` quando o caso deve bloquear, outro veredito quando não deve |
| **Severidade subestimada** | Problemas encontrados com severidade abaixo da mínima esperada |

Um problema da revisão **corresponde** a um esperado quando está no mesmo arquivo, a até `tolerancia`
linhas de distância, e cita uma das `palavrasChave` (sem diferenciar maiúsculas nem acentos) ou uma das
categorias `owasp` no título (ex.: `[A05:2025 ...]`). Cada problema conta para no máximo um esperado.

Problemas que não correspondem a nada só contam como **falso positivo** se forem `Medium` ou mais graves.
Achados `Low`/`Info` extras (estilo, nomes) são tratados como ruído aceitável.

## Casos

| Caso | O que testa |
|---|---|
| `sql-injection` | SQL montado por concatenação |
| `segredo-no-codigo` | Chave de API de produção no código |
| `sync-over-async` | `.Result` em controller |
| `consulta-n-mais-1` | Consulta ao banco dentro de laço |
| `path-traversal` | Nome de arquivo da URL usado sem validação |
| `desserializacao-insegura` | `TypeNameHandling.All` |
| `hash-de-senha-fraco` | MD5 para senhas |
| `idor-sem-checagem-de-dono` | Recurso retornado sem checar o dono |
| `excecao-engolida` | `catch` vazio |
| `sql-injection-com-log-de-senha` | Dois problemas no mesmo arquivo |
| `limpo-consulta-parametrizada` | Mudança segura: não deve gerar alarmes |
| `limpo-documentacao` | Refatoração de nomes e docs: não deve gerar alarmes |

## Adicionando um caso

1. Crie `evals/casos/<nome-do-caso>/`.
2. Gere o `diff.patch` com o git, para que os cabeçalhos dos hunks fiquem corretos:
   ```bash
   git diff main -- caminho/do/arquivo.cs > evals/casos/<nome-do-caso>/diff.patch
   ```
3. Escreva o `esperado.json`:
   ```json
   {
     "descricao": "O que o caso testa.",
     "deveBloquear": true,
     "problemas": [
       {
         "arquivo": "src/Api/Controller.cs",
         "linha": 14,
         "tolerancia": 2,
         "palavrasChave": ["traversal", "caminho"],
         "owasp": ["A01"],
         "severidadeMinima": "High"
       }
     ]
   }
   ```
   `linha` é a linha na **nova** versão do arquivo. `deveBloquear` deve ser `true` se e somente se
   algum problema tiver `severidadeMinima` `High` ou `Critical`.
4. Rode `dotnet test`: o teste `Every_labeled_case_in_the_dataset_is_consistent` confere se o arquivo e a
   linha existem no diff.
