# ADR 0004: Post the review as COMMENT, with inline comments

**Status:** accepted

## Context

The GitHub reviews API accepts `APPROVE`, `REQUEST_CHANGES` and `COMMENT`. The default `GITHUB_TOKEN` in Actions cannot
approve pull requests, and a bot requesting changes blocks the flow in a way people cannot easily dismiss.

## Decision

Always post as `COMMENT`. The verdict appears in the text. Repositories that want to block merges use `--fail-on`
(or the `fail-on` input) with the job marked as required.

Findings on lines that are part of the diff become inline comments (up to 30, most severe first); the rest go to the
body. If GitHub rejects a comment (422), the review is posted again with the body only.

## Consequences

- Works without extra permissions or personal tokens.
- Blocking merges is an explicit choice of the repository.
