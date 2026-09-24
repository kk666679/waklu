# Decision contract

## Trust boundary invariant

The AI layer is evidence-only. It may collect, summarize, and score evidence. It may never produce an autonomous decision outcome.

## OMP states

- `AUTONOMOUS`: fully deterministic policy execution, high confidence, and complete evidence set.
- `ASSISTED`: deterministic engine produces a decision but requires human review.
- `ESCALATED`: evidence is insufficient or policy confidence is below the assisted threshold.

## Required invariants

1. Every decision runs through the deterministic engine.
2. An AI-only result can never produce `AUTONOMOUS`.
3. Every decision emits an audit record.
4. Threshold values are configuration-driven and not hardcoded in business logic.
5. The policy engine must be the only component that emits final outcome state.

## Audit record shape

- action
- subject
- details
- timestamp
- signer

These are recorded in the append-only decision trail used by the compliance record.
