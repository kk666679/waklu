import { test } from 'node:test'
import assert from 'node:assert/strict'
import { banner } from '../src/lib/banner.js'

test('banner: renders the HalalChain wordmark with the tagline and version', () => {
  const art = banner('9.9.9')
  const lines = art.split('\n').filter((l) => l.length > 0)
  assert.ok(lines.length >= 6, 'wordmark should span several lines')
  assert.ok(art.includes('AI-native halal commerce toolkit'), 'tagline must be present')
  assert.ok(art.includes('halalchain v9.9.9'), 'version must be present')
})

test('banner: carries no trailing whitespace on any line', () => {
  for (const line of banner().split('\n')) {
    assert.equal(line, line.replace(/\s+$/, ''), `trailing whitespace in: ${JSON.stringify(line)}`)
  }
})

test('banner: falls back to a wordmark when the requested font is unknown', () => {
  const previous = process.env['HALALCHAIN_BANNER_FONT']
  process.env['HALALCHAIN_BANNER_FONT'] = 'definitely-not-a-figlet-font'
  try {
    const art = banner()
    assert.ok(art.includes('AI-native halal commerce toolkit'))
    assert.ok(art.split('\n').filter((l) => l.length > 0).length >= 6)
  } finally {
    if (previous === undefined) delete process.env['HALALCHAIN_BANNER_FONT']
    else process.env['HALALCHAIN_BANNER_FONT'] = previous
  }
})