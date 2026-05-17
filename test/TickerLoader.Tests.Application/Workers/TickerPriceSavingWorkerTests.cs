using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TickerLoader.Application.Abstractions;
using TickerLoader.Application.Models;
using TickerLoader.Application.Workers.Options;
using TickerLoader.Tests.Application.Helpers;

namespace TickerLoader.Tests.Application.Workers;

public sealed class TickerPriceSavingWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldCallReadBatch_WhenCalled()
    {
        // Arrange
        var bufferMock = CreateBufferMock();
        var options = CreateOptions();
        using var cts = new CancellationTokenSource();
        var worker = CreateWorker(bufferMock.Object, CreateRepositoryMock().Object, options);

        // Act
        _ = worker.ExecuteForTestAsync(cts.Token);
        await WaitUntilAsync(
            () => bufferMock.Invocations.Any(invocation => invocation.Method.Name == nameof(ITickerBuffer.ReadBatchAsync)),
            TimeSpan.FromSeconds(2));

        // Assert
        bufferMock.Verify(
            b => b.ReadBatchAsync(options.MaxBatchSize, It.IsAny<CancellationToken>()),
            Times.AtLeast(options.MaxDegreeOfParallelism));
    }

    [Fact]
    public async Task ExecuteAsync_WhenBufferHasTicks_ShouldSaveToRepository()
    {
        // Arrange
        var tick = TickerTickFactory.Create(price: 250.75m);
        var buffer = CreateBufferMock(firstBatchTicks: tick);
        var repository = CreateRepositoryMock();
        var saved = new TaskCompletionSource<IReadOnlyCollection<TickerTick>>(TaskCreationOptions.RunContinuationsAsynchronously);

        repository
            .Setup(r => r.SaveManyAsync(It.IsAny<IReadOnlyCollection<TickerTick>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<TickerTick>, CancellationToken>((batch, _) => saved.TrySetResult(batch))
            .Returns(ValueTask.CompletedTask);

        using var cts = new CancellationTokenSource();
        var worker = CreateWorker(buffer.Object, repository.Object);
        var executeTask = worker.ExecuteForTestAsync(cts.Token);

        // Act
        var batch = await saved.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        var savedTick = Assert.Single(batch);
        Assert.Equal(tick.TickKey, savedTick.TickKey);
        Assert.Equal(tick.Price, savedTick.Price);
        Assert.Equal(tick.Volume, savedTick.Volume);

        repository.Verify(
            r => r.SaveManyAsync(It.IsAny<IReadOnlyCollection<TickerTick>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSaveFails_ShouldReAddTicksToBuffer()
    {
        // Arrange
        var tick = TickerTickFactory.Create(tickerId: 2, price: 99.5m);
        var buffer = CreateBufferMock(firstBatchTicks: tick);
        var repository = CreateRepositoryMock();
        var saveFailed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        repository
            .Setup(r => r.SaveManyAsync(It.IsAny<IReadOnlyCollection<TickerTick>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("save failed"))
            .Callback(() => saveFailed.TrySetResult());

        using var cts = new CancellationTokenSource();
        var worker = CreateWorker(buffer.Object, repository.Object);
        var executeTask = worker.ExecuteForTestAsync(cts.Token);
        await saveFailed.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Act
        await Task.Delay(100);

        // Assert
        buffer.Verify(
            b => b.AddAsync(
                It.Is<TickerTick>(t =>
                    t.TickKey.TickerId == tick.TickKey.TickerId &&
                    t.Price == tick.Price &&
                    t.Volume == tick.Volume),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ShouldCancelReadBatch()
    {
        // Arrange
        var readStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var buffer = CreateBlockingBufferMock(readStarted, readCancelled);
        using var cts = new CancellationTokenSource();
        var worker = CreateWorker(buffer.Object, CreateRepositoryMock().Object);
        var executeTask = worker.ExecuteForTestAsync(cts.Token);
        await readStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Act
        await cts.CancelAsync();
        await AwaitExecuteAsync(executeTask);

        // Assert
        await readCancelled.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(readCancelled.Task.IsCompleted);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReadBatchWithConfiguredSize()
    {
        // Arrange
        const int batchSize = 42;
        var buffer = CreateBufferMock();
        var repository = CreateRepositoryMock();
        var options = CreateOptions(o => o.MaxBatchSize = batchSize);
        using var cts = new CancellationTokenSource();
        var worker = CreateWorker(buffer.Object, repository.Object, options);

        // Act
        var executeTask = worker.ExecuteForTestAsync(cts.Token);
        await WaitUntilAsync(
            () => buffer.Invocations.Any(i => i.Method.Name == nameof(ITickerBuffer.ReadBatchAsync)),
            TimeSpan.FromSeconds(2));

        // Assert
        buffer.Verify(
            b => b.ReadBatchAsync(batchSize, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    private static async Task AwaitExecuteAsync(Task executeTask)
    {
        try
        {
            await executeTask;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;

            await Task.Delay(10);
        }

        throw new TimeoutException("Condition was not met before timeout.");
    }

    private static TestableTickerPriceSavingWorker CreateWorker(
        ITickerBuffer buffer,
        ITicketPriceRepository repository,
        TickerSavingWorkerOptions? options = null) =>
        new(
            buffer,
            repository,
            Microsoft.Extensions.Options.Options.Create(options ?? CreateOptions()),
            NullLogger<TickerLoader.Application.Workers.TickerPriceSavingWorker>.Instance);

    private static TickerSavingWorkerOptions CreateOptions(
        Action<TickerSavingWorkerOptions>? configure = null)
    {
        var options = new TickerSavingWorkerOptions
        {
            EmptyReadDelay = TimeSpan.FromMilliseconds(1),
            ErrorDelay = TimeSpan.FromMilliseconds(1),
            MaxDegreeOfParallelism = 1,
            MaxBatchSize = 100
        };

        configure?.Invoke(options);
        return options;
    }

    private static Mock<ITickerBuffer> CreateBlockingBufferMock(
        TaskCompletionSource readStarted,
        TaskCompletionSource readCancelled)
    {
        var mock = new Mock<ITickerBuffer>();

        mock.Setup(b => b.ReadBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>(async (_, ct) =>
            {
                readStarted.TrySetResult();

                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                }
                catch (OperationCanceledException)
                {
                    readCancelled.TrySetResult();
                    throw;
                }

                return Array.Empty<TickerTick>();
            });

        return mock;
    }

    private static Mock<ITickerBuffer> CreateBufferMock(params TickerTick[] firstBatchTicks)
    {
        var mock = new Mock<ITickerBuffer>();
        var readCount = 0;

        mock.Setup(b => b.ReadBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((_, ct) =>
            {
                ct.ThrowIfCancellationRequested();

                if (firstBatchTicks.Length > 0 && Interlocked.Increment(ref readCount) == 1)
                    return Task.FromResult<IReadOnlyCollection<TickerTick>>(firstBatchTicks);

                return Task.FromResult<IReadOnlyCollection<TickerTick>>(Array.Empty<TickerTick>());
            });

        mock.Setup(b => b.AddAsync(It.IsAny<TickerTick>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        return mock;
    }

    private static Mock<ITicketPriceRepository> CreateRepositoryMock()
    {
        var mock = new Mock<ITicketPriceRepository>();

        mock.Setup(r => r.SaveManyAsync(It.IsAny<IReadOnlyCollection<TickerTick>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        return mock;
    }
}
