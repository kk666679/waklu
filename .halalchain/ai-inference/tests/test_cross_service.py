"""Test ai-inference service with local-models backend."""
import os
import pytest
from unittest.mock import AsyncMock, MagicMock, patch
from fastapi.testclient import TestClient


@pytest.fixture
def mock_local_models():
    """Mock local-models service responses."""
    with patch("halalchain_shared.service_client.create_local_models_client") as mock_create:
        mock_client = AsyncMock()
        mock_client.embeddings = AsyncMock(return_value={"model": "local", "embedding": [0.1] * 256})
        mock_client.classify = AsyncMock(return_value={
            "model": "local", "bestLabel": "halal", "bestScore": 0.9, "scores": {"halal": 0.9, "haram": 0.1}
        })
        mock_client.generate = AsyncMock(return_value={"model": "local", "response": "Generated"})
        mock_client.close = AsyncMock()
        mock_create.return_value = mock_client
        yield mock_client


def test_ai_inference_with_local_models_backend(mock_local_models):
    """Test ai-inference uses local-models when configured."""
    with patch.dict(os.environ, {
        "EMBEDDING_PROVIDER": "local-models",
        "LOCAL_MODELS_URL": "http://local-models:8080",
        "AI_GATEWAY_API_KEY": "test-key",
        "AI_BACKEND": "local",
    }):
        # Reload modules to pick up new env vars
        import importlib
        from ai_inference.src import embeddings
        importlib.reload(embeddings)

        # Test embedding generation
        result = embeddings.generate_embedding("test text")
        assert len(result) == 256


@pytest.fixture
def client():
    """Create test client for ai-inference."""
    with patch.dict(os.environ, {
        "AI_GATEWAY_API_KEY": "test-key",
        "ENVIRONMENT": "development",
        "AI_BACKEND": "local",
    }):
        from ai_inference.src.main import app
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
        headers={"X-API-Key": "test-key"},
    )
    assert response.status_code == 200
    data = response.json()
    assert "embedding" in data
    assert len(data["embedding"]) == 256


def test_classify_endpoint(client, mock_local_models):
    """Test classify endpoint."""
    response = client.post(
        "/classify",
        json={"text": "chicken rice", "labels": ["halal", "haram"]},
        headers={"X-API-Key": "test-key"},
    )
    assert response.status_code == 200
    data = response.json()
    assert "bestLabel" in data