"""Test cross-service communication."""
import os
import asyncio
import pytest
from unittest.mock import AsyncMock, patch, MagicMock


@pytest.mark.asyncio
async def test_ai_inference_client_embeddings():
    from halalchain_shared.service_client import AIInferenceClient

    client = AIInferenceClient("http://test:7071", "test-key")

    # Mock the HTTP client
    mock_response = MagicMock()
    mock_response.json.return_value = {"model": "test", "embedding": [0.1] * 256, "cached": False}
    mock_response.raise_for_status = MagicMock()

    client._client = AsyncMock()
    client._client.post = AsyncMock(return_value=mock_response)

    result = await client.embeddings("test text")
    assert result["model"] == "test"
    assert len(result["embedding"]) == 256

    await client.close()


@pytest.mark.asyncio
async def test_tawheed_client_evaluate():
    from halalchain_shared.service_client import TawheedClient

    client = TawheedClient("http://test:8000", "test-key")

    mock_response = MagicMock()
    mock_response.json.return_value = {"status": "halal", "confidence": 0.9}
    mock_response.raise_for_status = MagicMock()

    client._client = AsyncMock()
    client._client.post = AsyncMock(return_value=mock_response)

    result = await client.evaluate_policy("prod-123", {"ingredients": ["chicken", "rice"]})
    assert result["status"] == "halal"

    await client.close()


@pytest.mark.asyncio
async def test_local_models_client_generate():
    from halalchain_shared.service_client import LocalModelsClient

    client = LocalModelsClient("http://test:8080")

    mock_response = MagicMock()
    mock_response.json.return_value = {"model": "default", "response": "Generated text"}
    mock_response.raise_for_status = MagicMock()

    client._client = AsyncMock()
    client._client.post = AsyncMock(return_value=mock_response)

    result = await client.generate("Test prompt")
    assert result["response"] == "Generated text"

    await client.close()


@pytest.mark.asyncio
async def test_create_clients_from_env():
    with patch.dict(os.environ, {
        "AI_INFERENCE_URL": "http://ai-inference:7071",
        "TAWHEED_URL": "http://tawheed:8000",
        "LOCAL_MODELS_URL": "http://local-models:8080",
        "AI_GATEWAY_API_KEY": "test-key",
    }):
        from halalchain_shared.service_client import (
            create_ai_inference_client,
            create_tawheed_client,
            create_local_models_client,
        )

        ai_client = create_ai_inference_client()
        assert ai_client.base_url == "http://ai-inference:7071"
        assert ai_client.api_key == "test-key"
        await ai_client.close()

        tawheed_client = create_tawheed_client()
        assert tawheed_client.base_url == "http://tawheed:8000"
        await tawheed_client.close()

        local_client = create_local_models_client()
        assert local_client.base_url == "http://local-models:8080"
        await local_client.close()