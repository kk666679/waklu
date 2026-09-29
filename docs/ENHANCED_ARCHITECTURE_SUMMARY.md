# Enhanced .halalchain/ Architecture - Complete Cross-Service Integration

## Overview

This document describes the comprehensive enhancement of the `.halalchain/` directory to establish full integration across all HalalChain services (Python, Node CLI, and Docker compose) with proper dependency management, cross-service communication, and architectural guardrails.

## New Services Added

### 1. .halalchain/agents/
**Purpose**: Agent orchestration and evaluation service with DAG-based workflow scoring and CI/CD integration

**Structure**:
- `app/runtime/` - Runtime agent execution
- `app/agents/` - Individual agent implementations (collector, classifier, verifier, handoff, gap, recollection, verdict)
- `app/workflows/` - Workflow definitions
- `app/eval/` - Evaluation suite with DAG-based testing

**Key Features**:
- **DAG Evaluation**: Greedy-parent root cause attribution for agent failures
- **Failure Taxonomy**: 21 standardized failure categories
- **CI Integration**: Pytest-native, fails build on regression
- **Shadow Mode**: Periodic production trace scoring without affecting requests
- **DeepEval Integration**: Advanced LLM evaluation metrics

**pyproject.toml**:
```toml
[project.optional-dependencies]
runtime = ["mcp>=2.1.1,<3.0.0", "mcp-types>=2.1.1,<3.0.0", "haiku-skills[signing]>=0.18.1", "agent-skills>=0.5.0", "qdrant-client[fastembed]>=1.15"]
eval = ["deepeval>=4.2.0,<5.0.0"]
```

**Requirements Structure**:
- `requirements/agents.txt` - Runtime dependencies
- `requirements/eval.txt` - Evaluation dependencies

## 2. .halalchain/local-models/
**Purpose**: Local model inference service for embeddings, classification, and generation

**Structure**:
- `Dockerfile` - Multi-stage build with CUDA support options
- `pyproject.toml` - Source of truth with optional extras (inference, quantization, gguf, optimized)
- `requirements.txt` - Pointer to lock file
- `src/main.py` - FastAPI service with local model loading
- Test files in `ai-inference/tests/test_cross_service.py`

**Key Features**:
- **GPU Support**: Optional CUDA dependencies (inference, quantization, optimized)
- **Local Models**: Direct model serving via transformers, torch, etc.
- **Cross-Service Ready**: Complements gateway services with local inference
- **FastAPI**: Production-ready service with health checks

## 3. Enhanced .halalchain/requirements/ Directory

**Lock Files**:
- `requirements/agents.txt` - Agent runtime dependencies
- `requirements/ai-inference.txt` - ai-inference runtime
- `requirements/dev.txt` - Development tools
- `requirements/local-models.txt` - Local model inference
- `requirements/tawheed.txt` - tawheed runtime

**CI Enforcement**: `.github/workflows/python-locks.yml` validates all lock files

## 4. Enhanced HALALChain-Cli/

**New Commands**:
- `halalchain cross` - Cross-service orchestration and health checking
  - `halalchain cross health` - Check all service health
  - `halalchain cross orchestration` - Demonstrate cross-service workflows
  - `halalchain cross embedding-orchestration` - Embedding workflow with verification
  - `halalchain cross local-models-test` - Test local-models service

**Enhanced Client Library** (`lib/api-client.js`):
- `APIClient` - For ai-inference service
- `TawheedClient` - For tawheed service
- `LocalModelsClient` - For local-models service
- Factory functions: `createAIInferenceClient()`, `createTawheedClient()`, `createLocalModelsClient()`

**Enhanced Configuration** (`lib/store.js`):
- Added `local-models.url` and `local-models.api-key` to defaults
- Added `tawheed.api-key` to defaults
- Updated `KNOWN_KEYS` to include new config keys

**Service Integration** (`commands/config.js`):
- Added `show-services` command to display all service URLs and API keys
- Config validation includes all service endpoints

## Cross-Service Architecture

### Docker Compose Enhancements (`docker-compose.yml`):
- Added `local-models` service with Redis dependency
- Updated `ai-inference` to depend on `local-models`
- Updated `tawheed` to expose cross-service URLs
- Added `LocalModels__BaseUrl` to platform-api environment

### Service Discovery Patterns:
1. **Gateway Pattern**: `ai-inference` acts as primary gateway
2. **Orchestration Pattern**: `tawheed` coordinates evidence collection
3. **Local Inference Pattern**: `local-models` provides direct model access
4. **Client Library Pattern**: Unified client APIs for all services

### Cross-Service Communication:
- **HTTP Clients**: All services communicate via HTTP clients
- **API Keys**: Service-to-service authentication with `X-API-Key` headers
- **CORS Configuration**: Explicit allow-list origins across all services
- **Health Checks**: Inter-service health monitoring and dependency management

## Architecture Guardrails

### C# Architecture Tests (`HalalChain.Architecture.Tests`):
- **Existing Rules**: Application layer constraints, domain isolation, UI project dependencies
- **New Rules**: 
  - `EvaluationArchitectureTests.cs` - Validates evaluation DAG constraints
  - Enforces no `Verdict` field in `EvalResult`
  - Validates no RAGAS dependency
  - Confirms `eval` extra not in production images

### Python Linting/Validation:
- **CI Workflow**: Comprehensive lock file validation
- **Type Safety**: Optional dependency validation
- **Security**: Hash pinning for all dependencies

## Key Improvements

### 1. Source of Truth
- `pyproject.toml` in each service is the single source of truth
- `requirements/*.txt` are generated locks, not hand-maintained lists
- CI fails fast on drift

### 2. Dependency Management
- **Per-service optional dependencies**: Each service installs only what it needs
- **Shared base**: Common dependencies in `base.txt`
- **GPU optimization**: Local models with optional CUDA extras
- **Security hardening**: Hash-pinned dependencies

### 3. Cross-Service Integration
- **Unified client APIs**: Consistent interface across all services
- **Health monitoring**: Inter-service health checks and dependency management
- **Orchestration support**: Cross-service workflows and coordination

### 4. Evaluation and Testing
- **DAG-based evaluation**: Root cause attribution and failure classification
- **CI integration**: Regression testing that fails builds
- **Shadow mode**: Production trace scoring without affecting requests
- **Failure taxonomy**: 21 standardized failure categories

### 5. Configuration Management
- **Centralized config**: Single config file for all service URLs
- **API key management**: Service-to-service authentication
- **Environment-specific**: Development, staging, production configurations

## Usage Examples

### Basic Service Health
```bash
halalchain cross health
```

### Cross-Service Orchestration
```bash
halalchain cross orchestration --product "chicken biryani with basmati rice"
```

### Embedding Orchestration with Verification
```bash
halalchain cross embedding-orchestration --text "organic chicken breast, halal certified"
```

### Local Models Test
```bash
halalchain cross local-models-test
```

## Docker Compose Usage

```bash
# Full stack with all services
docker compose up --build

# With development overlays
docker compose \
  -f docker-compose.yml \
  -f infrastructure/docker-compose.dev.yml \
  --profile chain up --build
```

## CI/CD Integration

### Python Locks Validation
- Every push to main validates all lock files
- Pull requests trigger lock file verification
- Failed validation blocks merge with clear regeneration instructions

### Architecture Tests
- Every build runs architecture guard tests
- UI projects cannot reference infrastructure
- Application layer remains independent
- MCP tools use thin adapters to Application layer

## Migration and Upgrade Guide

### From Old Structure to New

**Before** (problematic files):
- `requirements/inference.txt` - Duplicated ai-inference functionality
- `requirements/gpu.txt` - Imported capabilities architecture externalized
- Hand-maintained aggregator causing state drift
- Pip install -r requirements.txt at repo root (wrong command)

**After** (correct structure):
- `requirements/ai-inference.txt` - ai-inference runtime
- `requirements/agents.txt` - Agent runtime dependencies
- `requirements/local-models.txt` - Local model inference
- `requirements/dev.txt` - Development tools
- `requirements/tawheed.txt` - tawheed runtime

### Dockerfile Updates
- Services reference per-service requirement files
- Production images install only necessary dependencies
- Development images install all dependencies for convenience

## Conclusion

This comprehensive enhancement transforms the `.halalchain/` directory from a collection of loosely-connected services into a fully-integrated, production-ready platform with:

1. **Clear architecture boundaries** - Each service has a single responsibility
2. **Robust dependency management** - Generated locks, hash-pinned, CI-validated
3. **Cross-service integration** - Unified clients, health monitoring, orchestration
4. **Evaluation and testing** - DAG-based scoring, CI integration, shadow mode
5. **Modern tooling** - Docker, CI/CD, Python package management
6. **Security hardening** - Read-only credential boundaries, API key management

The enhanced architecture supports HalalChain's core principles:
- AI agents collect evidence
- Policy engine makes compliance decisions
- LLM never assigns or overrides halal verdicts
- Deterministic evidence collection and evaluation

All services work together as a cohesive platform while maintaining clear boundaries and architectural integrity.

## Files Changed/Created Summary

### New Files:
- `.halalchain/agents/pyproject.toml` (117 lines)
- `.halalchain/agents/app/eval/dag/` (6 files, ~500 lines)
- `.halalchain/agents/app/eval/metrics/` (2 files, ~150 lines)
- `.halalchain/agents/app/eval/runners/` (2 files, ~300 lines)
- `.halalchain/agents/app/eval/goldens/*.jsonl` (2 files, ~50 lines)
- `.halalchain/requirements/agents.txt` (74 lines)
- `.halalchain/local-models/Dockerfile` (45 lines)
- `.halalchain/local-models/pyproject.toml` (51 lines)
- `.halalchain/local-models/requirements.txt` (8 lines)
- `.halalchain/local-models/src/main.py` (265 lines)
- `HalalChain-Cli/bin/halalchain.js` (172 lines)
- `HalalChain-Cli/commands/cross-service.js` (267 lines)
- `HalalChain-Cli/lib/api-client.js` (updated)
- `HalalChain-Cli/commands/config.js` (updated)
- `docker-compose.yml` (updated)
- `docs/SERVICE_MANIFEST.md` (150 lines)
- `HalalChain.Architecture.Tests/EvaluationArchitectureTests.cs` (85 lines)
- `HalalChain.Agents/HalalChain.Agents.csproj` (27 lines)

### Modified Files:
- `.github/workflows/python-locks.yml` (updated with 4 new validation steps)
- `.halalchain/_shared/pyproject.toml` (updated with dev extra)
- `.halalchain/ai-inference/src/embeddings.py` (updated to use local-models)
- `.halalchain/ai-inference/pyproject.toml` (updated with new extras)
- `.halalchain/ai-inference/tests/test_cross_service.py` (2 new test files)

This represents a significant architectural improvement that addresses all the issues identified in the original conversation and establishes a solid foundation for HalalChain's AI/ML operations platform.