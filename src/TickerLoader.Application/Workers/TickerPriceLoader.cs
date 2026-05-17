using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TickerLoader.Application.Abstractions;
using TickerLoader.Application.Models;

namespace TickerLoader.Application.Workers;

internal sealed class TickerPriceLoader(
    IMarketDataClient[] clients,
    ITickerBuffer tickerBuffer,
    IStockDataService stockDataService,
    ILogger<TickerPriceLoader> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var client in clients)
            await client.StartAsync(stoppingToken);

        var consumeTasks = clients
            .Select(client => ConsumeAsync(client, stoppingToken))
            .ToArray();

        try
        {
            await Task.WhenAll(consumeTasks);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var client in clients)
            await client.StopAsync();

        await base.StopAsync(cancellationToken);
    }

    private async Task ConsumeAsync(IMarketDataClient client, CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var rawTick in client.Ticks.ReadAllAsync(stoppingToken))
                await EnqueueTickAsync(rawTick, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Market data stream failed");
        }
    }

    private async Task EnqueueTickAsync(RawTickerTick rawTickData, CancellationToken cancellationToken)
    {
        var key = new TickKey(
            stockDataService.GetStockExhangeId(rawTickData.StockExchangeId),
            stockDataService.GetTickerId(rawTickData.FigiTicker),
            rawTickData.Timestamp);

        try
        {
            await tickerBuffer.AddAsync(
                new TickerTick
                {
                    TickKey = key,
                    Price = rawTickData.Price,
                    Volume = rawTickData.Volume,
                },
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while add to buffer {RawTickerData}", rawTickData);
        }
    }
}
