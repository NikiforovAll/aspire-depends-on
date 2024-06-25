namespace Aspire.Hosting;

using Aspire.Hosting.ApplicationModel;

internal sealed class WaitOnAnnotation(IResource resource) : IResourceAnnotation
{
    public IResource Resource { get; } = resource;

    public string[]? States { get; set; }

    public bool WaitUntilCompleted { get; set; }
}
