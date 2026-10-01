import pytest
from fastapi.testclient import TestClient

from src import main


@pytest.fixture
def client():
    return TestClient(main.app)


def test_embeddings_uses_configured_model_and_dimension(client, monkeypatch):
    monkeypatch.setattr(main, "_encode_embedding", lambda text: [0.6, 0.8])

    response = client.post("/embeddings", json={"text": "unseen unavailable query"})

    assert response.status_code == 200
    assert response.json() == {
        "model": main.settings.embedding_model,
        "embedding": [0.6, 0.8],
        "dimension": 2,
        "cached": False,
    }


def test_embeddings_rejects_unconfigured_model(client):
    response = client.post(
        "/embeddings",
        json={"text": "provided query", "model": "unconfigured-model"},
    )

    assert response.status_code == 400


def test_embeddings_report_model_unavailable(client, monkeypatch):
    def fail_model_load(text):
        raise RuntimeError("model weights unavailable")

    monkeypatch.setattr(main, "_encode_embedding", fail_model_load)

    response = client.post("/embeddings", json={"text": "provided query"})

    assert response.status_code == 503
    assert response.json()["detail"] == "Configured embedding model is unavailable"
