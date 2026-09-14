# `infrastructure/`

Dev-only Docker Compose and helper scripts for spinning up the local
infrastructure that the .NET services expect (Postgres, IPFS, an Anvil
Polygon fork).

## Layout

```
infrastructure/
├── docker-compose.dev.yml  # Postgres + IPFS + optional Anvil (--profile chain)
└── scripts/
    └── setup-dev.sh        # One-shot bootstrap (Foundry install, tests, deploys, compose)
```

## Usage

The dev compose file is intentionally separate from the root
`docker-compose.yml`. It exposes Postgres and IPFS on host ports for direct
access, and it only starts the local Polygon fork when invoked with the
`chain` profile:

```bash
docker compose -f infrastructure/docker-compose.dev.yml up
docker compose -f infrastructure/docker-compose.dev.yml --profile chain up
```

The full local stack (API + UIs + Python services + all infra) is defined at
the root `docker-compose.yml`.
