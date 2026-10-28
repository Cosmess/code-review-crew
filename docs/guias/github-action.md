# Usando a GitHub Action

## Configuração mínima

1. Crie o secret `OPENAI_API_KEY` no repositório (Settings → Secrets and variables → Actions).
2. Crie `.github/workflows/code-review.yml`:

```yaml
name: Code Review Crew

on:
  pull_request:
    types: [opened, synchronize, reopened, ready_for_review]

permissions:
  contents: read
  pull-requests: write

jobs:
  review:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: Cosmess/code-review-crew@main
        with:
          openai-api-key: ${{ secrets.OPENAI_API_KEY }}
```

O `checkout` é necessário para a action encontrar o `.crc.yml` do seu repositório.

## Inputs

| Input | Padrão | Descrição |
|---|---|---|
| `openai-api-key` | obrigatório | Chave da API da OpenAI |
| `github-token` | `github.token` | Token para ler o PR e publicar a revisão |
| `model` | `gpt-5-mini` | Modelo da OpenAI |
| `config` | `.crc.yml` | Arquivo de configuração (ignorado se não existir) |
| `post-review` | `true` | Publica a revisão no PR, com comentários inline |
| `sarif-file` | vazio | Caminho para salvar o SARIF |
| `fail-on` | vazio | Falha o job se houver problema desta severidade ou maior |

## Alertas na aba Security (SARIF)

```yaml
permissions:
  contents: read
  pull-requests: write
  security-events: write

    steps:
      - uses: actions/checkout@v4
      - uses: Cosmess/code-review-crew@main
        with:
          openai-api-key: ${{ secrets.OPENAI_API_KEY }}
          sarif-file: code-review-crew.sarif
      - uses: github/codeql-action/upload-sarif@v4
        if: always() && hashFiles('code-review-crew.sarif') != ''
        with:
          sarif_file: code-review-crew.sarif
          category: code-review-crew
```

## Bloquear o merge

Com `fail-on: High`, o job falha quando há problema High ou Critical. Marque o job como obrigatório em
Settings → Branches para impedir o merge. A revisão é publicada antes da falha.

## Observações

- A revisão é publicada como `COMMENT`, que funciona com o `GITHUB_TOKEN` padrão e não bloqueia o PR sozinha.
- PRs vindos de forks não recebem secrets; trate esse caso como no workflow deste repositório.
- Para fixar uma versão, use uma tag ou um SHA no lugar de `@main`.
