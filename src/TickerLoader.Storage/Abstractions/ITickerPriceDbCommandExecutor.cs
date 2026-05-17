namespace TickerLoader.Storage.Abstractions;

public interface ITickerPriceDbCommandExecutor
{
    Task ExecuteAsync(string sql, object parameters, CancellationToken cancellationToken);
}
