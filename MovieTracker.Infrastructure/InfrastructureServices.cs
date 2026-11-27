using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MovieTracker.Application;
using MovieTracker.Infrastructure.Persistence;

namespace MovieTracker.Infrastructure;

public static class InfrastructureServices
{
    public static IServiceCollection AddMovieTrackerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MovieTrackerDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("MovieTracker")));

        foreach (var type in typeof(InfrastructureServices).Assembly.GetExportedTypes().Where(type =>
                     type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters &&
                     type.Namespace == "MovieTracker.Infrastructure.Repositories" &&
                     type.Name.EndsWith("Repository", StringComparison.Ordinal)))
        {
            var contracts = type.GetInterfaces().Where(contract =>
                contract.Assembly == typeof(ApplicationServices).Assembly &&
                contract.Name.EndsWith("Repository", StringComparison.Ordinal));

            foreach (var contract in contracts)
                services.TryAddScoped(contract, type);
        }

        return services;
    }
}
