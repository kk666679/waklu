#!/usr/bin/env node
/**
 * gate-check.mjs — run the automated criteria for a phase gate.
 *
 * Zero dependencies, no bash, no yq, no jq. Node 18+ only, because this host
 * has none of the usual Unix tooling.
 *
 * Usage:
 *   node .cline_inbox/hooks/gate-check.mjs                 # list gates
 *   node .cline_inbox/hooks/gate-check.mjs gate-001-002    # one gate
 *   node .cline_inbox/hooks/gate-check.mjs --all
 *
 * Criteria marked automated:false are HUMAN judgements. This script reports
 * them PENDING and can never mark them PASS. A green run is necessary but
 * NOT sufficient to pass a gate.
 */

import { readFileSync, existsSync, readdirSync, statSync } from 'node:fs';
import { join, relative, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { execFileSync } from 'node:child_process';

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const GATES_FILE = join(ROOT, '.cline_inbox/manifests/gates.yaml');
const EVIDENCE_FILE = join(ROOT, '.cline_inbox/manifests/evidence-index.yaml');
const SKIP = ['obj', 'bin', 'node_modules', '.git', '.autoclaw', '.kilo', '.cline_inbox'];

/** Minimal YAML reader for the shapes used in this repo's manifests. */
export function parseYaml(text) {
  const lines = text.split('\n');
  let i = 0;
  const indentOf = (l) => l.match(/^ */)[0].length;

  function scalar(v) {
    v = v.trim();
    if (v === 'null' || v === '~' || v === '') return null;
    if (v === 'true') return true;
    if (v === 'false') return false;
    if (/^-?\d+$/.test(v)) return parseInt(v, 10);
    if (v.startsWith('[') && v.endsWith(']')) {
      const inner = v.slice(1, -1).trim();
      return inner ? inner.split(',').map(scalar) : [];
    }
    if ((v.startsWith('"') && v.endsWith('"')) || (v.startsWith("'") && v.endsWith("'"))) {
      return v.slice(1, -1);
    }
    return v.replace(/\s+#.*$/, '').trim();
  }

  function parseBlock(minIndent) {
    let first = null;
    for (let j = i; j < lines.length; j++) {
      const t = lines[j];
      if (!t.trim() || t.trim().startsWith('#')) continue;
      if (indentOf(t) < minIndent) break;
      first = t;
      break;
    }
    if (first === null) return null;

    if (first.trim().startsWith('- ')) {
      const arr = [];
      while (i < lines.length) {
        const t = lines[i];
        if (!t.trim() || t.trim().startsWith('#')) { i++; continue; }
        const ind = indentOf(t);
        if (ind < minIndent || !t.trim().startsWith('- ')) break;
        const rest = t.trim().slice(2);
        const itemIndent = ind + 2;
        i++;
        const m = /^([A-Za-z0-9_-]+):\s*(.*)$/.exec(rest);
        if (m) {
          const obj = {};
          obj[m[1]] = m[2] === '' ? parseBlock(itemIndent) : scalar(m[2]);
          while (i < lines.length) {
            const t2 = lines[i];
            if (!t2.trim() || t2.trim().startsWith('#')) { i++; continue; }
            if (indentOf(t2) < itemIndent || t2.trim().startsWith('- ')) break;
            const mm = /^([A-Za-z0-9_-]+):\s*(.*)$/.exec(t2.trim());
            if (!mm) { i++; continue; }
            i++;
            obj[mm[1]] = mm[2] === '' ? parseBlock(indentOf(t2) + 2) : scalar(mm[2]);
          }
          arr.push(obj);
        } else {
          arr.push(scalar(rest));
        }
      }
      return arr;
    }

    const obj = {};
    while (i < lines.length) {
      const t = lines[i];
      if (!t.trim() || t.trim().startsWith('#')) { i++; continue; }
      const ind = indentOf(t);
      if (ind < minIndent || t.trim().startsWith('- ')) break;
      const m = /^([A-Za-z0-9_-]+):\s*(.*)$/.exec(t.trim());
      if (!m) { i++; continue; }
      i++;
      obj[m[1]] = m[2] === '' ? parseBlock(ind + 1) : scalar(m[2]);
    }
    return obj;
  }

  return parseBlock(0);
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
