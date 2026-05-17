namespace TickerLoader.Storage.Schema;

internal static class TickerPriceSql
{
    public const string Insert = """
        INSERT INTO price_ticks (stock_exchange_id, ticker_id, timestamp, price, volume)
        SELECT * FROM UNNEST(
            @StockExchangeIds::int[],
            @TickerIds::int[],
            @Timestamps::timestamptz[],
            @Prices::numeric[],
            @Volumes::bigint[]
        )
        ON CONFLICT (stock_exchange_id, ticker_id, timestamp) DO NOTHING
        """;
}
