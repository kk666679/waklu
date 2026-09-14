"""Tests for the AI gateway's fail-closed auth behaviour."""

import os
import pytest

from src.config import Settings
from src import auth


@pytest.fixture(autouse=True)
def _clean_env(monkeypatch):
    for k in (
        "AI_GATEWAY_API_KEY",
        "AI_GATEWAY_DEV_AUTH_BYPASS",
        "ENVIRONMENT",
        "DEMO_MODE",
    ):
        monkeypatch.delenv(k, raising=False)
    yield


def _settings_with(**overrides):
    """Build a Settings instance with the given env overrides applied."""
    for k, v in overrides.items():
        if v is None:
            os.environ.pop(k, None)
        else:
            os.environ[k] = v
    return Settings()


def test_ensure_auth_configured_fails_when_no_key_outside_dev():
    os.environ["ENVIRONMENT"] = "production"
    os.environ.pop("AI_GATEWAY_API_KEY", None)
    s = Settings()
    with pytest.raises(RuntimeError):
        auth.ensure_auth_configured(s)


def test_ensure_auth_configured_fails_when_placeholder():
    os.environ["ENVIRONMENT"] = "production"
    os.environ["AI_GATEWAY_API_KEY"] = "changeme"
    s = Settings()
    with pytest.raises(RuntimeError):
        auth.ensure_auth_configured(s)


def test_ensure_auth_configured_fails_when_too_short():
    os.environ["ENVIRONMENT"] = "production"
    os.environ["AI_GATEWAY_API_KEY"] = "x" * 16
    s = Settings()
    with pytest.raises(RuntimeError):
        auth.ensure_auth_configured(s)


def test_ensure_auth_configured_succeeds_with_strong_key():
    os.environ["ENVIRONMENT"] = "production"
    os.environ["AI_GATEWAY_API_KEY"] = "x" * 64
    s = Settings()
    auth.ensure_auth_configured(s)  # should not raise


def test_dev_bypass_explicit_only():
    os.environ["ENVIRONMENT"] = "development"
    os.environ["AI_GATEWAY_DEV_AUTH_BYPASS"] = "true"
    s = Settings()
    auth.ensure_auth_configured(s)  # dev bypass, no key required


def test_dev_environment_does_not_implicitly_bypass():
    # Being in development is not enough on its own — operators must
    # also opt-in to the bypass explicitly.
    os.environ["ENVIRONMENT"] = "development"
    os.environ["AI_GATEWAY_API_KEY"] = "x" * 64
    s = Settings()
    auth.ensure_auth_configured(s)


def test_verify_api_key_rejects_missing():
    os.environ["ENVIRONMENT"] = "production"
    os.environ["AI_GATEWAY_API_KEY"] = "x" * 64
    s = Settings()
    auth.ensure_auth_configured(s)
    with pytest.raises(Exception) as excinfo:
        auth.verify_api_key(None)
    assert excinfo.value.status_code == 401


def test_verify_api_key_rejects_wrong_key():
    os.environ["ENVIRONMENT"] = "production"
    os.environ["AI_GATEWAY_API_KEY"] = "x" * 64
    s = Settings()
    auth.ensure_auth_configured(s)
    with pytest.raises(Exception) as excinfo:
        auth.verify_api_key("not-the-key")
    assert excinfo.value.status_code == 401


def test_verify_api_key_accepts_correct_key():
    os.environ["ENVIRONMENT"] = "production"
    os.environ["AI_GATEWAY_API_KEY"] = "x" * 64
    s = Settings()
    auth.ensure_auth_configured(s)
    assert auth.verify_api_key("x" * 64, settings=s) is True
