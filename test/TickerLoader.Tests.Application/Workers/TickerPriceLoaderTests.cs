using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TickerLoader.Application.Abstractions;
using TickerLoader.Application.Models;
using TickerLoader.Application.Workers;
using TickerLoader.Tests.Application.Helpers;

namespace TickerLoader.Tests.Application.Workers;

public sealed class TickerPriceLoaderTests
{
    [Fact]
    public async Task StartAsync_ShouldStartAllClients_WhenCalled()
    {
        // Arrange
        var client1 = CreateMarketDataClientMock();
        var client2 = CreateMarketDataClientMock();
        var loader = CreateLoader(client1.Object, client2.Object);

        // Act
        await loader.StartAsync(CancellationToken.None);
        await Task.Delay(50);

        // Assert
        client1.Verify(c => c.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
        client2.Verify(c => c.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldStopAllClients_WhenCalled()
    {
        // Arrange
        var client = CreateMarketDataClientMock();
        var loader = CreateLoader(client.Object);
        await loader.StartAsync(CancellationToken.None);
        await Task.Delay(50);

        // Act
        await loader.StopAsync(CancellationToken.None);

        // Assert
        client.Verify(c => c.StopAsync(), Times.Once);
    }

    [Fact]
    public async Task ConsumeTicks_ShouldAddMappedTickToBuffer_WhenTickReceived()
    {
        // Arrange
        var client = CreateMarketDataClientMock(out var pushTick);
        var buffer = new Mock<ITickerBuffer>();
        var stockData = CreateStockDataServiceMock();
        var loader = new TickerPriceLoader(
            [client.Object],
            buffer.Object,
            stockData.Object,
            NullLogger<TickerPriceLoader>.Instance);

        await loader.StartAsync(CancellationToken.None);

        // Act
        pushTick(TickerTickFactory.CreateRaw(price: 301.25m, volume: 7));
        await Task.Delay(100);

        // Assert
        buffer.Verify(
            b => b.AddAsync(
                It.Is<TickerTick>(t =>
                    t.TickKey.StockExchangeId == 1 &&
                    t.TickKey.TickerId == 1 &&
                    t.Price == 301.25m &&
                    t.Volume == 7),
                It.IsAny<CancellationToken>()),
            Times.Once);

        await loader.StopAsync(CancellationToken.None);
    }

    private static TickerPriceLoader CreateLoader(params IMarketDataClient[] clients)
    {
        var buffer = new Mock<ITickerBuffer>();
        buffer.Setup(b => b.AddAsync(It.IsAny<TickerTick>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        return new TickerPriceLoader(
            clients,
            buffer.Object,
            CreateStockDataServiceMock().Object,
            NullLogger<TickerPriceLoader>.Instance);
    }

    private static Mock<IMarketDataClient> CreateMarketDataClientMock() =>
        CreateMarketDataClientMock(out _);

    private static Mock<IMarketDataClient> CreateMarketDataClientMock(
        out Action<RawTickerTick> pushTick)
    {
        var channel = Channel.CreateUnbounded<RawTickerTick>();
        var mock = new Mock<IMarketDataClient>();

        mock.Setup(m => m.Ticks).Returns(channel.Reader);
        mock.Setup(m => m.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mock.Setup(m => m.StopAsync()).Returns(Task.CompletedTask);

        pushTick = tick => channel.Writer.TryWrite(tick);

        return mock;
    }

    private static Mock<IStockDataService> CreateStockDataServiceMock()
    {
        var mock = new Mock<IStockDataService>();

        mock.Setup(s => s.GetStockExhangeId("MOEX")).Returns(1);
        mock.Setup(s => s.GetTickerId("BBG004730N88")).Returns(1);

        return mock;
    }
}
