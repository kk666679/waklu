"""Shared HTTP client for cross-service communication."""
from __future__ import annotations

import os
from typing import Any, Optional
import httpx
from pydantic import BaseModel


class ServiceClient:
    """Base HTTP client for inter-service communication."""

    def __init__(
        self,
        base_url: str,
        api_key: Optional[str] = None,
        timeout: float = 30.0,
    ):
        self.base_url = base_url.rstrip("/")
        self.api_key = api_key
        self.timeout = timeout
        self._client: Optional[httpx.AsyncClient] = None

    async def _get_client(self) -> httpx.AsyncClient:
        if self._client is None or self._client.is_closed:
            headers = {"Content-Type": "application/json"}
            if self.api_key:
                headers["X-API-Key"] = self.api_key
            self._client = httpx.AsyncClient(
                base_url=self.base_url,
                headers=headers,
                timeout=self.timeout,
            )
        return self._client

    async def close(self):
        if self._client and not self._client.is_closed:
            await self._client.aclose()
            self._client = None

    async def get(self, path: str, **kwargs) -> httpx.Response:
        client = await self._get_client()
        return await client.get(path, **kwargs)

    async def post(self, path: str, **kwargs) -> httpx.Response:
        client = await self._get_client()
        return await client.post(path, **kwargs)

    async def put(self, path: str, **kwargs) -> httpx.Response:
        client = await self._get_client()
        return await client.put(path, **kwargs)

    async def delete(self, path: str, **kwargs) -> httpx.Response:
        client = await self._get_client()
        return await client.delete(path, **kwargs)


class AIInferenceClient(ServiceClient):
    """Client for ai-inference service."""

    async def embeddings(self, text: str, model: Optional[str] = None) -> dict:
        response = await self.post(
            "/embeddings",
            json={"text": text, "model": model},
        )
        response.raise_for_status()
        return response.json()

    async def classify(self, text: str, labels: list[str], model: Optional[str] = None) -> dict:
        response = await self.post(
            "/classify",
            json={"text": text, "labels": labels, "model": model},
        )
        response.raise_for_status()
        return response.json()

    async def summarize(self, text: str, max_tokens: int = 80, model: Optional[str] = None) -> dict:
        response = await self.post(
            "/summarize",
            json={"text": text, "maxTokens": max_tokens, "model": model},
        )
        response.raise_for_status()
        return response.json()

    async def rerank(self, query: str, passages: list[str], model: Optional[str] = None) -> dict:
        response = await self.post(
            "/rerank",
            json={"query": query, "passages": passages, "model": model},
        )
        response.raise_for_status()
        return response.json()

    async def llm_generate(
        self,
        prompt: str,
        max_tokens: int = 100,
        temperature: float = 0.7,
    ) -> dict:
        response = await self.post(
            "/llm/generate",
            json={"prompt": prompt, "max_tokens": max_tokens, "temperature": temperature},
        )
        response.raise_for_status()
        return response.json()

    async def rag_add_documents(self, documents: list[dict]) -> dict:
        response = await self.post("/rag/add-documents", json=documents)
        response.raise_for_status()
        return response.json()

    async def rag_search(self, query: str, top_k: int = 5) -> dict:
        response = await self.post(
            "/rag/search",
            json={"query": query, "top_k": top_k},
        )
        response.raise_for_status()
        return response.json()

    async def health(self) -> dict:
        response = await self.get("/health/ready")
        return response.json()


class TawheedClient(ServiceClient):
    """Client for tawheed service."""

    async def evaluate_policy(self, product_id: str, context: dict) -> dict:
        response = await self.post(
            "/api/v1/evaluate",
            json={"product_id": product_id, "context": context},
        )
        response.raise_for_status()
        return response.json()

    async def query_evidence(self, query: str, top_k: int = 10) -> dict:
        response = await self.post(
            "/api/v1/evidence/query",
            json={"query": query, "top_k": top_k},
        )
        response.raise_for_status()
        return response.json()

    async def health(self) -> dict:
        response = await self.get("/health")
        return response.json()


class LocalModelsClient(ServiceClient):
    """Client for local-models service."""

    async def embeddings(self, text: str, model: Optional[str] = None) -> dict:
        response = await self.post(
            "/embeddings",
            json={"text": text, "model": model},
        )
        response.raise_for_status()
        return response.json()

    async def classify(self, text: str, labels: list[str], model: Optional[str] = None) -> dict:
        response = await self.post(
            "/classify",
            json={"text": text, "labels": labels, "model": model},
        )
        response.raise_for_status()
        return response.json()

    async def generate(
        self,
        prompt: str,
        max_tokens: int = 100,
        temperature: float = 0.7,
        model: Optional[str] = None,
    ) -> dict:
        response = await self.post(
            "/generate",
            json={"prompt": prompt, "max_tokens": max_tokens, "temperature": temperature, "model": model},
        )
        response.raise_for_status()
        return response.json()

    async def health(self) -> dict:
        response = await self.get("/health/ready")
        return response.json()


def create_ai_inference_client() -> AIInferenceClient:
    """Create AI Inference client from environment."""
    base_url = os.getenv("AI_INFERENCE_URL", "http://localhost:7071")
    api_key = os.getenv("AI_GATEWAY_API_KEY")
    return AIInferenceClient(base_url, api_key)


def create_tawheed_client() -> TawheedClient:
    """Create Tawheed client from environment."""
    base_url = os.getenv("TAWHEED_URL", "http://localhost:8000")
    api_key = os.getenv("TAWHEED_API_KEY")
    return TawheedClient(base_url, api_key)


def create_local_models_client() -> LocalModelsClient:
    """Create Local Models client from environment."""
    base_url = os.getenv("LOCAL_MODELS_URL", "http://localhost:8080")
    api_key = os.getenv("LOCAL_MODELS_API_KEY")
    return LocalModelsClient(base_url, api_key)


__all__ = [
    "ServiceClient",
    "AIInferenceClient",
    "TawheedClient",
    "LocalModelsClient",
    "create_ai_inference_client",
    "create_tawheed_client",
    "create_local_models_client",
]