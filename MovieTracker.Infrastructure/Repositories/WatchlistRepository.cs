using Microsoft.EntityFrameworkCore;
using MovieTracker.Application.Users;
using MovieTracker.Domain.Users;
using MovieTracker.Infrastructure.Persistence;
using Npgsql;

namespace MovieTracker.Infrastructure.Repositories;

public sealed class WatchlistRepository(MovieTrackerDbContext db) : IWatchlistRepository
{
    public async Task<IReadOnlyList<WatchlistItem>> ListAsync(Guid userId, CancellationToken ct) =>
        await db.Watchlist.AsNoTracking().Where(x => x.UserId == userId).Include(x => x.Movie)
            .OrderBy(x => x.Movie.Title).ThenBy(x => x.MovieId).ToListAsync(ct);

    public async Task<bool> SetAsync(WatchlistItem item, CancellationToken ct)
    {
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Watchlist" ("UserId", "MovieId", "Status") VALUES ({item.UserId}, {item.MovieId}, {(int)item.Status})
                ON CONFLICT ("UserId", "MovieId") DO UPDATE SET "Status" = EXCLUDED."Status"
                """, ct);
            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return false;
        }
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid movieId, CancellationToken ct) =>
        await db.Watchlist.Where(x => x.UserId == userId && x.MovieId == movieId).ExecuteDeleteAsync(ct) != 0;
}
