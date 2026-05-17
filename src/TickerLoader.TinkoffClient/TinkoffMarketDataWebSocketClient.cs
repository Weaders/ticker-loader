using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TickerLoader.Application.Models;
using TickerLoader.WebSocketClient;
using TickerLoader.WebSocketClient.Extensions;
using TickerLoader.TinkoffClient.Models;
using TickerLoader.TinkoffClient.Models.Extensions;
using TickerLoader.TinkoffClient.Options;
using TickerLoader.TinkoffClient.Serialization;

namespace TickerLoader.TinkoffClient;

public class TinkoffMarketDataWebSocketClient : BrokerWebSocketClientBase
{
    private readonly TinkoffWebSocketOptions _options;
    private readonly ILogger<TinkoffMarketDataWebSocketClient> _logger;

    public TinkoffMarketDataWebSocketClient(
        IOptions<TinkoffWebSocketOptions> options,
        ILogger<TinkoffMarketDataWebSocketClient> logger) : base(options.Value, logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    protected override Uri BuildWebSocketUri() => _options.BuildStreamUri();

    protected override void ConfigureClientWebSocket(ClientWebSocket socket)
    {
        socket.Options.SetRequestHeader("Authorization", $"Bearer {_options.AccessToken}");
        socket.Options.AddSubProtocol("json");
    }

    internal MarketDataRequest BuildSubscribeTradesRequest() =>
        new()
        {
            SubscribeTradesRequest = new SubscribeTradesRequest
            {
                Instruments = _options.Instruments
                    .Select(InstrumentIdentifier.FromInstrumentId)
                    .ToArray(),
                SubscriptionAction = SubscriptionAction.Subscribe.ToApiValue()
            }
        };

    protected override async Task SendSubscriptionAsync(ClientWebSocket socket,CancellationToken cancellationToken) =>
        await socket.SendTextAsync(TinkoffJson.Serialize(BuildSubscribeTradesRequest()), cancellationToken);

    protected override RawTickerTick? ParseMessage(string json)
    {
        var response = TinkoffJson.DeserializeResponse(json);
        if (response?.Trade is not { } trade)
            return null;

        return new RawTickerTick
        {
            StockExchangeId = StockExchangeId,
            FigiTicker = trade.Figi,
            Volume = trade.Quantity,
            Price = trade.PriceValue,
            Timestamp = trade.Time
        };
    }

    protected override void OnConnectionError(Exception exception) =>
        _logger.LogError(exception, "Tinkoff disconnected: {ErrorMessage}. Retrying in {DelayOnError}", exception.Message, DelayOnError);
}
