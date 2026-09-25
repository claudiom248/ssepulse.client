using System.Text;
using Microsoft.AspNetCore.Http;

namespace SsePulse.Client.Tests.Common;

public sealed class ConnectionScript
{
    private readonly List<Func<ScriptState, Task>> _steps = [];

    public ConnectionScript Respond(int statusCode, params (string Name, string Value)[] headers)
    {
        return Add(state =>
        {
            if (state.Started)
            {
                throw new InvalidOperationException("Respond must be the first step of a connection script.");
            }

            state.Context.Response.StatusCode = statusCode;
            foreach ((string name, string value) in headers)
            {
                state.Context.Response.Headers[name] = value;
            }

            state.Ended = true;
            return Task.CompletedTask;
        });
    }

    public ConnectionScript Send(string eventType, string data, string? id = null, int? retryMilliseconds = null)
    {
        StringBuilder frame = new();
        if (id is not null)
        {
            frame.Append("id: ").Append(id).Append('\n');
        }

        if (retryMilliseconds is not null)
        {
            frame.Append("retry: ").Append(retryMilliseconds.Value).Append('\n');
        }

        frame.Append("event: ").Append(eventType).Append('\n');
        foreach (string line in data.Split('\n'))
        {
            frame.Append("data: ").Append(line).Append('\n');
        }

        frame.Append('\n');
        return Raw(frame.ToString());
    }

    public ConnectionScript Comment(string text) => Raw($": {text}\n\n");

    public ConnectionScript Retry(int milliseconds) => Raw($"retry: {milliseconds}\n\n");

    public ConnectionScript Raw(string text) => Add(state => state.WriteAsync(text));

    public ConnectionScript WaitFor(SseGate gate)
    {
        return Add(async state =>
        {
            await state.EnsureStartedAsync();
            await gate.WaitAsync(state.Token);
        });
    }

    public ConnectionScript KeepOpen()
    {
        return Add(async state =>
        {
            await state.EnsureStartedAsync();
            await TestWait.ForeverAsync(state.Token);
        });
    }

    public ConnectionScript Close()
    {
        return Add(async state =>
        {
            await state.EnsureStartedAsync();
            state.Ended = true;
        });
    }

    public ConnectionScript Drop()
    {
        return Add(async state =>
        {
            await state.EnsureStartedAsync();
            state.Context.Abort();
            state.Ended = true;
        });
    }

    internal async Task RunAsync(HttpContext context, CancellationToken cancellationToken)
    {
        ScriptState state = new(context, cancellationToken);
        foreach (Func<ScriptState, Task> step in _steps)
        {
            if (state.Ended)
            {
                break;
            }

            await step(state);
        }
    }

    private ConnectionScript Add(Func<ScriptState, Task> step)
    {
        _steps.Add(step);
        return this;
    }

    internal sealed class ScriptState(HttpContext context, CancellationToken token)
    {
        public HttpContext Context { get; } = context;

        public CancellationToken Token { get; } = token;

        public bool Started { get; private set; }

        public bool Ended { get; set; }

        public async Task EnsureStartedAsync()
        {
            if (Started)
            {
                return;
            }

            Started = true;
            Context.Response.ContentType = "text/event-stream";
            Context.Response.Headers.CacheControl = "no-cache";
            await Context.Response.StartAsync(Token);
            await Context.Response.Body.FlushAsync(Token);
        }

        public async Task WriteAsync(string text)
        {
            await EnsureStartedAsync();
            await Context.Response.WriteAsync(text, Token);
            await Context.Response.Body.FlushAsync(Token);
        }
    }
}
