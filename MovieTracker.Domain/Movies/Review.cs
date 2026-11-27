using MovieTracker.Domain.Common.Results;
using MovieTracker.Domain.Users;

namespace MovieTracker.Domain.Movies;

public sealed class Review
{
    private Review() { }

    public Guid Id { get; private init; }
    public Guid UserId { get; private init; }
    public Guid MovieId { get; private init; }
    public UserAccount Author { get; private init; } = null!;
    public string Text { get; private set; } = "";
    public DateTime CreatedAtUtc { get; private init; }
    public DateTime? ModifiedAtUtc { get; private set; }

    public static bool IsValid(string? text) => text?.Trim().Length is >= 20 and <= 2000;

    public static Result<Review> Create(Guid userId, Guid movieId, string text, DateTime nowUtc)
    {
        if (!IsValid(text)) return InvalidText;
        return Result<Review>.Success(new Review
        {
            Id = Guid.NewGuid(), UserId = userId, MovieId = movieId, Text = text.Trim(), CreatedAtUtc = nowUtc
        });
    }

    public Result Edit(string text, DateTime nowUtc)
    {
        if (!IsValid(text)) return Result.Failure(InvalidText);
        Text = text.Trim();
        ModifiedAtUtc = nowUtc;
        return Result.Success();
    }

    private static readonly Error InvalidText = new("InvalidReview", "Review text must contain 20 to 2000 characters after trimming.");
}
