using TickerLoader.TinkoffClient.Options;

namespace TickerLoader.Tests.TinkoffClient.Helpers;

internal static class TinkoffWebSocketOptionsFactory
{
    public static TinkoffWebSocketOptions Create(
        string accessToken = "test-token",
        string stockExchangeId = "MOEX",
        string baseUrl = "wss://invest-public-api.tinkoff.ru/ws",
        string streamPath = "tinkoff.public.invest.api.contract.v1.MarketDataStreamService/MarketDataStream",
        params string[] instruments) =>
        new()
        {
            AccessToken = accessToken,
            StockExchangeId = stockExchangeId,
            BaseUrl = baseUrl,
            StreamPath = streamPath,
            Instruments = instruments.Length > 0 ? instruments : ["BBG004730N88"],
            DelayOnError = TimeSpan.FromMilliseconds(50)
        };
}
