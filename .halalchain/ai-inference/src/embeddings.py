import math
from typing import List, Optional
from .config import settings

try:
    from halalchain_shared.service_client import create_local_models_client
except Exception:  # pragma: no cover
    create_local_models_client = None  # type: ignore


DIM = settings.embedding_dim  # 256


def _local_embedding(text: str) -> List[float]:
    """Deterministic 256-dim embedding from text (no external dependency)."""
    if not text:
        return [0.0] * DIM
    v = [0.0] * DIM
    for i, ch in enumerate(text):
        c = ord(ch)
        v[i % DIM] += math.sin(c * 0.1 + i * 0.05) * 0.5 + math.cos(c * 0.07) * 0.3
        v[(i * 7 + 3) % DIM] += ((c * 2654435761) % 4294967296) / 4294967296 - 0.5
    norm = math.sqrt(sum(x * x for x in v)) or 1.0
    return [round(x / norm, 8) for x in v]


def _resolve_embedding_endpoint() -> Optional[tuple[str, str, str]]:
    """Return (base_url, api_key, model) for OpenAI-compatible embedding backends."""
    provider = (settings.embedding_provider or "local").lower()
    if provider == "openai":
        if settings.openai_api_key:
            return None, settings.openai_api_key, settings.openai_model
    elif provider == "foundry":
        if settings.foundry_base_url and settings.foundry_api_key:
            return settings.foundry_base_url, settings.foundry_api_key, settings.foundry_model
    elif provider == "openclaw":
        if settings.openclaw_base_url:
            return settings.openclaw_base_url, settings.openclaw_api_key or "none", settings.openclaw_model
    elif provider == "local-models":
        if settings.local_models_url:
            return settings.local_models_url, "", "local"
    return None


def generate_embedding(text: str) -> List[float]:
    """Generate an embedding. Uses a remote OpenAI-compatible backend when
    embedding_provider is set to openai/foundry/openclaw/local-models and configured;
    otherwise falls back to the deterministic local embedding."""
    endpoint = _resolve_embedding_endpoint()
    if endpoint is not None:
        base_url, api_key, model = endpoint
        if base_url and base_url.startswith("http") and create_local_models_client:
            # Use local-models service
            import asyncio
            client = create_local_models_client()
            try:
                loop = asyncio.get_event_loop()
            except RuntimeError:
                loop = asyncio.new_event_loop()
                asyncio.set_event_loop(loop)
            result = loop.run_until_complete(client.embeddings(text))
            loop.run_until_complete(client.close())
            return result.get("embedding", _local_embedding(text))
        else:
            # OpenAI-compatible direct
            import openai
            client = openai.OpenAI(base_url=base_url, api_key=api_key)
            resp = client.embeddings.create(model=model, input=text)
            return resp.data[0].embedding
    return _local_embedding(text)
