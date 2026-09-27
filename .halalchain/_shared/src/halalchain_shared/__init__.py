"""Shared runtime helpers for the HalalChain Python services.

The two Python services — `.halalchain/ai-inference` and
`.halalchain/tawheed` — are independently deployable. They do NOT share
business logic, but they share:

* the canonical ``AI_BACKEND`` selector and the env-var name normalization
  for all LLM providers (OpenAI, Anthropic, Azure AI Foundry, OpenClaw/Ollama);
* an in-process LRU cache that can be promoted to Redis via a strategy flag.
* typed HTTP clients for cross-service communication (ai-inference, tawheed, local-models).

This package is intentionally tiny. It only contains things that are
genuinely identical across both services. Provider-specific business logic
stays in the service that uses it.
"""

__version__ = "0.1.0"

from .ai_backend import (
    AI_BACKEND_ENV,
    AIProviderConfig,
    ALL_BACKENDS,
    BACKEND_ANTHROPIC,
    BACKEND_DEMO,
    BACKEND_FOUNDRY,
    BACKEND_LOCAL,
    BACKEND_OPENAI,
    BACKEND_OPENCLAW,
    get_async_openai_client,
    resolve_ai_backend,
    resolve_ai_provider_config,
)
from .cache import LRUCache, build_cache
from .service_client import (
    AIInferenceClient,
    LocalModelsClient,
    TawheedClient,
    create_ai_inference_client,
    create_local_models_client,
    create_tawheed_client,
)

__all__ = [
    "AI_BACKEND_ENV",
    "AIProviderConfig",
    "ALL_BACKENDS",
    "BACKEND_ANTHROPIC",
    "BACKEND_DEMO",
    "BACKEND_FOUNDRY",
    "BACKEND_LOCAL",
    "BACKEND_OPENAI",
    "BACKEND_OPENCLAW",
    "get_async_openai_client",
    "resolve_ai_backend",
    "resolve_ai_provider_config",
    "LRUCache",
    "build_cache",
    "AIInferenceClient",
    "LocalModelsClient",
    "TawheedClient",
    "create_ai_inference_client",
    "create_local_models_client",
    "create_tawheed_client",
]
