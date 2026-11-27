using MovieTracker.Domain.Movies;

namespace MovieTracker.Application.Movies.Reviews;

internal static class ReviewsMapping
{
    public static ReviewResponse ToResponse(this Review review, Guid? userId) =>
        new(review.Id, review.Text, review.Author.DisplayName, review.CreatedAtUtc, review.ModifiedAtUtc,
            review.UserId == userId);
}
