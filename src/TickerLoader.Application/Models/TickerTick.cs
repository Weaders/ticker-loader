namespace TickerLoader.Application.Models;

public sealed class TickerTick
{
    public required TickKey TickKey { get; init; }
    public decimal Price { get; init; }
    public long Volume { get; init; }
}
