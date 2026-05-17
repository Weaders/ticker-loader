using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TickerLoader.Application.Abstractions;
using TickerLoader.Application.Extensions;
using TickerLoader.Application.Workers.Options;
using TickerLoader.Storage;
using TickerLoader.Storage.Abstractions;
using TickerLoader.Storage.Options;
using TickerLoader.TinkoffClient;
using TickerLoader.TinkoffClient.Options;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        IConfiguration configuration = context.Configuration;

        services.Configure<TinkoffWebSocketOptions>(configuration.GetSection("Tinkoff"));
        services.Configure<PostgresStorageOptions>(configuration.GetSection("Postgres"));
        services.Configure<TickerSavingWorkerOptions>(configuration.GetSection("TickerSaving"));
        
        services.AddOptions<PostgresStorageOptions>();
        services.AddOptions<TinkoffWebSocketOptions>();
        services.AddOptions<TickerSavingWorkerOptions>();

        services.AddSingleton<TinkoffMarketDataWebSocketClient>();

        services.AddSingleton<IMarketDataClient[]>(sp => 
        {
            return [sp.GetRequiredService<TinkoffMarketDataWebSocketClient>()];
        });

        services.AddSingleton<ITickerPriceDbCommandExecutor, NpgsqlTickerPriceDbCommandExecutor>();
        services.AddSingleton<ITicketPriceRepository, PostgresTicketPriceRepository>();
        services.AddApplication();
    })
    .Build();

await host.RunAsync();
