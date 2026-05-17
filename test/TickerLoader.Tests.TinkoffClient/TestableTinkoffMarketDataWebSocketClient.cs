using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TickerLoader.Application.Models;
using TickerLoader.TinkoffClient;
using TickerLoader.TinkoffClient.Models;
using TickerLoader.TinkoffClient.Options;

namespace TickerLoader.Tests.TinkoffClient;

internal sealed class TestableTinkoffMarketDataWebSocketClient : TinkoffMarketDataWebSocketClient
{
    public TestableTinkoffMarketDataWebSocketClient(
        IOptions<TinkoffWebSocketOptions> options,
        ILogger<TinkoffMarketDataWebSocketClient> logger)
        : base(options, logger)
    {
    }

    public Uri GetWebSocketUri() => BuildWebSocketUri();

    public void ApplyClientWebSocketOptions(System.Net.WebSockets.ClientWebSocket socket) =>
        ConfigureClientWebSocket(socket);

    public RawTickerTick? ProcessMessage(string json) => ParseMessage(json);

    public MarketDataRequest GetSubscribeTradesRequest() => BuildSubscribeTradesRequest();
}
