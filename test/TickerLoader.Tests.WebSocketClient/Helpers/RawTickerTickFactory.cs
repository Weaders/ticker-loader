using TickerLoader.Application.Models;

namespace TickerLoader.Tests.WebSocketClient.Helpers;

internal static class RawTickerTickFactory
{
    public static RawTickerTick Create(
        string stockExchangeId = "MOEX",
        string figiTicker = "BBG004730N88",
        decimal price = 100.5m,
        long volume = 10,
        DateTimeOffset? timestamp = null) =>
        new()
        {
            StockExchangeId = stockExchangeId,
            FigiTicker = figiTicker,
            Price = price,
            Volume = volume,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow
        };
}
