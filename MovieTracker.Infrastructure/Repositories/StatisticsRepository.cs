using Microsoft.EntityFrameworkCore;
using MovieTracker.Application.Users.Statistics;
using MovieTracker.Domain.Movies;
using MovieTracker.Domain.Users;
using MovieTracker.Infrastructure.Persistence;

namespace MovieTracker.Infrastructure.Repositories;

public sealed class StatisticsRepository(MovieTrackerDbContext db) : IStatisticsRepository
{
    public async Task<StatisticsResponse> GetAsync(Guid userId, DateTime monthStartUtc, DateTime nextMonthUtc, CancellationToken ct)
    {
        var watched = db.Watchlist.Where(x => x.UserId == userId && x.Status == WatchStatus.Watched);
        var totals = await watched.GroupBy(x => x.UserId).Select(g => new
        {
            Count = g.Count(),
            ThisMonth = g.Count(x => x.WatchedAtUtc >= monthStartUtc && x.WatchedAtUtc < nextMonthUtc)
        }).SingleOrDefaultAsync(ct);

        var favourite = await watched.GroupBy(x => x.Movie.Genre)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
            .Select(g => (MovieGenre?)g.Key).FirstOrDefaultAsync(ct);

        var average = await db.Ratings.Where(x => x.UserId == userId).AverageAsync(x => (double?)x.Value, ct);
        return new StatisticsResponse(totals?.Count ?? 0, average, favourite?.ToString(), totals?.ThisMonth ?? 0);
    }
}
