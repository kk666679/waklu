"""Backwards-compatible import shim.

The canonical in-process LRU cache now lives in
``halalchain_shared.cache``. This file re-exports the public surface so
existing ``from .cache import cache`` imports keep working, and the
service-level ``cache`` instance keeps its behaviour (singleton, populated
from settings) for the existing call sites in ``main.py``.
"""

from __future__ import annotations

from halalchain_shared.cache import LRUCache, build_cache  # noqa: F401

from .config import settings

# The shared ``build_cache`` factory returns ``None`` when the cache is
# disabled; we wrap that into a single shared instance that the FastAPI
# app imports as ``cache``. This is the only place an LRU cache should
# be instantiated; the rest of the service uses this singleton.
cache = build_cache(
    strategy=settings.cache_strategy,
    max_size=settings.cache_max_size,
    ttl=settings.cache_ttl,
) if settings.enable_cache else None

