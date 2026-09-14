import { randomBytes } from "node:crypto";

const MIN_JWT_KEY_LENGTH = 32;

const PLACEHOLDER_JWT_KEYS = new Set([
  "",
  "changeme",
  "change-me",
  "change-me-minimum-32-chars-secret-key",
  "your-32-plus-character-secret",
  "dev-secret",
  "dev-secret-do-not-use-in-production",
  "halalchain",
  "halalchaindevjwtsecret2024fortestingonly!",
  "halalchaindevjwtsecret2024fortestingonly",
]);

export const generateSecret = (byteLength = 32) =>
  randomBytes(byteLength).toString("base64url");

/**
 * Validate a JWT signing key. Returns an error message string or null.
 * The key MUST be at least 32 characters of high-entropy material and
 * must not be one of the well-known placeholders.
 */
export const validateJwtKey = (key) => {
  if (!key || typeof key !== "string" || !key.trim()) {
    return "Jwt:Key is required.";
  }
  if (PLACEHOLDER_JWT_KEYS.has(key.trim().toLowerCase())) {
    return "Jwt:Key is set to a known placeholder value. Replace it with a real high-entropy secret.";
  }
  if (key.length < MIN_JWT_KEY_LENGTH) {
    return `Jwt:Key must be at least ${MIN_JWT_KEY_LENGTH} characters (got ${key.length}).`;
  }
  return null;
};

/**
 * Validate an AI API key. Lighter than JWT validation but still rejects
 * obvious placeholders.
 */
export const validateApiKey = (key) => {
  if (!key || typeof key !== "string" || !key.trim()) return null; // empty is allowed (auth disabled)
  if (key.trim().toLowerCase() === "changeme" || key.trim().toLowerCase() === "your-api-key") {
    return "AI API key is set to a placeholder value.";
  }
  if (key.length < 16) {
    return "AI API key is suspiciously short.";
  }
  return null;
};
