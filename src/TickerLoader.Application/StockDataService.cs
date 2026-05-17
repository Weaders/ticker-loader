using TickerLoader.Application.Abstractions;

namespace TickerLoader.Application
{
    /// <summary>
    /// В проде данный сервис должен возвращать из памяти ид.
    /// И если не находит то ходить в redis/psql
    /// Но поскольку тут сугубо тестовое задание, я захордкодил значения
    /// </summary>
    internal class StockDataService : IStockDataService
    {
        public int GetStockExhangeId(string exchangeName)
        {
            if (exchangeName == "MOEX")
            {
                return 1;
            }

            throw new ArgumentException(nameof(exchangeName));
        }

        public int GetTickerId(string ticker)
        {
            if (ticker == "BBG004730N88")
            {
                return 1;
            }

            throw new ArgumentException(nameof(ticker));
        }
    }
}
