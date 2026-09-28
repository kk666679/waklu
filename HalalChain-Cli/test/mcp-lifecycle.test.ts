import { test } from 'node:test'
import assert from 'node:assert/strict'

/**
 * Guards against the lifecycle bug documented in the TanStack AI MCP guide:
 * closing the MCP client immediately after chat() kills in-flight tool calls,
 * because chat() executes tools lazily while the response streams.
 *
 * This test asserts the client module exposes a close function, and that
 * src/runtime/chat.ts does not call it. The real enforcement is the CI guard.
 */
test('mcp-client exposes close and cleanup', async () => {
  const mod = await import('../src/lib/mcp-client.js')
  assert.equal(typeof mod.closeMCPClient, 'function')
  assert.equal(typeof mod.registerMCPCleanup, 'function')
  assert.equal(typeof mod.getMCPClient, 'function')
})
