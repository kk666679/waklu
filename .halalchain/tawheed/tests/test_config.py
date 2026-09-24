"""Tests for the tawheed configuration validator.

The validator mirrors the .NET startup-validation behaviour: a weak or
placeholder JWT secret must be rejected in non-development environments.
"""

import os
import pytest

from src.core import config as tawheed_config


@pytest.fixture(autouse=True)
def _clean_env(monkeypatch):
    for k in (
        "JWT_SECRET", "NEO4J_PASSWORD", "ENVIRONMENT", "TAWHEED_ENV",
        "DEMO_MODE", "POSTGRES_URL", "REDIS_URL", "NEO4J_URI", "NEO4J_USER",
        "IOT_ENABLED", "IOT_DEVICE_KEYS_JSON", "IOT_MAX_CLOCK_SKEW_SECONDS",
    ):
        monkeypatch.delenv(k, raising=False)
    yield


def _make_settings(monkeypatch, **overrides):
    for k, v in overrides.items():
        monkeypatch.setenv(k, v)
    tawheed_config.get_settings.cache_clear()
    return tawheed_config.get_settings()


def test_production_rejects_empty_jwt_secret(monkeypatch):
    with pytest.raises(RuntimeError):
        _make_settings(monkeypatch, ENVIRONMENT="production", JWT_SECRET="")


def test_production_rejects_placeholder_jwt_secret(monkeypatch):
    with pytest.raises(RuntimeError):
        _make_settings(
            monkeypatch,
            ENVIRONMENT="production",
            JWT_SECRET="change-me-minimum-32-chars-secret-key",
        )


def test_production_rejects_short_jwt_secret(monkeypatch):
    with pytest.raises(RuntimeError):
        _make_settings(monkeypatch, ENVIRONMENT="production", JWT_SECRET="short")


def test_production_accepts_strong_jwt_secret(monkeypatch):
    s = _make_settings(
        monkeypatch,
        ENVIRONMENT="production",
        JWT_SECRET="x" * 64,
        NEO4J_PASSWORD="also-a-real-password-not-tawheed123",
    )
    assert s.jwt_secret == "x" * 64


def test_development_permits_placeholder(monkeypatch):
    s = _make_settings(
        monkeypatch,
        ENVIRONMENT="development",
        JWT_SECRET="change-me-minimum-32-chars-secret-key",
    )
    assert s.jwt_secret == "change-me-minimum-32-chars-secret-key"


def test_production_rejects_default_neo4j_password(monkeypatch):
    with pytest.raises(RuntimeError):
        _make_settings(
            monkeypatch,
            ENVIRONMENT="production",
            JWT_SECRET="x" * 64,
            NEO4J_PASSWORD="tawheed123",
        )


def test_production_allows_iot_to_be_disabled(monkeypatch):
    settings = _make_settings(
        monkeypatch,
        ENVIRONMENT="production",
        JWT_SECRET="x" * 64,
        NEO4J_PASSWORD="also-a-real-password-not-tawheed123",
        IOT_ENABLED="false",
        IOT_DEVICE_KEYS_JSON="{}",
    )
    assert settings.iot_enabled is False
    assert settings.iot_device_keys == {}


def test_production_rejects_empty_iot_device_keys_when_enabled(monkeypatch):
    with pytest.raises(RuntimeError, match="IOT_DEVICE_KEYS_JSON"):
        settings = _make_settings(
            monkeypatch,
            ENVIRONMENT="production",
            JWT_SECRET="x" * 64,
            NEO4J_PASSWORD="also-a-real-password-not-tawheed123",
            IOT_ENABLED="true",
            IOT_DEVICE_KEYS_JSON="{}",
        )
        settings.iot_device_keys


def test_production_rejects_short_iot_device_key(monkeypatch):
    with pytest.raises(RuntimeError, match="32 characters"):
        settings = _make_settings(
            monkeypatch,
            ENVIRONMENT="production",
            JWT_SECRET="x" * 64,
            NEO4J_PASSWORD="also-a-real-password-not-tawheed123",
            IOT_ENABLED="true",
            IOT_DEVICE_KEYS_JSON='{"device-1":"short"}',
        )
        settings.iot_device_keys
