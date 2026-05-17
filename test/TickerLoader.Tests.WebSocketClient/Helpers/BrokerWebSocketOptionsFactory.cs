using TickerLoader.Tests.WebSocketClient.Helpers;

namespace TickerLoader.Tests.WebSocketClient.Helpers;

internal static class BrokerWebSocketOptionsFactory
{
    public static TestBrokerWebSocketOptions Create(
        string stockExchangeId = "MOEX",
        TimeSpan? delayOnError = null) =>
        new()
        {
            StockExchangeId = stockExchangeId,
            DelayOnError = delayOnError ?? TimeSpan.FromMilliseconds(10)
        };
}
