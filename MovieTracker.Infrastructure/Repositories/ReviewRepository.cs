using Microsoft.EntityFrameworkCore;
using MovieTracker.Application.Movies.Reviews;
using MovieTracker.Domain.Common.Results;
using MovieTracker.Domain.Movies;
using MovieTracker.Infrastructure.Persistence;
using Npgsql;

namespace MovieTracker.Infrastructure.Repositories;

public sealed class ReviewRepository(MovieTrackerDbContext db) : IReviewRepository
{
    public async Task<Result> AddAsync(Review review, CancellationToken ct)
    {
        db.Reviews.Add(review);
        try
        {
            await db.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Reviews_UserId_MovieId" })
        {
            db.Entry(review).State = EntityState.Detached;
            return Result.Failure(Error.Exist);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            db.Entry(review).State = EntityState.Detached;
            return Result.Failure(Error.NotFound);
        }
    }

    public async Task<IReadOnlyList<Review>> ListAsync(Guid movieId, CancellationToken ct) =>
        await db.Reviews.AsNoTracking().Where(x => x.MovieId == movieId).Include(x => x.Author)
            .OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id).ToListAsync(ct);

    public Task<Review?> FindAsync(Guid movieId, Guid reviewId, CancellationToken ct) =>
        db.Reviews.AsNoTracking().SingleOrDefaultAsync(x => x.MovieId == movieId && x.Id == reviewId, ct);

    public async Task<bool> UpdateAsync(Review review, CancellationToken ct) =>
        await db.Reviews.Where(x => x.Id == review.Id && x.MovieId == review.MovieId && x.UserId == review.UserId)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.Text, review.Text)
                .SetProperty(x => x.ModifiedAtUtc, review.ModifiedAtUtc), ct) != 0;

    public async Task<bool> DeleteAsync(Guid movieId, Guid reviewId, Guid userId, CancellationToken ct) =>
        await db.Reviews.Where(x => x.MovieId == movieId && x.Id == reviewId && x.UserId == userId).ExecuteDeleteAsync(ct) != 0;
}
