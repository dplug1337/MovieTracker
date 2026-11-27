namespace MovieTracker.Web.Models.Movies;

public sealed record MovieDetailsModel(Guid Id, string Title, string Description, string Genre, int ReleaseYear,
    string PosterUrl, double? AverageRating, int RatingsCount, int? PersonalRating);

public sealed record ReviewModel(Guid Id, string Text, string AuthorName, DateTime CreatedAtUtc, DateTime? ModifiedAtUtc, bool IsMine);
public sealed record PersonalRatingModel(MovieListItemModel Movie, int Rating, DateTime UpdatedAtUtc);
public sealed record StatisticsModel(int WatchedMoviesCount, double? AverageRating, string? FavouriteGenre, int WatchedThisMonthCount);
public sealed record TopRatedMovieModel(Guid Id, string Title, string Genre, int ReleaseYear, string PosterUrl, double AverageRating, int RatingsCount);
public sealed record WatchlistModel(MovieListItemModel Movie, string Status, DateTime? WatchedAtUtc);
public sealed record LoginSession(string AccessToken, DateTime ExpiresAtUtc, string DisplayName);
