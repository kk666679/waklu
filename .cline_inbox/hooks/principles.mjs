#!/usr/bin/env node
/**
 * principles.mjs — verify the non-negotiable principles P1..P6.
 *
 * Why this exists instead of a grep: a text grep for "verdict" in
 * EvidenceProposal.cs matches the type's own doc comment, which names the
 * forbidden fields on purpose. A naive grep blocks edits to the file that most
 * carefully enforces the principle. These checks therefore match DECLARATIONS
 * and TOKENS, never prose.
 *
 * Usage:
 *   node .cline_inbox/hooks/principles.mjs          # run all
 *   node .cline_inbox/hooks/principles.mjs P1 P5    # run selected
 *
 * Exit 0 = all requested principles hold. Exit 1 = at least one violated.
 */

import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join, relative, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const SKIP = ['obj', 'bin', 'node_modules', '.git', '.autoclaw', '.kilo', '.cline_inbox'];

const results = [];
const record = (id, ok, detail) => results.push({ id, ok, detail });

/** Strip // and block comments so prose can never trigger a match. */
function stripComments(src) {
  let out = '';
  let i = 0;
  while (i < src.length) {
    const two = src.slice(i, i + 2);
    if (two === '/*') {
      const end = src.indexOf('*/', i + 2);
      i = end === -1 ? src.length : end + 2;
      continue;
    }
    if (two === '//') {
      const end = src.indexOf('\n', i);
      i = end === -1 ? src.length : end;
      continue;
    }
    const ch = src[i];
    if (ch === '"' || ch === "'" || ch === '`') {
      out += ch;
      i++;
      while (i < src.length && src[i] !== ch) {
        if (src[i] === '\\') i++;
        i++;
      }
      out += ch;
      i++;
      continue;
    }
    out += ch;
    i++;
  }
  return out;
}

function walk(dir, match, acc = []) {
  let entries;
  try { entries = readdirSync(dir); } catch { return acc; }
  for (const e of entries) {
    if (SKIP.includes(e)) continue;
    const p = join(dir, e);
    let st;
    try { st = statSync(p); } catch { continue; }
    if (st.isDirectory()) walk(p, match, acc);
    else if (match.test(e)) acc.push(p);
  }
  return acc;
}

const P1_FORBIDDEN = ['verdict', 'decision', 'ishalal', 'halalstatus', 'approved', 'rejected', 'status'];

function checkP1() {
  const targets = [
    ...walk(join(ROOT, 'HalalChain.Application/Agentic'), /\.cs$/),
    ...walk(join(ROOT, '.halalchain/agents/app'), /\.pyi?$/),
  ];
  if (targets.length === 0) {
    record('P1', false, 'no agent proposal sources found — cannot verify');
    return;
  }
  const violations = [];
  for (const file of targets) {
    const isPy = file.endsWith('.py');
    const src = stripComments(readFileSync(file, 'utf8'));
    const rel = relative(ROOT, file);
    src.split('\n').forEach((line, idx) => {
      const decl = isPy
        ? /^\s*(?:self\.)?([A-Za-z_][A-Za-z0-9_]*)\s*:\s*[A-Za-z]/.exec(line)
        : /\b(?:public|private|protected|internal)\b[^;{]*?\b([A-Za-z_][A-Za-z0-9_]*)\s*(?:\{|=>|;|=)/.exec(line);
      if (!decl) return;
      if (P1_FORBIDDEN.includes(decl[1].toLowerCase())) {
        violations.push(`${rel}:${idx + 1} declares '${decl[1]}'`);
      }
    });
  }
  record('P1', violations.length === 0,
    violations.length === 0
      ? `no verdict field declared across ${targets.length} source file(s)`
      : violations.join('; '));
}

function checkP3() {
  const file = join(ROOT, 'HalalChain.Application/Storage/IBlobStore.cs');
  let src;
  try { src = readFileSync(file, 'utf8'); }
  catch { record('P3', false, 'IBlobStore.cs not found — cannot verify'); return; }
  const body = stripComments(src);
  const hasDelete = /\bDelete(Async)?\s*[(<]/.test(body) || /\bDelete\b\s*(?:\{|=>)/.test(body);
  record('P3', !hasDelete,
    hasDelete ? 'IBlobStore declares a Delete member' : 'IBlobStore has no Delete member (append-only holds)');
}

function checkP5() {
  const hits = walk(ROOT, /Sha256MerkleTree/i);
  const rel = hits.map((h) => relative(ROOT, h));
  record('P5', rel.length === 0,
    rel.length === 0 ? 'Sha256MerkleTree does not exist' : `found: ${rel.join(', ')}`);
}

const P6_FORBIDDEN = ['verdict', 'halalstatus', 'ishalal', 'approved'];

function checkP6() {
  const files = walk(join(ROOT, 'HalalChain.Platform.Contracts/contracts/src'), /\.sol$/);
  if (files.length === 0) {
    record('P6', false, 'no .sol sources found under contracts/src — cannot verify');
    return;
  }
  const violations = [];
  for (const file of files) {
    const body = stripComments(readFileSync(file, 'utf8'));
    const tokens = body.match(/[A-Za-z_][A-Za-z0-9_]*/g) || [];
    for (const t of tokens) {
      if (P6_FORBIDDEN.includes(t.toLowerCase())) {
        violations.push(`${relative(ROOT, file)}: identifier '${t}'`);
        break;
      }
    }
  }
  record('P6', violations.length === 0,
    violations.length === 0
      ? `no verdict identifier in ${files.length} contract(s)`
      : violations.join('; '));
}

const CHECKS = { P1: checkP1, P3: checkP3, P5: checkP5, P6: checkP6 };
const requested = process.argv.slice(2).filter((a) => CHECKS[a]);

if (requested.length === 0) {
  console.log('HalalChain principle checks\n');
  for (const id of Object.keys(CHECKS)) CHECKS[id]();
} else {
  for (const id of requested) CHECKS[id]();
}

console.log('');
for (const r of results) console.log(`  ${r.ok ? 'PASS' : 'FAIL'}  ${r.id}  ${r.detail}`);
const failed = results.filter((r) => !r.ok);
console.log('');
if (failed.length === 0) {
  console.log(`All ${results.length} principle check(s) hold.`);
  process.exit(0);
}
console.log(`${failed.length} of ${results.length} principle check(s) VIOLATED.`);
process.exit(1);
