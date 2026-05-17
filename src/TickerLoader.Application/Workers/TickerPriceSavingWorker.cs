using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TickerLoader.Application.Abstractions;
using TickerLoader.Application.Models;
using TickerLoader.Application.Workers.Options;

namespace TickerLoader.Application.Workers;

public class TickerPriceSavingWorker : BackgroundService
{
    private readonly ITickerBuffer _buffer;
    private readonly ITicketPriceRepository _repository;
    private readonly TickerSavingWorkerOptions _options;
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<TickerPriceSavingWorker> _logger;

    private long _savedTicksSinceLastReport;

    public TickerPriceSavingWorker(
        ITickerBuffer buffer,
        ITicketPriceRepository repository,
        IOptions<TickerSavingWorkerOptions> options,
        ILogger<TickerPriceSavingWorker> logger)
    {
        _options = options.Value;
        _buffer = buffer;
        _repository = repository;
        _logger = logger;
        _semaphore = new SemaphoreSlim(_options.MaxDegreeOfParallelism, _options.MaxDegreeOfParallelism);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var reportTimer = new PeriodicTimer(_options.ReportLogginEvery);
        var reportingTask = ReportSavedTicksAsync(reportTimer, stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _semaphore.WaitAsync(stoppingToken);

                try
                {
                    _ = RunBatchAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Exception while saving to database ticks. Will retry in {ErrorDelay}", _options.ErrorDelay);
                    await Task.Delay(_options.ErrorDelay, stoppingToken);
                }
            }
        }
        finally
        {
            await reportingTask;
        }
    }

    private async Task ReportSavedTicksAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var tickCount = Interlocked.Exchange(ref _savedTicksSinceLastReport, 0);
                _logger.LogInformation(
                    "Saved {TickCount} ticks in the last {ReportIntervalMs}ms",
                    tickCount,
                    _options.ReportLogginEvery.TotalMilliseconds);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RunBatchAsync(CancellationToken stoppingToken)
    {
        IReadOnlyCollection<TickerTick> batch = [];

        try
        {
            batch = await _buffer.ReadBatchAsync(_options.MaxBatchSize, stoppingToken);

            if (batch.Count == 0)
            {
                await Task.Delay(_options.EmptyReadDelay, stoppingToken);
                return;
            }

            await _repository.SaveManyAsync(batch, stoppingToken);

            Interlocked.Add(ref _savedTicksSinceLastReport, batch.Count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while saving to database ticks");

            foreach (var item in batch)
                await _buffer.AddAsync(item, stoppingToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
