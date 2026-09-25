using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.Core.Internal;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Tests.Runtime;

public class BackPressureTests
{
    [Fact]
    public async Task SlowHandler_StopsTheReadLoopOnceTheBufferIsFull()
    {
        SseGate release = new();
        Recorder<string> handled = new();
        SseSourceOptions options = new() { MaxBufferedEvents = 2 };
        SseHandlersDictionary handlers = new(options.JsonSerializerOptions);
        handlers.AddDataHandler("order", data =>
        {
            release.WaitAsync().GetAwaiter().GetResult();
            handled.Add(data);
        });
        StreamConsumer consumer = new(handlers, options, NullLogger<Core.SseSource>.Instance, _ => { });
        await using OneEventPerReadStream stream = new(10);

        Task consumption = consumer.ConsumeAsync(stream, CancellationToken.None);
        await TestWait.UntilAsync(() => Task.FromResult(stream.ReadCount >= 3));
        for (int i = 0; i < 100; i++)
        {
            await Task.Yield();
        }

        int readsWhileBlocked = stream.ReadCount;
        release.Open();
        await consumption;

        Assert.Equal(3, readsWhileBlocked);
        Assert.Equal(10, handled.Count);
    }

    private sealed class OneEventPerReadStream(int events) : Stream
    {
        private int _reads;

        public int ReadCount => Volatile.Read(ref _reads);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int index = Interlocked.Increment(ref _reads);
            if (index > events)
            {
                return Task.FromResult(0);
            }

            byte[] frame = Encoding.UTF8.GetBytes($"event: order\ndata: {index}\n\n");
            frame.CopyTo(buffer, offset);
            return Task.FromResult(frame.Length);
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
