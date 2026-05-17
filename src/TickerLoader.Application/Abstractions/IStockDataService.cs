namespace TickerLoader.Application.Abstractions
{
    internal interface IStockDataService
    {
        int GetStockExhangeId(string exchangeName);
        int GetTickerId(string ticker);
    }
}