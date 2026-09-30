#!/usr/bin/env python3
"""Validate documentation and runtime declarations stay in sync.

Two declarations have to agree with docker-compose.yml:

* ``service-manifest.yaml`` — the frozen image list. Every compose service needs
  an entry, every entry needs a compose service, tags must be explicit (no
  ``:latest``, no bare major), and a service that declares an ``image:`` in
  compose must use exactly the frozen tag.
* ``docs/RUNTIME-MATRIX.md`` — the human-facing inventory. Every compose service
  must be listed, and the documented port must be one the service actually
  publishes or exposes.

The manifest used to carry a ``services:`` block of host ports. It is now an
image registry (``images:`` / ``third_party:`` / ``policy:``), so the port-drift
check moved to RUNTIME-MATRIX.md, which already held the same table.
"""

from __future__ import annotations

import re
from pathlib import Path

# Walk up to the repo root by marker file rather than by a fixed `..` hop.
# This file moved under infrastructure/ in commit 5e58296, so
# `parent.parent` began resolving to <root>/infrastructure and every
# repository-rooted path below silently pointed at a non-existent file.
REPO_MARKER = "HalalChain.Platform.sln"


def _find_root(start: Path) -> Path:
    for candidate in (start, *start.parents):
        if (candidate / REPO_MARKER).is_file():
            return candidate
    raise SystemExit(
        f"Cannot locate the repository root: no {REPO_MARKER} found at or above {start}."
    )


ROOT = _find_root(Path(__file__).resolve().parent)
COMPOSE_PATH = ROOT / "docker-compose.yml"
MANIFEST_PATH = ROOT / "service-manifest.yaml"
RUNTIME_MATRIX_PATH = ROOT / "docs" / "RUNTIME-MATRIX.md"

# Top-level manifest blocks that map a service name to a frozen image tag.
MANIFEST_IMAGE_BLOCKS = ("images", "third_party")
MANIFEST_POLICY_BLOCK = "policy"


def parse_service_manifest(path: Path) -> tuple[dict[str, str], dict[str, bool]]:
    """Return (frozen image per service, image-tag policy flags).

    The manifest is a two-level YAML subset: top-level keys holding either a
    nested block or a scalar. A line parser is enough here and keeps this gate
    free of a PyYAML dependency on the runner image.
    """
    images: dict[str, str] = {}
    policy: dict[str, bool] = {}
    block: str | None = None

    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.rstrip()
        if not line.strip() or line.lstrip().startswith("#"):
            continue

        top_level = re.match(r"^([A-Za-z0-9_-]+):\s*(.*)$", line)
        if top_level:
            block = top_level.group(1)
            continue

        entry = re.match(r"^  ([A-Za-z0-9_-]+):\s*(.*)$", line)
        if not entry or block is None:
            continue
        name, value = entry.group(1), entry.group(2).strip().strip('"').strip("'")
        if block in MANIFEST_IMAGE_BLOCKS:
            images[name] = value
        elif block == MANIFEST_POLICY_BLOCK:
            policy[name] = value.lower() == "true"

    return images, policy


def split_image_tag(reference: str) -> tuple[str, str]:
    """Split an image reference into (name, tag).

    A pinned digest stands in for a tag, and a colon belonging to a registry
    host (``registry:5000/name``) is not a tag separator.
    """
    if "@" in reference:
        name, _, digest = reference.partition("@")
        return name, digest
    name, separator, tag = reference.rpartition(":")
    if separator and "/" in tag:
        return reference, ""
    return name, tag


def parse_compose(path: Path) -> tuple[dict[str, set[int]], dict[str, str]]:
    """Return (published ports per service, declared image tag per service).

    A port lands in the set from either ``ports:`` (both sides of the mapping)
    or ``expose:``, which is what a human-facing port table documents.
    """
    services: dict[str, set[int]] = {}
    images: dict[str, str] = {}
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
            images.pop(current, None)
            in_ports = False
            in_expose = False
            continue
        if current is None:
            continue
        image_match = re.match(r"^    image:\s*(\S+)\s*$", line)
        if image_match:
            images[current] = image_match.group(1).strip('"').strip("'")
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

    return services, images


def parse_runtime_matrix(path: Path) -> dict[str, int | None]:
    """Return the documented port per service from the RUNTIME-MATRIX tables.

    A prose port cell (``none (outbound only)``, used by the non-compose runtime
    hosts) maps to None. Rows for hosts that are not compose services are simply
    never looked up, so documenting them stays legitimate.
    """
    documented: dict[str, int | None] = {}
    row = re.compile(r"^\|\s*`([^`]+)`\s*\|([^|]*)\|")
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line.startswith("|"):
            continue
        match = row.match(line)
        if not match or match.group(1) in documented:
            continue
        port_match = re.search(r"\d+", match.group(2))
        documented[match.group(1)] = int(port_match.group(0)) if port_match else None
    return documented


def main() -> int:
    for path, label in (
        (COMPOSE_PATH, "compose file"),
        (MANIFEST_PATH, "service manifest"),
        (RUNTIME_MATRIX_PATH, "runtime matrix"),
    ):
        if not path.exists():
            print(f"Missing {label}: {path}")
            return 1

    compose_ports, compose_images = parse_compose(COMPOSE_PATH)
    frozen_images, tag_policy = parse_service_manifest(MANIFEST_PATH)
    documented_ports = parse_runtime_matrix(RUNTIME_MATRIX_PATH)

    errors: list[str] = []

    # 1. The frozen image list must describe exactly the compose stack.
    for service_name in sorted(set(compose_ports) - set(frozen_images)):
        errors.append(
            f"Service {service_name!r} exists in docker-compose.yml but has no "
            "frozen image in service-manifest.yaml"
        )
    for service_name in sorted(set(frozen_images) - set(compose_ports)):
        errors.append(
            f"Frozen image {service_name!r} in service-manifest.yaml has no "
            "docker-compose.yml service"
        )

    # 2. Tags must be explicit — no floating :latest, no bare major.
    forbid_latest = tag_policy.get("forbid_latest", True)
    forbid_bare_major = tag_policy.get("forbid_bare_major", True)
    for service_name, reference in sorted(frozen_images.items()):
        _, tag = split_image_tag(reference)
        if not tag:
            errors.append(
                f"Frozen image {service_name!r} in service-manifest.yaml has no "
                f"explicit tag: {reference}"
            )
        elif tag == "latest" and forbid_latest:
            errors.append(
                f"Frozen image {service_name!r} in service-manifest.yaml uses the "
                "floating ':latest' tag"
            )
        elif forbid_bare_major and re.fullmatch(r"\d+(\.\d+)?", tag):
            errors.append(
                f"Frozen image {service_name!r} in service-manifest.yaml uses the "
                f"bare major tag ':{tag}'"
            )

    # 3. A service that pins an image in compose must run the frozen tag.
    for service_name, reference in sorted(compose_images.items()):
        frozen = frozen_images.get(service_name)
        if frozen is not None and frozen != reference:
            errors.append(
                f"Image tag drift for {service_name!r}: docker-compose.yml uses "
                f"{reference}, service-manifest.yaml freezes {frozen}"
            )

    # 4. The human-facing inventory must cover the compose stack and its ports.
    for service_name in sorted(compose_ports):
        if service_name not in documented_ports:
            errors.append(
                f"Service {service_name!r} exists in docker-compose.yml but is not "
                "listed in docs/RUNTIME-MATRIX.md"
            )
            continue
        documented = documented_ports[service_name]
        if documented is None:
            errors.append(
                f"Service {service_name!r} has no port documented in "
                "docs/RUNTIME-MATRIX.md"
            )
        elif documented not in compose_ports[service_name]:
            errors.append(
                f"Port mismatch for {service_name!r}: docs/RUNTIME-MATRIX.md says "
                f"{documented}, docker-compose.yml publishes "
                f"{sorted(compose_ports[service_name])}"
            )

    if errors:
        print("Documentation drift detected:")
        for error in errors:
            print(f" - {error}")
        return 1

    print(
        f"Runtime inventory matches: {len(compose_ports)} compose services, "
        f"{len(frozen_images)} frozen images, "
        f"{len(documented_ports)} runtime-matrix rows."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
