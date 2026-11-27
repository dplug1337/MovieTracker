using MovieTracker.Application.Common.Repositories;
using MovieTracker.Domain.Movies;

namespace MovieTracker.Infrastructure.Repositories;

public sealed class InMemoryMovieRepository : IMovieRepository
{
    private static IReadOnlyList<Movie> Movies => MovieTracker.Infrastructure.Persistence.MovieSeed.Movies;

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Movies.Count);
    }

    public Task<IReadOnlyList<Movie>> GetPagedAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Movie> movies = Movies
            .OrderBy(movie => movie.Title, StringComparer.OrdinalIgnoreCase)
            .Skip(skip)
            .Take(take)
            .ToArray();

        return Task.FromResult(movies);
    }
}
