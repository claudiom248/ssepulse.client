namespace SsePulse.Client.Core.Internal;

internal partial class SseConnection
{
    private class SseStream : Stream
    {
        private readonly SseConnection _connection;
        private readonly Stream _innerStream;

        private SseStream(SseConnection connection, Stream innerStream)
        {
            _innerStream = innerStream;
            _connection = connection;
        }

        public static SseStream Wrap(SseConnection connection, Stream innerStream) => new(connection, innerStream);

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _innerStream.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            try
            {
#if NET8_0_OR_GREATER
                return await _innerStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
#else
                return await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
#endif
            }
            catch (Exception ex)
            {
                _connection.SetDisconnected(ex);
                throw;
            }
        }

#if NET8_0_OR_GREATER
        public override int Read(Span<byte> buffer)
        {
            try
            {
                return base.Read(buffer);
            }
            catch (Exception ex)
            {
                _connection.SetDisconnected(ex);
                throw;
            }
        }
#endif

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