# HalalChain Service Manifest

Documents all services, their ports, dependencies, and cross-service communication.

## Services

### .NET Services (Docker)

| Service | Port (Container) | Port (Host) | Health Check | Depends On |
|---------|------------------|-------------|--------------|------------|
| platform-api | 8080 | 5001 | /health/live | ai-inference, postgres, redis, local-models |
| marketplace | 8080 | 5201 | /health/live | platform-api |
| halalchain (Web) | 8080 | 5200 | /health/live | platform-api |

### Python Services (FastAPI)

| Service | Port (Container) | Port (Host) | Health Check | Depends On |
|---------|------------------|-------------|--------------|------------|
| ai-inference | 7071 | 7071 | /health/live | redis, qdrant, local-models |
| tawheed | 8000 | 8000 | /health | postgres, redis, neo4j |
| local-models | 8080 | 8080 | /health/live | redis |

### Infrastructure

| Service | Port (Container) | Port (Host) | Purpose |
|---------|------------------|-------------|---------|
| postgres | 5432 | - | Primary database |
| redis | 6379 | 6379 | Caching, sessions |
| neo4j | 7474/7687 | 7474/7687 | Knowledge graph |
| qdrant | 6333/6334 | 6333/6334 | Vector database |

## Cross-Service Communication

### platform-api → Python Services
- **AiGateway__BaseUrl**: `http://ai-inference:7071`
- **AiGateway__ApiKey**: From `AI_GATEWAY_API_KEY` env var
- **Tawheed__BaseUrl**: `http://tawheed:8000`
- **LocalModels__BaseUrl**: `http://local-models:8080`

### ai-inference → Other Services
- **LOCAL_MODELS_URL**: `http://local-models:8080`
- **TAWHEED_URL**: `http://tawheed:8000`
- **AI_GATEWAY_API_KEY**: Shared auth key
- **REDIS_URL**: `redis://redis:6379/0`
- **QDRANT_HOST**: `qdrant` (port 6333)

### tawheed → Other Services
- **LOCAL_MODELS_URL**: `http://local-models:8080`
- **AI_INFERENCE_URL**: `http://ai-inference:7071`
- **POSTGRES_URL**: From env
- **REDIS_URL**: From env
- **NEO4J_URI**: `bolt://neo4j:7687`

### local-models → Infrastructure
- **REDIS_URL**: `redis://redis:6379/0`
- **TRANSFORMERS_CACHE**: `/models`
- **HF_HOME**: `/models`

## Service Clients (Shared)

All cross-service communication uses typed clients in `halalchain_shared.service_client`:

- `AIInferenceClient` - embeddings, classify, summarize, rerank, LLM generate, RAG
- `TawheedClient` - policy evaluation, evidence query
- `LocalModelsClient` - embeddings, classify, generate
- Factory functions: `create_ai_inference_client()`, `create_tawheed_client()`, `create_local_models_client()`

Environment variables for client creation:
- `AI_INFERENCE_URL` + `AI_GATEWAY_API_KEY`
- `TAWHEED_URL` + `TAWHEED_API_KEY`
- `LOCAL_MODELS_URL` + `LOCAL_MODELS_API_KEY`

## Authentication

All Python services use `X-API-Key` header for authentication:
- `AI_GATEWAY_API_KEY` - shared secret for ai-inference
- Services validate API key at startup (fail-closed in non-dev)

## CORS Configuration

All services use explicit allow-list origins (no wildcards with credentials):
- Development defaults: `http://localhost:5001,http://localhost:5200,http://localhost:5201,http://localhost:7071,http://localhost:8000,http://localhost:8080`
- Production: MUST set via `*_ALLOWED_ORIGINS` env vars

## Health Check Dependencies

Services use `depends_on` with `condition: service_healthy` to ensure proper startup order:
1. Infrastructure (postgres, redis, neo4j, qdrant) → healthy
2. local-models → healthy
3. ai-inference → healthy (depends on redis, qdrant, local-models)
4. tawheed → healthy (depends on postgres, redis, neo4j)
5. platform-api → healthy (depends on ai-inference, postgres, redis, local-models)
6. marketplace, halalchain → healthy (depends on platform-api)

## Environment Files

- `.env` - Local development overrides
- `.env.example` - Template with all required variables
- Docker Compose uses `${VAR:-default}` syntax for optional vars with defaults
- Required vars in production use `${VAR:?error message}` syntax to fail fast