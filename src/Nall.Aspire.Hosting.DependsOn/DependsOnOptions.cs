namespace Aspire.Hosting;

using Polly.Retry;
using Polly.Timeout;

public class DependsOnOptions
{
    public RetryStrategyOptions Retry { get; set; } = new();

    public TimeoutStrategyOptions Timeout { get; set; } = new();
}
