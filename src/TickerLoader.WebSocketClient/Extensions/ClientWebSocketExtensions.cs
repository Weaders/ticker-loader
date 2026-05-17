using System.Net.WebSockets;
using System.Text;

namespace TickerLoader.WebSocketClient.Extensions;

public static class ClientWebSocketExtensions
{
    public static async Task SendTextAsync(
        this ClientWebSocket socket,
        string payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(socket);

        var bytes = Encoding.UTF8.GetBytes(payload);

        await socket.SendAsync(
            bytes,
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);
    }
}
