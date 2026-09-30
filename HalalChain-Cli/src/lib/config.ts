import { chmodSync, existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { homedir } from 'node:os'
import { join } from 'node:path'

/**
 * The single canonical CLI configuration store.
 *
 * This module is deliberately the *only* config path in the CLI. The v3
 * migration briefly had two: a legacy `lib/store.js` writing
 * `~/.halalchain/config.json` as flat dotted keys, and a `src/lib/store.ts`
 * writing `~/.halalchain/cli.yaml` as a structured document. Two stores meant
 * `config list` could not show what a command actually read, and a user who
 * ran `halalchain config set tawheed.url` left the agent path on the old
 * default. One file, one schema, one resolution order.
 *
 * Resolution order (last wins):
 *   1. DEFAULTS below
 *   2. the user store at ~/.halalchain/config.json
 *   3. environment variables (see ENV_OVERRIDES)
 *
 * Secrets are never echoed. `getAllRedacted` masks every key in SECRET_KEYS —
 * including `tawheed.api-key` and `local-models.api-key`, which the legacy
 * implementation forgot and therefore printed in cleartext via `config list`.
 */
export const DEFAULTS = {
  // .NET platform surfaces
  'platform-api.url': 'http://localhost:5001',
  'marketplace.url': 'http://localhost:5201',
  'halalchain.url': 'http://localhost:5200',
  // .halalchain/* Python services — ports match docker-compose.yml
  'ai-inference.url': 'http://localhost:7071',
  'tawheed.url': 'http://localhost:8000',
  'local-models.url': 'http://localhost:8080',
  'agents.url': 'http://localhost:8081',
  // MCP transport used by `halalchain agent`
  'mcp.url': '',
  'mcp.token': '',
  // Platform JWT (used by `halalchain env generate`)
  'jwt.issuer': 'HalalChainPlatform',
  'jwt.audience': 'HalalChainClients',
  'jwt.expires-minutes': '60',
  'jwt.key': '',
  // Policy Engine defaults
  'tawheed.jurisdiction': 'MY',
  'tawheed.policy-version': 'MY-v3',
  // Per-service API keys (sent as X-API-Key)
  'ai-inference.api-key': '',
  'tawheed.api-key': '',
  'local-models.api-key': '',
  // Agent loop
  'agent.model': 'gpt-5.2',
  'agent.max-iterations': '10',
  'verbose': 'false',
} as const

export type ConfigKey = keyof typeof DEFAULTS
export type ConfigStore = Record<string, string>

/** Keys masked by `getAllRedacted`, `config list` and `show-services`. */
export const SECRET_KEYS: ReadonlySet<string> = new Set([
  'jwt.key',
  'mcp.token',
  'ai-inference.api-key',
  'tawheed.api-key',
  'local-models.api-key',
])

export const KNOWN_KEYS: readonly string[] = Object.keys(DEFAULTS)

/** Environment variable -> config key. Highest precedence. */
export const ENV_OVERRIDES: Readonly<Record<string, ConfigKey>> = {
  HALALCHAIN_API_URL: 'platform-api.url',
  MARKETPLACE_URL: 'marketplace.url',
  HALALCHAIN_WEB_URL: 'halalchain.url',
  AI_INFERENCE_URL: 'ai-inference.url',
  TAWHEED_URL: 'tawheed.url',
  LOCAL_MODELS_URL: 'local-models.url',
  AGENTS_URL: 'agents.url',
  HALALCHAIN_MCP_URL: 'mcp.url',
  HALALCHAIN_MCP_TOKEN: 'mcp.token',
  JWT__KEY: 'jwt.key',
  HALALCHAIN_JURISDICTION: 'tawheed.jurisdiction',
  HALALCHAIN_POLICY_VERSION: 'tawheed.policy-version',
}

export const CONFIG_DIR = join(homedir(), '.halalchain')
export const CONFIG_PATH = join(CONFIG_DIR, 'config.json')
function readStore(): ConfigStore {
  if (!existsSync(CONFIG_PATH)) return {}
  try {
    const parsed: unknown = JSON.parse(readFileSync(CONFIG_PATH, 'utf8'))
    if (parsed === null || typeof parsed !== 'object' || Array.isArray(parsed)) return {}
    // Coerce to a string map so a hand-edited numeric/boolean value cannot
    // smuggle a non-string into request building.
    const out: ConfigStore = {}
    for (const [k, v] of Object.entries(parsed as Record<string, unknown>)) {
      if (typeof v === 'string') out[k] = v
      else if (typeof v === 'number' || typeof v === 'boolean') out[k] = String(v)
    }
    return out
  } catch {
    // A corrupt config must not make every command unusable.
    return {}
  }
}

function writeStore(data: ConfigStore): void {
  mkdirSync(CONFIG_DIR, { recursive: true })
  writeFileSync(CONFIG_PATH, JSON.stringify(data, null, 2) + '\n', { mode: 0o600 })
  // writeFileSync applies `mode` only when *creating* the file; an existing file
  // keeps its old (possibly world-readable) permissions, so chmod explicitly.
  try {
    chmodSync(CONFIG_PATH, 0o600)
  } catch {
    /* non-POSIX or read-only fs */
  }
}

function envLayer(): ConfigStore {
  const out: ConfigStore = {}
  for (const [envName, key] of Object.entries(ENV_OVERRIDES)) {
    const value = process.env[envName]
    if (value !== undefined && value !== '') out[key] = value
  }
  return out
}

/** Fully resolved config: DEFAULTS <- user store <- environment. */
export function loadConfig(): ConfigStore {
  return { ...DEFAULTS, ...readStore(), ...envLayer() }
}

export function getAll(): ConfigStore {
  return loadConfig()
}

export function redact(store: ConfigStore): ConfigStore {
  const out: ConfigStore = {}
  for (const [k, v] of Object.entries(store)) {
    out[k] = SECRET_KEYS.has(k) && v ? '***' : v
  }
  return out
}

export function getAllRedacted(): ConfigStore {
  return redact(loadConfig())
}

/**
 * Read one key. Unknown keys return `fallback` rather than throwing so a newer
 * config file never breaks an older CLI.
 */
export function get(key: string, fallback?: string): string | undefined {
  const value = loadConfig()[key]
  if (value !== undefined) return value
  return fallback
}

/** True when `key` is part of the known schema. */
export function isKnownKey(key: string): key is ConfigKey {
  return Object.prototype.hasOwnProperty.call(DEFAULTS, key)
}

export function set(key: string, value: string): void {
  const data = readStore()
  data[key] = value
  writeStore(data)
}

export function unset(key: string): void {
  const data = readStore()
  delete data[key]
  writeStore(data)
}

/** Drop the user file; resolution falls back to DEFAULTS + environment. */
export function reset(): void {
  writeStore({})
}

/** Mask one value for display. Non-secret values pass through unchanged. */
export function mask(key: string, value: string | undefined): string {
  if (value === undefined || value === '') return '(unset)'
  return SECRET_KEYS.has(key) ? '***' : value
}

