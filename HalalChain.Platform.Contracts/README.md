# HalalChain.Platform.Contracts

Shared DTOs, request/response shapes, enums, and Solidity sources consumed by
every other .NET project in the solution.

## Layout

```
HalalChain.Platform.Contracts/
├── AI/                 # AI gateway request/response DTOs
├── Api/Errors/         # Canonical API error DTOs
├── Auth/               # AuthConstants, AuthDtos, JwtSettings
├── Catalog/            # Product / halal-profile DTOs, enums, taxonomy
├── Commerce/           # Cart, order, checkout DTOs and requests
├── Halal/              # Halal evidence DTOs and requests
├── Vendors/            # Vendor profile DTOs and requests
└── contracts/          # Solidity (Foundry) — see contracts/README.md
    ├── foundry.toml
    ├── script/         # Deployment scripts
    ├── src/            # HalalAccessControl, registries
    └── test/           # Forge test suite
```

The `contracts/` subfolder has its own dedicated README at
`contracts/README.md` covering the Solidity work.

## Dependencies

This project intentionally has no NuGet dependencies — it is consumed by every
other project and must remain side-effect free.

## Build

```bash
dotnet build HalalChain.Platform.Contracts
```
