/**
 * Isolated script execution for `halalchain sandbox`, built on Node's
 * permission model.
 *
 * Two facts drove this design, both verified against the running runtime
 * rather than assumed:
 *
 * 1. **The flag was renamed.** Node exposed the model as
 *    `--experimental-permission` until v23.5, where it became `--permission`.
 *    The old name was removed outright in v24, so a hardcoded
 *    `--experimental-permission` fails with `bad option` on the current
 *    runtime. `resolvePermissionFlag()` probes for whichever this Node
 *    accepts, which keeps the CLI working on the Node 22 that CI pins and on
 *    Node 24+.
 *
 * 2. **There is no network control.** The model governs the filesystem,
 *    child processes, workers, addons, WASI and the inspector. It has no
 *    notion of an egress allowlist, so network isolation is NOT implemented
 *    and `NETWORK_ISOLATION` below is the honest string to surface. Do not
 *    reintroduce an `allowNet` option: it could only ever be a no-op, and
 *    reporting it as a guarantee would be worse than not offering it.
 */
import { spawn } from 'node:child_process'
import { promises as fs } from 'node:fs'
import { tmpdir } from 'node:os'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const HERE = path.dirname(fileURLToPath(import.meta.url))

/** What this sandbox does and does not enforce. */
export const NETWORK_ISOLATION = 'none' as const

export interface SandboxOptions {
  /** Path to a script, or inline source when `inline` is true. */
  script: string
  /** True when `script` is inline source. Default false. */
  inline?: boolean
  /** Arguments appended to the script path. */
  args?: string[]
  /** Hard wall-clock cap. Default 60_000. */
  timeoutMs?: number
  /**
   * Environment for the child. The parent environment is NOT inherited;
   * every variable must be listed here or it is simply absent.
   */
  env?: Record<string, string>
  /** Extra read roots beyond the sandbox dir and node_modules. */
  allowFsRead?: string[]
  /** Extra write roots beyond the sandbox dir. */
  allowFsWrite?: string[]
  /** Injected as HALALCHAIN_TENANT_ID. */
  tenantId?: string
  /** Capture budget for stdout+stderr combined. Default 1_000_000. */
  maxOutputBytes?: number
  /** Keep the sandbox directory after the run. */
  keepDir?: boolean
}

export interface SandboxResult {
  exitCode: number | null
  signal: NodeJS.Signals | null
  stdout: string
  stderr: string
  durationMs: number
  timedOut: boolean
  outputTruncated: boolean
  sandboxDir: string
  cleanup(): Promise<void>
}

/**
 * Node's permission flag for this runtime, or undefined when unsupported.
 *
 * Probed by actually spawning the candidate flag: a bad flag is a startup
 * crash, not a catchable runtime error, so `--help` output is not a reliable
 * signal.
 */
export async function resolvePermissionFlag(): Promise<string | undefined> {
  for (const flag of ['--permission', '--experimental-permission']) {
    const ok = await new Promise<boolean>((resolve) => {
      const child = spawn(process.execPath, [flag, '--eval', '0'], {
        stdio: 'ignore',
        windowsHide: true,
      })
      child.once('error', () => resolve(false))
      child.once('exit', (code) => resolve(code === 0))
    })
    if (ok) return flag
  }
  return undefined
}

export async function sandboxSupported(): Promise<{ ok: boolean; reason?: string }> {
  const [major] = process.versions.node.split('.').map(Number)
  if ((major ?? 0) < 20) {
    return {
      ok: false,
      reason: `Node >= 20 is required for the permission model (found ${process.version}).`,
    }
  }
  const flag = await resolvePermissionFlag()
  if (!flag) {
    return {
      ok: false,
      reason:
        'This Node build exposes neither --permission nor --experimental-permission, ' +
        'so an isolated run cannot be guaranteed.',
    }
  }
  return { ok: true }
}

/**
 * The node_modules tree that owns this file, so `import` resolves inside the
 * sandbox. Walking up avoids granting read access to the whole filesystem.
 */
function resolveNodeModules(): string | undefined {
  let dir = HERE
  for (let i = 0; i < 8; i += 1) {
    if (path.basename(dir) === 'node_modules') return dir
    const parent = path.dirname(dir)
    if (parent === dir) return undefined
    dir = parent
  }
  return undefined
}

function buildPermissionArgs(opts: {
  sandboxDir: string
  allowFsRead: string[]
  allowFsWrite: string[]
}): string[] {
  const args: string[] = []
  // The sandbox dir is always readable and writable.
  args.push(`--allow-fs-read=${opts.sandboxDir}`, `--allow-fs-write=${opts.sandboxDir}`)

  // Resolve `import` for the script.
  const nodeModules = resolveNodeModules()
  if (nodeModules) args.push(`--allow-fs-read=${nodeModules}`)

  // Node's own internals, required before any script runs.
  args.push(`--allow-fs-read=${path.dirname(process.execPath)}`)

  for (const p of opts.allowFsRead) args.push(`--allow-fs-read=${path.resolve(p)}`)
  for (const p of opts.allowFsWrite) args.push(`--allow-fs-write=${path.resolve(p)}`)

  // Deliberately NOT granted: --allow-child-process, --allow-worker,
  // --allow-addons, --allow-wasi, --allow-inspector. Those are the point.
  return args
}

/**
 * Classify a permission denial.
 *
 * Node reports denials as ERR_ACCESS_DENIED, but the code only appears when
 * the throw is uncaught. A script that catches its own rejection and prints
 * `e.message` yields only the prose — "Access to this API has been
 * restricted. Use --allow-worker to manage permissions." — with no code at
 * all. Matching on the flag name alone covers both shapes; the prose prefix
 * keeps an unrelated error that merely mentions a flag from being mislabelled.
 */
export function classifyDenial(stderr: string): string | undefined {
  if (!stderr.includes('ERR_ACCESS_DENIED') && !stderr.includes('Access to this API has been restricted')) {
    return undefined
  }
  if (stderr.includes('--allow-fs-read')) return 'SANDBOX_FS_READ_DENIED'
  if (stderr.includes('--allow-fs-write')) return 'SANDBOX_FS_WRITE_DENIED'
  if (stderr.includes('--allow-child-process')) return 'SANDBOX_CHILD_PROCESS_DENIED'
  if (stderr.includes('--allow-worker')) return 'SANDBOX_WORKER_DENIED'
  if (stderr.includes('--allow-addons')) return 'SANDBOX_ADDON_DENIED'
  if (stderr.includes('--allow-wasi')) return 'SANDBOX_WASI_DENIED'
  if (stderr.includes('--allow-inspector')) return 'SANDBOX_INSPECTOR_DENIED'
  return 'SANDBOX_ACCESS_DENIED'
}

/** Run a script under the permission model and capture its output. */
export async function runInSandbox(options: SandboxOptions): Promise<SandboxResult> {
  const {
    script,
    inline = false,
    args = [],
    timeoutMs = 60_000,
    env = {},
    allowFsRead = [],
    allowFsWrite = [],
    tenantId,
    maxOutputBytes = 1_000_000,
    keepDir = false,
  } = options

  const flag = await resolvePermissionFlag()
  if (!flag) {
    throw new Error(
      'This Node build has no permission-model flag, so the sandbox cannot be ' +
        'enforced. Refusing to run the script unisolated.'
    )
  }

  const sandboxDir = await fs.mkdtemp(path.join(tmpdir(), 'halalchain-sandbox-'))
  let entry = script
  if (inline) {
    // .mjs so `import` works regardless of any package.json nearby.
    entry = path.join(sandboxDir, 'inline.mjs')
    await fs.writeFile(entry, script, 'utf8')
  } else {
    const resolved = path.resolve(script)
    if (!(await fs.stat(resolved).catch(() => null))?.isFile()) {
      throw new Error(`No such script: ${resolved}`)
    }
    // A script outside the sandbox needs explicit read access to itself.
    allowFsRead.push(resolved)
    entry = resolved
  }

  const childEnv: Record<string, string> = { ...env }
  if (tenantId) childEnv['HALALCHAIN_TENANT_ID'] = tenantId

  const argv = [
    flag,
    ...buildPermissionArgs({ sandboxDir, allowFsRead, allowFsWrite }),
    entry,
    ...args,
  ]

  const started = Date.now()
  const child = spawn(process.execPath, argv, {
    env: childEnv,
    cwd: sandboxDir,
    stdio: ['ignore', 'pipe', 'pipe'],
    windowsHide: true,
  })

  let stdout = ''
  let stderr = ''
  let captured = 0
  let outputTruncated = false
  const cap = (chunk: string, into: 'out' | 'err') => {
    if (captured >= maxOutputBytes) {
      outputTruncated = true
      return
    }
    const room = maxOutputBytes - captured
    const text = chunk.length > room ? chunk.slice(0, room) : chunk
    captured += text.length
    if (into === 'out') stdout += text
    else stderr += text
    if (chunk.length > room) outputTruncated = true
  }

  child.stdout.setEncoding('utf8')
  child.stderr.setEncoding('utf8')
  child.stdout.on('data', (c: string) => cap(c, 'out'))
  child.stderr.on('data', (c: string) => cap(c, 'err'))

  let timedOut = false
  const timer = setTimeout(() => {
    timedOut = true
    child.kill('SIGKILL')
  }, timeoutMs)

  const exit = await new Promise<{ code: number | null; signal: NodeJS.Signals | null }>(
    (resolve) => {
      child.once('error', () => resolve({ code: null, signal: null }))
      child.once('close', (code, signal) => resolve({ code, signal }))
    }
  )
  clearTimeout(timer)

  const cleanup = async () => {
    await fs.rm(sandboxDir, { recursive: true, force: true }).catch(() => undefined)
  }
  // Keep the directory only when asked, or after a failure an operator will
  // want to inspect.
  if (!keepDir && !timedOut && exit.code === 0) await cleanup()

  return {
    exitCode: exit.code,
    signal: exit.signal,
    stdout,
    stderr,
    durationMs: Date.now() - started,
    timedOut,
    outputTruncated,
    sandboxDir,
    cleanup,
  }
}

