using System.Threading.Channels;
using TickerLoader.Application.Models;

namespace TickerLoader.Application.Abstractions;

public interface IMarketDataClient
{
    ChannelReader<RawTickerTick> Ticks { get; }

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync();
}
