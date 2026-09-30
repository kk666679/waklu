import Table from 'cli-table3'
import chalk from 'chalk'

/** Human byte size. */
export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export function truncate(value: string | undefined, maxLen = 60): string {
  if (!value) return ''
  return value.length > maxLen ? `${value.slice(0, maxLen)}...` : value
}

/**
 * Cosine similarity between two vectors.
 *
 * Returns 0 for mismatched or empty vectors rather than NaN. Callers print the
 * result directly, and "0%" is a safer reading of a degenerate pair than
 * "NaN%" — it says "no signal" instead of "broken".
 */
export function cosineSimilarity(a: number[] | undefined, b: number[] | undefined): number {
  if (!a || !b || a.length === 0 || a.length !== b.length) return 0
  let dot = 0
  let magA = 0
  let magB = 0
  for (let i = 0; i < a.length; i += 1) {
    const x = a[i] ?? 0
    const y = b[i] ?? 0
    dot += x * y
    magA += x * x
    magB += y * y
  }
  const denom = Math.sqrt(magA) * Math.sqrt(magB)
  return denom === 0 ? 0 : dot / denom
}

export function percent(value: number | undefined, digits = 1): string {
  if (value === undefined || !Number.isFinite(value)) return 'n/a'
  return `${(value * 100).toFixed(digits)}%`
}

export function scoreColor(score: number | undefined): (text: string) => string {
  if (score === undefined) return chalk.dim
  if (score >= 0.7) return chalk.green
  if (score >= 0.4) return chalk.yellow
  return chalk.red
}

/** Fixed-width bar for a 0..1 score. */
export function scoreBar(score: number | undefined, width = 20): string {
  const clamped = Math.max(0, Math.min(1, score ?? 0))
  return '█'.repeat(Math.round(clamped * width))
}

export function heading(text: string): void {
  console.log(chalk.cyan(`\n${text}\n`))
}

export function printJson(value: unknown): void {
  console.log(JSON.stringify(value, null, 2))
}

export function printTable(head: string[], rows: (string | number)[][], colWidths?: number[]): void {
  const table = new Table({ head, ...(colWidths ? { colWidths } : {}) })
  for (const row of rows) table.push(row.map((c) => String(c)))
  console.log(table.toString())
}
