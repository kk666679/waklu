# ADR-003 — MCP v2 stateless transport

**Status:** Accepted
**Date:** 2026-09-26
**Deciders:** Platform Architecture
**Consulted:** Platform Engineering, Security

## Context

The platform exposes an MCP server so coding agents and compliant AI clients
can inspect the solution, query evidence, and evaluate policy through a
standardized tool interface.

MCP v1 requires a stateful session per client and sticky routing across
instances. MCP C# SDK v2.0 (July 2026) made the protocol stateless by
default, added Multi-Round-Trip Requests (MRTR) for approval-gated tools,
and standardized routing headers.

## Decision

**MCP C# SDK v2.0 with stateless transport.** No session affinity. No
in-memory session state. Tools are pure functions of input and ambient
authentication.

MRTR handles multi-step approvals. Routing headers standardize discovery
across hosts.

The MCP server in `HalalChain.Mcp/` is AOT-compiled and read-only. Tools
that would mutate state require either explicit MRTR approval or execution
through the platform API — never direct MCP mutation.

Every MCP call requires authentication. There is no public discovery
endpoint.

## Consequences

**Easier:** Horizontal scaling behind any load balancer. No sticky session
config. No session expiry logic. Simpler testing — every call is
self-contained. AOT-compilable, small footprint. MRTR gives a natural
approval gate.

**Harder:** Cross-request state must persist externally. Older clients
assuming a session need adaptation. MRTR adds round trips for approvals.

## Alternatives considered

**MCP v1 with sticky sessions.** Rejected. Sticky sessions break under
autoscaling. Failure mode is subtle — pod restart drops in-flight state.

**Custom tool protocol.** Rejected. Ecosystem interop is the point.
Building bespoke means every agent client needs a bespoke adapter.

**MCP v2 with a session cache layer.** Rejected. Cache reintroduces the
affinity v2 removed.

**v1 for reads, v2 for writes.** Rejected. Two protocols, two bugs. Writes
go through the API instead.

## Dissent

**Platform Engineering** wanted to wait for 2.1 or 2.2. *Overruled:* the
stateless change is architectural. Adopting later means refactoring every
tool that assumed session state. Adopting now means never writing that code.
The SDK is small enough to vendor if needed.

**Security** raised MRTR as an approval-fatigue vector. *Mitigated:* MRTR
approval requests are rate-limited per client and per actor. Repeated
prompts are throttled. `IAccessLog` records every request, so abuse patterns
are detectable.

**One rule held despite pressure:** no public discovery endpoint. Some
early discussion suggested unauthenticated read-only tools were acceptable.
*Overruled:* read-only tools can still leak vendor data, evidence metadata,
and policy details. Every call authenticates.

## Reversibility

Cheap. Reintroducing session state means adding a session store (Redis is
present) and sticky routing. ~2 engineer-weeks. We would only do this if a
future spec required stateful semantics for a critical tool.

## References

- `HalalChain.Mcp/Program.cs`
- MCP C# SDK v2.0 release notes (July 2026)
