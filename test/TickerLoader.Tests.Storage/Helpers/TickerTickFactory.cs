using TickerLoader.Application.Models;

namespace TickerLoader.Tests.Storage.Helpers;

internal static class TickerTickFactory
{
    public static TickerTick Create(
        int stockExchangeId = 1,
        int tickerId = 1,
        decimal price = 100.5m,
        long volume = 10,
        DateTimeOffset? timestamp = null) =>
        new()
        {
            TickKey = new TickKey(stockExchangeId, tickerId, timestamp ?? DateTimeOffset.UtcNow),
            Price = price,
            Volume = volume
        };
}
