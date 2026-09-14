import os
import pytest

from halalchain_shared.ai_backend import (
    AI_BACKEND_ENV,
    BACKEND_DEMO,
    BACKEND_FOUNDRY,
    BACKEND_LOCAL,
    BACKEND_OPENAI,
    BACKEND_OPENCLAW,
    resolve_ai_backend,
    resolve_ai_provider_config,
)


@pytest.fixture(autouse=True)
def _clean_env(monkeypatch):
    for k in (
        AI_BACKEND_ENV,
        "LLM_PROVIDER",
        "DEFAULT_LLM_PROVIDER",
        "FOUNDRY_BASE_URL",
        "FOUNDRY_API_KEY",
        "FOUNDRY_MODEL",
        "OPENCLAW_BASE_URL",
        "OPENCLAW_API_KEY",
        "OPENCLAW_MODEL",
        "OPENAI_API_KEY",
        "ANTHROPIC_API_KEY",
        "ANTHROPIC_BASE_URL",
    ):
        monkeypatch.delenv(k, raising=False)
    yield


def test_default_backend_is_local():
    assert resolve_ai_backend() == BACKEND_LOCAL


def test_canonical_env_wins(monkeypatch):
    monkeypatch.setenv(AI_BACKEND_ENV, "foundry")
    assert resolve_ai_backend() == BACKEND_FOUNDRY


def test_legacy_aliases_preserved(monkeypatch):
    monkeypatch.setenv("LLM_PROVIDER", "openclaw")
    assert resolve_ai_backend() == BACKEND_OPENCLAW
    monkeypatch.delenv("LLM_PROVIDER")
    monkeypatch.setenv("DEFAULT_LLM_PROVIDER", "openai")
    assert resolve_ai_backend() == BACKEND_OPENAI


def test_unknown_value_falls_back_to_default():
    os.environ[AI_BACKEND_ENV] = "nonsense"
    try:
        assert resolve_ai_backend(default=BACKEND_DEMO) == BACKEND_DEMO
    finally:
        os.environ.pop(AI_BACKEND_ENV)


def test_foundry_provider_requires_both_creds(monkeypatch):
    monkeypatch.setenv(AI_BACKEND_ENV, "foundry")
    monkeypatch.setenv("FOUNDRY_BASE_URL", "https://example.test/v1")
    cfg = resolve_ai_provider_config()
    assert cfg.enabled is False  # no API key
    monkeypatch.setenv("FOUNDRY_API_KEY", "x")
    cfg = resolve_ai_provider_config()
    assert cfg.enabled is True
    assert cfg.backend == "foundry"


def test_openclaw_default_key_is_ollama(monkeypatch):
    monkeypatch.setenv(AI_BACKEND_ENV, "openclaw")
    monkeypatch.setenv("OPENCLAW_BASE_URL", "http://localhost:11434/v1")
    cfg = resolve_ai_provider_config()
    assert cfg.api_key == "ollama"
    assert cfg.enabled is True
