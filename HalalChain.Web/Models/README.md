# HalalChain.Web — domain models

This directory contains the Blazor WebApp's local view-model types. Many
of them predate the canonical DTOs in
`HalalChain.Platform.Contracts` and have divergent shapes (e.g. `int Id`
vs `Guid Id`, `int ProductId` vs `Guid ProductId`). They are still
actively referenced by the Blazor components in `Components/`.

The long-term direction is to migrate every consumer to the contracts
DTOs (see `docs/runbooks/`). Until that migration is complete, treat the
types in this folder as **demo/UI-only** types and do not depend on them
from cross-service code paths.

Audit summary:

- 18 files are still consumed by the Blazor components and must not be
  deleted blindly (see individual `*.cs` files for the contracts DTO that
  they should eventually be replaced with).
- No file in this folder is genuinely unused today.
