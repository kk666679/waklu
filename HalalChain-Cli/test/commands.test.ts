import { test } from 'node:test'
import assert from 'node:assert/strict'
import { mkdtempSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import type { Command } from 'commander'

// The config store reads homedir() at import time; give the suite a temp home
// so building the program never touches the developer's real config.
const home = mkdtempSync(join(tmpdir(), 'halalchain-surface-'))
process.env['HOME'] = home
process.env['USERPROFILE'] = home

const { buildProgram } = await import(`../src/index.js?case=${Math.random()}`)

function subcommandNames(parent: Command): string[] {
  return parent.commands.map((c) => c.name())
}

test('cli: the top-level surface registers every operator command', () => {
  const names = subcommandNames(buildProgram())
  for (const expected of [
    'agent',
    'config',
    'env',
    'server',
    'embedding',
    'classify',
    'summarize',
    'rerank',
    'rag',
    'vector',
    'certificate',
    'ingredient',
    'llm',
    'evaluate',
    'agents',
    'local',
    'skills',
  ]) {
    assert.ok(names.includes(expected), `\`${expected}\` must be registered`)
  }
})

/**
 * These commands targeted endpoints that do not exist in any `.halalchain`
 * service — `/ai-context/*`, `/ml/*`, `/pipeline/*` and `/evaluate/model`,
 * `/evaluate/rag`. Every one was a guaranteed 404. They are gone rather than
 * ported, and this test fails if one creeps back.
 */
test('cli: commands that targeted non-existent endpoints stay removed', () => {
  const names = subcommandNames(buildProgram())
  for (const removed of ['ai-context', 'ml', 'pipeline', 'batch', 'cross', 'monitor']) {
    assert.ok(!names.includes(removed), `\`${removed}\` must not be registered`)
  }
})

test('cli: llm exposes only the route ai-inference actually implements', () => {
  const llm = buildProgram().commands.find((c) => c.name() === 'llm')
  assert.ok(llm)
  const names = subcommandNames(llm)
  assert.ok(names.includes('generate'))
  for (const fictional of ['chat', 'complete', 'models']) {
    assert.ok(!names.includes(fictional), `llm ${fictional} posts to a route that does not exist`)
  }
})

test('cli: evaluate targets tawheed policy verification, not AI/ML eval', () => {
  const evaluate = buildProgram().commands.find((c) => c.name() === 'evaluate')
  assert.ok(evaluate)
  assert.equal(evaluate.alias(), 'eval')
  // The product id is a required positional argument.
  assert.ok(evaluate.registeredArguments.some((a) => a.name() === 'product-id'))
  const names = subcommandNames(evaluate)
  assert.ok(names.includes('policies'))
  assert.ok(names.includes('agents-health'))
  for (const fictional of ['model', 'rag']) {
    assert.ok(!names.includes(fictional), `evaluate ${fictional} posts to a route that does not exist`)
  }
})

test('cli: evaluate forwards the evidence fields tawheed accepts', () => {
  const evaluate = buildProgram().commands.find((c) => c.name() === 'evaluate')
  assert.ok(evaluate)
  const longs = evaluate.options.map((o) => o.long)
  for (const expected of [
    '--certificate-number',
    '--certification-body',
    '--ingredients',
    '--supplier-name',
    '--jurisdiction',
    '--policy-version',
    '--input',
  ]) {
    assert.ok(longs.includes(expected), `${expected} must be an option`)
  }
})

test('cli: vector exposes search, as the halalchain-vector skill documents', () => {
  const vector = buildProgram().commands.find((c) => c.name() === 'vector')
  assert.ok(vector)
  const names = subcommandNames(vector)
  assert.ok(names.includes('search'), 'the skill documents `halalchain vector search`')
  assert.ok(names.includes('stats'))
})

test('cli: agents exposes the workflow surface of .halalchain/agents', () => {
  const agents = buildProgram().commands.find((c) => c.name() === 'agents')
  assert.ok(agents)
  const names = subcommandNames(agents)
  for (const expected of ['workflows', 'run', 'health']) {
    assert.ok(names.includes(expected), `agents ${expected} must be registered`)
  }
})

test('cli: config exposes the full store lifecycle', () => {
  const config = buildProgram().commands.find((c) => c.name() === 'config')
  assert.ok(config)
  const names = subcommandNames(config)
  for (const expected of ['init', 'get', 'set', 'unset', 'list', 'validate', 'reset', 'path', 'keys']) {
    assert.ok(names.includes(expected), `config ${expected} must be registered`)
  }
})

test('cli: every command carries a description for --help', () => {
  const program = buildProgram()
  for (const cmd of program.commands) {
    assert.ok(cmd.description().length > 0, `${cmd.name()} must have a description`)
    for (const sub of cmd.commands) {
      assert.ok(sub.description().length > 0, `${cmd.name()} ${sub.name()} must have a description`)
    }
  }
})
