namespace Aspire.Hosting;

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Polly;

public static class WaitForDependenciesExtensions
{
    /// <summary>
    /// Wait for a resource to be running before starting another resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="other">The resource to wait for.</param>
    public static IResourceBuilder<T> WaitFor<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource> other)
        where T : IResource
    {
        builder.ApplicationBuilder.AddWaitForDependencies();
        return builder.WithAnnotation(new WaitOnAnnotation(other.Resource));
    }

    /// <summary>
    /// Wait for a resource to run to completion before starting another resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="other">The resource to wait for.</param>
    public static IResourceBuilder<T> WaitForCompletion<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<IResource> other
    )
        where T : IResource
    {
        builder.ApplicationBuilder.AddWaitForDependencies();
        return builder.WithAnnotation(new WaitOnAnnotation(other.Resource) { WaitUntilCompleted = true });
    }

    /// <summary>
    /// Adds a lifecycle hook that waits for all dependencies to be "running" before starting resources. If that resource
    /// has a health check, it will be executed before the resource is considered "running".
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    private static IDistributedApplicationBuilder AddWaitForDependencies(this IDistributedApplicationBuilder builder)
    {
        builder.Services.TryAddLifecycleHook<WaitForDependenciesRunningHook>();

        // configures default options
        builder
            .Services.AddOptions<DependsOnOptions>()
            .Configure(options =>
            {
                options.Retry ??= new()
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    MaxDelay = TimeSpan.FromSeconds(30),
                    BackoffType = DelayBackoffType.Exponential,
                    MaxRetryAttempts = 10,
                };

                options.Timeout ??= new() { Timeout = TimeSpan.FromMinutes(1) };
            });

        return builder;
    }
}
