import { readFileSync } from 'node:fs'
import { input, select } from '@inquirer/prompts'

/**
 * Resolve text from `--text`, `--file`, or an interactive prompt.
 *
 * Ordering matters: an explicit flag always wins over a prompt so the command
 * stays scriptable, and `--file` wins over `--text` only in the sense that the
 * file is read when supplied. Non-TTY stdin (CI, pipes) still works because
 * the prompt is only reached when neither flag was given.
 *
 * The option bag is typed `string | undefined` rather than bare optional so a
 * caller can forward Commander's `string | undefined` options directly under
 * `exactOptionalPropertyTypes`.
 */
export async function resolveText(
  opts: { text?: string | undefined; file?: string | undefined },
  promptMessage: string
): Promise<string> {
  if (opts.file) return readFileSync(opts.file, 'utf-8')
  if (opts.text !== undefined && opts.text !== '') return opts.text
  const answer = await input({ message: promptMessage })
  return typeof answer === 'string' ? answer : String(answer)
}

/** Read and parse a JSON file passed via `--file`. */
export function readJsonFile<T = unknown>(path: string): T {
  const raw = readFileSync(path, 'utf-8')
  try {
    return JSON.parse(raw) as T
  } catch (err) {
    throw new Error(`${path} is not valid JSON: ${(err as Error).message}`)
  }
}

/** Parse a JSON string supplied inline via an option. */
export function parseJsonOption<T = unknown>(raw: string | undefined, label: string): T {
  if (raw === undefined) return {} as T
  try {
    return JSON.parse(raw) as T
  } catch (err) {
    throw new Error(`${label} is not valid JSON: ${(err as Error).message}`)
  }
}

/** Split a comma-separated list into trimmed, non-empty parts. */
export function splitList(raw: string | undefined): string[] {
  if (!raw) return []
  return raw
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s.length > 0)
}

/** Single-choice prompt used by the config wizard. */
export async function choose(
  message: string,
  choices: { value: string; name: string }[],
  fallback: string
): Promise<string> {
  const answer = await select({ message, choices, default: fallback })
  return typeof answer === 'string' ? answer : String(answer)
}
