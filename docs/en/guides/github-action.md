# Using the GitHub Action

**English** | [Português](../../pt-BR/guides/github-action.md) | [简体中文](../../zh-CN/guides/github-action.md)

## Minimal setup

1. Add the `OPENAI_API_KEY` secret to the repository (*Settings → Secrets and variables → Actions*).
2. Create `.github/workflows/code-review.yml`:

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
```

`actions/checkout` is needed so the Action finds your `.crc.yml` and the agents can read the repository.

## Inputs

| Input | Default | Description |
|---|---|---|
| `openai-api-key` | required | OpenAI API key |
| `github-token` | `github.token` | Token to read the PR and post the review |
| `model` | `gpt-5-mini` | OpenAI model |
| `language` | empty (uses `.crc.yml`, or `en`) | Review language: `en`, `pt-BR` or `zh-CN` |
| `config` | `.crc.yml` | Configuration file (ignored if missing) |
| `post-review` | `true` | Post the review with inline comments |
| `read-repository` | `true` | Give the agents read-only MCP tools over the checked-out repository |
| `sarif-file` | empty | Path to save the SARIF file |
| `fail-on` | empty | Fail the job on findings of this severity or higher |

## Alerts in the Security tab (SARIF)

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

## Blocking the merge

With `fail-on: High`, the job fails when there is a High or Critical finding. Mark the job as required in the
branch protection rules to block the merge. The review is posted before the job fails.

## Notes

- The review is posted as `COMMENT`, which works with the default `GITHUB_TOKEN` and never blocks the PR by itself.
- Pull requests from forks do not receive secrets; skip the review in that case, as this repository's
  [workflow](../../../.github/workflows/code-review.yml) does.
- Pin a version (`@v1`, a tag or a commit SHA) instead of `@main`.

## Runners and startup time

| Runner | How the Action runs | Startup |
|---|---|---|
| Linux (`ubuntu-latest`, amd64 or arm64) with a version tag (`@v1`, `@v1.0.1`) | Pulls the prebuilt image `ghcr.io/cosmess/code-review-crew` | a few seconds |
| Windows, macOS, or a branch / commit SHA | Builds the tool from source (and installs the .NET SDK if the runner lacks it) | about 15 seconds on GitHub-hosted runners |

Both modes behave the same way. The build uses the Action's own folder, so a `global.json` in your repository does not
affect it.
