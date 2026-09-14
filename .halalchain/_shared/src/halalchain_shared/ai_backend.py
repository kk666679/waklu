"""Canonical AI backend selection shared by ai-inference and tawheed.

Both Python services historically accepted slightly different env-var
names to describe which LLM provider to talk to. The canonical,
service-wide name is now :data:`AI_BACKEND_ENV`. The historical names are
still accepted as fallbacks for backward compatibility but new code MUST
read :data:`AI_BACKEND_ENV`.

Canonical backend values (case-insensitive):

* ``local``  — keyword/rule-based stub. Default for ai-inference.
* ``demo``   — no LLM calls; rule-based agents. Default for tawheed.
* ``openai``  — OpenAI's hosted API.
* ``anthropic`` — Anthropic's hosted API (or an Anthropic-compatible
  endpoint via ``ANTHROPIC_BASE_URL``, e.g. Ollama / OpenClaw Anthropic mode).
* ``foundry`` — Azure AI Foundry OpenAI-compatible endpoint (production).
* ``openclaw`` — OpenClaw + Ollama gateway (dev / offline / CI).
  OpenAI-compatible.
"""

from __future__ import annotations

import os
from dataclasses import dataclass

# Canonical env var name used across the platform.
AI_BACKEND_ENV = "AI_BACKEND"

# Backward-compat aliases. Read-only; never write to them.
_LEGACY_ENV_ALIASES = (
    "LLM_PROVIDER",  # tawheed
    "DEFAULT_LLM_PROVIDER",  # ai-inference
)

# Canonical backend tokens.
BACKEND_LOCAL = "local"
BACKEND_DEMO = "demo"
BACKEND_OPENAI = "openai"
BACKEND_ANTHROPIC = "anthropic"
BACKEND_FOUNDRY = "foundry"
BACKEND_OPENCLAW = "openclaw"

ALL_BACKENDS = (
    BACKEND_LOCAL,
    BACKEND_DEMO,
    BACKEND_OPENAI,
    BACKEND_ANTHROPIC,
    BACKEND_FOUNDRY,
    BACKEND_OPENCLAW,
)


@dataclass(frozen=True)
class AIProviderConfig:
    """Resolved provider configuration for the current process."""

    backend: str
    base_url: str
    api_key: str
    model: str
    enabled: bool


def _read_env(*names: str, default: str = "") -> str:
    for n in names:
        v = os.getenv(n)
        if v is not None and v != "":
            return v
    return default


def resolve_ai_backend(default: str = BACKEND_LOCAL) -> str:
    """Read the canonical ``AI_BACKEND`` value, falling back to legacy
    env-var aliases (``LLM_PROVIDER``, ``DEFAULT_LLM_PROVIDER``)."""
    raw = os.getenv(AI_BACKEND_ENV)
    if not raw:
        for legacy in _LEGACY_ENV_ALIASES:
            raw = os.getenv(legacy)
            if raw:
                break
    if not raw:
        raw = default
    backend = raw.strip().lower()
    return backend if backend in ALL_BACKENDS else default


def resolve_ai_provider_config(
    *,
    default_backend: str = BACKEND_LOCAL,
    openai_model_default: str = "gpt-4o-mini",
    foundry_model_default: str = "halalchain-assistant",
    openclaw_model_default: str = "halalchain-assistant",
) -> AIProviderConfig:
    """Resolve the canonical provider configuration from environment variables.

    Services may override the default model names and the default backend
    (e.g. tawheed passes ``default_backend="demo"``).
    """
    backend = resolve_ai_backend(default=default_backend)

    if backend == BACKEND_FOUNDRY:
        base_url = _read_env("FOUNDRY_BASE_URL")
        api_key = _read_env("FOUNDRY_API_KEY")
        model = _read_env("FOUNDRY_MODEL", default=foundry_model_default)
        return AIProviderConfig(backend, base_url, api_key, model,
                                enabled=bool(base_url and api_key))

    if backend == BACKEND_OPENCLAW:
        base_url = _read_env("OPENCLAW_BASE_URL")
        api_key = _read_env("OPENCLAW_API_KEY", default="ollama")
        model = _read_env("OPENCLAW_MODEL", default=openclaw_model_default)
        return AIProviderConfig(backend, base_url, api_key, model,
                                enabled=bool(base_url))

    if backend == BACKEND_OPENAI:
        api_key = _read_env("OPENAI_API_KEY")
        base_url = _read_env("OPENAI_BASE_URL")
        model = _read_env("OPENAI_MODEL", default=openai_model_default)
        return AIProviderConfig(backend, base_url or "", api_key, model,
                                enabled=bool(api_key))

    if backend == BACKEND_ANTHROPIC:
        api_key = _read_env("ANTHROPIC_API_KEY", default="")
        base_url = _read_env("ANTHROPIC_BASE_URL", default="")
        model = _read_env("ANTHROPIC_MODEL", default="claude-3-sonnet-20240229")
        return AIProviderConfig(backend, base_url, api_key, model,
                                enabled=bool(api_key or base_url))

    # local / demo — never reach out to a network LLM.
    return AIProviderConfig(backend, "", "", "", enabled=False)


def get_async_openai_client(cfg: AIProviderConfig):
    """Return an OpenAI-compatible async client for ``cfg`` or ``None`` when
    disabled. The deterministic policy engine in tawheed must NEVER call
    this — it exists only for evidence-collection agents."""
    if not cfg.enabled or not cfg.backend:
        return None
    try:
        import openai  # type: ignore
    except ImportError:
        return None
    return openai.AsyncOpenAI(
        base_url=cfg.base_url or None,
        api_key=cfg.api_key or "none",
    )
