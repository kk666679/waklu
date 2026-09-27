#!/usr/bin/env node
// Build the generated stylesheets for every UI project that owns an SCSS
// source tree.
//
// The source of truth is wwwroot/scss/. Everything under wwwroot/css/ is
// generated and is NOT tracked in git (see .gitignore and
// docs/architecture/tech-debt.md TD-02/TD-17). The Web/Marketplace
// Dockerfiles run this same script in their `styles` stage, so a container
// never depends on a committed artefact.
//
// Two kinds of entry point are compiled:
//
//   wwwroot/scss/entries/<name>.scss  ->  wwwroot/css/<name>.css
//   wwwroot/scss/<name>.scss          ->  wwwroot/css/<name>.css   (top-level bundle)
//
// plus, for HalalChain.Web, every theme under
// wwwroot/scss/themes/<framework>/_<name>.scss -> wwwroot/css/<name>.css.
// Marketplace has no theme sources of its own; it consumes the same Radzen
// theme bundle as Web, so the compiled themes are copied across.

import { createRequire } from 'node:module';
import { existsSync } from 'node:fs';
import { mkdir, readdir, readFile, writeFile, rm, stat } from 'node:fs/promises';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const require = createRequire(import.meta.url);
const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');

// `sass` is a declared devDependency of the root workspace.
let sass;
try {
  sass = require('sass');
} catch {
  console.error(
    'error: the "sass" package is not installed.\n' +
      '       Run `npm install` at the repository root first.'
  );
  process.exit(1);
}

const silent = process.argv.includes('--silent');
const log = (...args) => {
  if (!silent) console.log(...args);
};

/** UI projects that own a wwwroot/scss tree, in build order. */
const PROJECTS = [
  { name: 'HalalChain.Web', copiesThemesFrom: null },
  { name: 'HalalChain.Marketplace', copiesThemesFrom: 'HalalChain.Web' },
];

const SASS_OPTIONS = {
  // The committed bundles were emitted without a BOM/@charset prelude and
  // without a source map; keep it that way so output is byte-stable.
  charset: false,
  sourceMap: false,
  style: 'expanded',
  loadPaths: [],
  quietDeps: true,
  silenceDeprecations: ['import', 'global-builtin', 'mixed-decls', 'color-functions'],
};

async function scssFilesIn(dir) {
  if (!existsSync(dir)) return [];
  const out = [];
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) out.push(...(await scssFilesIn(full)));
    else if (entry.name.endsWith('.scss')) out.push(full);
  }
  return out;
}

async function compile(src, outDir, outName) {
  await mkdir(outDir, { recursive: true });
  const result = sass.compile(src, SASS_OPTIONS);
  const dest = join(outDir, `${outName}.css`);
  await writeFile(dest, result.css, 'utf8');
  return { dest, bytes: Buffer.byteLength(result.css) };
}

/**
 * Theme partials are named with a leading underscore (_fluent-base.scss) so
 * that @use only pulls them in on request, but each one is also a
 * standalone stylesheet that the runtime <link> tags load directly.
 */
async function compileThemes(projectDir, cssDir) {
  const themesDir = join(projectDir, 'wwwroot', 'scss', 'themes');
  const files = await scssFilesIn(themesDir);
  const compiled = [];

  for (const file of files) {
    const name = file.split('/').pop().replace(/^_/, '').replace(/\.scss$/, '');
    // _index.scss / _index-*.scss are the @use aggregators, not bundles.
    if (name.startsWith('index')) continue;
    // _radzen.scss is a partial shared by the theme bundles.
    if (name === 'radzen') continue;

    const result = sass.compile(file, SASS_OPTIONS);
    const dest = join(cssDir, `${name}.css`);
    await writeFile(dest, result.css, 'utf8');
    compiled.push({ dest, bytes: Buffer.byteLength(result.css) });
  }

  return compiled;
}

async function copyThemes(fromCssDir, toCssDir) {
  const copied = [];
  if (!existsSync(fromCssDir)) return copied;

  for (const name of await readdir(fromCssDir)) {
    // Only the Radzen theme bundles are shared; the layout/entry bundles are
    // per-project and were already compiled from that project's own sources.
    if (!/^(fluent|material3)/.test(name) || !name.endsWith('.css')) continue;

    const dest = join(toCssDir, name);
    await writeFile(dest, await readFile(join(fromCssDir, name)));
    copied.push({ dest, bytes: (await stat(dest)).size });
  }

  return copied;
}

async function buildProject(project) {
  const projectDir = join(repoRoot, project.name);
  const scssDir = join(projectDir, 'wwwroot', 'scss');
  const cssDir = join(projectDir, 'wwwroot', 'css');

  if (!existsSync(scssDir)) {
    log(`  ${project.name}: no wwwroot/scss tree, skipping`);
    return [];
  }

  await mkdir(cssDir, { recursive: true });
  const built = [];

  // 1. entries/<name>.scss — the per-concern bundles.
  const entriesDir = join(scssDir, 'entries');
  for (const file of await scssFilesIn(entriesDir)) {
    const name = file.split('/').pop().replace(/^_/, '').replace(/\.scss$/, '');
    built.push(await compile(file, cssDir, name));
  }

  // 2. Top-level bundles that are not partials (app.scss and friends).
  //    Anything starting with "_" is a partial and is skipped.
  for (const entry of await readdir(scssDir, { withFileTypes: true })) {
    if (!entry.isFile()) continue;
    if (!entry.name.endsWith('.scss') || entry.name.startsWith('_')) continue;
    const name = entry.name.replace(/\.scss$/, '');
    built.push(await compile(join(scssDir, entry.name), cssDir, name));
  }

  // 3. Radzen theme bundles — only for the project that owns the theme
  //    sources. Others copy the compiled result (see copyThemes).
  if (!project.copiesThemesFrom) {
    built.push(...(await compileThemes(projectDir, cssDir)));
  }

  return built;
}

async function main() {
  let total = 0;
  const written = new Set();

  for (const project of PROJECTS) {
    const built = await buildProject(project);
    if (built.length === 0) continue;

    for (const { dest, bytes } of built) {
      written.add(dest);
      total += bytes;
    }
    log(`  ${project.name}: ${built.length} stylesheet(s)`);
  }

  // Share the compiled Radzen theme bundles with any project that has no
  // theme sources of its own.
  for (const project of PROJECTS) {
    if (!project.copiesThemesFrom) continue;
    const from = join(repoRoot, project.copiesThemesFrom, 'wwwroot', 'css');
    const to = join(repoRoot, project.name, 'wwwroot', 'css');
    const copied = await copyThemes(from, to);
    for (const { dest, bytes } of copied) {
      written.add(dest);
      total += bytes;
    }
    if (copied.length) log(`  ${project.name}: ${copied.length} shared theme bundle(s)`);
  }

  // Remove stale generated output so a renamed entry point cannot leave an
  // orphan CSS file behind. Scoped Razor CSS (*.razor.css) is never touched.
  for (const project of PROJECTS) {
    const cssDir = join(repoRoot, project.name, 'wwwroot', 'css');
    if (!existsSync(cssDir)) continue;
    for (const name of await readdir(cssDir)) {
      const full = join(cssDir, name);
      if (name.endsWith('.razor.css') || written.has(full)) continue;
      if (!name.endsWith('.css')) continue;
      await rm(full);
      log(`  removed stale ${relative(repoRoot, full)}`);
    }
  }

  log(`\nSCSS build OK — ${written.size} stylesheet(s), ${(total / 1024).toFixed(1)} KiB`);
}

main().catch((error) => {
  console.error(`SCSS build failed: ${error.message}`);
  process.exit(1);
});
