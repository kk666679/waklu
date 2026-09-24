# Reference Layer

Canonical, versioned, read-only ground truth for HalalChain.
Agents and skills MUST cite reference IDs; they MUST NOT mutate this layer.

## Categories
- standards/           Halal standards (MS 1500, GSO, MUIS, BPJPH HAS, etc.)
- jurisdictions/       Country-specific rules (MY, ID, AE, SA, TR, SG, ...)
- certificate_authorities/  Recognised certification bodies and scopes
- ingredients/         E-codes, animal-derived, alcohol, gelatin, enzymes
- policies/            Proprietary and marketplace rules
- evidence_templates/  Supplier declarations, lab reports, audit checklists
- api_contracts/       .NET OpenAPI, Python AI OpenAPI, marketplace contracts
- data_dictionaries/   Supplier, vendor, product, evidence, certificate fields
- glossary/            Multilingual terms (EN/AR/MS)
- snapshots/           Immutable snapshots used by eval baselines

## Update cadence
- Standards: on regulatory change (JAKIM 5-year cycle, BPJPH phase-in).
- Jurisdictions: monthly review, especially Indonesia mandatory halal Oct 2026.
- API contracts: on every breaking change in .NET/Python services.
