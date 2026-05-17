using Microsoft.Extensions.DependencyInjection;
using TickerLoader.Application.Abstractions;
using TickerLoader.Application.Workers;

namespace TickerLoader.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services
            .AddSingleton<ITickerBuffer, TickerBuffer>()
            .AddSingleton<IStockDataService, StockDataService>()
            .AddHostedService<TickerPriceSavingWorker>()
            .AddHostedService<TickerPriceLoader>();
}
