import { randomBytes } from 'node:crypto'

const MIN_JWT_KEY_LENGTH = 32

const PLACEHOLDER_JWT_KEYS = new Set([
  '',
  'changeme',
  'change-me',
  'change-me-minimum-32-chars-secret-key',
  'your-32-plus-character-secret',
  'dev-secret',
  'dev-secret-do-not-use-in-production',
  'halalchain',
  'halalchaindevjwtsecret2024fortestingonly!',
  'halalchaindevjwtsecret2024fortestingonly',
  'super-secret-key-that-is-long-enough-32chars',
])

export function generateSecret(byteLength = 32): string {
  return randomBytes(byteLength).toString('base64url')
}

/**
 * Validate a JWT signing key. Returns an error message, or null when valid.
 * The key must be >= 32 chars of high-entropy material and must not be one of
 * the well-known placeholders.
 *
 * `super-secret-key-that-is-long-enough-32chars` is in the placeholder set
 * because it is the literal value committed at `.halalchain/config.json`. It
 * is long enough to satisfy a length check, so without this entry the CLI
 * would cheerfully report the dev key as production-safe.
 */
export function validateJwtKey(key: string | undefined): string | null {
  if (!key || !key.trim()) return 'Jwt:Key is required.'
  if (PLACEHOLDER_JWT_KEYS.has(key.trim().toLowerCase())) {
    return 'Jwt:Key is set to a known placeholder value. Replace it with a real high-entropy secret.'
  }
  if (key.length < MIN_JWT_KEY_LENGTH) {
    return `Jwt:Key must be at least ${MIN_JWT_KEY_LENGTH} characters (got ${key.length}).`
  }
  return null
}

/**
 * Validate a service API key. Lighter than JWT validation but still rejects
 * obvious placeholders. Empty is allowed: it means auth is disabled.
 */
export function validateApiKey(key: string | undefined): string | null {
  if (!key || !key.trim()) return null
  const lowered = key.trim().toLowerCase()
  if (lowered === 'changeme' || lowered === 'your-api-key' || lowered === 'change-me') {
    return 'API key is set to a placeholder value.'
  }
  if (key.length < 16) return 'API key is suspiciously short.'
  return null
}

export const JURISDICTIONS = ['MY', 'ID', 'SG', 'BN', 'GCC', 'EU'] as const
export type Jurisdiction = (typeof JURISDICTIONS)[number]

export function isJurisdiction(value: string): value is Jurisdiction {
  return (JURISDICTIONS as readonly string[]).includes(value)
}
