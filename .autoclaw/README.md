# .autoclaw — HalalChain AI Control Plane

AI-native orchestration, compliance policy, evidence, and blockchain control plane
for the HalalChain Platform.

## Layers
- agents/         AI agents (supplier, evidence, policy, vendor, marketplace)
- skills/         Python skills + implementations
- workflows/      Durable orchestration
- reference/      Canonical, versioned source of truth (Aug 2026)
- eval/           CI quality gate for agents, skills, workflows, RAG, policy, blockchain
- integrations/   Bindings to .NET, Python AI, IoT, frontends, blockchain, storage, identity
- schemas/        JSON schemas
- prompts/        Versioned prompts
- kg/             Knowledge graph
- memory/         Memory tiers
- vector/         RAG index
- blockchain/     Contracts, networks, events
- security/       OPA/Casbin, access control, audit
- observability/  OpenTelemetry
- deploy/         Local dev stack

## Device evidence boundary

Device telemetry is normalized through `integrations/iot.yaml` and validated by
`schemas/sensor_observation.schema.json`. Observations require device provenance,
timestamp, hash, and signature before they can be anchored through the
`SensorObservationAnchored` evidence event. They remain evidence inputs only;
the deterministic Policy Engine owns compliance decisions.

IoT ingestion is opt-in. Set `IOT_ENABLED=true` and provide
`IOT_DEVICE_KEYS_JSON` through the deployment secret manager to enable it. When
disabled, the platform remains available and the ingestion endpoint returns a
controlled `503` rather than accepting unsigned telemetry.

## 2026 Trends Incorporated
- MCP (Model Context Protocol) for tool connectivity
- Agentic RAG + GraphRAG for multi-hop compliance reasoning
- Microsoft Agent Framework (MAF) 1.0 GA
- NVIDIA Agent Toolkit / OpenShell
- JAKIM MyeHALAL 2.0 (AI-assisted certification)
- Indonesia mandatory halal (Oct 2026)
- Turkey HAK mandatory import halal (Jan 2026)
- Saudi Global Halal Mark harmonisation
- Blockchain supply chain market growth (60% CAGR)
- .NET AI ecosystem: MEAI, MEDI, MEVD, Foundry Local

## CI
```bash
make eval
```
