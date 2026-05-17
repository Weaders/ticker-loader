namespace TickerLoader.WebSocketClient.Options;

public abstract class BrokerWebSocketOptionsBase
{
    public string StockExchangeId { get; set; } = string.Empty;

    public TimeSpan DelayOnError { get; set; } = TimeSpan.FromMilliseconds(150);
}
