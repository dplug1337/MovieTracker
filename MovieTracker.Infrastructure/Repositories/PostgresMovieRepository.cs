using Microsoft.EntityFrameworkCore;
using MovieTracker.Application.Common.Repositories;
using MovieTracker.Application.Movies;
using MovieTracker.Domain.Movies;
using MovieTracker.Infrastructure.Persistence;

namespace MovieTracker.Infrastructure.Repositories;

public sealed class PostgresMovieRepository(MovieTrackerDbContext db) : IMovieRepository, IMovieCatalogRepository
{
    public Task<int> CountAsync(CancellationToken cancellationToken = default) => db.Movies.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Movie>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default) =>
        await db.Movies.AsNoTracking().OrderBy(x => x.Title).ThenBy(x => x.Id)
            .Skip(skip).Take(take).ToListAsync(cancellationToken);

    public Task<Movie?> FindAsync(Guid id, CancellationToken ct) =>
        db.Movies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(Movie movie, CancellationToken ct)
    {
        db.Movies.Add(movie);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> UpdateAsync(Movie movie, CancellationToken ct) =>
        await db.Movies.Where(x => x.Id == movie.Id).ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.Title, movie.Title)
            .SetProperty(x => x.Description, movie.Description)
            .SetProperty(x => x.Genre, movie.Genre)
            .SetProperty(x => x.ReleaseYear, movie.ReleaseYear)
            .SetProperty(x => x.PosterUrl, movie.PosterUrl), ct) != 0;

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct) =>
        await db.Movies.Where(x => x.Id == id).ExecuteDeleteAsync(ct) != 0;
}
