namespace TickerLoader.Application.Workers.Options;

public sealed class TickerSavingWorkerOptions
{
    public TimeSpan EmptyReadDelay { get; set; } = TimeSpan.FromMilliseconds(5);
    public TimeSpan ErrorDelay { get; set; } = TimeSpan.FromMilliseconds(50);
    public TimeSpan ReportLogginEvery { get; set; } = TimeSpan.FromSeconds(1);

    public int MaxDegreeOfParallelism { get; set; } = 1;
    public int MaxBatchSize { get; set; } = 100;
}
