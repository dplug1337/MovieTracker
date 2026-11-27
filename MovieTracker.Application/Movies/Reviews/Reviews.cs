using MovieTracker.Domain.Common.Results;
using MovieTracker.Domain.Movies;

namespace MovieTracker.Application.Movies.Reviews;

public sealed record WriteReviewRequest(string Text);
public sealed record ReviewResponse(Guid Id, string Text, string AuthorName, DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc, bool IsMine);

public interface IReviewRepository
{
    Task<Result> AddAsync(Review review, CancellationToken ct);
    Task<IReadOnlyList<Review>> ListAsync(Guid movieId, CancellationToken ct);
    Task<Review?> FindAsync(Guid movieId, Guid reviewId, CancellationToken ct);
    Task<bool> UpdateAsync(Review review, CancellationToken ct);
    Task<bool> DeleteAsync(Guid movieId, Guid reviewId, Guid userId, CancellationToken ct);
}

public sealed class ReviewsHandler(IReviewRepository reviews, IMovieCatalogRepository movies)
{
    private static readonly Error Forbidden = new("Forbidden", "Only the author may change the review.");

    public async Task<Result> CreateAsync(Guid movieId, Guid userId, WriteReviewRequest request, CancellationToken ct)
    {
        var review = Review.Create(userId, movieId, request.Text, DateTime.UtcNow);
        if (review.IsFailure) return review;
        if (await movies.FindAsync(movieId, ct) is null) return Result.Failure(Error.NotFound);
        return await reviews.AddAsync(review.Value!, ct);
    }

    public async Task<Result<IReadOnlyList<ReviewResponse>>> ListAsync(Guid movieId, Guid? userId, CancellationToken ct)
    {
        if (await movies.FindAsync(movieId, ct) is null) return Error.NotFound;
        var items = await reviews.ListAsync(movieId, ct);
        return Result<IReadOnlyList<ReviewResponse>>.Success(items.Select(x => x.ToResponse(userId)).ToArray());
    }

    public async Task<Result> UpdateAsync(Guid movieId, Guid reviewId, Guid userId, WriteReviewRequest request, CancellationToken ct)
    {
        var review = await reviews.FindAsync(movieId, reviewId, ct);
        if (review is null) return Result.Failure(Error.NotFound);
        if (review.UserId != userId) return Result.Failure(Forbidden);
        var edit = review.Edit(request.Text, DateTime.UtcNow);
        if (edit.IsFailure) return edit;
        return await reviews.UpdateAsync(review, ct) ? Result.Success() : Result.Failure(Error.NotFound);
    }

    public async Task<Result> DeleteAsync(Guid movieId, Guid reviewId, Guid userId, CancellationToken ct)
    {
        var review = await reviews.FindAsync(movieId, reviewId, ct);
        if (review is null) return Result.Failure(Error.NotFound);
        if (review.UserId != userId) return Result.Failure(Forbidden);
        return await reviews.DeleteAsync(movieId, reviewId, userId, ct) ? Result.Success() : Result.Failure(Error.NotFound);
    }
}
