# ADR 0005: Read-only MCP tools confined to the repository

**Status:** accepted

## Context

Looking only at the diff, agents make mistakes for lack of context: they do not know whether the input is validated
elsewhere or what the project conventions are. Giving them access to the repository solves this, but the content of a
pull request is controlled by whoever opened it and may try to steer the model (prompt injection) into reading secrets.

## Decision

- The tools live in a separate **MCP server** (`CodeReviewCrew.McpServer`) speaking stdio. This reuses the standard
  protocol and lets other servers be plugged in through `.crc.yml`.
- The tools are **read-only** and `RepositoryReader` only reads inside the root: it refuses absolute paths, `..`,
  symbolic links, `.git` and secret files (`.env`, keys, `secrets.json`...).
- MCP processes **do not inherit environment variables**; they only receive `PATH`, `HOME` and similar, plus the
  configured `env`.
- The agents' instructions treat the diff and files as data, never as instructions.
- Only the specialists get tools; the synthesizer works on their findings.

## Consequences

- Reviews with more context and fewer false positives, at the cost of more tokens per tool call.
- Access is opt-in in the CLI (`--repo-path`) and on by default in the GitHub Action (`read-repository`).
- Every new tool must follow the same restrictions and have attack tests.
