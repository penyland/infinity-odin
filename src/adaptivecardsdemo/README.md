# Adaptive Card Playground

A Vite React and TypeScript 7 app that displays Adaptive Cards received over
server-sent events.

## Run

Use Node.js 22.18 or later. Install dependencies with `npm install`. Configure
`VITE_API_BASE_URL` to the API
base URL in the Aspire AppHost environment, then start the app with `npm run dev`.
For example:

```csharp
app.AddJavaScriptApp("cards", "../AdaptiveCardPlayground")
   .WithEnvironment("VITE_API_BASE_URL", "https://localhost:5001");
```

Replace the example path and URL with the values for your AppHost and API. The
URL must be reachable by the browser. When the API and Vite use different
origins, the API must allow the Vite origin via CORS.

## Sample API

The local .NET 11 sample broadcasts events only to currently connected
clients. It has no authentication and allows any origin; use it for local
testing only.

Start it with `dotnet run --project SampleApi`, then start Vite with
`VITE_API_BASE_URL=http://localhost:5254`. With Aspire, set that variable to
the browser-reachable URL of the API resource.

To publish a card from PowerShell while the app is connected:

```powershell
$card = @{
  type = 'AdaptiveCard'
  version = '1.0'
  body = @(@{ type = 'TextBlock'; text = 'Pull request created' })
} | ConvertTo-Json -Depth 10 -Compress
$event = @{
  pullRequestId = 42
  eventType = 'Created'
  content = $card
} | ConvertTo-Json -Compress
Invoke-RestMethod -Method Post -Uri 'http://localhost:5254/cards' -ContentType 'application/json' -Body $event
```

Post another event with `eventType = 'Updated'`, the same `pullRequestId`,
and new card JSON in `content` to replace it. The POST returns `202 Accepted`
even if no clients are connected; it does not store or replay events.

## Event stream

The app connects to `GET /pullrequests` on the configured API using `EventSource`.
Send default SSE `message` events with a JSON payload, for example:

```text
data: {"pullRequestId":42,"eventType":"Created","content":"{\"type\":\"AdaptiveCard\",\"version\":\"1.0\",\"body\":[{\"type\":\"TextBlock\",\"text\":\"Hello\"}]}"}

data: {"pullRequestId":42,"eventType":"Updated","content":"{\"type\":\"AdaptiveCard\",\"version\":\"1.0\",\"body\":[{\"type\":\"TextBlock\",\"text\":\"Updated\"}]}"}

```

Each event ends with a blank line. Created cards are appended in arrival order;
updates replace the content in place. Duplicate creates and updates for unknown
IDs are ignored and reported. `content` is a string containing the card JSON.
The browser reconnects automatically, but the API
only sends new events: cards created before connection and events missed during
disconnection will not be recovered.

The sample API sends response headers with its first event, so the app may show
`Connecting...` until an event is published.
