import { useEffect, useRef, useState } from 'react'
import * as AdaptiveCards from 'adaptivecards'
import MarkdownIt from 'markdown-it'
import { applyCardEvent, type CardState } from './cardEvents.ts'
import './App.css'

const markdown = new MarkdownIt()
AdaptiveCards.AdaptiveCard.onProcessMarkdown = (text, result) => {
  result.outputHtml = markdown.render(text)
  result.didProcess = true
}

function Card({ content }: { content: object }) {
  const container = useRef<HTMLElement>(null)

  useEffect(() => {
    const element = container.current
    if (!element) return
    try {
      const card = new AdaptiveCards.AdaptiveCard()
      card.parse(content)
      const rendered = card.render()
      if (!rendered) throw new Error('The renderer did not produce a card.')
      element.replaceChildren(rendered)
    } catch (error) {
      const message = document.createElement('p')
      message.className = 'error'
      message.setAttribute('role', 'alert')
      message.textContent = `Could not render card: ${error instanceof Error ? error.message : String(error)}`
      element.replaceChildren(message)
    }

    return () => element.replaceChildren()
  }, [content])

  return <article className="card" ref={container} />
}

function getStreamConfig() {
  const baseUrl = import.meta.env.VITE_API_BASE_URL
  if (!baseUrl) return { error: 'Set VITE_API_BASE_URL to the API base URL.' }
  try {
    const url = new URL('/teams/pullrequests', baseUrl)
    if (!['http:', 'https:'].includes(url.protocol)) {
      throw new Error('The API URL must use HTTP or HTTPS.')
    }
    return { url: url.href }
  } catch (error) {
    return { error: `Invalid VITE_API_BASE_URL: ${error instanceof Error ? error.message : String(error)}` }
  }
}

const streamConfig = getStreamConfig()

function App() {
  const [state, setState] = useState<CardState>({ cards: [], error: '' })
  const [status, setStatus] = useState(streamConfig.error || 'Connecting...')

  useEffect(() => {
    if (!streamConfig.url) return
    const source = new EventSource(streamConfig.url)
    source.onopen = () => setStatus('Connected')
    source.onerror = () => setStatus('Disconnected. Retrying...')
    source.onmessage = (event) => {
      let payload: unknown
      try {
        payload = JSON.parse(event.data)
      } catch {
        setState((previous) => ({ ...previous, error: 'Received invalid event JSON.' }))
        return
      }
      setState((previous) => applyCardEvent(previous, payload))
    }
    return () => source.close()
  }, [])

  return (
    <main>
      <h1>Adaptive Cards</h1>
      <p className="status" role="status">{status}</p>
      {state.error && <p className="error" role="alert">{state.error}</p>}
      {state.cards.length === 0 ? (
        <p className="empty">Waiting for cards...</p>
      ) : (
        <div className="cards">
          {state.cards.map((card) => <Card key={card.id} content={card.content} />)}
        </div>
      )}
    </main>
  )
}

export default App
