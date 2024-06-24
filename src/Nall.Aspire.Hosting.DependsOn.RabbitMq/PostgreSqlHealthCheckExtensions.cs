namespace Aspire.Hosting;

using Aspire.Hosting.ApplicationModel;
using HealthChecks.RabbitMQ;

/// <summary>
/// Provides extension methods for adding health checks to RabbitMQ resources.
/// </summary>
public static class RabbitMQHealthCheckExtensions
{
    /// <summary>
    /// Adds a health check to the RabbitMQ server resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder with the health check added.</returns>
    public static IResourceBuilder<RabbitMQServerResource> WithHealthCheck(
        this IResourceBuilder<RabbitMQServerResource> builder
    ) =>
        builder.WithAnnotation(
            HealthCheckAnnotation.Create(connectionString => new RabbitMQHealthCheck(
                new RabbitMQHealthCheckOptions() { ConnectionUri = new Uri(connectionString) }
            ))
        );
}
