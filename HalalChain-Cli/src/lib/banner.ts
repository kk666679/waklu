import figlet from 'figlet'
import chalk from 'chalk'

/**
 * ASCII-art banner for the operator CLI.
 *
 * Rendered with figlet so the wordmark can be changed without hand-editing
 * block characters. The banner is only shown on the bare `halalchain`
 * invocation and on `--help`; subcommand output stays machine-readable.
 */

const DEFAULT_FONT = 'ANSI Shadow'

/** Fallback used when the requested font is unavailable to this figlet build. */
const FALLBACK = [
  '██╗  ██╗ █████╗ ██╗      ██████╗ ██╗   ██╗ █████╗ ██╗   ██╗ ██████╗ ██╗ █████╗  ██╗',
  '██║  ██║██╔══██╗██║     ██╔════╝ ██║   ██║██╔══██╗██║   ██║██╔════╝ ██║██╔══██╗ ██║',
  '███████║███████║██║     ██║     ██║   ██║███████║██║   ██║██║     ██║███████║ ██║',
  '██╔══██║██╔══██║██║     ██║     ██║   ██║██╔══██║██║   ██║██║     ██║██╔══██║ ██║',
  '██║  ██║██║  ██║███████╗╚██████╗ ╚██████╔╝██║  ██║╚██████╔╝╚██████╗██║  ██║ ██║',
  '╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝ ╚═════╝  ╚═════╝ ╚═╝  ╚═╝ ╚═════╝  ╚═════╝╚═╝  ╚═╝ ╚═╝',
].join('\n')

/** Colours applied top-to-bottom to the rendered art, so it reads as a gradient. */
const GRADIENT: readonly string[] = ['cyan', 'cyan', 'blue', 'blue', 'magenta', 'magenta']

function render(text: string, font: string): string {
  try {
    return figlet.textSync(text, { font })
  } catch {
    return FALLBACK
  }
}

function paint(art: string): string {
  const lines = art.replace(/\n+$/, '').split('\n')
  return lines
    .map((line, i) => {
      const colour = GRADIENT[Math.min(i, GRADIENT.length - 1)]
      return chalk[colour as 'cyan'](line)
    })
    .join('\n')
    .replace(/ +$/gm, '')
}

/** The full banner block: wordmark, tagline, and version. */
export function banner(version = '3.0.0'): string {
  const font = process.env.HALALCHAIN_BANNER_FONT?.trim() || DEFAULT_FONT
  const art = paint(render('HalalChain', font))
  const tagline = chalk.gray('AI-native halal commerce toolkit')
  const release = chalk.gray(`halalchain v${version}`)
  return `${art}\n${tagline}  ${release}\n`
}

/** Write the banner to stdout. */
export function printBanner(version?: string): void {
  process.stdout.write(`${banner(version)}\n`)
}