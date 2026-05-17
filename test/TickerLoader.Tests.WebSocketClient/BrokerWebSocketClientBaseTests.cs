using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TickerLoader.Application.Models;
using TickerLoader.Tests.WebSocketClient.Helpers;

namespace TickerLoader.Tests.WebSocketClient;

public sealed class BrokerWebSocketClientBaseTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenOptionsIsNull()
    {
        // Arrange
        // Act
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TestBrokerWebSocketClient(null!, NullLogger.Instance));

        // Assert
        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenLoggerIsNull()
    {
        // Arrange
        // Act
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TestBrokerWebSocketClient(BrokerWebSocketOptionsFactory.Create(), null!));

        // Assert
        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void StockExchangeId_ShouldReturnConfiguredValue_WhenCalled()
    {
        // Arrange
        var client = CreateClient(BrokerWebSocketOptionsFactory.Create(stockExchangeId: "MOEX"));

        // Act
        var stockExchangeId = client.ExchangeId;

        // Assert
        Assert.Equal("MOEX", stockExchangeId);
    }

    [Fact]
    public void DelayOnError_ShouldReturnConfiguredValue_WhenCalled()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(25);
        var client = CreateClient(BrokerWebSocketOptionsFactory.Create(delayOnError: delay));

        // Act
        var delayOnError = client.ErrorDelay;

        // Assert
        Assert.Equal(delay, delayOnError);
    }

    [Fact]
    public void ParseMessage_ShouldReturnConfiguredTick_WhenNextResultSet()
    {
        // Arrange
        var client = CreateClient();
        var tick = RawTickerTickFactory.Create(price: 301.25m, volume: 7);
        client.SetNextParseResult(tick);

        // Act
        var receivedTick = client.ProcessMessage("""{"tick":true}""");

        // Assert
        Assert.NotNull(receivedTick);
        Assert.Equal(tick.FigiTicker, receivedTick.FigiTicker);
        Assert.Equal(tick.Price, receivedTick.Price);
        Assert.Equal(tick.Volume, receivedTick.Volume);
    }

    [Fact]
    public async Task StartAsync_ShouldExit_WhenCancellationRequested()
    {
        // Arrange
        var client = CreateClient();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        await client.StartAsync(cts.Token);

        // Assert
        Assert.False(client.ConfigureCalled);
    }

    [Fact]
    public async Task StartAsync_ShouldInvokeOnConnectionError_WhenConnectionFails()
    {
        // Arrange
        var client = CreateClient(BrokerWebSocketOptionsFactory.Create(delayOnError: TimeSpan.FromMilliseconds(50)));
        using var cts = new CancellationTokenSource();

        // Act
        await client.StartAsync(cts.Token);
        var consumeTask = ConsumeTicksAsync(client, cts.Token);
        await WaitUntilAsync(() => client.ConnectionErrors.Count > 0, TimeSpan.FromSeconds(5));
        await cts.CancelAsync();
        await client.StopAsync();

        // Assert
        Assert.NotEmpty(client.ConnectionErrors);

        await consumeTask;
    }

    [Fact]
    public void ParseMessage_ShouldStoreMessage_WhenCalled()
    {
        // Arrange
        var client = CreateClient();
        const string json = """{"trade":{"figi":"BBG004730N88"}}""";

        // Act
        client.ProcessMessage(json);

        // Assert
        Assert.Equal(json, Assert.Single(client.ReceivedMessages));
    }

    private static TestBrokerWebSocketClient CreateClient(
        TestBrokerWebSocketOptions? options = null,
        ILogger? logger = null) =>
        new(
            options ?? BrokerWebSocketOptionsFactory.Create(),
            logger ?? NullLogger.Instance);

    private static async Task ConsumeTicksAsync(TestBrokerWebSocketClient client, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var _ in client.Ticks.ReadAllAsync(cancellationToken))
            {
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
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
}
