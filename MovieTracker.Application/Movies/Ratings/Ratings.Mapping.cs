using MovieTracker.Application.Movies.GetMovieList;
using MovieTracker.Domain.Movies;

namespace MovieTracker.Application.Movies.Ratings;

internal static class RatingsMapping
{
    public static PersonalRatingResponse ToResponse(this Rating rating) =>
        new(rating.Movie.ToResponse(), rating.Value, rating.UpdatedAtUtc);

    public static TopRatedMovieResponse ToTopRated(this Movie movie, double average, int count) =>
        new(movie.Id, movie.Title, movie.Description, movie.Genre.ToString(), movie.ReleaseYear, movie.PosterUrl, average, count);

    public static MovieDetailsResponse ToDetails(this Movie movie, MovieRatingSummary summary) =>
        new(movie.Id, movie.Title, movie.Description, movie.Genre.ToString(), movie.ReleaseYear, movie.PosterUrl,
            summary.AverageRating, summary.RatingsCount, summary.PersonalRating);
}
