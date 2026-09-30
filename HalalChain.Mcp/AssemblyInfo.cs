using System.Runtime.CompilerServices;

// The architecture tests assert that the verdict-authority pattern list and the
// guard-marker convention are identical to the ones the MCP server enforces at
// runtime. Two copies of that list would drift, and a guardrail whose pattern
// set has quietly widened is worse than no guardrail at all — it reports clean.
//
// Exposing the internals lets AutoclawStructureTests read
// GovernanceService.ForbiddenPatterns and GovernanceService.GuardMarker as the
// single source of truth instead of restating them.
[assembly: InternalsVisibleTo("HalalChain.Architecture.Tests")]
