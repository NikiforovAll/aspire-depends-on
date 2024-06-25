namespace Nall.Aspire.Hosting.DependsOn.NeverEnds.IntegrationTesting;

using Microsoft.Extensions.DependencyInjection;

public class ApiExampleTests()
{
    [Fact]
    public async Task TryToStartApplicationThatNeverCompletes()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.Configure<DependsOnOptions>(options =>
        {
            options.Timeout.Timeout = TimeSpan.FromSeconds(3);
            options.Retry.MaxRetryAttempts = 10;
            options.Retry.MaxDelay = TimeSpan.FromSeconds(1);
            options.Retry.BackoffType = Polly.DelayBackoffType.Linear;
        });

        var app = await appHost.BuildAsync();

        await app.StartAsync();
    }
}
