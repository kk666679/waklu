# ADR-003 — MCP v2 stateless transport

**Status:** Accepted
**Date:** 2026-09-26
**Deciders:** Platform Architecture
**Consulted:** Platform Engineering, Security
**Informed:** Steering Committee

## Context

The platform exposes an MCP (Model Context Protocol) server so that coding
agents and compliant AI clients can inspect the solution, query evidence,
and evaluate policies through a standardized tool interface. Early designs
used MCP v1, which establishes a stateful session per client and requires
sticky routing across instances.

MCP C# SDK v2.0 (released July 2026) made the protocol stateless by
default, added Multi-Round-Trip Requests (MRTR) for tools that need
user confirmation, and standardized routing headers. This changes the
deployment and threat model materially.

## Decision

We adopt **MCP C# SDK v2.0 with stateless transport** for all MCP servers
in the platform.

Implications:

- No session affinity required. Any MCP host instance can serve any request.
- No in-memory session state. Tools are pure functions of their input and
  the ambient authentication context.
- MRTR handles tools that need multi-step confirmation (e.g. an evidence
  fetch that requires explicit approval).
- Routing headers standardize tool discovery across hosts.

The MCP server in `HalalChain.Mcp/` is AOT-compiled and read-only. Tools
that would mutate state require either (a) explicit MRTR approval or
(b) execution through the platform API rather than MCP.

## Consequences

**Easier:**
- Horizontal scaling: add MCP host replicas behind any load balancer
- No sticky session requirement in the ingress or service mesh
- No session memory leak; no session expiry logic
- Simpler testing: every tool call is a self-contained request
- AOT-compilable, so cold start is fast and memory footprint is small
- MRTR gives us a natural approval gate for sensitive operations

**Harder:**
- Any tool that genuinely needs cross-request state must persist it
  externally, which is a real design constraint (we accept it; our tools
  are read-only).
- Clients that assume a session context must be updated. Agent Framework
  1.0 is compatible; older clients need adaptation.
- MRTR adds round trips for approval-gated operations, which is the point,
  but it is not free.

## Alternatives considered

### Alternative A — MCP v1 with sticky sessions

Rejected. Sticky sessions require ingress configuration, break under
autoscaling, and complicate failure recovery. The failure mode is subtle
(a pod restart drops in-flight state) and the benefit is convenience.

### Alternative B — Custom tool protocol, no MCP

Rejected. The whole point of MCP is ecosystem interoperation. Building a
bespoke protocol means every agent client needs a bespoke adapter. We would
be reimplementing MCP badly.

### Alternative C — MCP v2 with a session cache layer

Rejected. The cache layer would reintroduce session affinity, which is what
v2 removed. Adding it back defeats the purpose.

### Alternative D — MCP v1 for read tools, v2 for write tools

Rejected. Two protocols is two bugs. And we have very few write tools;
they can go through the API instead.

## Dissent

**Platform Engineering** initially argued to wait for a 2.1 or 2.2 release
before adopting, citing the recentness of 2.0. Their position: "Ship on
proven infrastructure; 2.0 is 3 months old."

**Overruled because:** the stateless change is architectural, not cosmetic.
Adopting it later means refactoring every tool that assumed session state.
Adopting it now means never writing that code. The 3-month track record is
thin, but the SDK targets `net8.0` through `net10.0` and the API surface
is small enough to vendor if necessary.

**Security** argued that MRTR adds attack surface — a malicious client
could trigger approval prompts to exhaust the reviewer. Their position:
"Approval fatigue is a real vulnerability."

**Accepted as a risk, mitigated.** MRTR approval requests are rate-limited
per client and per actor. Repeated prompts from the same source are
throttled. The `AccessLog` records every approval request, so a pattern of
abuse is detectable.

**One dissent held:** the MCP server must never be exposed to unauthenticated
clients, even for read-only tools. Some early discussion suggested a public
"discovery" endpoint. That was rejected; every MCP call requires
authentication. The dissent was that authentication adds friction for
legitimate public use cases.

**Overruled because:** "read-only" tools can still leak information. The
solution has vendor data, evidence metadata, and policy details. None of it
is public. Every call authenticates.

## Reversibility

**Cheap to reverse if we ever need to.** Removing stateless transport means
introducing a session store (Redis is already present) and adding sticky
routing. Estimated cost: 2 engineer-weeks. We would only do this if a
future MCP spec required stateful semantics for a critical tool, which
seems unlikely.

**Vendoring is possible.** The SDK is small. If v2.0 has bugs that upstream
abandons, we can vendor and patch.

## References

- ADR-001 — Modular monolith (the MCP host is one deployment)
- `HalalChain.Mcp/Program.cs` — server registration
- MCP C# SDK v2.0 release notes (July 2026)
- Agent Framework 1.0 MCP integration docs
