using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TickerLoader.Application.Abstractions;
using TickerLoader.Application.Workers;
using TickerLoader.Application.Workers.Options;

namespace TickerLoader.Tests.Application.Workers;

internal sealed class TestableTickerPriceSavingWorker : TickerPriceSavingWorker
{
    public TestableTickerPriceSavingWorker(
        ITickerBuffer buffer,
        ITicketPriceRepository repository,
        IOptions<TickerSavingWorkerOptions> options,
        ILogger<TickerPriceSavingWorker> logger)
        : base(buffer, repository, options, logger)
    {
    }

    public Task ExecuteForTestAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(cancellationToken);
}
