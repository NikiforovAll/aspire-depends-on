namespace Nall.Aspire.Hosting.DependsOn.IntegrationTesting;

using System.Net;
using System.Net.Http.Json;

[Collection(nameof(AspireFixtureCollection))]
public class ApiExampleTests(AspireAppHostFixture fixture)
{
    private readonly DistributedApplication distributedApplication =
        fixture.DistributedApplicationInstance;

    [Fact]
    public async Task ProductsApi_GetProducts_ReturnsAtLeastOne()
    {
        using var httpClient = this.distributedApplication.CreateHttpClient("api");
        var response = await httpClient.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<List<Product>>();

        body.Should().NotBeNullOrEmpty();
        body!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenericApi_GetIndex_ReturnsHelloWorld()
    {
        using var httpClient = this.distributedApplication.CreateHttpClient("api-unhealthy-for-a-little-bit");
        var response = await httpClient.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Be("Hello World!");
    }

    private class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
    }
}
