namespace Aspire.Hosting;

using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nall.Aspire.Hosting.DependsOn;
using Polly;

internal class WaitForDependenciesRunningHook(
    DistributedApplicationExecutionContext executionContext,
    ResourceNotificationService resourceNotificationService,
    IOptions<DependsOnOptions> options,
    ILogger<WaitForDependenciesRunningHook> logger
) : IDistributedApplicationLifecycleHook, IAsyncDisposable
{
    private readonly CancellationTokenSource cts = new();

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        // We don't need to execute any of this logic in publish mode
        if (executionContext.IsPublishMode)
        {
            return Task.CompletedTask;
        }

        // The global list of resources being waited on
        var waitingResources =
            new ConcurrentDictionary<IResource, ConcurrentDictionary<WaitOnAnnotation, TaskCompletionSource>>();

        // For each resource, add an environment callback that waits for dependencies to be running
        foreach (var r in appModel.Resources)
        {
            var resourcesToWaitOn = r.Annotations.OfType<WaitOnAnnotation>().ToLookup(a => a.Resource);

            if (resourcesToWaitOn.Count == 0)
            {
                continue;
            }

            // Abuse the environment callback to wait for dependencies to be running
            r.Annotations.Add(
                new EnvironmentCallbackAnnotation(async context =>
                    await BuildDependenciesGraphAsync(
                        resourceNotificationService,
                        context,
                        waitingResources,
                        r,
                        resourcesToWaitOn
                    )
                )
            );
        }

        _ = Task.Run(this.ExecuteStateMachineAsync(resourceNotificationService, waitingResources), cancellationToken);

        return Task.CompletedTask;
    }

    private async Task BuildDependenciesGraphAsync(
        ResourceNotificationService resourceNotificationService,
        EnvironmentCallbackContext context,
        ConcurrentDictionary<IResource, ConcurrentDictionary<WaitOnAnnotation, TaskCompletionSource>> waitingResources,
        IResource r,
        ILookup<IResource, WaitOnAnnotation> resourcesToWaitOn
    )
    {
        var dependencies = new List<Task>();

        // Find connection strings and endpoint references and get the resource they point to
        foreach (var group in resourcesToWaitOn)
        {
            var resource = group.Key;

            // REVIEW: This logic does not handle cycles in the dependency graph (that would result in a deadlock)

            // Don't wait for yourself
            if (resource != r && resource is not null)
            {
                var pendingAnnotations = waitingResources.GetOrAdd(resource, _ => new());

                foreach (var waitOn in group)
                {
                    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                    async Task Wait()
                    {
                        context.Logger?.LogInformation("Waiting for {Resource}.", waitOn.Resource.Name);

                        await tcs.Task;

                        context.Logger?.LogInformation("Waiting for {Resource} completed.", waitOn.Resource.Name);
                    }

                    pendingAnnotations[waitOn] = tcs;

                    dependencies.Add(Wait());
                }
            }
        }

        await resourceNotificationService.PublishUpdateAsync(
            r,
            s => s with { State = new("Waiting", KnownResourceStateStyles.Info) }
        );

        await Task.WhenAll(dependencies).WaitAsync(context.CancellationToken);
    }

    private Func<Task?> ExecuteStateMachineAsync(
        ResourceNotificationService resourceNotificationService,
        ConcurrentDictionary<IResource, ConcurrentDictionary<WaitOnAnnotation, TaskCompletionSource>> waitingResources
    ) =>
        async () =>
        {
            var stoppingToken = this.cts.Token;

            // These states are terminal but we need a better way to detect that
            static bool IsKnownTerminalState(CustomResourceSnapshot snapshot) =>
                snapshot.State == "FailedToStart" || snapshot.State == "Exited" || snapshot.ExitCode is not null;

            // Watch for global resource state changes
            await foreach (var resourceEvent in resourceNotificationService.WatchAsync(stoppingToken))
            {
                if (waitingResources.TryGetValue(resourceEvent.Resource, out var pendingAnnotations))
                {
                    foreach (var (waitOn, tcs) in pendingAnnotations)
                    {
                        if (
                            waitOn.States is string[] states
                            && states.Contains(resourceEvent.Snapshot.State?.Text, StringComparer.Ordinal)
                        )
                        {
                            pendingAnnotations.TryRemove(waitOn, out _);

                            _ = this.DoTheHealthCheck(resourceEvent, tcs);
                        }
                        else if (waitOn.WaitUntilCompleted)
                        {
                            if (IsKnownTerminalState(resourceEvent.Snapshot))
                            {
                                pendingAnnotations.TryRemove(waitOn, out _);

                                _ = this.DoTheHealthCheck(resourceEvent, tcs);
                            }
                        }
                        else if (waitOn.States is null)
                        {
                            if (resourceEvent.Snapshot.State == "Running")
                            {
                                pendingAnnotations.TryRemove(waitOn, out _);

                                _ = this.DoTheHealthCheck(resourceEvent, tcs);
                            }
                            else if (IsKnownTerminalState(resourceEvent.Snapshot))
                            {
                                pendingAnnotations.TryRemove(waitOn, out _);

                                tcs.TrySetException(
                                    new AspireHostException($"Dependency {waitOn.Resource.Name} failed to start")
                                );
                            }
                        }
                    }
                }
            }
        };

    private async Task DoTheHealthCheck(ResourceEvent resourceEvent, TaskCompletionSource tcs)
    {
        var resource = resourceEvent.Resource;

        // REVIEW: Right now, every resource does an independent health check, we could instead cache
        // the health check result and reuse it for all resources that depend on the same resource

        HealthCheckAnnotation? healthCheckAnnotation;

        // Find the relevant health check annotation. If the resource has a parent, walk up the tree
        // until we find the health check annotation.
        while (true)
        {
            // If we find a health check annotation, break out of the loop
            if (resource.TryGetLastAnnotation(out healthCheckAnnotation))
            {
                break;
            }

            // If the resource has a parent, walk up the tree
            if (resource is IResourceWithParent parent)
            {
                resource = parent.Parent;
            }
            else
            {
                break;
            }
        }

        var operation = await ConstructHealthCheck(logger, tcs, resource, healthCheckAnnotation);

        try
        {
            if (operation is not null)
            {
                var pipeline = this.CreateResiliencyPipeline();

                await pipeline.ExecuteAsync(operation);
            }

            tcs.TrySetResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to wait for the resource - {Name}", resource.Name);
            tcs.TrySetException(ex);
        }
    }

    private static async Task<Func<CancellationToken, ValueTask>?> ConstructHealthCheck(
        ILogger<WaitForDependenciesRunningHook> logger,
        TaskCompletionSource tcs,
        IResource resource,
        HealthCheckAnnotation? healthCheckAnnotation
    )
    {
        Func<CancellationToken, ValueTask>? operation = null;
        if (healthCheckAnnotation?.HealthCheckFactory is { } factory)
        {
            IHealthCheck? check;

            try
            {
                check = await factory(resource, default);

                if (check is not null)
                {
                    var context = new HealthCheckContext()
                    {
                        Registration = new HealthCheckRegistration("", check, HealthStatus.Unhealthy, [])
                    };

                    operation = async (cancellationToken) =>
                    {
                        var result = await check.CheckHealthAsync(context, cancellationToken);

                        if (result.Exception is not null)
                        {
                            ExceptionDispatchInfo.Throw(result.Exception);
                        }

                        if (result.Status != HealthStatus.Healthy)
                        {
                            throw new AspireHostException("Health check failed");
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
                logger.LogError(ex, "Failed to construct a health check - {Name}", resource.Name);

                return operation;
            }
        }

        return operation;
    }

    private ResiliencePipeline CreateResiliencyPipeline() =>
        new ResiliencePipelineBuilder().AddRetry(options.Value.Retry).AddTimeout(options.Value.Timeout).Build();

    public ValueTask DisposeAsync()
    {
        this.cts.Cancel();

        return default;
    }
}
