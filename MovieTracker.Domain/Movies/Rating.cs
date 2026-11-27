using MovieTracker.Domain.Common.Results;
using MovieTracker.Domain.Users;

namespace MovieTracker.Domain.Movies;

public sealed class Rating
{
    private Rating() { }

    public Guid UserId { get; private init; }
    public Guid MovieId { get; private init; }
    public Movie Movie { get; private init; } = null!;
    public UserAccount User { get; private init; } = null!;
    public int Value { get; private init; }
    public DateTime UpdatedAtUtc { get; private init; }

    public static bool IsValid(int value) => value is >= 1 and <= 10;

    public static Result<Rating> Create(Guid userId, Guid movieId, int value, DateTime nowUtc) =>
        IsValid(value)
            ? Result<Rating>.Success(new Rating { UserId = userId, MovieId = movieId, Value = value, UpdatedAtUtc = nowUtc })
            : new Error("InvalidRating", "Rating must be between 1 and 10.");
}
