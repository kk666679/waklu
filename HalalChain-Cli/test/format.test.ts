import { test } from 'node:test'
import assert from 'node:assert/strict'
import {
  cosineSimilarity,
  formatBytes,
  percent,
  scoreBar,
  truncate,
} from '../src/lib/format.js'
import { isValidTemperature, isValidTopK, isValidUrl, toFloat, toInt } from '../src/lib/validators.js'
import { parseJsonOption, splitList } from '../src/lib/input.js'

test('format: cosineSimilarity is 1 for identical vectors', () => {
  assert.equal(cosineSimilarity([1, 2, 3], [1, 2, 3]), 1)
  assert.ok(Math.abs(cosineSimilarity([1, 0], [0, 1]) - 0) < 1e-9)
})

test('format: cosineSimilarity returns 0 rather than NaN for degenerate input', () => {
  // A NaN would print as "NaN%", which reads as a bug rather than "no signal".
  assert.equal(cosineSimilarity([1, 2, 3], [1, 2]), 0)
  assert.equal(cosineSimilarity([], []), 0)
  assert.equal(cosineSimilarity([0, 0], [1, 1]), 0)
  assert.equal(cosineSimilarity(undefined, [1]), 0)
  assert.equal(cosineSimilarity([1], undefined), 0)
})

test('format: truncate and formatBytes', () => {
  assert.equal(truncate('abcdef', 3), 'abc...')
  assert.equal(truncate('abc', 10), 'abc')
  assert.equal(truncate(undefined), '')
  assert.equal(formatBytes(512), '512 B')
  assert.equal(formatBytes(2048), '2.0 KB')
  assert.equal(formatBytes(5 * 1024 * 1024), '5.0 MB')
})

test('format: percent handles missing and non-finite scores', () => {
  assert.equal(percent(0.5), '50.0%')
  assert.equal(percent(0.5, 0), '50%')
  assert.equal(percent(undefined), 'n/a')
  assert.equal(percent(Number.NaN), 'n/a')
})

test('format: scoreBar clamps to the 0..1 range', () => {
  assert.equal(scoreBar(0).length, 0)
  assert.equal(scoreBar(1).length, 20)
  assert.equal(scoreBar(5).length, 20, 'scores above 1 must not overflow the bar')
  assert.equal(scoreBar(-1).length, 0)
})

test('validators: URL, top-k and temperature bounds', () => {
  assert.ok(isValidUrl('http://localhost:7071'))
  assert.ok(!isValidUrl('not a url'))

  assert.ok(isValidTopK('5'))
  assert.ok(!isValidTopK('0'))
  assert.ok(!isValidTopK('101'))
  assert.ok(!isValidTopK('abc'))

  assert.ok(isValidTemperature('0'))
  assert.ok(isValidTemperature(0.7))
  assert.ok(!isValidTemperature('2.1'))
  assert.ok(!isValidTemperature(-0.1))
})

test('validators: toInt and toFloat fall back instead of producing NaN', () => {
  assert.equal(toInt('7', 1), 7)
  assert.equal(toInt(undefined, 1), 1)
  assert.equal(toInt('nope', 42), 42)
  assert.equal(toFloat('0.5', 0.7), 0.5)
  assert.equal(toFloat('nope', 0.7), 0.7)
})

test('input: splitList trims and drops empties', () => {
  assert.deepEqual(splitList('a, b ,c'), ['a', 'b', 'c'])
  assert.deepEqual(splitList(''), [])
  assert.deepEqual(splitList(undefined), [])
  assert.deepEqual(splitList('a,,b'), ['a', 'b'])
})

test('input: parseJsonOption reports the label on bad JSON', () => {
  assert.deepEqual(parseJsonOption('{"a":1}', '--input'), { a: 1 })
  assert.deepEqual(parseJsonOption(undefined, '--input'), {})
  assert.throws(() => parseJsonOption('{oops', '--input'), /--input is not valid JSON/)
})
