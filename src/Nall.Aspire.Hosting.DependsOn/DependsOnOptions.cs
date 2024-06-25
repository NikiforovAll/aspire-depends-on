namespace Aspire.Hosting;

using Polly.Retry;
using Polly.Timeout;

public class DependsOnOptions
{
    public RetryStrategyOptions Retry { get; set; }

    public TimeoutStrategyOptions Timeout { get; set; }
}
