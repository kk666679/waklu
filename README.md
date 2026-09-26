# HalalChain Platform — Full Architecture and Workflows

AI-native halal commerce and compliance platform for supplier verification, evidence collection, policy evaluation, and vendor-facing workflows. The repository combines a .NET modular platform, customer + marketplace front ends, Python AI/evidence services, and a local developer stack for running the full system together.

## Architectural principle

> AI gathers evidence. Deterministic systems decide business and compliance outcomes.

This project keeps the LLM and AI services in a supporting role. They observe, classify, summarize, and enrich evidence; the deterministic policy engine is responsible for the halal verdict and operating decisions. That rule is reflected in the local assistant prompt and in the API design where evidence is sent to the `tawheed` service rather than letting an LLM assign the final verdict.

## Repository overview

```text
.
├── AGENTS.md                     # Contributor/build guide for the repository
├── HalalChain.Platform.sln      # Main .NET solution
├── Directory.Build.props        # Shared .NET defaults (net10.0, nullable, implicit usings)
├── global.json                  # Pins the .NET SDK (10.0.200 with latestFeature roll-forward)
├── docker-compose.yml           # Full local stack definition for the platform and infra
├── service-manifest.yaml        # Canonical service inventory and ports
├── Modelfile                    # Local assistant system prompt for Ollama/OpenClaw
├── package.json                 # Root Node workspace scripts and tooling
├── README.md                    # This document
├── docs/                        # Architecture, development, and operations docs
├── infrastructure/              # Compose overlays and helper scripts
├── HalalChain-Cli/              # Node-based operator/CLI toolkit
├── HalalChain.Application/      # Application-layer services and orchestration
├── HalalChain.Domain/           # Domain model for catalog, vendors, halal policy, and commerce
├── HalalChain.Platform.Api/     # ASP.NET Core modular monolith API
├── HalalChain.Platform.Contracts/ # Shared DTOs, API contracts, and Solidity contract artifacts
├── HalalChain.Platform.Http/    # Typed HTTP client used by front ends
├── HalalChain.Platform.Tests/   # API and platform integration tests
├── HalalChain.Web/              # Customer-facing Blazor Server UI
├── HalalChain.Marketplace/      # Vendor marketplace UI (Razor Pages + Blazor Server + SignalR)
├── HalalChain.Mcp/              # MCP server for solution inspection and tooling
├── HalalChain.Mcp.Tests/        # MCP server tests
├── HalalChain.Architecture.Tests/ # Architecture guard tests
├── .halalchain/                 # Python services and shared library
│   ├── _shared/                 # Shared Python package for AI/inference and policy engine
│   ├── ai-inference/            # FastAPI AI gateway for embeddings, classification, rerank, and model access
│   ├── tawheed/                 # FastAPI evidence ingestion and deterministic policy engine
│   └── config.json              # Local-only shared config placeholder
├── scripts/                     # Supporting scripts
└── .env.example                 # Example env file for local setup (when present in the repo)
```