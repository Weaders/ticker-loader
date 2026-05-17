using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using TickerLoader.Application.Abstractions;
using TickerLoader.Application.Models;
using TickerLoader.WebSocketClient.Options;

namespace TickerLoader.WebSocketClient;

public abstract class BrokerWebSocketClientBase : IMarketDataClient, IAsyncDisposable
{
    private const int ReceiveBufferSize = 16 * 1024;

    private readonly BrokerWebSocketOptionsBase _options;
    private readonly Channel<RawTickerTick> _channel = Channel.CreateUnbounded<RawTickerTick>(
        new UnboundedChannelOptions()
        {
            SingleWriter = true,
            SingleReader = false,
        });

    private ClientWebSocket? _clientWebSocket;
    private CancellationTokenSource? _producerCts;
    private Task? _producerTask;
    private readonly ILogger _logger;

    protected BrokerWebSocketClientBase(BrokerWebSocketOptionsBase options, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _logger = logger;
    }

    public ChannelReader<RawTickerTick> Ticks => _channel.Reader;

    protected string StockExchangeId => _options.StockExchangeId;

    protected TimeSpan DelayOnError => _options.DelayOnError;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _producerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _producerTask = Task.Run(() => ProduceAsync(_producerCts.Token), _producerCts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_producerCts is not null)
        {
            await _producerCts.CancelAsync();
            _producerCts.Dispose();
            _producerCts = null;
        }

        if (_producerTask is not null)
        {
            try
            {
                await _producerTask;
            }
            catch (OperationCanceledException)
            {
            }

            _producerTask = null;
        }

        var socket = Interlocked.Exchange(ref _clientWebSocket, null);
        await DisposeSocketAsync(socket);
    }

    public ValueTask DisposeAsync() => new(StopAsync());

    protected virtual void OnConnectionError(Exception exception)
    {
    }

    protected abstract Uri BuildWebSocketUri();

    protected abstract void ConfigureClientWebSocket(ClientWebSocket socket);

    protected abstract Task SendSubscriptionAsync(ClientWebSocket socket, CancellationToken cancellationToken);

    protected abstract RawTickerTick? ParseMessage(string json);

    private async Task ProduceAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await ConnectAndSubscribeAsync(ct);

                    await foreach (var tick in ReceiveLoopAsync(ct))
                        await _channel.Writer.WriteAsync(tick, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    OnConnectionError(ex);
                    await Task.Delay(DelayOnError, ct);
                }
                finally
                {
                    var socket = Interlocked.Exchange(ref _clientWebSocket, null);
                    await DisposeSocketAsync(socket);
                }
            }
        }
        finally
        {
            _channel.Writer.Complete();
        }
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken cancellationToken)
    {
        var socket = new ClientWebSocket();

        ConfigureClientWebSocket(socket);

        var uri = BuildWebSocketUri();

        await socket.ConnectAsync(uri, cancellationToken);

        await SendSubscriptionAsync(socket, cancellationToken);

        _clientWebSocket = socket;

        _logger.LogInformation("Connected to {Uri}", uri);
    }

    private async IAsyncEnumerable<RawTickerTick> ReceiveLoopAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var socket = _clientWebSocket ?? throw new InvalidOperationException("WebSocket is not connected.");

        var buffer = new byte[ReceiveBufferSize];
        var messageBuffer = new ArraySegment<byte>(buffer);
        var builder = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            builder.Clear();
            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(messageBuffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                    yield break;

                builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            if (builder.Length == 0)
                continue;

            if (ParseMessage(builder.ToString()) is { } tick)
                yield return tick;
        }
    }

    private static async Task DisposeSocketAsync(ClientWebSocket? socket)
    {
        if (socket is null)
            return;

        try
        {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Client disconnect",
                    CancellationToken.None);
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            socket.Dispose();
        }
    }
}
