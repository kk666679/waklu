from __future__ import annotations

from dataclasses import dataclass

from src.core.config import get_settings

try:
    from halalchain_shared.ai_backend import (  # type: ignore
        AIProviderConfig,
        resolve_ai_provider_config,
        BACKEND_DEMO,
    )
except Exception:  # pragma: no cover - fallback when shared package is not installed
    AIProviderConfig = None  # type: ignore
    resolve_ai_provider_config = None  # type: ignore
    BACKEND_DEMO = "demo"


@dataclass
class LLMConfig:
    backend: str
    base_url: str
    api_key: str
    model: str
    enabled: bool


def get_llm_config() -> LLMConfig:
    """Resolve the LLM configuration from the canonical ``AI_BACKEND`` selector.

    Backends:
      demo     → no LLM (rule-based agents); enabled=False
      openai   → OpenAI API
      foundry  → Azure AI Foundry OpenAI-compatible endpoint (production)
      openclaw → OpenClaw+Ollama gateway (dev/offline/CI)

    The deterministic policy engine never uses this client.
    """
    s = get_settings()

    if resolve_ai_provider_config is not None:
        cfg = resolve_ai_provider_config(
            default_backend=BACKEND_DEMO,
            openai_model_default=s.llm_model or "gpt-4o-mini",
            foundry_model_default=s.foundry_model or "halalchain-assistant",
            openclaw_model_default=s.openclaw_model or "halalchain-assistant",
        )
        return LLMConfig(
            backend=cfg.backend,
            base_url=cfg.base_url,
            api_key=cfg.api_key,
            model=cfg.model,
            enabled=cfg.enabled,
        )

    # Fallback path — preserves the historical tawheed-only behavior when
    # the shared package is not importable.
    backend = (s.ai_backend or "demo").lower()
    if backend == "foundry":
        enabled = bool(s.foundry_base_url and s.foundry_api_key)
        return LLMConfig("foundry", s.foundry_base_url, s.foundry_api_key,
                         s.foundry_model or "halalchain-assistant", enabled)
    if backend == "openclaw":
        enabled = bool(s.openclaw_base_url)
        return LLMConfig("openclaw", s.openclaw_base_url, s.openclaw_api_key or "ollama",
                         s.openclaw_model or "halalchain-assistant", enabled)
    if backend == "openai":
        enabled = bool(s.llm_api_key)
        return LLMConfig("openai", s.llm_base_url or "", s.llm_api_key,
                         s.llm_model or "gpt-4o-mini", enabled)
    return LLMConfig("demo", "", "", "", False)


def get_llm_client():
    """Return an OpenAI-compatible async client for the selected backend, or
    None when in demo mode / misconfigured (agents then run rule-based)."""
    cfg = get_llm_config()
    if not cfg.enabled:
        return None
    try:
        from halalchain_shared.ai_backend import get_async_openai_client  # type: ignore
    except Exception:
        import openai
        return openai.AsyncOpenAI(base_url=cfg.base_url or None, api_key=cfg.api_key or "none")
    return get_async_openai_client(cfg)
