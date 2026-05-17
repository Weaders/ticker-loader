using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using TickerLoader.Application.Models;
using TickerLoader.WebSocketClient;
using TickerLoader.WebSocketClient.Options;

namespace TickerLoader.Tests.WebSocketClient;

internal sealed class TestBrokerWebSocketClient : BrokerWebSocketClientBase
{
    private readonly Func<Uri> _buildUri;
    private RawTickerTick? _nextParseResult;

    public TestBrokerWebSocketClient(
        BrokerWebSocketOptionsBase options,
        ILogger logger,
        Func<Uri>? buildUri = null)
        : base(options, logger)
    {
        _buildUri = buildUri ?? (() => new Uri("ws://127.0.0.1:9/"));
    }

    public bool ConfigureCalled { get; private set; }

    public bool SubscriptionSent { get; private set; }

    public List<string> ReceivedMessages { get; } = [];

    public List<Exception> ConnectionErrors { get; } = [];

    public string ExchangeId => StockExchangeId;

    public TimeSpan ErrorDelay => DelayOnError;

    public void SetNextParseResult(RawTickerTick tick) => _nextParseResult = tick;

    public RawTickerTick? ProcessMessage(string json) => ParseMessage(json);

    protected override Uri BuildWebSocketUri() => _buildUri();

    protected override void ConfigureClientWebSocket(ClientWebSocket socket) =>
        ConfigureCalled = true;

    protected override Task SendSubscriptionAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        SubscriptionSent = true;
        return Task.CompletedTask;
    }

    protected override RawTickerTick? ParseMessage(string json)
    {
        ReceivedMessages.Add(json);
        var tick = _nextParseResult;
        _nextParseResult = null;
        return tick;
    }

    protected override void OnConnectionError(Exception exception) => ConnectionErrors.Add(exception);
}
