# ADR-0001 — Phase 1: Domain entity migration strategy

**Status:** Accepted
**Date:** 2026-08-27
**Phase:** 1 — Domain consolidation
**Source:** `docs/architecture/implementation-reconnaissance.md`

## Context

The reconnaissance report (`docs/architecture/implementation-reconnaissance.md` §18, §19) identifies that business entities currently live in three locations:

1. `HalalChain.Platform.Api/Persistence/Entities/` — the core EF entities (`Product`, `Vendor`, `Order`, `Certificate`, etc.).
2. `HalalChain.Platform.Contracts/Catalog/` — taxonomy (`Department`, `Category`, `Subcategory`, `ProductType`) and reference data (`CertificationBody`, `Facility`, `Brand`, `Country`), declared as DTO-looking types but actually used as EF entities.
3. `HalalChain.Web/Models/` — Blazor view-models named identically to entities (`Product`, `Vendor`, `Order`, etc.).

The migration target is: **one authoritative `HalalChain.Domain` aggregate per business concept**; all other layers project the aggregate.

The single existing EF migration (`HalalChain.Platform.Api/Migrations/20260826191514_InitialSchema.cs`) binds every column and FK to a fully-qualified type name. Changing those type names without a coordinated migration will cause a model-snapshot mismatch that crashes `db.Database.MigrateAsync()` at startup.

The master implementation prompt forbids:
- Deleting the migration blindly.
- Wholesale entity moves in one commit.
- Domain → Contracts or Domain → EF Core references.

## Decision

The Catalog entity move will be performed as a sequence of **additive migrations**, one bounded context at a time:

1. **Phase 1a — Create Domain skeleton + marker types.** DONE. `HalalChain.Domain` exists with `IAggregateRoot`. No entity types yet.
2. **Phase 1b — Move a single, self-contained concept first.** The recommended first concept is `ProductEmbedding` (a single-field entity) or `OutboxMessage` (a single-table persistence concept) to validate the move pattern end-to-end before attempting the larger `Product`/`Vendor` moves. These moves are backed by an **additive EF migration** that drops the old `DbSet` and re-adds it with a new fully-qualified type name. Schema is preserved; only the CLR type identity changes.
3. **Phase 1c — Move the legacy `Category` collision out of the way.** Rename `HalalChain.Platform.Api.Persistence.Entities.Category` → `HalalChain.Platform.Api.Persistence.Entities.LegacyCategory` (or move to Domain as `LegacyCategory`). This is the precondition for moving the new `Category` cleanly.
4. **Phase 1d — Move taxonomy + reference data** (`Department`, `Category`, `Subcategory`, `ProductType`, `CertificationBody`, `Facility`, `Brand`, `Country`, `ProductVariant`) into `HalalChain.Domain.Catalog`. Backed by additive migration. Old Contracts types remain as `[Obsolete]` type aliases (using `public sealed class Category : Domain.Catalog.Category {}` or just `using` aliases in consumers) for one release.
5. **Phase 1e — Move `Vendor` and `Product` into `HalalChain.Domain.Catalog`.** These are the highest-coupling moves (referenced by every other entity). They are the last catalog moves and require the most callers to be updated.
6. **Phase 1f — Remove `[Obsolete]` Contracts aliases after all callers are migrated.**

Each phase lands with:
- `dotnet build` clean.
- `dotnet test` green (39 platform + 16 mcp + architecture).
- A new architecture test that prevents the moved type from leaking back into its old location.

## Consequences

- The `HalalChain.Platform.Api/Persistence/Entities/` folder will be emptied in stages, not in one commit. Each removal is reversible via git.
- The Contracts project will shrink over time, but will keep a small set of `[Obsolete]` type aliases for one release to avoid breaking external consumers.
- The `InitialSchema` migration is **never edited or deleted**. New additive migrations extend the model.
- The legacy `Category` (self-referential) is removed by **renaming the DbSet mapping** to `LegacyCategories` and pointing the new domain `Category` at the taxonomy table — the data is preserved, the code is split.

## Alternatives considered

- **Single big-bang entity move + migration regeneration.** Rejected. Loses the InitialSchema and forces a one-shot data migration in production.
- **Keep Contracts types as-is and create Domain types as wrappers.** Rejected. Duplicates the business concept (violates Rule 6 of the master prompt) and forces every consumer to choose between two equivalent types.
- **Move to Domain immediately without a new migration, by changing the CLR type name in place.** Rejected. The model snapshot will go red on `db.Database.MigrateAsync()` and the build will fail in docker compose.

## Followups

- ADR-0002 will cover the Application layer (MediatR) and CQRS conventions.
- ADR-0003 will cover the Infrastructure layer's role relative to the existing API project.
