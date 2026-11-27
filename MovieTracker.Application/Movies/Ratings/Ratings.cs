using MovieTracker.Application.Movies.GetMovieList;
using MovieTracker.Domain.Common.Results;
using MovieTracker.Domain.Movies;

namespace MovieTracker.Application.Movies.Ratings;

public sealed record RateMovieRequest(int Rating);
public sealed record PersonalRatingResponse(MovieListItemResponse Movie, int Rating, DateTime UpdatedAtUtc);
public sealed record TopRatedMovieResponse(Guid Id, string Title, string Description, string Genre, int ReleaseYear,
    string PosterUrl, double AverageRating, int RatingsCount);
public sealed record MovieDetailsResponse(Guid Id, string Title, string Description, string Genre, int ReleaseYear,
    string PosterUrl, double? AverageRating, int RatingsCount, int? PersonalRating);

public sealed record MovieRatingSummary(double? AverageRating, int RatingsCount, int? PersonalRating);

public interface IRatingRepository
{
    Task<bool> SetAsync(Rating rating, CancellationToken ct);
    Task<IReadOnlyList<Rating>> ListAsync(Guid userId, CancellationToken ct);
    Task<MovieRatingSummary> SummarizeAsync(Guid movieId, Guid? userId, CancellationToken ct);
    Task<IReadOnlyList<(Movie Movie, double Average, int Count)>> TopAsync(CancellationToken ct);
}

public sealed class RatingsHandler(IRatingRepository ratings, IMovieCatalogRepository movies)
{
    public async Task<Result> SetAsync(Guid userId, Guid movieId, RateMovieRequest request, CancellationToken ct)
    {
        var rating = Rating.Create(userId, movieId, request.Rating, DateTime.UtcNow);
        if (rating.IsFailure) return rating;
        if (await movies.FindAsync(movieId, ct) is null) return Result.Failure(Error.NotFound);
        return await ratings.SetAsync(rating.Value!, ct) ? Result.Success() : Result.Failure(Error.NotFound);
    }

    public async Task<IReadOnlyList<PersonalRatingResponse>> ListAsync(Guid userId, CancellationToken ct) =>
        (await ratings.ListAsync(userId, ct)).Select(RatingsMapping.ToResponse).ToArray();

    public async Task<IReadOnlyList<TopRatedMovieResponse>> TopAsync(CancellationToken ct) =>
        (await ratings.TopAsync(ct)).Select(row => row.Movie.ToTopRated(row.Average, row.Count)).ToArray();

    public async Task<Result<MovieDetailsResponse>> DetailsAsync(Guid movieId, Guid? userId, CancellationToken ct)
    {
        var movie = await movies.FindAsync(movieId, ct);
        if (movie is null) return Error.NotFound;
        var summary = await ratings.SummarizeAsync(movieId, userId, ct);
        return Result<MovieDetailsResponse>.Success(movie.ToDetails(summary));
    }
}
