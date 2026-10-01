#!/usr/bin/env node
/**
 * Published entry point for the `halalchain` binary.
 *
 * This file is a thin launcher. It prefers the compiled output in `dist/`
 * (what `npm run build` produces and what the npm package ships), and falls
 * back to running the TypeScript sources through tsx so that
 * `halalchain ...` works in a fresh clone that has not been built yet.
 *
 * The pre-merge tree declared this path in package.json but never shipped the
 * file, so `npm run halalchain:cli` and any global install failed with
 * "Cannot find module" before a single command could run.
 */
import { createRequire } from 'node:module'
import { existsSync } from 'node:fs'
import { fileURLToPath, pathToFileURL } from 'node:url'
import { dirname, join } from 'node:path'

const require = createRequire(import.meta.url)
const here = dirname(fileURLToPath(import.meta.url))
const compiled = join(here, '..', 'dist', 'index.js')
const source = join(here, '..', 'src', 'index.ts')

// The entry module only parses argv when it believes it is the process entry
// point. It is imported here, so `process.argv[1]` is this launcher and the
// check would fail; tell it to drive argv itself.
process.env.HALALCHAIN_CLI_LAUNCHER = '1'

async function main() {
  if (existsSync(compiled)) {
    await import(pathToFileURL(compiled).href)
    return
  }

  if (existsSync(source)) {
    let tsx
    try {
      // `tsx/esm` registers the TypeScript loader without a separate process.
      tsx = require.resolve('tsx/esm/api')
    } catch {
      process.stderr.write(
        'halalchain: not built and tsx is unavailable.\n' +
          'Run `npm run build` (or `npm install`) in HalalChain-Cli first.\n'
      )
      process.exit(1)
    }
    const { register } = await import(pathToFileURL(tsx).href)
    register()
    await import(pathToFileURL(source).href)
    return
  }

  process.stderr.write(
    `halalchain: no entry point found.\nLooked for:\n  ${compiled}\n  ${source}\n`
  )
  process.exit(1)
}

main().catch((err) => {
  process.stderr.write(`halalchain: ${err instanceof Error ? err.message : String(err)}\n`)
  process.exit(1)
})
