using System.Threading.Channels;
using TickerLoader.Application.Abstractions;
using TickerLoader.Application.Models;

namespace TickerLoader.Application;

public sealed class TickerBuffer() : ITickerBuffer
{    
    private readonly Channel<TickerTick> _channel = Channel.CreateUnbounded<TickerTick>();

    public ValueTask AddAsync(TickerTick tick, CancellationToken cancellationToken)
    {
        return _channel.Writer.WriteAsync(tick, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TickerTick>> ReadBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        await _channel.Reader.WaitToReadAsync(cancellationToken);

        var batch = new List<TickerTick>(batchSize);

        while (batch.Count < batchSize && _channel.Reader.TryRead(out var item))
        {
            batch.Add(item);
        }

        return batch;
    }
}
