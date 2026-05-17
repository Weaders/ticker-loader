using TickerLoader.Application.Models;

namespace TickerLoader.Application.Abstractions
{
    public interface ITickerBuffer
    {
        ValueTask AddAsync(TickerTick tick, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<TickerTick>> ReadBatchAsync(int batchSize, CancellationToken cancellationToken);
    }
}
