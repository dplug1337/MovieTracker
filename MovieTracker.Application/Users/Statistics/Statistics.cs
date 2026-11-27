using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MovieTracker.Application.Users.Statistics;

public sealed record StatisticsResponse(int WatchedMoviesCount, double? AverageRating, string? FavouriteGenre, int WatchedThisMonthCount);

public interface IStatisticsRepository
{
    Task<StatisticsResponse> GetAsync(Guid userId, DateTime monthStartUtc, DateTime nextMonthUtc, CancellationToken ct);
}

public sealed class StatisticsHandler(IStatisticsRepository repository)
{
    public Task<StatisticsResponse> GetAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var month = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return repository.GetAsync(userId, month, month.AddMonths(1), ct);
    }
}

public static class StatisticsEndpoints
{
    public static void MapStatistics(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/api/users/me/statistics", async (ClaimsPrincipal user, StatisticsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.GetAsync(user.UserId(), ct))).RequireAuthorization();
}
