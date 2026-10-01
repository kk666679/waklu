#!/usr/bin/env python3
"""Materialize Alertmanager's secret files from the repo-root ``.env``.

Alertmanager does **not** expand ``${VAR}`` in its configuration file, so a
config that references ``${SLACK_WEBHOOK_URL}`` is read literally and fails to
load. The supported mechanism is the ``*_file`` field variants
(``slack_api_url_file``, ``routing_key_file``, ...), which read the secret from
a file at notify time.

This script bridges the existing ``.env`` workflow to that mechanism: it reads
the observability secrets from ``.env`` and writes one file per secret into
``infrastructure/alertmanager/secrets/``, which
``docker-compose.observability.yml`` mounts at ``/etc/alertmanager/secrets``.

Placeholders (values that still look like ``...``, ``REPLACE_ME`` or ``xxx``)
are written as empty strings on purpose: Alertmanager still starts and the
config still validates, and notifications for that integration fail loudly at
send time instead of the whole process refusing to boot.

Usage:
    python3 infrastructure/scripts/write-alertmanager-secrets.py
    python3 infrastructure/scripts/write-alertmanager-secrets.py --check
"""

from __future__ import annotations

import argparse
import os
import re
import sys
from pathlib import Path

REPO_MARKER = "HalalChain.Platform.sln"

# secret file name -> env var it is populated from
SECRETS: dict[str, str] = {
    "slack_webhook_url": "SLACK_WEBHOOK_URL",
    "pagerduty_key": "PAGERDUTY_KEY",
    "pagerduty_p0_key": "PAGERDUTY_P0_KEY",
    "smtp_pass": "SMTP_PASS",
}

# Values that are documentation placeholders rather than real secrets. These
# are written empty so a fresh clone boots without inventing credentials.
PLACEHOLDER = re.compile(
    r"^(?:\.{3}|x+|y+|REPLACE_ME.*|SG\.x+|changeme|xxx)$", re.IGNORECASE
)


def find_root(start: Path) -> Path:
    for candidate in (start, *start.parents):
        if (candidate / REPO_MARKER).is_file():
            return candidate
    raise SystemExit(
        f"Cannot locate the repository root: no {REPO_MARKER} found at or above {start}."
    )


ROOT = find_root(Path(__file__).resolve().parent)
ENV_PATH = ROOT / ".env"
SECRETS_DIR = ROOT / "infrastructure" / "alertmanager" / "secrets"


def parse_env(path: Path) -> dict[str, str]:
    """Parse ``.env`` without requiring python-dotenv.

    Only the subset this repo actually uses is supported: ``KEY=value`` lines,
    an optional ``export`` prefix, optional surrounding quotes, and ``#``
    comments on their own line.
    """
    if not path.is_file():
        return {}

    values: dict[str, str] = {}
    # utf-8-sig drops a byte-order mark if present. Editors on Windows add one
    # by default, and it would otherwise fuse onto the FIRST key's name, making
    # that secret look unset while every other key parsed fine.
    text = path.read_text(encoding="utf-8-sig")
    for raw_line in text.splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#"):
            continue
        if line.startswith("export "):
            line = line[len("export ") :].strip()
        key, sep, value = line.partition("=")
        if not sep:
            continue
        key = key.strip()
        value = value.strip()
        if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
            value = value[1:-1]
        values[key] = value
    return values


def resolve(value: str | None) -> str:
    """Map a raw ``.env`` value to the string Alertmanager will read."""
    if not value:
        return ""
    value = value.strip()
    if not value or PLACEHOLDER.match(value):
        return ""
    return value


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Write Alertmanager secret files from the repo-root .env."
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="verify the secret files match .env without writing (exit 1 if stale)",
    )
    args = parser.parse_args()

    if not ENV_PATH.is_file() and not args.check:
        print(
            f"No {ENV_PATH.name} found. Copy .env.example to {ENV_PATH.name} and set\n"
            "the observability secrets; empty placeholders are written for now.",
            file=sys.stderr,
        )

    env = parse_env(ENV_PATH)
    SECRETS_DIR.mkdir(parents=True, exist_ok=True)

    missing: list[str] = []
    stale: list[str] = []

    for filename, var in SECRETS.items():
        raw = env.get(var)
        if raw is None:
            missing.append(var)
        secret = resolve(raw)
        target = SECRETS_DIR / filename

        if args.check:
            current = target.read_text(encoding="utf-8") if target.is_file() else None
            ok = current == secret
            print(f" - {filename:<20} {'ok' if ok else 'stale or missing'}")
            if not ok:
                stale.append(filename)
            continue

        # 0600: these are live credentials on a developer machine.
        target.write_text(secret, encoding="utf-8")
        try:
            os.chmod(target, 0o600)
        except OSError:
            # Windows and some mounted filesystems do not honour POSIX modes.
            pass
        suffix = "" if secret else f"  (empty - set {var})"
        print(f" - {filename}{suffix}")

    if args.check:
        if stale:
            print(
                "Alertmanager secrets are missing or stale. Re-run without --check:\n"
                "    python3 infrastructure/scripts/write-alertmanager-secrets.py"
            )
            return 1
        print("Alertmanager secrets are up to date.")
        return 0

    print(f"\nWrote {len(SECRETS)} secret file(s) to {SECRETS_DIR.relative_to(ROOT)}.")
    if missing:
        print(
            f"Not present in {ENV_PATH.name}: {', '.join(missing)}.\n"
            f"Add them to {ENV_PATH.name} (see .env.example) to enable those receivers.",
            file=sys.stderr,
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
