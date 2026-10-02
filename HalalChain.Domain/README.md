# HalalChain.Domain

The domain model: pure business concepts with no infrastructure attached.

## Purpose

This project holds the nouns and rules of the halal-commerce domain — products,
variants, vendors, orders, certificates, on-chain records, and the enums that
describe halal attributes. It is the bottom of the dependency graph: everything
else may reference it, and it references nothing.

The domain is deliberately **dependency-free**. `HalalChain.Domain.csproj`
contains no `ProjectReference` and no `PackageReference` at all, and a comment in
the project file enumerates what must stay out (EF Core, ASP.NET Core, Radzen,
Infrastructure, Application, the API, UIs, MCP, external provider SDKs).

## Responsibilities

- Define aggregate roots and entities (`Product`, `Vendor`, `Order`, `Certificate`,
  `HalalVerification`, `SupplierOnChain`, …).
- Define value objects and strongly typed identifiers (`ProductId`, `CertificateId`,
  `HalalProfile`, `WholesalePriceTier`, `VerdictBinding`, `StatusTransition`).
- Define the halal attribute enumerations (`HalalStatus`, `AnimalDerivative`,
  `GelatinSource`, `EnzymeSource`, `FermentationMedium`, `DietaryTags`,
  `CommercialChannel`, `AudienceSegment`, `CertificationTrustTier`,
  `CertificationScope`).
- Define the compliance status vocabulary (`ComplianceStatus`, `CertificateStatus`,
  `VerdictState`, `ProductStatus`).
- Provide the `OutboxMessage` aggregate used by the transactional outbox.

## Structure

```
HalalChain.Domain/
├── IAggregateRoot.cs          # Empty marker interface
├── HalalChain.Domain.csproj   # No package or project references
├── Blockchain/                # SupplierOnChain, ProductOnChain, CertificateOnChain,
│                              #   TraceabilityEventOnChain, ChainTxOutbox
├── Catalog/                   # Product, ProductVariant, ProductMediaAsset, Brand,
│                              #   Category, TaxonomyCategory, Subcategory, ProductType,
│                              #   Department, Country, Facility, CertificationBody,
│                              #   ProductAttribute(+Value), ProductEmbedding,
│                              #   ProductSustainability, DynamicPricingRule,
│                              #   HalalProfile, WholesalePriceTier, HalalEnums.cs
├── Commerce/                  # CartItem, Order, OrderItem, VendorOrder, WishlistItem
├── Common/                    # OutboxMessage
├── Halal/                     # Certificate, HalalVerification, VerificationEvidence,
│                              #   VerificationAudit, ProductId, CertificateId,
│                              #   VerdictBinding, StatusTransition, HalalEnums.cs,
│                              #   ValueObjects.cs
└── Vendors/                   # Vendor
```

## Architecture / Flow

```text
HalalChain.Platform.Api ─┐
HalalChain.Application  ─┼─▶ HalalChain.Domain  (entities + value objects, no deps)
HalalChain.Agents       ─┤
HalalChain.DataFlow     ─┤
HalalChain.Marketplace  ─┘
```

Consumers never mutate domain objects from here without going through the
handlers in `HalalChain.Application`; persistence lives in
`HalalChain.Platform.Api/Persistence/`.

## Dependencies

Internal: none by design.

External: none.

Enforced by `HalalChain.Architecture.Tests`:

- `Domain_ShouldNotReference_EFCore_Or_AnyOtherProject`
- `Domain_ShouldNotReference_Storage`
- `MigratedTypes_ShouldResideIn_Domain`
- `MovedEntities_ShouldNotExistIn_ApiPersistenceEntities`

## Interfaces

`IAggregateRoot` is the only interface. It is an empty marker — no members are
required of an aggregate root.

> **Note on duplication.** `HalalStatus`, `ComplianceStatus`, `CertificateStatus`,
> `HalalProfile` and friends are also declared in
> `HalalChain.Platform.Contracts`. The two copies are independent types; there is
> no mapping layer between them. This is a known duplication, recorded in
> [`docs/architecture/tech-debt.md`](../docs/architecture/tech-debt.md).

## Usage

The project is a library and has no entry point. Reference it and use the types:

```csharp
using HalalChain.Domain.Catalog;

var product = new Product
{
    Name = "Muesli",
    HalalStatus = HalalStatus.Halal,
};
```

Build it alone:

```bash
dotnet build HalalChain.Domain/HalalChain.Domain.csproj
```

## Testing

There is no dedicated test project. The domain is covered indirectly:

- `HalalChain.Platform.Tests` uses domain types as seed data in its persistence
  and integration tests.
- `HalalChain.Architecture.Tests` asserts both the dependency rules above and
  that every migrated type lives in the expected `HalalChain.Domain.*` namespace.

## Deployment

Not deployable. It ships only as an assembly referenced by the other .NET
projects.

## Related Components

- [HalalChain.Application](../HalalChain.Application/README.md) — use cases and handlers over these types
- [HalalChain.Platform.Contracts](../HalalChain.Platform.Contracts/README.md) — wire-level DTOs, duplicated for some enums
- [HalalChain.DataFlow](../HalalChain.DataFlow/README.md) — normalises external records into `CanonicalHalalRecord`
- [HalalChain.Architecture.Tests](../HalalChain.Architecture.Tests/README.md) — enforces the zero-dependency rule

## Notes / Limitations

- The project is not independently testable in isolation; there is no
  `HalalChain.Domain.Tests`.
- Duplicate enum definitions between Domain and Contracts will not compare
  equal and cannot be assigned to one another.