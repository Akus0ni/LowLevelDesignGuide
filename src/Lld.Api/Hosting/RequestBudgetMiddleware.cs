using Lld.Application.Hosting;

namespace Lld.Api.Hosting;

/// <summary>
/// Counts every tunnelled request and its body bytes (both directions) against the monthly tunnel budget.
/// Local calls (Aspire health checks, tests, the PC itself) are free and not counted.
/// </summary>
internal sealed class RequestBudgetMiddleware(RequestDelegate next, RequestBudgetMeter meter, TunnelMetrics metrics)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!TunnelTraffic.IsTunnelled(context))
        {
            await next(context);
            return;
        }

        var originalBody = context.Response.Body;
        await using var counting = new CountingStream(originalBody);
        context.Response.Body = counting;
        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
            var bytes = (context.Request.ContentLength ?? 0) + counting.BytesWritten;
            meter.Record(bytes);
            metrics.Record(bytes);
        }
    }

    /// <summary>Pass-through write stream that remembers how many bytes went out.</summary>
    private sealed class CountingStream(Stream inner) : Stream
    {
        public long BytesWritten { get; private set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            inner.Write(buffer, offset, count);
            BytesWritten += count;
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await inner.WriteAsync(buffer, cancellationToken);
            BytesWritten += buffer.Length;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override void Flush() => inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        // The inner stream belongs to the server; disposing this wrapper leaves it open (Stream.Dispose
        // only calls Dispose(bool), which this class does not override).
    }
}
