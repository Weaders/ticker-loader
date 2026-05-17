using TickerLoader.Application.Workers.Options;

namespace TickerLoader.Tests.Application.Workers.Options;

public sealed class TickerSavingWorkerOptionsTests
{
    [Fact]
    public void DefaultValues_AreExpected()
    {
        // Act
        var options = new TickerSavingWorkerOptions();

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(5), options.EmptyReadDelay);
        Assert.Equal(TimeSpan.FromMilliseconds(50), options.ErrorDelay);
        Assert.Equal(1, options.MaxDegreeOfParallelism);
        Assert.Equal(100, options.MaxBatchSize);
    }
}
