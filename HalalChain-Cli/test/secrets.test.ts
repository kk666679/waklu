import { test } from 'node:test'
import assert from 'node:assert/strict'
import {
  JURISDICTIONS,
  generateSecret,
  isJurisdiction,
  validateApiKey,
  validateJwtKey,
} from '../src/lib/secrets.js'

test('secrets: validateJwtKey rejects empty, short and placeholder values', () => {
  assert.ok(validateJwtKey(undefined))
  assert.ok(validateJwtKey(''))
  assert.ok(validateJwtKey('short'))
  assert.ok(validateJwtKey('change-me-minimum-32-chars-secret-key'))
  assert.ok(validateJwtKey('HalalChainDevJwtSecret2024ForTestingOnly!'))
  assert.equal(validateJwtKey('a'.repeat(40)), null)
})

test('secrets: the committed .halalchain dev key is treated as a placeholder', () => {
  // `.halalchain/config.json` ships this literal value. It is longer than the
  // 32-char minimum, so a length check alone would wave it through.
  assert.ok(validateJwtKey('super-secret-key-that-is-long-enough-32chars'))
})

test('secrets: generateSecret returns high-entropy material of the right size', () => {
  const s = generateSecret(32)
  assert.ok(s.length >= 40)
  assert.notEqual(s, generateSecret(32), 'each call must produce a distinct secret')
})

test('secrets: validateApiKey allows empty but rejects placeholders and short keys', () => {
  assert.equal(validateApiKey(''), null)
  assert.equal(validateApiKey(undefined), null)
  assert.ok(validateApiKey('changeme'))
  assert.ok(validateApiKey('change-me'))
  assert.ok(validateApiKey('tooshort'))
  assert.equal(validateApiKey('a'.repeat(40)), null)
})

test('secrets: jurisdiction allow-list matches the Policy Engine', () => {
  for (const j of JURISDICTIONS) {
    assert.ok(isJurisdiction(j), `${j} must be a valid jurisdiction`)
  }
  assert.ok(!isJurisdiction('XX'))
  assert.ok(!isJurisdiction('my'), 'the allow-list is case-sensitive')
})
