import chalk from 'chalk'

type Level = 'debug' | 'info' | 'warn' | 'error'

let currentLevel: Level = 'info'

const order: Record<Level, number> = { debug: 0, info: 1, warn: 2, error: 3 }

function shouldLog(level: Level): boolean {
  return order[level] >= order[currentLevel]
}

export const logger = {
  setLevel(level: Level) {
    currentLevel = level
  },
  debug(msg: string) {
    if (shouldLog('debug')) console.error(chalk.gray(`[debug] ${msg}`))
  },
  info(msg: string) {
    if (shouldLog('info')) console.log(chalk.blue(`[info] ${msg}`))
  },
  warn(msg: string) {
    if (shouldLog('warn')) console.warn(chalk.yellow(`[warn] ${msg}`))
  },
  error(msg: string) {
    if (shouldLog('error')) console.error(chalk.red(`[error] ${msg}`))
  },
}
