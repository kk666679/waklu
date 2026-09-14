import os
import hmac
import secrets
import hashlib
import logging
from typing import Optional

from fastapi import HTTPException, status
from fastapi.security import APIKeyHeader

from .config import Settings

logger = logging.getLogger(__name__)

api_key_header = APIKeyHeader(name="X-API-Key", auto_error=False)

# Explicitly opt-in dev bypass. Never default to true.
_DEV_BYPASS_ENV = "AI_GATEWAY_DEV_AUTH_BYPASS"
_DEFAULT_PLACEHOLDERS = {
    "",
    "changeme",
    "change-me",
    "change-me-minimum-32-chars-secret-key",
    "your-api-key",
    "test",
    "secret",
}


def _is_dev_environment(settings: Settings) -> bool:
    env = (settings.environment or "").lower()
    return env in ("development", "dev", "test", "testing") or bool(getattr(settings, "demo_mode", False))


def _dev_bypass_enabled(settings: Settings) -> bool:
    if not _is_dev_environment(settings):
        return False
    return os.getenv(_DEV_BYPASS_ENV, "false").lower() in ("1", "true", "yes", "on")


def ensure_auth_configured(settings: Settings) -> None:
    """Fail-closed startup check.

    Outside of an explicit dev/demo bypass the AI gateway MUST have a
    non-empty, non-placeholder API key configured. We raise ``RuntimeError``
    at process import so a misconfigured deployment cannot silently start
    with auth disabled.
    """
    if _dev_bypass_enabled(settings):
        logger.warning(
            "AI gateway auth DEV bypass active (env=%s, %s=true). DO NOT use in production.",
            settings.environment, _DEV_BYPASS_ENV,
        )
        return

    key = (settings.api_key or "").strip()
    if not key:
        raise RuntimeError(
            "AI_GATEWAY_API_KEY is not set. The AI gateway refuses to start "
            "without an explicit API key. Set AI_GATEWAY_API_KEY in the "
            "environment, or enable the explicit dev bypass "
            f"({_DEV_BYPASS_ENV}=true) only in development/demo."
        )
    if key.lower() in _DEFAULT_PLACEHOLDERS:
        raise RuntimeError(
            "AI_GATEWAY_API_KEY is set to a known placeholder value. "
            "Replace it with a real high-entropy secret before starting the service."
        )
    if len(key) < 32:
        raise RuntimeError(
            "AI_GATEWAY_API_KEY must be at least 32 characters of high-entropy material."
        )


def verify_api_key(api_key: Optional[str] = None, settings: Optional[Settings] = None) -> bool:
    """Verify the API key on an incoming request.

    ``api_key`` is resolved by FastAPI from the ``X-API-Key`` header via
    :data:`api_key_header`. The parameter is exposed for direct testability.
    ``settings`` is resolved for the same reason; callers normally rely on
    the singleton from :mod:`.config` but tests can inject a fresh
    instance.
    """
    if settings is None:
        from .config import settings as _settings
        settings = _settings

    if _dev_bypass_enabled(settings):
        return True

    expected = (settings.api_key or "").strip()
    if not expected:
        # Belt-and-braces: the startup check should have caught this. If we
        # get here we have a configuration regression — refuse the request
        # rather than authenticate silently.
        logger.error("AI_GATEWAY_API_KEY missing at request time; refusing request")
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="AI gateway is not configured; contact the operator.",
        )

    if not api_key:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Missing X-API-Key header.",
        )

    if not hmac.compare_digest(api_key, expected):
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid API key.",
        )
    return True


def generate_api_key() -> str:
    """Generate a new API key (32 bytes of high-entropy material)."""
    return f"halalchain_{secrets.token_urlsafe(32)}"
