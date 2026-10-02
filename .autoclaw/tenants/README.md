# `.autoclaw/tenants/` — tenancy as a first-class domain

Formerly a `tenant_id` column. Promoting it to a domain is the point: an
isolation model that lives in a comment is an isolation model that erodes.

Every layer that stores or caches tenant-scoped data has exactly one isolation
key, and there is no layer without one. `contracts/tenancy.schema.json` is a
published contract for that reason, not a table comment.

## Layout

```
model.yaml          the isolation key for every layer, and the fail-closed rules
schemas/            tenant.schema.json — the published tenancy contract
policies/           cross-tenant-access.yaml, data-residency.yaml
isolation/          per-layer enforcement notes
```

## The keys

| Layer      | Key                                        |
|------------|--------------------------------------------|
| `database` | `tenant_id` (row-level, EF global query filter) |
| `cache`    | key prefix `t:{tenant_id}:`                |
| `blob`     | path prefix `/tenants/{tenant_id}/`        |
| `search`   | index-per-tenant, or filter-by-`tenant_id` as fallback |
| `mcp`      | request header `X-Tenant-Id`, validated against the auth claim |

## Absent tenant is a denial

There is no shared-tenant fallback. A query with no tenant filter must throw,
not widen. That applies to reads and writes alike, and to joins: a join across
tenants leaks even when every row is individually authorised, so aggregation
that spans tenants goes through the compliance-zone route rather than a broader
query.

## The query filter is silent, and that is the risk

`model.yaml` → `isolation_keys.database` is enforced by an EF global query
filter. It is silent by design, and that is exactly the hazard: a forgotten
`Where` clause does not fail loudly, it returns the tenant's own rows and looks
correct.

`HalalChain.Architecture.Tests.Rules.AutoclawStructureTests.Tenant_Isolation_Keys_Are_Declared`
fails the build when a layer loses its key. `Cross_Tenant_Read_Is_Denied` exists
to catch the forgotten `Where`. Both must stay green; the second is the one that
covers the first's blind spot.

## Fail-closed

Tenant-context resolution failure, cache-key construction failure, index
selection failure, and blob-path construction failure all deny. The one
documented exception is a cache-key construction failure, which bypasses the
cache rather than falling back to an unscoped key — an unscoped key is a
cross-tenant read with extra steps.

## Residency

`policies/data-residency.yaml` decides where a tenant's data may sit.
Infrastructure is shared-with-prefix by default; a dedicated deployment is
opt-in per `deploy/compliance-zones/` for tenants whose residency obligations
or scale justify the isolation cost.