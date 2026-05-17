namespace TickerLoader.Application.Models;

public record TickKey(int StockExchangeId, int TickerId, DateTimeOffset Timestamp);