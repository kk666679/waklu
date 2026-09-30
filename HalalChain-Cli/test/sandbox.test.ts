import { test } from 'node:test'
import assert from 'node:assert/strict'
import { promises as fs } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import {
  NETWORK_ISOLATION,
  classifyDenial,
  resolvePermissionFlag,
  runInSandbox,
  sandboxSupported,
} from '../src/lib/sandbox.js'

/**
 * Isolation contract for `halalchain sandbox`.
 *
 * These spawn real child processes under the Node permission model, so they
 * assert behaviour rather than configuration. Two things are pinned here that
 * are easy to get wrong:
 *
 *  - The permission flag was renamed: `--experimental-permission` became
 *    `--permission` in v23.5 and the old name was removed in v24. CI pins
 *    Node 22 and this machine runs Node 24, so the runtime probe must work
 *    on both.
 *  - There is NO network isolation and no such flag. NETWORK_ISOLATION is
 *    asserted to be 'none' so this cannot quietly become a false guarantee.
 */

test('sandbox: the runtime exposes a usable permission flag', async () => {
  const flag = await resolvePermissionFlag()
  assert.ok(flag, 'neither --permission nor --experimental-permission was accepted')
  assert.ok(['--permission', '--experimental-permission'].includes(flag))
})

test('sandbox: support check agrees with the probe', async () => {
  const support = await sandboxSupported()
  assert.equal(support.ok, true, support.reason)
})

test('sandbox: an inline script runs and its stdout is captured', async () => {
  const r = await runInSandbox({
    script: 'console.log("hello from the sandbox")',
    inline: true,
    timeoutMs: 20_000,
  })
  try {
    assert.equal(r.exitCode, 0)
    assert.match(r.stdout, /hello from the sandbox/)
    assert.equal(r.timedOut, false)
  } finally {
    await r.cleanup()
  }
})

test('sandbox: the working directory is a throwaway under the temp folder', async () => {
  const r = await runInSandbox({ script: 'console.log(process.cwd())', inline: true, timeoutMs: 20_000 })
  try {
    assert.match(r.stdout.trim(), /halalchain-sandbox-/)
  } finally {
    await r.cleanup()
  }
})

test('sandbox: child processes are denied', async () => {
  const r = await runInSandbox({
    script: 'import("node:child_process").then((c) => c.execSync("whoami"))',
    inline: true,
    timeoutMs: 20_000,
  })
  try {
    assert.notEqual(r.exitCode, 0)
    assert.equal(classifyDenial(r.stderr), 'SANDBOX_CHILD_PROCESS_DENIED')
  } finally {
    await r.cleanup()
  }
})

test('sandbox: worker threads are denied', async () => {
  // The rejection is handled so the failure surfaces as a permission denial
  // rather than an unhandled rejection that the runtime reports as a write
  // error, which would classify as the wrong kind of denial.
  const r = await runInSandbox({
    script:
      'import("node:worker_threads").then((w) => new w.Worker("1", { eval: true }))' +
      '.catch((e) => { console.error("DENIAL:", e && e.message); process.exit(9) })',
    inline: true,
    timeoutMs: 20_000,
  })
  try {
    assert.notEqual(r.exitCode, 0)
    assert.equal(
      classifyDenial(r.stderr),
      'SANDBOX_WORKER_DENIED',
      `stderr was: ${r.stderr.slice(0, 300)}`
    )
  } finally {
    await r.cleanup()
  }
})

test('sandbox: writing inside the sandbox is allowed, outside is denied', async () => {
  const ok = await runInSandbox({
    script: 'import("node:fs").then((f) => f.writeFileSync("inside.txt", "ok"))',
    inline: true,
    timeoutMs: 20_000,
  })
  try {
    assert.equal(ok.exitCode, 0, ok.stderr)
  } finally {
    await ok.cleanup()
  }

  const outside =
    process.platform === 'win32' ? 'C:/Windows/halalchain-sbx.txt' : '/tmp/halalchain-sbx.txt'
  const denied = await runInSandbox({
    script: `import("node:fs").then((f) => f.writeFileSync(${JSON.stringify(outside)}, "x"))`,
    inline: true,
    timeoutMs: 20_000,
  })
  try {
    assert.notEqual(denied.exitCode, 0)
    assert.equal(classifyDenial(denied.stderr), 'SANDBOX_FS_WRITE_DENIED')
  } finally {
    await denied.cleanup()
    await fs.rm(outside, { force: true }).catch(() => undefined)
  }
})

test('sandbox: the parent environment does not leak into the child', async () => {
  process.env['HALALCHAIN_SBX_PARENT_ONLY'] = 'leaked'
  try {
    const r = await runInSandbox({
      script: 'console.log(process.env.HALALCHAIN_SBX_PARENT_ONLY ?? "absent")',
      inline: true,
      timeoutMs: 20_000,
      env: { MY_FLAG: 'enabled' },
    })
    try {
      assert.match(r.stdout, /absent/)
    } finally {
      await r.cleanup()
    }
  } finally {
    delete process.env['HALALCHAIN_SBX_PARENT_ONLY']
  }
})

test('sandbox: explicitly listed env vars are visible', async () => {
  const r = await runInSandbox({
    script: 'console.log(process.env.MY_FLAG ?? "absent")',
    inline: true,
    timeoutMs: 20_000,
    env: { MY_FLAG: 'enabled' },
  })
  try {
    assert.match(r.stdout, /enabled/)
  } finally {
    await r.cleanup()
  }
})

test('sandbox: tenantId is injected as HALALCHAIN_TENANT_ID', async () => {
  const r = await runInSandbox({
    script: 'console.log(process.env.HALALCHAIN_TENANT_ID ?? "absent")',
    inline: true,
    timeoutMs: 20_000,
    tenantId: 'acme',
  })
  try {
    assert.match(r.stdout, /acme/)
  } finally {
    await r.cleanup()
  }
})

test('sandbox: a script path outside the sandbox gets explicit read access', async () => {
  const dir = await fs.mkdtemp(join(tmpdir(), 'halalchain-script-'))
  const scriptPath = join(dir, 'entry.mjs')
  await fs.writeFile(scriptPath, 'console.log("from a file")', 'utf8')
  const r = await runInSandbox({ script: scriptPath, timeoutMs: 20_000 })
  try {
    assert.equal(r.exitCode, 0, r.stderr)
    assert.match(r.stdout, /from a file/)
  } finally {
    await r.cleanup()
    await fs.rm(dir, { recursive: true, force: true })
  }
})

test('sandbox: a missing script is reported rather than run', async () => {
  await assert.rejects(
    () => runInSandbox({ script: join(tmpdir(), 'definitely-not-here.mjs') }),
    /No such script/
  )
})

test('sandbox: a hung script is killed at the timeout', async () => {
  const r = await runInSandbox({
    script: 'setInterval(() => {}, 1000)',
    inline: true,
    timeoutMs: 1_500,
  })
  try {
    assert.equal(r.timedOut, true)
  } finally {
    await r.cleanup()
  }
})

test('sandbox: output is truncated at the capture budget', async () => {
  const r = await runInSandbox({
    script: 'console.log("x".repeat(5000))',
    inline: true,
    timeoutMs: 20_000,
    maxOutputBytes: 500,
  })
  try {
    assert.equal(r.outputTruncated, true)
    assert.ok(r.stdout.length <= 500, `captured ${r.stdout.length} bytes`)
  } finally {
    await r.cleanup()
  }
})

test('sandbox: the sandbox dir is removed on a clean exit, kept with keepDir', async () => {
  const auto = await runInSandbox({ script: 'console.log(1)', inline: true, timeoutMs: 20_000 })
  const gone = await fs
    .access(auto.sandboxDir)
    .then(() => false)
    .catch(() => true)
  await auto.cleanup()
  assert.equal(gone, true, 'a successful run should clean up after itself')

  const kept = await runInSandbox({
    script: 'console.log(1)',
    inline: true,
    timeoutMs: 20_000,
    keepDir: true,
  })
  try {
    const exists = await fs
      .access(kept.sandboxDir)
      .then(() => true)
      .catch(() => false)
    assert.equal(exists, true, 'keepDir must retain the directory')
  } finally {
    await kept.cleanup()
  }
})

test('sandbox: NETWORK_ISOLATION is none — the model has no egress control', () => {
  // If a future Node adds a network allowlist this must change deliberately,
  // not drift into a guarantee the runtime cannot honour.
  assert.equal(NETWORK_ISOLATION, 'none')
})

test('sandbox: classifyDenial ignores ordinary failures', () => {
  assert.equal(classifyDenial(''), undefined)
  assert.equal(classifyDenial('TypeError: boom'), undefined)
  assert.equal(classifyDenial('ReferenceError: require is not defined'), undefined)
})

test('sandbox: reading outside the sandbox is denied', async () => {
  // A file that exists on every supported host, so this exercises the
  // permission model rather than a missing path.
  const outside = process.platform === 'win32' ? 'C:/Windows/win.ini' : '/etc/hostname'
  const r = await runInSandbox({
    script: `import("node:fs").then((f) => f.readFileSync(${JSON.stringify(outside)}, "utf8"))`,
    inline: true,
    timeoutMs: 20_000,
  })
  try {
    assert.notEqual(r.exitCode, 0)
    assert.equal(classifyDenial(r.stderr), 'SANDBOX_FS_READ_DENIED')
  } finally {
    await r.cleanup()
  }
})
