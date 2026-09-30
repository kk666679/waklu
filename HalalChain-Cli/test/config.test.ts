import { test } from 'node:test'
import assert from 'node:assert/strict'
import { existsSync, mkdirSync, mkdtempSync, statSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'

/**
 * The config store resolves `CONFIG_PATH` from `homedir()` at module load, so
 * each test needs an isolated home *and* a fresh module instance. The cache
 * busting query forces Node to re-evaluate the module rather than hand back the
 * already-initialised copy.
 */
function useTempHome(prefix: string): string {
  const home = mkdtempSync(join(tmpdir(), prefix))
  process.env['HOME'] = home
  process.env['USERPROFILE'] = home
  return home
}

function withFreshStore() {
  useTempHome('halalchain-config-')
  return import(`../src/lib/config.js?case=${Math.random()}`)
}

test('config: get/set round-trips and falls back to defaults', async () => {
  const { get, set, DEFAULTS } = await withFreshStore()
  assert.equal(get('platform-api.url'), DEFAULTS['platform-api.url'])
  set('platform-api.url', 'https://example.test')
  assert.equal(get('platform-api.url'), 'https://example.test')
})

test('config: unset falls back to the default', async () => {
  const { get, set, unset, DEFAULTS } = await withFreshStore()
  set('tawheed.url', 'https://tawheed.test')
  assert.equal(get('tawheed.url'), 'https://tawheed.test')
  unset('tawheed.url')
  assert.equal(get('tawheed.url'), DEFAULTS['tawheed.url'])
})

test('config: reset clears the user file', async () => {
  const { get, set, reset, DEFAULTS } = await withFreshStore()
  set('jwt.key', 'x'.repeat(40))
  reset()
  assert.equal(get('jwt.key'), DEFAULTS['jwt.key'])
})

test('config: the store file is created with 0600 permissions', async () => {
  const { set, CONFIG_PATH } = await withFreshStore()
  set('ai-inference.url', 'http://localhost:9999')
  assert.ok(existsSync(CONFIG_PATH))
  if (process.platform !== 'win32') {
    assert.equal(statSync(CONFIG_PATH).mode & 0o777, 0o600)
  }
})

test('config: every secret key is masked, not just jwt.key', async () => {
  const { set, getAllRedacted, SECRET_KEYS } = await withFreshStore()
  // The pre-merge store masked only jwt.key and ai-inference.api-key, so
  // `config list` printed tawheed.api-key and local-models.api-key in cleartext.
  set('jwt.key', 'a'.repeat(40))
  set('mcp.token', 'b'.repeat(40))
  set('ai-inference.api-key', 'c'.repeat(40))
  set('tawheed.api-key', 'd'.repeat(40))
  set('local-models.api-key', 'e'.repeat(40))

  const redacted = getAllRedacted()
  for (const key of SECRET_KEYS) {
    assert.equal(redacted[key], '***', `${key} must be masked`)
  }
})

test('config: non-secret values are not masked', async () => {
  const { set, getAllRedacted } = await withFreshStore()
  set('tawheed.url', 'http://localhost:8000')
  assert.equal(getAllRedacted()['tawheed.url'], 'http://localhost:8000')
})

test('config: mask() reports unset as a placeholder', async () => {
  const { mask } = await withFreshStore()
  assert.equal(mask('jwt.key', undefined), '(unset)')
  assert.equal(mask('jwt.key', ''), '(unset)')
  assert.equal(mask('jwt.key', 'a'.repeat(40)), '***')
  assert.equal(mask('tawheed.url', 'http://x'), 'http://x')
})

test('config: known keys cover every .halalchain service', async () => {
  const { KNOWN_KEYS } = await withFreshStore()
  for (const key of [
    'ai-inference.url',
    'tawheed.url',
    'local-models.url',
    'agents.url',
    'platform-api.url',
    'mcp.url',
  ]) {
    assert.ok(KNOWN_KEYS.includes(key), `${key} must be a known config key`)
  }
})

test('config: isKnownKey rejects typos and prototype keys', async () => {
  const { isKnownKey } = await withFreshStore()
  assert.equal(isKnownKey('tawheed.url'), true)
  assert.equal(isKnownKey('tawheed.URL'), false)
  assert.equal(isKnownKey('__proto__'), false)
})
test('config: environment variables override the stored value', async () => {
  useTempHome('halalchain-config-env-')
  const { set, get, ENV_OVERRIDES } = await import(`../src/lib/config.js?case=${Math.random()}`)

  set('tawheed.url', 'https://stored.test')
  assert.equal(get('tawheed.url'), 'https://stored.test')

  process.env['TAWHEED_URL'] = 'https://from-env.test'
  try {
    assert.equal(get('tawheed.url'), 'https://from-env.test')
    assert.equal(ENV_OVERRIDES['TAWHEED_URL'], 'tawheed.url')
  } finally {
    delete process.env['TAWHEED_URL']
  }
})

test('config: an empty environment variable does not blank out a stored value', async () => {
  useTempHome('halalchain-config-emptyenv-')
  const { set, get } = await import(`../src/lib/config.js?case=${Math.random()}`)
  set('tawheed.url', 'https://stored.test')

  process.env['TAWHEED_URL'] = ''
  try {
    assert.equal(get('tawheed.url'), 'https://stored.test')
  } finally {
    delete process.env['TAWHEED_URL']
  }
})

test('config: a corrupt store file does not break resolution', async () => {
  useTempHome('halalchain-config-corrupt-')
  const { CONFIG_DIR, get, DEFAULTS } = await import(`../src/lib/config.js?case=${Math.random()}`)
  mkdirSync(CONFIG_DIR, { recursive: true })
  writeFileSync(join(CONFIG_DIR, 'config.json'), '{ this is not json', 'utf8')
  assert.equal(get('tawheed.url'), DEFAULTS['tawheed.url'])
})

test('config: non-string stored values are coerced and arrays dropped', async () => {
  useTempHome('halalchain-config-coerce-')
  const { CONFIG_DIR, get, DEFAULTS } = await import(`../src/lib/config.js?case=${Math.random()}`)
  // CONFIG_DIR is ~/.halalchain, which does not exist in a fresh temp home, so
  // writing straight into it raised ENOENT. The sibling "corrupt store" test
  // already mkdirs first; this one did not.
  mkdirSync(CONFIG_DIR, { recursive: true })
  writeFileSync(
    join(CONFIG_DIR, 'config.json'),
    JSON.stringify({ 'tawheed.url': 8080, 'agents.url': ['nope'] }),
    'utf8'
  )

  assert.equal(get('tawheed.url'), '8080')
  // The array value is not a string, so the default survives.
  assert.equal(get('agents.url'), DEFAULTS['agents.url'])
})

test('config: ENV_OVERRIDES only references keys that exist in DEFAULTS', async () => {
  const { ENV_OVERRIDES, DEFAULTS } = await withFreshStore()
  for (const [envName, key] of Object.entries(ENV_OVERRIDES)) {
    assert.ok(
      Object.prototype.hasOwnProperty.call(DEFAULTS, key),
      `${envName} maps to unknown key ${key}`
    )
  }
})

