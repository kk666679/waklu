export function isValidUrl(url: string): boolean {
  try {
    new URL(url)
    return true
  } catch {
    return false
  }
}

export function isValidLabel(label: string): boolean {
  return /^[a-zA-Z0-9_-]+$/.test(label)
}

export function isValidTopK(value: string | number): boolean {
  const n = typeof value === 'number' ? value : Number.parseInt(value, 10)
  return Number.isFinite(n) && n > 0 && n <= 100
}

export function isValidTemperature(value: string | number): boolean {
  const n = typeof value === 'number' ? value : Number.parseFloat(value)
  return Number.isFinite(n) && n >= 0 && n <= 2
}

/** Parse an integer CLI option, returning `fallback` on anything unparseable. */
export function toInt(value: string | undefined, fallback: number): number {
  if (value === undefined) return fallback
  const n = Number.parseInt(value, 10)
  return Number.isNaN(n) ? fallback : n
}

export function toFloat(value: string | undefined, fallback: number): number {
  if (value === undefined) return fallback
  const n = Number.parseFloat(value)
  return Number.isNaN(n) ? fallback : n
}

/**
 * Throw a user-facing error for a failed validation. Commander surfaces the
 * message and exits non-zero without a stack trace.
 */
export function assertValid(check: boolean, message: string): asserts check {
  if (!check) throw new Error(message)
}
