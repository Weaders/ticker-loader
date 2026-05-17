using TickerLoader.Application;
using TickerLoader.Tests.Application.Helpers;

namespace TickerLoader.Tests.Application;

public sealed class TickerBufferTests
{
    [Fact]
    public async Task ReadBatchAsync_ShouldReturnAddedData_WhenCalled()
    {
        // Arrange
        var buffer = new TickerBuffer();
        var tick = TickerTickFactory.Create(price: 250.75m);

        await buffer.AddAsync(tick, CancellationToken.None);

        // Act
        var batch = await buffer.ReadBatchAsync(batchSize: 10, CancellationToken.None);

        // Assert
        var saved = Assert.Single(batch);
        Assert.Equal(tick.TickKey, saved.TickKey);
        Assert.Equal(tick.Price, saved.Price);
        Assert.Equal(tick.Volume, saved.Volume);
    }

    [Fact]
    public async Task ReadBatchAsync_ShouldRespectsBatchSize_WhenCalled()
    {
        // Arrange
        var buffer = new TickerBuffer();

        await buffer.AddAsync(TickerTickFactory.Create(tickerId: 1), CancellationToken.None);
        await buffer.AddAsync(TickerTickFactory.Create(tickerId: 2), CancellationToken.None);
        await buffer.AddAsync(TickerTickFactory.Create(tickerId: 3), CancellationToken.None);

        // Act
        var batch = await buffer.ReadBatchAsync(batchSize: 2, CancellationToken.None);

        // Assert
        Assert.Equal(2, batch.Count);
    }

    [Fact]
    public async Task ReadBatchAsync_ShouldThrowsException_WhenCalledWithCanceledToken()
    {
        // Arrange
        var buffer = new TickerBuffer();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            buffer.ReadBatchAsync(batchSize: 10, cts.Token));
    }
}
