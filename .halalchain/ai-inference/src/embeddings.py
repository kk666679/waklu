from typing import List, Optional
from .config import settings

try:
    from halalchain_shared.service_client import create_local_models_client
except Exception:  # pragma: no cover
    create_local_models_client = None  # type: ignore


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
            return settings.local_models_url, "", settings.embedding_model
    return None


def embedding_model_name(model_override: Optional[str] = None) -> str:
    endpoint = _resolve_embedding_endpoint()
    if endpoint is None:
        raise RuntimeError("No embedding provider is configured")
    return model_override or endpoint[2]


async def generate_embedding(text: str, model: Optional[str] = None) -> List[float]:
    """Generate embeddings only through a configured model provider."""
    endpoint = _resolve_embedding_endpoint()
    if endpoint is None:
        raise RuntimeError("No embedding provider is configured")

    base_url, api_key, configured_model = endpoint
    model_name = model or configured_model
    if settings.embedding_provider.lower() == "local-models":
        if create_local_models_client is None:
            raise RuntimeError("The local-models client is unavailable")
        client = create_local_models_client()
        try:
            response = await client.embeddings(text, model_name)
            embedding = response.get("embedding")
        finally:
            await client.close()
    else:
        import openai

        options = {"api_key": api_key}
        if base_url:
            options["base_url"] = base_url
        client = openai.AsyncOpenAI(**options)
        try:
            response = await client.embeddings.create(model=model_name, input=text)
            embedding = response.data[0].embedding
        finally:
            await client.close()

    if not embedding:
        raise RuntimeError(f"Embedding provider returned no vector for model {model_name}")
    return [float(value) for value in embedding]
