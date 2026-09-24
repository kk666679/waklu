from __future__ import annotations
import os
import json
from functools import lru_cache
from typing import List
from pydantic_settings import BaseSettings, SettingsConfigDict
from pydantic import field_validator


# Placeholder values that must never be accepted in non-development environments.
_PLACEHOLDER_SECRETS = {
    "",
    "change-me",
    "change-me-minimum-32-chars-secret-key",
    "changeme",
    "secret",
    "dev-secret-do-not-use-in-production",
    "tawheed",
    "halalchain",
    "tawheed123",
}


def _is_dev_environment() -> bool:
    env = (os.getenv("ENVIRONMENT") or os.getenv("TAWHEED_ENV") or "development").lower()
    return env in ("development", "dev", "test", "testing", "demo")


def _require_strong_secret(name: str, value: str, min_length: int = 32) -> str:
    if not value or not value.strip():
        if _is_dev_environment():
            return value
        raise RuntimeError(
            f"{name} is not set. Tawheed refuses to start without an explicit "
            f"secret in non-development environments. Set {name} in the environment."
        )
    if value in _PLACEHOLDER_SECRETS:
        if _is_dev_environment():
            return value
        raise RuntimeError(
            f"{name} is set to a known placeholder value. Configure a real "
            f"high-entropy secret in non-development environments."
        )
    if len(value) < min_length:
        if _is_dev_environment():
            return value
        raise RuntimeError(
            f"{name} is shorter than the required {min_length}-character minimum."
        )
    return value


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    demo_mode: bool = True
    environment: str = "development"

    # ── Persistence ──────────────────────────────────────────────────────
    # These defaults are explicit "no connection" sentinels for non-dev environments.
    # In dev they fall back to localhost so the service still boots, but operators
    # MUST override them in any environment that handles real evidence.
    postgres_url: str = "postgresql+asyncpg://tawheed:tawheed@localhost:5432/tawheed"
    redis_url: str = "redis://localhost:6379/0"
    qdrant_url: str = "http://localhost:6333"
    qdrant_collection: str = "halal_knowledge"
    neo4j_uri: str = "bolt://localhost:7687"
    neo4j_user: str = "neo4j"
    neo4j_password: str = "tawheed123"

    @field_validator("neo4j_password")
    @classmethod
    def _reject_default_neo4j_password(cls, v: str) -> str:
        if v in ("tawheed123", "neo4j", "password", "admin"):
            if _is_dev_environment():
                return v
            raise RuntimeError(
                "NEO4J_PASSWORD is set to a known default/placeholder. "
                "Configure a real high-entropy password for non-development environments."
            )
        return v

    # ── LLM ──────────────────────────────────────────────────────────────
    llm_provider: str = "demo"
    llm_api_key: str = ""
    llm_model: str = "gpt-4o-mini"
    llm_base_url: str = ""

    # AI Backend selector for agent LLM calls: demo, openai, foundry, openclaw
    ai_backend: str = "demo"

    foundry_base_url: str = ""
    foundry_api_key: str = ""
    foundry_model: str = "halalchain-assistant"

    openclaw_base_url: str = ""
    openclaw_api_key: str = ""
    openclaw_model: str = "halalchain-assistant"

    onnx_model_dir: str = "models/onnx"

    # ── JWT ──────────────────────────────────────────────────────────────
    # No usable default — see __init__ below.
    jwt_secret: str = ""
    jwt_algorithm: str = "HS256"
    jwt_expire_minutes: int = 60

    otlp_endpoint: str = "http://localhost:4317"
    jaeger_endpoint: str = "http://localhost:14268/api/traces"

    default_policy_version: str = "MY-v3"
    default_jurisdiction: str = "MY"

    risk_threshold_manual_review: float = 0.65
    risk_threshold_hold: float = 0.85

    # JSON map of device ID to HMAC key. Keep this in a secret manager in production.
    iot_enabled: bool = False
    iot_device_keys_json: str = "{}"
    iot_max_clock_skew_seconds: int = 300
    iot_observation_store_path: str = ""

    @property
    def iot_device_keys(self) -> dict[str, str]:
        try:
            keys = json.loads(self.iot_device_keys_json)
        except json.JSONDecodeError as exc:
            raise RuntimeError("IOT_DEVICE_KEYS_JSON must be valid JSON") from exc
        if not isinstance(keys, dict) or not all(
            isinstance(k, str) and isinstance(v, str) for k, v in keys.items()
        ):
            raise RuntimeError("IOT_DEVICE_KEYS_JSON must map string device IDs to string keys")
        if self.iot_enabled and not _is_dev_environment():
            if not keys:
                raise RuntimeError("IOT_DEVICE_KEYS_JSON must contain at least one device in non-development environments")
            if any(len(value) < 32 for value in keys.values()):
                raise RuntimeError("IoT device keys must be at least 32 characters in non-development environments")
        return keys


@lru_cache
def get_settings() -> Settings:
    s = Settings()
    # Mirror the .NET startup-validation behavior: reject weak / placeholder JWT
    # secrets in non-development environments.
    s.jwt_secret = _require_strong_secret("JWT_SECRET", s.jwt_secret)
    return s
