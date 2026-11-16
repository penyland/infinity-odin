import { test } from 'node:test'
import assert from 'node:assert/strict'
import { applyCardEvent, type CardState } from './cardEvents.ts'

const event = (eventType: string, pullRequestId: number, text: string) => ({
  eventType,
  pullRequestId,
  content: JSON.stringify({ type: 'AdaptiveCard', text }),
})

test('creates cards in arrival order and updates in place', () => {
  let state: CardState = { cards: [], error: '' }
  state = applyCardEvent(state, event('Created', 1, 'first'))
  state = applyCardEvent(state, event('Created', 2, 'second'))
  state = applyCardEvent(state, event('Updated', 1, 'replacement'))
  assert.deepEqual(state.cards, [
    { id: 1, content: { type: 'AdaptiveCard', text: 'replacement' } },
    { id: 2, content: { type: 'AdaptiveCard', text: 'second' } },
  ])
})

test('reports duplicate creates and unknown updates without changing cards', () => {
  const initial = applyCardEvent({ cards: [], error: '' }, event('Created', 1, 'first'))
  const duplicate = applyCardEvent(initial, event('Created', 1, 'duplicate'))
  const unknown = applyCardEvent(duplicate, event('Updated', 2, 'missing'))
  assert.deepEqual(duplicate.cards, initial.cards)
  assert.match(duplicate.error, /existing card 1/)
  assert.deepEqual(unknown.cards, initial.cards)
  assert.match(unknown.error, /unknown card 2/)
})

test('reports invalid and unknown events', () => {
  const initial = { cards: [], error: '' }
  assert.match(applyCardEvent(initial, null).error, /invalid/)
  assert.match(applyCardEvent(initial, event('Removed', 1, '')).error, /unknown event type/)
  assert.match(applyCardEvent(initial, { pullRequestId: 1, eventType: 'Created', content: '{' }).error, /Invalid card JSON/)
  assert.match(applyCardEvent(initial, { pullRequestId: 1, eventType: 'Created', content: '[]' }).error, /Invalid card JSON/)
  assert.match(applyCardEvent(initial, { pullRequestId: '1', eventType: 'Created', content: '{}' }).error, /invalid card event/)
})
