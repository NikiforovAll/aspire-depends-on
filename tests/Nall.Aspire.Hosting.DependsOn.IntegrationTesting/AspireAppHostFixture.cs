namespace Nall.Aspire.Hosting.DependsOn.IntegrationTesting;

public class AspireAppHostFixture : IAsyncLifetime
{
    public DistributedApplication DistributedApplicationInstance { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        this.DistributedApplicationInstance = await appHost.BuildAsync();

        await this.DistributedApplicationInstance.StartAsync();
    }

    public Task DisposeAsync() => this.DistributedApplicationInstance.DisposeAsync().AsTask();
}

public class AspireAppHostFixture2 : IAsyncLifetime
{
    public DistributedApplication DistributedApplicationInstance { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<global::Projects.AppHost>();
        this.DistributedApplicationInstance = await appHost.BuildAsync();

        await this.DistributedApplicationInstance.StartAsync();
    }

    public Task DisposeAsync() => this.DistributedApplicationInstance.DisposeAsync().AsTask();
}
