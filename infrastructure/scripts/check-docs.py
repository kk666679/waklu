#!/usr/bin/env python3
"""Validate documentation and runtime declarations stay in sync."""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
COMPOSE_PATH = ROOT / "docker-compose.yml"
MANIFEST_PATH = ROOT / "service-manifest.yaml"
RUNTIME_MATRIX_PATH = ROOT / "docs" / "RUNTIME-MATRIX.md"


def parse_service_manifest(path: Path) -> dict[str, int]:
    services: dict[str, int] = {}
    current: str | None = None
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.rstrip()
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        if re.match(r"^services:\s*$", line):
            continue
        match = re.match(r"^  ([A-Za-z0-9_-]+):\s*$", line)
        if match:
            current = match.group(1)
            services[current] = -1
            continue
        if current is not None:
            port_match = re.match(r"^    port:\s*(\d+)\s*$", line)
            if port_match:
                services[current] = int(port_match.group(1))
    return services


def parse_compose(path: Path) -> dict[str, set[int]]:
    services: dict[str, set[int]] = {}
    current: str | None = None
    in_ports = False
    in_expose = False
    in_services = False

    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.rstrip()
        if not line.strip() or line.lstrip().startswith("#"):
            continue

        if re.match(r"^services:\s*$", line):
            in_services = True
            continue
        if in_services and re.match(r"^[A-Za-z0-9_-]+:\s*$", line) and not line.startswith("  "):
            in_services = False
            current = None

        if not in_services:
            continue

        service_match = re.match(r"^  ([A-Za-z0-9_-]+):\s*$", line)
        if service_match:
            current = service_match.group(1)
            services[current] = set()
            in_ports = False
            in_expose = False
            continue
        if current is None:
            continue
        if re.match(r"^    (ports|expose):\s*$", line):
            key = re.match(r"^    (ports|expose):\s*$", line).group(1)
            in_ports = key == "ports"
            in_expose = key == "expose"
            continue
        if in_ports:
            port_match = re.match(r'^\s{6}-\s*["\']?(\d+):(\d+)["\']?\s*$', line)
            if port_match:
                services[current].add(int(port_match.group(1)))
                services[current].add(int(port_match.group(2)))
                continue
        if in_expose:
            port_match = re.match(r'^\s{6}-\s*["\']?(\d+)["\']?\s*$', line)
            if port_match:
                services[current].add(int(port_match.group(1)))
                continue

    return services


def main() -> int:
    if not COMPOSE_PATH.exists():
        print(f"Missing compose file: {COMPOSE_PATH}")
        return 1
    if not MANIFEST_PATH.exists():
        print(f"Missing service manifest: {MANIFEST_PATH}")
        return 1
    if not RUNTIME_MATRIX_PATH.exists():
        print(f"Missing runtime matrix: {RUNTIME_MATRIX_PATH}")
        return 1

    manifest = parse_service_manifest(MANIFEST_PATH)
    compose = parse_compose(COMPOSE_PATH)

    errors: list[str] = []
    for service_name, manifest_port in sorted(manifest.items()):
        if service_name not in compose:
            errors.append(f"Service {service_name!r} exists in manifest but not in docker-compose.yml")
            continue
        if manifest_port == -1:
            errors.append(f"Service {service_name!r} is missing a port in service-manifest.yaml")
            continue
        if manifest_port not in compose[service_name]:
            errors.append(
                f"Port mismatch for {service_name!r}: manifest says {manifest_port}, "
                f"docker-compose exposes {sorted(compose[service_name])}"
            )

    for service_name in sorted(set(compose) - set(manifest)):
        errors.append(f"Service {service_name!r} exists in docker-compose.yml but not in service-manifest.yaml")

    if errors:
        print("Documentation drift detected:")
        for error in errors:
            print(f" - {error}")
        return 1

    print(f"Runtime inventory matches: {len(manifest)} services checked.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
