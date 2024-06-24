namespace TodoApi;

using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;
using MiniValidation;

public static class ValidationFilterExtensions
{
    public static TBuilder WithParameterValidation<TBuilder>(
        this TBuilder builder,
        params Type[] typesToValidate
    )
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder =>
        {
            var methodInfo = endpointBuilder.Metadata.OfType<MethodInfo>().FirstOrDefault();

            if (methodInfo is null)
            {
                return;
            }

            // Track the indices of validatable parameters
            List<int>? parameterIndexesToValidate = null;
            foreach (
                var p in methodInfo
                    .GetParameters()
                    .Where(p => typesToValidate.Contains(p.ParameterType))
            )
            {
                parameterIndexesToValidate ??= [];
                parameterIndexesToValidate.Add(p.Position);
            }

            if (parameterIndexesToValidate is null)
            {
                // Nothing to validate so don't add the filter to this endpoint
                return;
            }

            // We can respond with problem details if there's a validation error
            endpointBuilder.Metadata.Add(
                new ProducesResponseTypeMetadata(
                    typeof(HttpValidationProblemDetails),
                    400,
                    "application/problem+json"
                )
            );

            AddValidation(endpointBuilder, parameterIndexesToValidate);
        });

        return builder;
    }

    private static void AddValidation(
        EndpointBuilder endpointBuilder,
        List<int>? parameterIndexesToValidate
    ) =>
        endpointBuilder.FilterFactories.Add(
            (context, next) =>
                ctx =>
                {
                    foreach (var index in parameterIndexesToValidate!)
                    {
                        if (
                            ctx.Arguments[index] is { } arg
                            && !MiniValidator.TryValidate(arg, out var errors)
                        )
                        {
                            return new ValueTask<object?>(Results.ValidationProblem(errors));
                        }
                    }

                    return next(ctx);
                }
        );

    // Equivalent to the .Produces call to add metadata to endpoints
    private sealed class ProducesResponseTypeMetadata(Type type, int statusCode, string contentType)
        : IProducesResponseTypeMetadata
    {
        public Type Type { get; } = type;
        public int StatusCode { get; } = statusCode;
        public IEnumerable<string> ContentTypes { get; } = [contentType];
    }
}
