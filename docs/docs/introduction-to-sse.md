# Introduction to Server-Sent Events

## What is a Server-Sent Event?

Imagine you are waiting for a pizza delivery. Instead of calling the restaurant every two minutes
to ask "is it ready yet?", you give them your phone number and they call **you** the moment
something happens — "your order is being prepared", "the driver is on the way", "it's at the door!".

That is exactly how **Server-Sent Events (SSE)** works. Your app opens a connection to a server
and then just waits. The server pushes updates whenever it has something new to say, without your
app having to ask again and again.

---

## How does it work?

SSE is built on top of plain HTTP, the same protocol your browser uses every time you open a
website. Here is what happens step by step:

1. Your app sends a normal HTTP `GET` request to a special endpoint (e.g. `/events`).
2. Instead of closing the connection after replying, the server keeps it **open** and starts
   streaming data.
3. Whenever the server has a new event it writes it into the open stream.
4. Your app reads each event as it arrives.
5. The connection stays open until your app closes it, the server closes it, or the network
   drops.

---

## What does an SSE event look like?

Events are plain text. Each one looks like this:

```
event: OrderCreated
data: {"id":"abc-123","total":49.99}

```

- **`event`** — the name of the event. Your app uses this to decide which handler to run.
- **`data`** — the payload. It can be any text; JSON is the most common format.
- An **empty line** marks the end of the event (like pressing Enter twice).

A stream can carry many different event types mixed together:

```
event: OrderCreated
data: {"id":"abc-123","total":49.99}

event: OrderShipped
data: {"id":"abc-123","carrier":"FedEx"}

event: ping
data: keep-alive

```

---

## What about reconnections?

SSE has built-in reconnection support. If the network drops, the browser (or any well-behaved
client) will automatically try to reconnect. To avoid missing events, the server can assign each
event an **`id`**:

```
id: 42
event: OrderCreated
data: {"id":"abc-123"}

```

When the client reconnects it sends the last `id` it received in a `Last-Event-ID` header. The
server can use that to replay any events the client missed while disconnected.

SsePulse handles this automatically when you call `.AddLastEventId()` on the builder.

---

## SSE vs WebSockets vs polling

| | **Polling** | **SSE** | **WebSockets** |
|---|---|---|---|
| Direction | Client → Server (repeated) | Server → Client (one-way) | Both directions |
| Protocol | HTTP | HTTP | Upgraded HTTP (ws://) |
| Complexity | Low | Low | Higher |
| Reconnection | Manual | Built-in | Manual |
| Good for | Infrequent updates | Continuous server updates | Chat, games, bidirectional |

**Rule of thumb**: if the server needs to push data to the client and the client does not need
to send messages back, SSE is the simplest and most efficient choice.

---

## A real-world example

Think of a stock ticker, a live order status page, or a progress bar for a long-running job.
The server knows when something changes; SSE lets it tell your app immediately, without wasting
bandwidth on repeated "anything new?" requests.

With SsePulse, consuming those events looks like this:

```csharp
source.On<StockPriceChanged>(e =>
    Console.WriteLine($"{e.Symbol}: {e.Price:C}"));

await source.StartConsumeAsync(CancellationToken.None);
```

No polling loops, no manual reconnection logic — SsePulse takes care of it.

