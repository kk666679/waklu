# `.halalchain/requirements/`

Hash-pinned dependency locks for every Python service in the `.halalchain/`
tier.

## Purpose

This directory is the reproducibility boundary for the Python tier. Each file
is a `uv pip compile` output with `--generate-hashes`, so an install is both
deterministic and tamper-evident. CI re-compiles these files and byte-diffs the
result — if a transitive dependency changes underneath the project, the job
fails rather than silently shipping a different image.

## Structure

| File | Lines | Generated from | Notable pins |
| --- | --- | --- | --- |
| `base.txt` | 582 | `_shared/pyproject.toml --extra base` | `fastapi 0.120.0`, `uvicorn 0.40.0`, `httpx 0.28.0`, `pydantic 2.13.5`, `structlog 25.1.0` |
| `dev.txt` | 813 | `_shared/pyproject.toml --extra dev` | `pytest 8.4.0`, `pytest-asyncio 0.24.0`, `pytest-httpx 0.35.0`, `coverage 7.10.0`, `ruff 0.9.0`, `mypy 1.15.0` |
| `llm.txt` | 744 | `_shared/pyproject.toml --extra llm` | `openai 3.22.1` + base closure |
| `mcp.txt` | 933 | `_shared/pyproject.toml --extra mcp` | `mcp 2.2.0`, `mcp-types 2.2.0`, `sse-starlette`, `jsonschema` |
| `ai-inference.txt` | 1408 | `ai-inference/pyproject.toml` | `anthropic 1.11.0`, `qdrant-client 1.19.1`, `pypdf 6.19.0`, `python-docx 1.2.0`, `openpyxl 3.1.5`, `beautifulsoup4 4.15.0`, `prometheus-fastapi-instrumentator 7.1.0` |
| `tawheed.txt` | 894 | `tawheed/pyproject.toml --extra observability --extra llm` | 46 packages total; `prometheus-client`, `opentelemetry` 1.45.0, `openai 3.22.1` |
| `agents.txt` | 1364 | `agents/pyproject.toml --extra vector` | `qdrant-client 1.19.1`, `fastembed 0.8.1`, `onnxruntime 1.30.0`, `huggingface-hub 1.33.0` |
| `local-models.txt` | 2625 | `local-models/pyproject.toml --extra inference --extra quantization --extra gguf` | `torch 2.14.0`, `transformers 5.17.0`, `bitsandbytes 0.50.2`, `auto-gptq 0.7.1`, `llama-cpp-python 0.3.35`, plus the CUDA 13 stack |
| `local-models-gguf.txt` | 762 | `local-models/pyproject.toml --extra gguf` | `llama-cpp-python 0.3.35` + base |
| `vector.txt` | 1375 | `agents/pyproject.toml --extra vector` | Same body as `agents.txt`, plus a leading `-e file:///…/_shared` absolute path |

Every service's own `requirements.txt` is a one-line include, for example
`ai-inference/requirements.txt` contains only `-r ../requirements/ai-inference.txt`.

## Dependencies

`uv` with `astral-sh/setup-uv@v4` pinned to `version: "0.12.9"`. The workspace
root `.halalchain/pyproject.toml` constrains `qdrant-client==1.19.1`.

## Usage

```bash
# Install a service
cd .halalchain/ai-inference
pip install -r requirements.txt
pip install -e .          # the service itself; pip cannot hash-check a local requirement

# Recompile a lock
cd .halalchain
uv pip compile ai-inference/pyproject.toml \
  --python-version 3.11 --python-platform x86_64-unknown-linux-gnu \
  --generate-hashes --no-emit-package halalchain-shared
```

## CI verification

`.github/workflows/python-locks.yml` re-compiles 8 of the 10 locks and
byte-diffs them against the committed files. It uses
`--exclude-newer-package` overrides for `mcp`, `mcp-types`, and `deepeval` so
those packages resolve identically over time.

Two files are deliberately **not** in the verification matrix:

- `vector.txt` — differs from `agents.txt` only by a leading absolute
  `file:///` path, which would not match across machines
- `llm.txt` — documented as deliberately unverifiable in the workflow

## Related Components

- [.halalchain/](../README.md) — the Python tier these locks serve
- [.halalchain/_shared](../_shared/README.md) — the package `base.txt`, `dev.txt`, `llm.txt`, and `mcp.txt` are compiled from
- [.github/workflows/python-locks.yml](../../.github/workflows/python-locks.yml) — the verification job
- [AGENTS.md](../../AGENTS.md#updating-versions) — the rule that these must be re-pinned with hashes after every functional change

## Notes / Limitations

- `vector.txt` embeds the absolute path `/workspaces/waklu/.halalchain/_shared`.
  It is not portable and should be regenerated with `--no-emit-package` like
  `agents.txt` is.
- `requirements.txt` and `requirements-test.txt` at the `.halalchain/` root
  reference an `all.txt` that does not exist. They are documentation, not
  installable requirements.
- CI installs `../requirements/base.txt` for `local-models` rather than its own
  lock, so the `local-models.txt` CUDA stack is never installed by CI.