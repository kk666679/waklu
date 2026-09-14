export function validateUrl(url) {
  try { new URL(url); return true; } catch { return false; }
}

export function validateApiKey(key) {
  return typeof key === "string" && key.length > 0;
}

export function validateLabel(label) {
  return typeof label === "string" && /^[a-zA-Z0-9_-]+$/.test(label);
}

export function validateTopK(k) {
  const n = parseInt(k, 10);
  return !isNaN(n) && n > 0 && n <= 100;
}

export function validateTemperature(t) {
  const n = parseFloat(t);
  return !isNaN(n) && n >= 0 && n <= 2;
}
