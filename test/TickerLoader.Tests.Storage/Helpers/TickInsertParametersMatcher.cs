using TickerLoader.Application.Models;

namespace TickerLoader.Tests.Storage.Helpers;

internal static class TickInsertParametersMatcher
{
    public static bool Matches(object parameters, params TickerTick[] ticks)
    {
        var type = parameters.GetType();

        var stockExchangeIds = (int[])type.GetProperty("StockExchangeIds")!.GetValue(parameters)!;
        var tickerIds = (int[])type.GetProperty("TickerIds")!.GetValue(parameters)!;
        var timestamps = (DateTimeOffset[])type.GetProperty("Timestamps")!.GetValue(parameters)!;
        var prices = (decimal[])type.GetProperty("Prices")!.GetValue(parameters)!;
        var volumes = (long[])type.GetProperty("Volumes")!.GetValue(parameters)!;

        if (stockExchangeIds.Length != ticks.Length)
            return false;

        for (var i = 0; i < ticks.Length; i++)
        {
            if (stockExchangeIds[i] != ticks[i].TickKey.StockExchangeId)
                return false;

            if (tickerIds[i] != ticks[i].TickKey.TickerId)
                return false;

            if (timestamps[i] != ticks[i].TickKey.Timestamp)
                return false;

            if (prices[i] != ticks[i].Price)
                return false;

            if (volumes[i] != ticks[i].Volume)
                return false;
        }

        return true;
    }
}
