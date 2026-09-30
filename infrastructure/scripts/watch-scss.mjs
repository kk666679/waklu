#!/usr/bin/env node
// Watch wwwroot/scss/ across the UI projects and rebuild on change.
//
// Thin wrapper around scripts/build-scss.mjs: the initial build is the same
// one-shot build, then every subsequent change re-runs it. The build is
// idempotent and fast enough (~1s) that debouncing per-file would add
// complexity for no benefit.

import { spawn } from 'node:child_process';
import { watch, existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

// Walk up to the repo root by marker file rather than counting `..` hops.
// This file moved from <root>/scripts/ to <root>/infrastructure/scripts/ in
// commit 5e58296, which turned the old `resolve(__dirname, '..')` into
// <root>/infrastructure — so the watcher bound to directories that do not
// exist and the child build ran with the wrong root.
const REPO_MARKER = 'HalalChain.Platform.sln';
function findRepoRoot(start) {
  let dir = start;
  for (;;) {
    if (existsSync(join(dir, REPO_MARKER))) return dir;
    const parent = dirname(dir);
    if (parent === dir) {
      throw new Error(
        `Cannot locate the repository root: no ${REPO_MARKER} found at or above ${start}.`
      );
    }
    dir = parent;
  }
}
const repoRoot = findRepoRoot(dirname(fileURLToPath(import.meta.url)));
const buildScript = join(repoRoot, 'infrastructure', 'scripts', 'build-scss.mjs');

const WATCHED = [
  join(repoRoot, 'HalalChain.Web', 'wwwroot', 'scss'),
  join(repoRoot, 'HalalChain.Marketplace', 'wwwroot', 'scss'),
];

let building = false;
let pending = false;

function build(label) {
  if (building) {
    pending = true;
    return;
  }
  building = true;

  const child = spawn(process.execPath, [buildScript, '--silent'], {
    stdio: ['ignore', 'ignore', 'inherit'],
  });

  child.on('exit', (code) => {
    building = false;
    if (code === 0) {
      console.log(`[scss] rebuilt (${label})`);
    }
    if (pending) {
      pending = false;
      build('coalesced change');
    }
  });
}

console.log('[scss] initial build…');
build('initial');

for (const dir of WATCHED) {
  try {
    watch(dir, { recursive: true }, (_event, filename) => {
      if (!filename || !filename.endsWith('.scss')) return;
      build(filename);
    });
    console.log(`[scss] watching ${dir.replace(`${repoRoot}/`, '')}`);
  } catch (error) {
    console.error(`[scss] cannot watch ${dir}: ${error.message}`);
    process.exit(1);
  }
}
