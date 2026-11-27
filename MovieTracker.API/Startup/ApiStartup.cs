using Microsoft.EntityFrameworkCore;
using MovieTracker.API.Security;
using MovieTracker.Application.Movies;
using MovieTracker.Application.Movies.GetMovieList;
using MovieTracker.Application.Movies.Ratings;
using MovieTracker.Application.Movies.Reviews;
using MovieTracker.Application.Users;
using MovieTracker.Application.Users.Statistics;
using MovieTracker.Infrastructure.Persistence;

namespace MovieTracker.API.Startup;

internal static class ApiStartup
{
    private const string CorsPolicy = "MovieTrackerWeb";

    public static IServiceCollection AddMovieTrackerApi(this IServiceCollection services, IConfiguration config)
    {
        JwtAccessTokens.AddMovieTrackerSecurity(services, config);
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

        var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

        return services;
    }

    public static void UseMovieTrackerMiddleware(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseHttpsRedirection();
        app.UseCors(CorsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();
    }

    public static void MapMovieTrackerEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("").AddEndpointFilter<ValidationFilter>();
        endpoints.MapGetMovieListEndpoint();
        endpoints.MapMovieCatalog();
        endpoints.MapAccounts();
        endpoints.MapWatchlist();
        endpoints.MapRatings();
        endpoints.MapReviews();
        endpoints.MapStatistics();
    }

    public static async Task InitializeMovieTrackerDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:MigrateOnStartup")) return;
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MovieTrackerDbContext>().Database.MigrateAsync();
    }
}
