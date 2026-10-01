"""Test ai-inference service with local-models backend."""
import os
import pytest
from unittest.mock import AsyncMock, MagicMock, patch
from fastapi.testclient import TestClient

TEST_API_KEY = "test-only-key-for-local-model-tests-1234567890"


@pytest.fixture
def mock_local_models():
    """Mock local-models service responses."""
    with patch.dict(os.environ, {
        "AI_GATEWAY_API_KEY": TEST_API_KEY,
        "ENVIRONMENT": "development",
        "AI_BACKEND": "local",
    }):
        from src import embeddings

        with patch.object(embeddings, "create_local_models_client") as mock_create:
            mock_client = AsyncMock()
            mock_client.embeddings = AsyncMock(return_value={
                "model": "sentence-transformers/all-MiniLM-L6-v2",
                "embedding": [0.1] * 384,
            })
            mock_client.classify = AsyncMock(return_value={
                "model": "local", "bestLabel": "halal", "bestScore": 0.9, "scores": {"halal": 0.9, "haram": 0.1}
            })
            mock_client.generate = AsyncMock(return_value={"model": "local", "response": "Generated"})
            mock_client.close = AsyncMock()
            mock_create.return_value = mock_client
            yield mock_client


@pytest.mark.asyncio
async def test_ai_inference_with_local_models_backend(mock_local_models, monkeypatch):
    """Test ai-inference uses local-models when configured."""
    from src import embeddings

    monkeypatch.setattr(embeddings.settings, "embedding_provider", "local-models")
    result = await embeddings.generate_embedding("test text")
    assert len(result) == 384
    mock_local_models.embeddings.assert_awaited_once_with(
        "test text", "sentence-transformers/all-MiniLM-L6-v2"
    )


@pytest.fixture
def client():
    """Create test client for ai-inference."""
    with patch.dict(os.environ, {
        "AI_GATEWAY_API_KEY": TEST_API_KEY,
        "ENVIRONMENT": "development",
        "AI_BACKEND": "local",
    }):
        from src.main import app
        return TestClient(app)


def test_health_endpoints(client):
    """Test health endpoints."""
    response = client.get("/health/live")
    assert response.status_code == 200
    assert response.json()["status"] == "ok"

    response = client.post("/health")
    assert response.status_code == 200


def test_embeddings_endpoint(client, mock_local_models):
    """Test embeddings endpoint."""
    response = client.post(
        "/embeddings",
        json={"text": "test text"},
        headers={"X-API-Key": TEST_API_KEY},
    )
    assert response.status_code == 200
    data = response.json()
    assert "embedding" in data
    assert len(data["embedding"]) == 384


@pytest.mark.asyncio
async def test_embedding_without_provider_fails_closed(monkeypatch):
    from src import embeddings

    monkeypatch.setattr(embeddings.settings, "embedding_provider", "local")
    with pytest.raises(RuntimeError, match="No embedding provider is configured"):
        await embeddings.generate_embedding("query")


def test_classify_endpoint(client, mock_local_models):
    """Test classify endpoint."""
    response = client.post(
        "/classify",
        json={"text": "chicken rice", "labels": ["halal", "haram"]},
        headers={"X-API-Key": TEST_API_KEY},
    )
    assert response.status_code == 200
    data = response.json()
    assert "bestLabel" in data