using TickerLoader.Application.Models;

namespace TickerLoader.Application.Abstractions;

public interface ITicketPriceRepository
{
    ValueTask SaveManyAsync(
        IReadOnlyCollection<TickerTick> prices,
        CancellationToken cancellationToken);
}
