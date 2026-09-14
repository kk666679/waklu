// Tests for the CLI configuration store.
// Uses node:test (built into Node ≥ 18) so no test runner install needed.

import { test } from "node:test";
import assert from "node:assert/strict";
import { mkdtempSync, existsSync, statSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

// Each test gets its own HOME so the store module is loaded fresh
// against an isolated config dir. This avoids cross-test pollution
// when the suite runs in parallel.
function withFreshStore() {
  const home = mkdtempSync(join(tmpdir(), "halalchain-test-"));
  process.env.HOME = home;
  process.env.USERPROFILE = home;
  return import(`../lib/store.js?cache=${Math.random()}`);
}

test("store: get/set round-trip with a default fallback", async () => {
  const { get, set, DEFAULTS } = await withFreshStore();
  const v = get("platform-api.url");
  assert.equal(v, DEFAULTS["platform-api.url"]);
  set("platform-api.url", "https://example.test");
  assert.equal(get("platform-api.url"), "https://example.test");
});

test("store: reset clears the user file", async () => {
  const { get, set, reset } = await withFreshStore();
  set("jwt.key", "x".repeat(40));
  reset();
  assert.equal(get("jwt.key"), undefined);
});

test("store: config file is created with 0600 permissions", async () => {
  const { set, configPath } = await withFreshStore();
  set("ai-inference.url", "http://localhost:9999");
  assert.ok(existsSync(configPath()));
  const stat = statSync(configPath());
  if (process.platform !== "win32") {
    assert.equal(stat.mode & 0o777, 0o600);
  }
});

test("secrets: validateJwtKey rejects short / placeholder values", async () => {
  const { validateJwtKey } = await import("../lib/secrets.js");
  assert.ok(validateJwtKey(""));
  assert.ok(validateJwtKey("short"));
  assert.ok(validateJwtKey("change-me-minimum-32-chars-secret-key"));
  assert.ok(validateJwtKey("HalalChainDevJwtSecret2024ForTestingOnly!"));
  assert.equal(validateJwtKey("a".repeat(40)), null);
});

test("secrets: generateSecret returns high-entropy strings of expected length", async () => {
  const { generateSecret } = await import("../lib/secrets.js");
  const s = generateSecret(32);
  assert.equal(s.length >= 40, true);
});

test("secrets: validateApiKey is permissive about empty keys but rejects obvious placeholders", async () => {
  const { validateApiKey } = await import("../lib/secrets.js");
  assert.equal(validateApiKey(""), null);
  assert.equal(validateApiKey("changeme")?.length > 0, true);
  assert.equal(validateApiKey("a".repeat(40)), null);
});

test("ConfigManager: class API matches module API", async () => {
  const { ConfigManager, get } = await withFreshStore();
  const cm = new ConfigManager();
  const before = cm.get("ai-inference.url");
  cm.set("ai-inference.url", "http://test:1234");
  assert.equal(cm.get("ai-inference.url"), "http://test:1234");
  cm.delete("ai-inference.url");
  assert.equal(cm.get("ai-inference.url"), before);
});

test("store: getAllRedacted hides secret values", async () => {
  const { set, getAllRedacted } = await withFreshStore();
  set("jwt.key", "x".repeat(40));
  const redacted = getAllRedacted();
  assert.equal(redacted["jwt.key"], "***");
});
