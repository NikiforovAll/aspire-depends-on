namespace Aspire.Hosting;

using Aspire.Hosting.ApplicationModel;
using HealthChecks.SqlServer;

/// <summary>
/// Provides extension methods for adding health checks to SqlServer resources.
/// </summary>
public static class SqlServerHealthCheckExtensions
{
    /// <summary>
    /// Adds a health check to the SqlServer server resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder with the health check added.</returns>
    public static IResourceBuilder<SqlServerServerResource> WithHealthCheck(
        this IResourceBuilder<SqlServerServerResource> builder
    ) =>
        builder.WithAnnotation(
            HealthCheckAnnotation.Create(connectionString => new SqlServerHealthCheck(
                new SqlServerHealthCheckOptions() { ConnectionString = connectionString }
            ))
        );

    /// <summary>
    /// Adds a health check to the SqlServer database resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder with the health check added.</returns>
    public static IResourceBuilder<SqlServerDatabaseResource> WithHealthCheck(
        this IResourceBuilder<SqlServerDatabaseResource> builder
    ) =>
        builder.WithAnnotation(
            HealthCheckAnnotation.Create(connectionString => new SqlServerHealthCheck(
                new SqlServerHealthCheckOptions() { ConnectionString = connectionString }
            ))
        );
}
