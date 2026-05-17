using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TickerLoader.Application.Models;
using TickerLoader.TinkoffClient;
using TickerLoader.TinkoffClient.Models;
using TickerLoader.TinkoffClient.Models.Extensions;
using TickerLoader.TinkoffClient.Options;
using TickerLoader.Tests.TinkoffClient.Helpers;

namespace TickerLoader.Tests.TinkoffClient;

public sealed class TinkoffMarketDataWebSocketClientTests
{
    [Fact]
    public void BuildWebSocketUri_ShouldReturnConfiguredStreamUri_WhenCalled()
    {
        // Arrange
        var options = TinkoffWebSocketOptionsFactory.Create(
            baseUrl: "wss://example.test/ws",
            streamPath: "stream/market-data");
        var client = CreateClient(options);

        // Act
        var uri = client.GetWebSocketUri();

        // Assert
        Assert.Equal(new Uri("wss://example.test/ws/stream/market-data"), uri);
    }

    [Fact]
    public void ConfigureClientWebSocket_ShouldSetAuthorizationAndJsonSubProtocol_WhenCalled()
    {
        // Arrange
        var options = TinkoffWebSocketOptionsFactory.Create(accessToken: "secret-token");
        var client = CreateClient(options);
        using var socket = new ClientWebSocket();

        // Act
        client.ApplyClientWebSocketOptions(socket);

        // Assert
        Assert.Equal("Bearer secret-token", GetRequestHeader(socket, "Authorization"));
        Assert.Contains("json", GetRequestedSubProtocols(socket));
    }

    [Fact]
    public void BuildSubscribeTradesRequest_ShouldIncludeConfiguredInstruments_WhenCalled()
    {
        // Arrange
        var options = TinkoffWebSocketOptionsFactory.Create(
            instruments: ["BBG004730N88", "BBG004731489"]);
        var client = CreateClient(options);

        // Act
        var request = client.GetSubscribeTradesRequest();

        // Assert
        Assert.Equal(SubscriptionAction.Subscribe.ToApiValue(), request.SubscribeTradesRequest.SubscriptionAction);
        Assert.Equal(
            ["BBG004730N88", "BBG004731489"],
            request.SubscribeTradesRequest.Instruments.Select(i => i.InstrumentId).ToArray());
    }

    [Fact]
    public void ParseMessage_ShouldReturnTick_WhenTradeMessageReceived()
    {
        // Arrange
        var options = TinkoffWebSocketOptionsFactory.Create(stockExchangeId: "MOEX");
        var client = CreateClient(options);

        // Act
        var tick = client.ProcessMessage(TinkoffTradeMessageFactory.CreateTradeMessage());

        // Assert
        Assert.NotNull(tick);
    }

    [Fact]
    public void ParseMessage_ShouldReturnNull_WhenTradeIsMissing()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var tick = client.ProcessMessage(TinkoffTradeMessageFactory.CreateNonTradeMessage());

        // Assert
        Assert.Null(tick);
    }

    [Fact]
    public void ParseMessage_ShouldMapTradeFieldsToRawTickerTick_WhenCalled()
    {
        // Arrange
        var options = TinkoffWebSocketOptionsFactory.Create(stockExchangeId: "MOEX");
        var client = CreateClient(options);
        var expectedTime = DateTimeOffset.Parse("2026-05-16T12:00:00Z");

        // Act
        var receivedTick = client.ProcessMessage(TinkoffTradeMessageFactory.CreateTradeMessage(
            figi: "BBG004730N88",
            units: "250",
            nano: 750_000_000,
            quantity: 7,
            time: "2026-05-16T12:00:00Z"));

        // Assert
        Assert.NotNull(receivedTick);
        Assert.Equal("MOEX", receivedTick.StockExchangeId);
        Assert.Equal("BBG004730N88", receivedTick.FigiTicker);
        Assert.Equal(250.75m, receivedTick.Price);
        Assert.Equal(7L, receivedTick.Volume);
        Assert.Equal(expectedTime, receivedTick.Timestamp);
    }

    private static string? GetRequestHeader(ClientWebSocket socket, string headerName)
    {
        var optionsType = typeof(ClientWebSocketOptions);
        var headers = optionsType
                .GetField("requestHeaders", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(socket.Options)
            ?? optionsType
                .GetField("_requestHeaders", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(socket.Options);

        return headers is WebHeaderCollection webHeaders
            ? webHeaders[headerName]
            : null;
    }

    private static IReadOnlyCollection<string> GetRequestedSubProtocols(ClientWebSocket socket)
    {
        var optionsType = typeof(ClientWebSocketOptions);
        var subProtocols = optionsType
                .GetField("requestedSubProtocols", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(socket.Options)
            ?? optionsType
                .GetField("_requestedSubProtocols", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(socket.Options);

        return subProtocols as IReadOnlyCollection<string> ?? [];
    }

    private static TestableTinkoffMarketDataWebSocketClient CreateClient(
        TinkoffWebSocketOptions? options = null)
    {
        options ??= TinkoffWebSocketOptionsFactory.Create();

        return new TestableTinkoffMarketDataWebSocketClient(
            Options.Create(options),
            NullLogger<TinkoffMarketDataWebSocketClient>.Instance);
    }
}
