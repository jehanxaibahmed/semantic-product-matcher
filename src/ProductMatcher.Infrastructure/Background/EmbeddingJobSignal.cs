using System.Threading.Channels;
using ProductMatcher.Application.Abstractions;

namespace ProductMatcher.Infrastructure.Background;

/// <summary>Coalescing wake-up signal: many notifications before the worker runs collapse into one.</summary>
internal sealed class EmbeddingJobSignal : IEmbeddingJobSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public void Notify() => _channel.Writer.TryWrite(true);

    public ValueTask<bool> WaitAsync(CancellationToken cancellationToken) =>
        _channel.Reader.WaitToReadAsync(cancellationToken);

    public void Reset() => _channel.Reader.TryRead(out _);
}
