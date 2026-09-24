import hashlib
import hmac
import json
from datetime import datetime, timedelta, timezone
from uuid import uuid4

import pytest

from src.adapters.iot import ObservationRejected, ObservationStore, ObservationVerifier, SensorObservation


DEVICE_ID = "cold-chain-01"
DEVICE_KEY = "device-secret"
PRODUCT_ID = uuid4()


def _observation(**overrides) -> SensorObservation:
    values = {
        "id": uuid4(),
        "product_id": PRODUCT_ID,
        "device_id": DEVICE_ID,
        "observed_at": datetime.now(timezone.utc),
        "metric": "temperature",
        "value": 4.2,
        "unit": "celsius",
        "reading_hash": "0x" + "a" * 64,
        "signature": "0" * 64,
    }
    values.update(overrides)
    unsigned = SensorObservation(**values)
    values["signature"] = hmac.new(
        DEVICE_KEY.encode(), unsigned.signing_payload(), hashlib.sha256
    ).hexdigest()
    return SensorObservation(**values)


def test_signed_observation_is_accepted_once():
    store = ObservationStore(ObservationVerifier({DEVICE_ID: DEVICE_KEY}))
    observation = _observation()

    store.accept(observation)

    assert store.count() == 1
    with pytest.raises(ObservationRejected, match="replayed"):
        store.accept(observation)


def test_tampered_observation_is_rejected():
    verifier = ObservationVerifier({DEVICE_ID: DEVICE_KEY})
    observation = _observation(value=4.2)
    tampered = observation.model_copy(update={"value": 9.9})

    with pytest.raises(ObservationRejected, match="signature"):
        verifier.verify(tampered)


def test_stale_observation_is_rejected():
    verifier = ObservationVerifier({DEVICE_ID: DEVICE_KEY}, max_clock_skew_seconds=60)
    observation = _observation(observed_at=datetime.now(timezone.utc) - timedelta(minutes=2))

    with pytest.raises(ObservationRejected, match="clock skew"):
        verifier.verify(observation)


def test_location_observation_requires_valid_coordinates():
    with pytest.raises(ValueError, match="latitude"):
        _observation(
            metric="location",
            unit="latitude_longitude",
            location={"latitude": 91, "longitude": 10},
        )


def test_persistent_store_reloads_observations(tmp_path):
    path = tmp_path / "observations.jsonl"
    observation = _observation(reading_hash="0x" + "f" * 64)
    verifier = ObservationVerifier({DEVICE_ID: DEVICE_KEY})

    ObservationStore(verifier, str(path)).accept(observation)
    restored = ObservationStore(verifier, str(path))

    assert restored.count() == 1
    with pytest.raises(ObservationRejected, match="replayed"):
        restored.accept(observation)
