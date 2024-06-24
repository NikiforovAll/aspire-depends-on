namespace Aspire.Hosting;

using Aspire.Hosting.ApplicationModel;
using HealthChecks.NpgSql;

/// <summary>
/// Provides extension methods for adding health checks to PostgreSQL resources.
/// </summary>
public static class PostgreSqlHealthCheckExtensions
{
    /// <summary>
    /// Adds a health check to the PostgreSQL server resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder with the health check added.</returns>
    public static IResourceBuilder<PostgresServerResource> WithHealthCheck(
        this IResourceBuilder<PostgresServerResource> builder
    ) =>
        builder.WithAnnotation(
            HealthCheckAnnotation.Create(connectionString => new NpgSqlHealthCheck(
                new NpgSqlHealthCheckOptions(connectionString)
            ))
        );

    /// <summary>
    /// Adds a health check to the PostgreSQL database resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder with the health check added.</returns>
    public static IResourceBuilder<PostgresDatabaseResource> WithHealthCheck(
        this IResourceBuilder<PostgresDatabaseResource> builder
    ) =>
        builder.WithAnnotation(
            HealthCheckAnnotation.Create(connectionString => new NpgSqlHealthCheck(
                new NpgSqlHealthCheckOptions(connectionString)
            ))
        );
}
