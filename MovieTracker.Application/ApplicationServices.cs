using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MovieTracker.Application;

public static class ApplicationServices
{
    public static IServiceCollection AddMovieTrackerApplication(this IServiceCollection services)
    {
        foreach (var type in typeof(ApplicationServices).Assembly.GetExportedTypes()
                     .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters))
        {
            if (type.Name.EndsWith("Handler", StringComparison.Ordinal))
                services.TryAddScoped(type);

            foreach (var contract in type.GetInterfaces().Where(IsValidator))
                services.TryAddEnumerable(ServiceDescriptor.Scoped(contract, type));
        }

        return services;
    }

    private static bool IsValidator(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IValidator<>);
}
