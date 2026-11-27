using Microsoft.EntityFrameworkCore;
using MovieTracker.Application.Movies.Ratings;
using MovieTracker.Domain.Movies;
using MovieTracker.Infrastructure.Persistence;
using Npgsql;

namespace MovieTracker.Infrastructure.Repositories;

public sealed class RatingRepository(MovieTrackerDbContext db) : IRatingRepository
{
    public async Task<bool> SetAsync(Rating rating, CancellationToken ct)
    {
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Ratings" ("UserId", "MovieId", "Value", "UpdatedAtUtc")
                VALUES ({rating.UserId}, {rating.MovieId}, {rating.Value}, {rating.UpdatedAtUtc})
                ON CONFLICT ("UserId", "MovieId") DO UPDATE
                SET "Value" = EXCLUDED."Value", "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc"
                """, ct);
            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<Rating>> ListAsync(Guid userId, CancellationToken ct) =>
        await db.Ratings.AsNoTracking().Where(x => x.UserId == userId).Include(x => x.Movie)
            .OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.MovieId).ToListAsync(ct);

    public async Task<MovieRatingSummary> SummarizeAsync(Guid movieId, Guid? userId, CancellationToken ct)
    {
        return await db.Ratings.Where(x => x.MovieId == movieId).GroupBy(x => x.MovieId)
            .Select(g => new MovieRatingSummary(g.Average(x => x.Value), g.Count(),
                g.Max(x => x.UserId == userId ? (int?)x.Value : null)))
            .SingleOrDefaultAsync(ct) ?? new MovieRatingSummary(null, 0, null);
    }

    public async Task<IReadOnlyList<(Movie Movie, double Average, int Count)>> TopAsync(CancellationToken ct)
    {
        var top = db.Ratings.GroupBy(x => x.MovieId)
            .Where(g => g.Count() >= 3)
            .Select(g => new { MovieId = g.Key, Average = g.Average(x => x.Value), Count = g.Count() })
            .OrderByDescending(x => x.Average).ThenByDescending(x => x.Count).ThenBy(x => x.MovieId).Take(10);
        var rows = await (from rating in top
                          join movie in db.Movies.AsNoTracking() on rating.MovieId equals movie.Id
                          orderby rating.Average descending, rating.Count descending, rating.MovieId
                          select new { Movie = movie, rating.Average, rating.Count }).ToListAsync(ct);
        return rows.Select(x => (x.Movie, x.Average, x.Count)).ToArray();
    }
}
