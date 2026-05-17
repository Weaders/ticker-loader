namespace TickerLoader.Tests.TinkoffClient.Helpers;

internal static class TinkoffTradeMessageFactory
{
    public static string CreateTradeMessage(
        string figi = "BBG004730N88",
        string units = "250",
        int nano = 750_000_000,
        long quantity = 7,
        string time = "2026-05-16T12:00:00Z") =>
        $$"""
        {
          "trade": {
            "figi": "{{figi}}",
            "price": { "units": "{{units}}", "nano": {{nano}} },
            "time": "{{time}}",
            "quantity": {{quantity}}
          }
        }
        """;

    public static string CreateNonTradeMessage() =>
        """
        {
          "subscribeTradesResponse": {
            "trackingId": "test-tracking-id"
          }
        }
        """;
}
