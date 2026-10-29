# ADR 0002: Fan-out / fan-in orchestration with a synthesizer

**Status:** accepted

## Context

A single "review everything" prompt mixes concerns and misses issues. Agents chatting freely with each other are
expensive and hard to test.

## Decision

Independent specialists run in parallel over the same diff (**fan-out**) and a synthesizer consolidates the result
(**fan-in**). The orchestration is explicit C# code in `ReviewOrchestrator`, not a chat between agents.

## Consequences

- Each specialist has a focused prompt; adding or disabling agents is simple.
- Latency ≈ slowest specialist + synthesizer.
- The flow is deterministic and testable with fakes.
- Duplicates across specialists are expected; resolving them is the synthesizer's job.
