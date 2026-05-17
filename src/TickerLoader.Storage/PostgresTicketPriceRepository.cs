using TickerLoader.Application.Abstractions;
using TickerLoader.Application.Models;
using TickerLoader.Storage.Abstractions;
using TickerLoader.Storage.Schema;

namespace TickerLoader.Storage;

public sealed class PostgresTicketPriceRepository : ITicketPriceRepository
{
    private readonly ITickerPriceDbCommandExecutor _executor;

    public PostgresTicketPriceRepository(ITickerPriceDbCommandExecutor executor)
    {
        _executor = executor;
    }

    public async ValueTask SaveManyAsync(
        IReadOnlyCollection<TickerTick> tickerTicks,
        CancellationToken cancellationToken)
    {
        if (tickerTicks.Count == 0)
            return;

        await _executor.ExecuteAsync(
            TickerPriceSql.Insert,
            new
            {
                StockExchangeIds = tickerTicks.Select(tickerTick => tickerTick.TickKey.StockExchangeId).ToArray(),
                TickerIds = tickerTicks.Select(tickerTick => tickerTick.TickKey.TickerId).ToArray(),
                Timestamps = tickerTicks.Select(tickerTick => tickerTick.TickKey.Timestamp).ToArray(),
                Prices = tickerTicks.Select(tickerTick => tickerTick.Price).ToArray(),
                Volumes = tickerTicks.Select(tickerTick => tickerTick.Volume).ToArray(),
            },
            cancellationToken);
    }
}
