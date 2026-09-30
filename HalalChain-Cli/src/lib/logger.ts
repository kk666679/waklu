import chalk from 'chalk'

export type LogLevel = 'debug' | 'info' | 'warn' | 'error'

const order: Record<LogLevel, number> = { debug: 0, info: 1, warn: 2, error: 3 }

let currentLevel: LogLevel = 'info'

function shouldLog(level: LogLevel): boolean {
  return order[level] >= order[currentLevel]
}

function fmt(args: unknown[]): string {
  return args.map((a) => (typeof a === 'string' ? a : JSON.stringify(a))).join(' ')
}

/**
 * Single logger for the whole CLI.
 *
 * The pre-merge tree shipped two (`Logger` in lib/logger.js, `logger` in
 * src/lib/logger.ts) with different signatures; the JS one is gone. `debug`
 * and `warn` write to stderr so that piping stdout to a file — or to `jq` —
 * does not capture diagnostics alongside the payload.
 */
export const logger = {
  setLevel(level: LogLevel): void {
    currentLevel = level
  },
  getLevel(): LogLevel {
    return currentLevel
  },
  debug(...args: unknown[]): void {
    if (shouldLog('debug')) console.error(chalk.gray(`[debug] ${fmt(args)}`))
  },
  info(...args: unknown[]): void {
    if (shouldLog('info')) console.log(fmt(args))
  },
  warn(...args: unknown[]): void {
    if (shouldLog('warn')) console.error(chalk.yellow(`[warn] ${fmt(args)}`))
  },
  error(...args: unknown[]): void {
    console.error(chalk.red(`[error] ${fmt(args)}`))
  },
  success(...args: unknown[]): void {
    if (shouldLog('info')) console.log(chalk.green('✔'), fmt(args))
  },
  fail(...args: unknown[]): void {
    console.error(chalk.red('✖'), fmt(args))
  },
}

