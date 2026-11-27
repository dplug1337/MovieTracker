using FluentValidation;
using FluentValidation.Results;

namespace MovieTracker.API.Startup;

internal sealed class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var cancellationToken = context.HttpContext.RequestAborted;
        var failures = new List<ValidationFailure>();

        foreach (var argument in context.Arguments)
        {
            if (argument is null) continue;
            var contract = typeof(IValidator<>).MakeGenericType(argument.GetType());
            foreach (var service in services.GetServices(contract))
            {
                var validator = (IValidator)service!;
                var result = await validator.ValidateAsync(new ValidationContext<object>(argument), cancellationToken);
                failures.AddRange(result.Errors);
            }
        }

        return failures.Count == 0
            ? await next(context)
            : Results.ValidationProblem(new ValidationResult(failures).ToDictionary());
    }
}
