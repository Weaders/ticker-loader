using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using TickerLoader.Storage.Abstractions;
using TickerLoader.Storage.Options;

namespace TickerLoader.Storage;

public sealed class NpgsqlTickerPriceDbCommandExecutor : ITickerPriceDbCommandExecutor
{
    private readonly string _connectionString;

    public NpgsqlTickerPriceDbCommandExecutor(IOptions<PostgresStorageOptions> options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Value.ConnectionString);
        _connectionString = options.Value.ConnectionString;
    }

    public async Task ExecuteAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }
}
