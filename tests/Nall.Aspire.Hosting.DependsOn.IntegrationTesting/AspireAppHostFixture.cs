namespace Nall.Aspire.Hosting.DependsOn.IntegrationTesting;

using Projects;

public class AspireAppHostFixture : IAsyncLifetime
{
    public DistributedApplication DistributedApplicationInstance { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<AppHost>();
        this.DistributedApplicationInstance = await appHost.BuildAsync();

        await this.DistributedApplicationInstance.StartAsync();
    }

    public Task DisposeAsync() => this.DistributedApplicationInstance.DisposeAsync().AsTask();
}
