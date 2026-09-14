// Canonical CLI configuration store.
//
// A single JSON file at ~/.halalchain/config.json holds user-specific
// overrides; module-level DEFAULTS provides the safe fallbacks the
// CLI uses when the user has not configured a value.

import { readFileSync, writeFileSync, mkdirSync, existsSync, chmodSync } from "node:fs";
import { homedir } from "node:os";
import { join } from "node:path";

const CONFIG_DIR = join(homedir(), ".halalchain");
const CONFIG_FILE = join(CONFIG_DIR, "config.json");

const DEFAULTS = {
  "platform-api.url": "https://localhost:5001",
  "marketplace.url": "https://localhost:5201",
  "halalchain.url": "https://localhost:5200",
  "ai-inference.url": "http://localhost:7071",
  "tawheed.url": "http://localhost:8000",
  "jwt.issuer": "HalalChainPlatform",
  "jwt.audience": "HalalChainClients",
  "jwt.expires-minutes": "60",
  "tawheed.jurisdiction": "MY",
  "tawheed.policy-version": "MY-v3",
};

const KNOWN_KEYS = [...Object.keys(DEFAULTS), "jwt.key", "ai-inference.api-key"];
const SECRET_KEYS = new Set(["jwt.key", "ai-inference.api-key"]);

function load() {
  if (!existsSync(CONFIG_FILE)) return {};
  try {
    return JSON.parse(readFileSync(CONFIG_FILE, "utf8"));
  } catch {
    return {};
  }
}

function save(data) {
  mkdirSync(CONFIG_DIR, { recursive: true });
  // Write atomically and lock the file down to 0600 so generated
  // config cannot be world-readable.
  writeFileSync(CONFIG_FILE, JSON.stringify(data, null, 2) + "\n", { mode: 0o600 });
  try { chmodSync(CONFIG_FILE, 0o600); } catch { /* non-POSIX fs */ }
}

function redact(data) {
  const out = {};
  for (const [k, v] of Object.entries(data)) {
    out[k] = SECRET_KEYS.has(k) && v ? "***" : v;
  }
  return out;
}

export function getAll() { return { ...DEFAULTS, ...load() }; }
export function getAllRedacted() { return redact({ ...DEFAULTS, ...load() }); }
export function get(key, fallback) {
  const v = load()[key];
  return v === undefined ? (DEFAULTS[key] ?? fallback) : v;
}
export function set(key, value) {
  const d = load(); d[key] = value; save(d);
}
export function unset(key) {
  const d = load(); delete d[key]; save(d);
}
export function reset() { save({}); }
export function configPath() { return CONFIG_FILE; }

export { DEFAULTS, KNOWN_KEYS };

// Class-form wrapper kept for callers that instantiate
// ``new ConfigManager()`` from bin/halalchain.js.
export class ConfigManager {
  get(key, fallback) { return get(key, fallback); }
  set(key, value) { set(key, value); }
  delete(key) { unset(key); }
  getAll() { return getAll(); }
  getAllRedacted() { return getAllRedacted(); }
  reset() { reset(); }
}
