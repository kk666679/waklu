# `.autoclaw/governance/`

State machines. Not policies-as-prose, not checklists — actual machines, with
declared states, declared transitions, named guards, and invariants that no
role and no scheme override can relax.

## Why machines

`CONTROLS.yaml` lists controls with an `enforced_by`. Where that says
`process:`, this is why: those controls depend on a human applying the machine
correctly. Everything the machine can check itself, it checks itself. A
transition that names a guard which does not exist is a transition nobody can
audit, so
`AutoclawStructureTests.Certification_Guards_Are_Documented` fails the build on
it.

## Certification

`certification/transitions.yaml` — thirteen transitions across thirteen states,
four invariants, and a small set of named guards defined in
`certification/guards.md`.

The invariants are the part worth reading:

- `consultant-cannot-certify` — reaching Certified, Rejected, Suspended or
  Renewed implies the actor is a certification body. Consultants prepare;
  they do not conclude.
- `ai-never-authoritative` — an authoritative transition implies the actor is
  not an `ai-agent`. There is no configuration that relaxes this.
- `tawheed-required-pre-authority` — reaching Certified or Renewed implies a
  recorded tawheed evaluation. The machine does not accept an assertion that
  one happened.
- `audit-immutable-on-authority` — an authoritative transition implies an
  immutable audit record.

Scheme overrides under `certification/schemes/` may add state restrictions and
transition guards. They may not widen `allowed_roles` and may not touch
`invariants` — a scheme that could relax `ai-never-authoritative` would be a
scheme that lets a model certify itself.

## Skills

`skills/transitions.yaml` plus five checks under `skills/checks/`: manifest
validation, capability review, static security, no-verdict scan, dependency
review. A skill that has not passed all five is in `skills/staging/` and is
inert. `skills/published/` is readable and executable;
`skills/suspended/` is retained for forensics and is not executable.

## Audits and controls

`audits/` — scheduling, findings, corrective actions. `controls/` —
separation of duties and delegation limits, the two constraints that do not
belong to any single machine because they span them.
