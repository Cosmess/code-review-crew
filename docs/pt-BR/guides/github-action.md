# Usando a GitHub Action

[English](../../en/guides/github-action.md) | **Português** | [简体中文](../../zh-CN/guides/github-action.md)

## Configuração mínima

1. Crie o secret `OPENAI_API_KEY` no repositório (*Settings → Secrets and variables → Actions*).
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
      - uses: Cosmess/code-review-crew@v1
        with:
          openai-api-key: ${{ secrets.OPENAI_API_KEY }}
          language: pt-BR
```

O `actions/checkout` é necessário para a Action encontrar o seu `.crc.yml` e para os agentes lerem o repositório.

## Inputs

| Input | Padrão | Descrição |
|---|---|---|
| `openai-api-key` | obrigatório | Chave da API da OpenAI |
| `github-token` | `github.token` | Token para ler o PR e publicar a revisão |
| `model` | `gpt-5-mini` | Modelo da OpenAI |
| `language` | vazio (usa o `.crc.yml`, ou `en`) | Idioma da revisão: `en`, `pt-BR` ou `zh-CN` |
| `config` | `.crc.yml` | Arquivo de configuração (ignorado se não existir) |
| `post-review` | `true` | Publica a revisão com comentários inline |
| `read-repository` | `true` | Dá aos agentes ferramentas MCP somente leitura sobre o repositório |
| `sarif-file` | vazio | Caminho para salvar o SARIF |
| `fail-on` | vazio | Falha o job com problemas desta severidade ou maior |

## Alertas na aba Security (SARIF)

```yaml
permissions:
  contents: read
  pull-requests: write
  security-events: write

    steps:
      - uses: actions/checkout@v4
      - uses: Cosmess/code-review-crew@v1
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

Com `fail-on: High`, o job falha quando há problema High ou Critical. Marque o job como obrigatório nas regras de
proteção da branch para impedir o merge. A revisão é publicada antes da falha.

## Observações

- A revisão é publicada como `COMMENT`, que funciona com o `GITHUB_TOKEN` padrão e não bloqueia o PR sozinha.
- PRs vindos de forks não recebem secrets; pule a revisão nesse caso, como faz o
  [workflow](../../../.github/workflows/code-review.yml) deste repositório.
- Fixe uma versão (`@v1`, uma tag ou um SHA de commit) em vez de `@main`.
- A Action compila a ferramenta a partir do código a cada execução, o que leva cerca de um minuto.
