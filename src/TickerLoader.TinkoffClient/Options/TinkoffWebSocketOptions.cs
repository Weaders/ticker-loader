using TickerLoader.WebSocketClient.Options;

namespace TickerLoader.TinkoffClient.Options;

public sealed class TinkoffWebSocketOptions : BrokerWebSocketOptionsBase
{
    public string AccessToken { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "wss://invest-public-api.tinkoff.ru/ws";

    public string StreamPath { get; set; } =
        "tinkoff.public.invest.api.contract.v1.MarketDataStreamService/MarketDataStream";

    public string[] Instruments { get; set; } = ["BBG004730N88"];

    public Uri BuildStreamUri() =>
        new($"{BaseUrl.TrimEnd('/')}/{StreamPath.TrimStart('/')}");
}
