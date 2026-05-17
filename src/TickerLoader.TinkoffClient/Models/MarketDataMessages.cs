using System.Text.Json.Serialization;

namespace TickerLoader.TinkoffClient.Models;

public sealed class MarketDataRequest
{
    [JsonPropertyName("subscribeTradesRequest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required SubscribeTradesRequest SubscribeTradesRequest { get; init; }
}

public sealed class SubscribeTradesRequest
{
    [JsonPropertyName("subscriptionAction")]
    public required string SubscriptionAction { get; init; }

    [JsonPropertyName("instruments")]
    public required IReadOnlyCollection<InstrumentIdentifier> Instruments { get; init; }
}

public sealed class MarketDataResponse
{
    [JsonPropertyName("trade")]
    public Trade? Trade { get; init; }
}

public sealed class Trade
{

    [JsonPropertyName("figi")]
    public string Figi { get; init; }

    [JsonPropertyName("price")]
    public Quotation Price { get; init; }

    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; init; }

    [JsonPropertyName("quantity")]
    public long Quantity { get; init; }

    public decimal PriceValue => Price.ToDecimal();
}
