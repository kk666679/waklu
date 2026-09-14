# Local development

## Prerequisites

- .NET 10 SDK (`global.json` pins `10.0.200`, `rollForward: latestFeature`)
- Node.js ≥ 22 (CLI under `HalalChain-Cli/`)
- Python ≥ 3.11 (services under `.halalchain/`)
- Docker Desktop (optional, for full stack)

## Secrets

`appsettings.json` files contain no secrets. Configure before starting:

```bash
dotnet user-secrets set "Jwt:Key" "<32-char-minimum-secret>" --project HalalChain.Platform.Api
dotnet user-secrets set "Jwt:Key" "<32-char-minimum-secret>" --project HalalChain.Web
```

For Docker Compose, copy `.env.example` to `.env` and set `JWT__KEY`.

## Run locally (separate terminals)

```bash
# 1. AI inference gateway
cd .halalchain/ai-inference
pip install -r requirements.txt
uvicorn src.main:app --reload          # production FastAPI
# (optional) node src/server.js        # Node-based smoke harness

# 2. Platform API
cd HalalChain.Platform.Api
dotnet user-secrets set "Jwt:Key" "<secret>"
dotnet run

# 3. Marketplace
cd HalalChain.Marketplace
dotnet run

# 4. Blazor frontend
cd HalalChain.Web
dotnet run

# 5. Tawheed (optional)
cd .halalchain/tawheed
pip install -r requirements.txt
uvicorn src.main:app --reload
```

## Run with Docker Compose

```bash
cp .env.example .env   # set JWT__KEY
docker compose up --build
```

| Service        | URL                              |
|----------------|----------------------------------|
| Platform API   | http://localhost:5001/swagger    |
| Blazor (Web)   | http://localhost:5200            |
| Marketplace    | http://localhost:5201            |
| Tawheed        | http://localhost:8000/docs       |
| AI Inference   | http://localhost:7071/health     |

## Port Configuration

Application-owned ports are centrally documented here. Infrastructure ports
(PostgreSQL, Redis, Neo4j, Qdrant) are defined in `service-manifest.yaml` and
`docker-compose.yml` and should not be changed without understanding the full
stack impact.

### Local development (`dotnet run`)

| Component    | HTTP Port | HTTPS Port | Source of truth                |
|--------------|-----------|------------|--------------------------------|
| Platform API | 5001      | 5003       | `Properties/launchSettings.json` |
| HalalChain   | 5200      | 5201       | `Properties/launchSettings.json` |
| Marketplace  | 5201      | —          | Docker Compose host mapping    |

### Docker Compose

| Component    | Host Port | Container Port | Source of truth     |
|--------------|-----------|----------------|---------------------|
| Platform API | 5001      | 8080           | `docker-compose.yml` |
| HalalChain   | 5200      | 8080           | `docker-compose.yml` |
| Marketplace  | 5201      | 8080           | `docker-compose.yml` |

### Overriding ports

Prefer environment variables over editing configuration files:

```bash
# API — override HTTP/HTTPS ports
ASPNETCORE_HTTP_PORTS=5001 ASPNETCORE_HTTPS_PORTS=5003 dotnet run --project HalalChain.Platform.Api

# Frontends — override via launch profile or ASPNETCORE_URLS
ASPNETCORE_URLS=http://localhost:5200 dotnet run --project HalalChain.Web
```

### Port conflict diagnostics

If a port is already in use, the application fails with a clear error rather
than silently selecting another port.

**Linux / macOS:**

```bash
lsof -i :5001
# or
ss -ltnp | grep 5001
```

**Windows (PowerShell):**

```powershell
Get-NetTCPConnection -LocalPort 5001
# or
netstat -ano | findstr :5001
```

### Infrastructure ports (do not change)

| Service  | Port | Protocol | Purpose                          |
|----------|------|----------|----------------------------------|
| Postgres | 5432 | TCP      | Primary relational database      |
| Redis    | 6379 | TCP      | Cache, sessions, distributed locks |
| Neo4j    | 7474 | HTTP     | Graph browser (planned)          |
| Neo4j    | 7687 | Bolt     | Graph protocol (planned)         |
| Qdrant   | 6333 | HTTP     | Vector embeddings                |
| Qdrant   | 6334 | gRPC     | Vector protocol                  |

### Development overlay ports (opt-in with `--profile chain`)

| Service  | Host Port | Container Port | Purpose                          |
|----------|-----------|----------------|----------------------------------|
| Kubo     | 5011      | 5001           | IPFS RPC API                     |
| Kubo     | 8081      | 8080           | IPFS Gateway                     |
| Anvil    | 8545      | 8545           | Polygon Amoy fork RPC            |

> **Note:** The dev overlay host ports (5011, 8081) differ from the container
> ports (5001, 8080) to avoid collisions with the Platform API host port 5001
> and the .NET container port 8080.

## CLI helper

```bash
cd HalalChain-Cli
npm install
node bin/halalchain.js config init     # interactive setup
node bin/halalchain.js env generate    # write .env files
node bin/halalchain.js env check       # ping all services
```
