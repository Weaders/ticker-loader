namespace TickerLoader.Application.Models;

public sealed class RawTickerTick
{
    public required string StockExchangeId { get; init; }
    public required string FigiTicker { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public decimal Price { get; init; }
    public long Volume { get; init; }
}
