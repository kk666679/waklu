from __future__ import annotations

import hashlib
import hmac
import json
from datetime import datetime, timedelta, timezone
from pathlib import Path
from threading import Lock
from typing import Literal
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field, model_validator


Metric = Literal["temperature", "humidity", "location"]
Unit = Literal["celsius", "percent", "latitude_longitude"]


class SensorObservation(BaseModel):
    """Normalized, signed device telemetry. It is evidence, never a verdict."""

    model_config = ConfigDict(extra="forbid", populate_by_name=True)

    id: UUID
    product_id: UUID = Field(alias="productId")
    device_id: str = Field(alias="deviceId", min_length=1, max_length=128)
    observed_at: datetime = Field(alias="observedAt")
    metric: Metric
    value: float
    unit: Unit
    reading_hash: str = Field(alias="readingHash", pattern=r"^0x[a-f0-9]{64}$")
    signature: str = Field(min_length=64, max_length=128)
    received_at: datetime | None = Field(default=None, alias="receivedAt")
    location: dict[str, float] | None = None

    @model_validator(mode="after")
    def validate_metric_unit(self) -> "SensorObservation":
        expected_units = {
            "temperature": "celsius",
            "humidity": "percent",
            "location": "latitude_longitude",
        }
        if self.unit != expected_units[self.metric]:
            raise ValueError(f"metric {self.metric} requires unit {expected_units[self.metric]}")
        if self.metric == "location":
            if self.location is None:
                raise ValueError("location observations require latitude and longitude")
            if not -90 <= self.location.get("latitude", 999) <= 90:
                raise ValueError("latitude must be between -90 and 90")
            if not -180 <= self.location.get("longitude", 999) <= 180:
                raise ValueError("longitude must be between -180 and 180")
        return self

    def signing_payload(self) -> bytes:
        payload = self.model_dump(
            mode="json", by_alias=True, exclude={"signature"}, exclude_none=True
        )
        return json.dumps(payload, sort_keys=True, separators=(",", ":")).encode()


class ObservationRejected(ValueError):
    """Raised when a device observation cannot enter the evidence stream."""


class ObservationVerifier:
    def __init__(self, device_keys: dict[str, str], max_clock_skew_seconds: int = 300) -> None:
        self._device_keys = device_keys
        self._max_clock_skew = timedelta(seconds=max_clock_skew_seconds)

    def verify(self, observation: SensorObservation, now: datetime | None = None) -> None:
        key = self._device_keys.get(observation.device_id)
        if not key:
            raise ObservationRejected("unknown device")

        expected = hmac.new(
            key.encode(), observation.signing_payload(), hashlib.sha256
        ).hexdigest()
        if not hmac.compare_digest(expected, observation.signature.lower()):
            raise ObservationRejected("invalid device signature")

        current = now or datetime.now(timezone.utc)
        observed = observation.observed_at
        if observed.tzinfo is None:
            observed = observed.replace(tzinfo=timezone.utc)
        if abs(current - observed) > self._max_clock_skew:
            raise ObservationRejected("observation timestamp outside allowed clock skew")


class ObservationStore:
    """Small process-local store; replaceable with the platform evidence repository."""

    def __init__(self, verifier: ObservationVerifier, persistence_path: str | None = None) -> None:
        self._verifier = verifier
        self._persistence_path = Path(persistence_path) if persistence_path else None
        self._lock = Lock()
        self._observations: dict[UUID, SensorObservation] = {}
        self._reading_hashes: set[str] = set()
        self._load()

    def _load(self) -> None:
        if self._persistence_path is None or not self._persistence_path.exists():
            return
        for line in self._persistence_path.read_text().splitlines():
            if not line.strip():
                continue
            observation = SensorObservation.model_validate_json(line)
            self._observations[observation.id] = observation
            self._reading_hashes.add(observation.reading_hash)

    def accept(self, observation: SensorObservation, now: datetime | None = None) -> SensorObservation:
        self._verifier.verify(observation, now)
        with self._lock:
            if observation.id in self._observations or observation.reading_hash in self._reading_hashes:
                raise ObservationRejected("replayed observation")
            if self._persistence_path is not None:
                self._persistence_path.parent.mkdir(parents=True, exist_ok=True)
                with self._persistence_path.open("a", encoding="utf-8") as stream:
                    stream.write(observation.model_dump_json(by_alias=True) + "\n")
            self._observations[observation.id] = observation
            self._reading_hashes.add(observation.reading_hash)
        return observation

    def count(self) -> int:
        return len(self._observations)
