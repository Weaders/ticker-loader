using Moq;
using TickerLoader.Application.Models;
using TickerLoader.Storage;
using TickerLoader.Storage.Abstractions;
using TickerLoader.Storage.Schema;
using TickerLoader.Tests.Storage.Helpers;

namespace TickerLoader.Tests.Storage;

public sealed class PostgresTicketPriceRepositoryTests
{
    [Fact]
    public async Task SaveManyAsync_ShouldNotExecuteCommand_WhenCollectionIsEmpty()
    {
        // Arrange
        var executor = new Mock<ITickerPriceDbCommandExecutor>();
        var repository = new PostgresTicketPriceRepository(executor.Object);

        // Act
        await repository.SaveManyAsync([], CancellationToken.None);

        // Assert
        executor.Verify(
            e => e.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveManyAsync_ShouldExecuteInsertCommand_WhenCalled()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 5, 16, 12, 0, 0, TimeSpan.Zero);
        var tick = TickerTickFactory.Create(
            stockExchangeId: 1,
            tickerId: 42,
            price: 250.75m,
            volume: 7,
            timestamp: timestamp);
        var executor = new Mock<ITickerPriceDbCommandExecutor>();
        var repository = new PostgresTicketPriceRepository(executor.Object);

        // Act
        await repository.SaveManyAsync([tick], CancellationToken.None);

        // Assert
        executor.Verify(
            e => e.ExecuteAsync(
                TickerPriceSql.Insert,
                It.Is<object>(parameters => TickInsertParametersMatcher.Matches(parameters, tick)),
                CancellationToken.None),
            Times.Once);
    }
}
