from typing import Optional, Dict, Any
from .config import settings
import logging

logger = logging.getLogger(__name__)

try:
    from halalchain_shared.ai_backend import (  # type: ignore
        resolve_ai_provider_config,
        BACKEND_LOCAL as _BACKEND_LOCAL,
        BACKEND_FOUNDRY as _BACKEND_FOUNDRY,
        BACKEND_OPENCLAW as _BACKEND_OPENCLAW,
        BACKEND_OPENAI as _BACKEND_OPENAI,
        BACKEND_ANTHROPIC as _BACKEND_ANTHROPIC,
    )
except Exception:  # pragma: no cover
    resolve_ai_provider_config = None  # type: ignore


class LLMProvider:
    """Base LLM provider interface."""
    async def generate(self, prompt: str, max_tokens: int = 100, temperature: float = 0.7) -> str:
        raise NotImplementedError


class LocalLLMProvider(LLMProvider):
    """Local keyword-based LLM (no external dependencies)."""
    async def generate(self, prompt: str, max_tokens: int = 100, temperature: float = 0.7) -> str:
        # Simple extractive response for demo
        words = prompt.split()
        return " ".join(words[:min(max_tokens, len(words))])


class OpenAILLMProvider(LLMProvider):
    """OpenAI LLM provider."""
    def __init__(self, api_key: str, model: str):
        self.api_key = api_key
        self.model = model

    async def generate(self, prompt: str, max_tokens: int = 100, temperature: float = 0.7) -> str:
        import openai
        client = openai.AsyncOpenAI(api_key=self.api_key)
        response = await client.chat.completions.create(
            model=self.model,
            messages=[{"role": "user", "content": prompt}],
            max_tokens=max_tokens,
            temperature=temperature
        )
        return response.choices[0].message.content


class AnthropicLLMProvider(LLMProvider):
    """Anthropic Claude LLM provider.

    When `base_url` is set (e.g. Ollama/OpenClaw Anthropic-compatible endpoint
    at http://localhost:11434), requests are routed there instead of Anthropic's
    cloud API. The api_key is accepted but ignored by Ollama.
    """

    # Anthropic SDK versions sometimes drop top-level ``temperature`` for
    # chat-completions-compatible providers (Ollama, OpenClaw). We probe
    # that specific case, but preserve any unexpected ``TypeError`` for
    # diagnostics — silently swallowing arbitrary TypeErrors hides SDK
    # incompatibilities.
    _TEMPERATURE_FALLBACK_ATTRS = ("temperature", "extra_body")

    def __init__(self, api_key: str, model: str, base_url: Optional[str] = None):
        self.api_key = api_key
        self.model = model
        self.base_url = base_url

    async def generate(self, prompt: str, max_tokens: int = 100, temperature: float = 0.7) -> str:
        import anthropic
        kwargs = {"api_key": self.api_key}
        if self.base_url:
            kwargs["base_url"] = self.base_url
        client = anthropic.AsyncAnthropic(**kwargs)
        try:
            response = await client.messages.create(
                model=self.model,
                max_tokens=max_tokens,
                temperature=temperature,
                messages=[{"role": "user", "content": prompt}],
            )
        except TypeError as exc:
            # The fallback only applies if the SDK rejected the call
            # specifically because of an unexpected ``temperature`` kwarg.
            if "temperature" not in str(exc):
                raise
            logger.info(
                "Anthropic SDK rejected top-level temperature; retrying via extra_body"
            )
            response = await client.messages.create(
                model=self.model,
                max_tokens=max_tokens,
                messages=[{"role": "user", "content": prompt}],
                extra_body={"temperature": temperature},
            )
        return response.content[0].text


class OpenAICompatibleProvider(LLMProvider):
    """OpenAI-compatible provider for Azure AI Foundry or OpenClaw gateways.

    Both backends expose an OpenAI-compatible chat completions endpoint, so a
    single implementation covers them — only the base_url / key differ.
    """

    def __init__(self, base_url: str, api_key: str, model: str):
        self.base_url = base_url
        self.api_key = api_key
        self.model = model

    async def generate(self, prompt: str, max_tokens: int = 100, temperature: float = 0.7) -> str:
        import openai
        client = openai.AsyncOpenAI(base_url=self.base_url, api_key=self.api_key)
        response = await client.chat.completions.create(
            model=self.model,
            messages=[{"role": "user", "content": prompt}],
            max_tokens=max_tokens,
            temperature=temperature,
        )
        return response.choices[0].message.content


def get_llm_provider() -> LLMProvider:
    """Get the configured LLM provider via the canonical ``AI_BACKEND`` selector.

    Backends (case-insensitive; canonical name ``AI_BACKEND``):
      local     → keyword stub (default, no external dependency)
      openai    → OpenAI API
      anthropic → Anthropic API (or Anthropic-compatible endpoint)
      foundry   → Azure AI Foundry OpenAI-compatible endpoint (production)
      openclaw  → OpenClaw+Ollama gateway (dev/offline/CI)

    Falls back to :class:`LocalLLMProvider` when backend credentials/endpoints
    are unset. The shared ``halalchain_shared.ai_backend`` module owns the
    canonical env-var resolution; this function only maps that into the
    concrete provider classes.
    """
    if resolve_ai_provider_config is not None:
        cfg = resolve_ai_provider_config(
            default_backend=_BACKEND_LOCAL,
            openai_model_default=settings.openai_model,
            foundry_model_default=settings.foundry_model,
            openclaw_model_default=settings.openclaw_model,
        )
        backend = cfg.backend
    else:
        backend = (settings.ai_backend or "local").lower()

    if backend == _BACKEND_FOUNDRY:
        if settings.foundry_base_url and settings.foundry_api_key:
            return OpenAICompatibleProvider(
                settings.foundry_base_url, settings.foundry_api_key, settings.foundry_model
            )
        logger.warning("AI_BACKEND=foundry but foundry_base_url/foundry_api_key unset; using local")

    elif backend == _BACKEND_OPENCLAW:
        if settings.openclaw_base_url:
            return OpenAICompatibleProvider(
                settings.openclaw_base_url, settings.openclaw_api_key or "ollama", settings.openclaw_model
            )
        logger.warning("AI_BACKEND=openclaw but openclaw_base_url unset; using local")

    elif backend == _BACKEND_OPENAI and settings.openai_api_key:
        return OpenAILLMProvider(settings.openai_api_key, settings.openai_model)

    elif backend == _BACKEND_ANTHROPIC:
        if settings.anthropic_api_key or settings.anthropic_base_url:
            return AnthropicLLMProvider(
                settings.anthropic_api_key or "ollama",
                settings.anthropic_model,
                settings.anthropic_base_url,
            )

    return LocalLLMProvider()
