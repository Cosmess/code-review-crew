# ADR 0003: An agent failure never takes the review down

**Status:** accepted

## Context

LLM calls fail because of timeouts, rate limits or invalid JSON. In a pull request pipeline, failing the whole review
because of one agent is worse than delivering a partial review and saying so.

## Decision

- Every call goes through `RetryPolicy` (per-attempt timeout + exponential backoff).
- When the attempts run out, the specialist is marked with `Error` and the others continue.
- If the synthesizer fails, the findings are merged without it.
- The review shows which agents failed.

## Consequences

- Reviews can be partial; the Markdown makes that explicit.
- No dependency on Polly: the policy is small and tested directly.
