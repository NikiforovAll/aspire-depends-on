namespace Nall.Aspire.Hosting.DependsOn;

public class AspireHostException : Exception
{
    public AspireHostException() { }

    public AspireHostException(string message)
        : base(message) { }

    public AspireHostException(string message, Exception inner)
        : base(message, inner) { }
}
