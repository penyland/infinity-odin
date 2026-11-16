export interface CardState {
  cards: { id: number; content: object }[]
  error: string
}

export function applyCardEvent(state: CardState, event: unknown): CardState {
  if (
    !event || typeof event !== 'object' ||
    !('pullRequestId' in event) || !('content' in event) ||
    typeof event.pullRequestId !== 'number' ||
    !Number.isInteger(event.pullRequestId) ||
    event.pullRequestId < -2147483648 || event.pullRequestId > 2147483647 ||
    typeof event.content !== 'string'
  ) {
    return { ...state, error: 'Received an invalid card event.' }
  }

  let content: unknown
  try {
    content = JSON.parse(event.content)
  } catch {
    return { ...state, error: `Invalid card JSON for pull request ${event.pullRequestId}.` }
  }
  if (!content || typeof content !== 'object' || Array.isArray(content)) {
    return { ...state, error: `Invalid card JSON for pull request ${event.pullRequestId}.` }
  }

  const index = state.cards.findIndex((card) => card.id === event.pullRequestId)

  const eventType = 'eventType' in event ? event.eventType : undefined

  if (eventType === 'Created') {
    if (index !== -1) {
      return { ...state, error: `Ignored Created event for existing card ${event.pullRequestId}.` }
    }
    return { cards: [...state.cards, { id: event.pullRequestId, content }], error: '' }
  }

  if (eventType === 'Updated') {
    if (index === -1) {
      return { ...state, error: `Ignored Updated event for unknown card ${event.pullRequestId}.` }
    }
    const cards = state.cards.slice()
    cards[index] = { id: event.pullRequestId, content }
    return { cards, error: '' }
  }

  return { ...state, error: `Ignored unknown event type: ${String(eventType)}.` }
}
