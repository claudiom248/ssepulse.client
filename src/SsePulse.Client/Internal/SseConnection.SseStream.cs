namespace SsePulse.Client.Internal;

internal partial class SseConnection
{
    private class SseStream : Stream
    {
        private readonly SseConnection _connection;
        private readonly Stream _innerStream;
        private readonly HttpResponseMessage _response;

        private SseStream(SseConnection connection, Stream innerStream, HttpResponseMessage response)
        {
            _innerStream = innerStream;
            _connection = connection;
            _response = response;
        }

        public static SseStream Wrap(SseConnection connection, Stream innerStream, HttpResponseMessage response) =>
            new(connection, innerStream, response);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerStream.Dispose();
                _response.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _innerStream.DisposeAsync().ConfigureAwait(false);
            _response.Dispose();
            await base.DisposeAsync().ConfigureAwait(false);
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                return _innerStream.Read(buffer, offset, count);
            }
            catch (Exception ex)
            {
                _ = _connection.SetDisconnectedAsync(ex).AsTask();
                throw;
            }
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _innerStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
            }
            catch (Exception ex)
            {
                await _connection.SetDisconnectedAsync(ex).ConfigureAwait(false);
                throw;
            }
        }

        public override int Read(Span<byte> buffer)
        {
            try
            {
                return base.Read(buffer);
            }
            catch (Exception ex)
            {
                _ = _connection.SetDisconnectedAsync(ex).AsTask();
                throw;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }
    }
}