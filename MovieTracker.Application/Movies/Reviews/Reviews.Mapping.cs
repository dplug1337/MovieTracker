using Mapster;
using MovieTracker.Application.Common;
using MovieTracker.Domain.Movies;

namespace MovieTracker.Application.Movies.Reviews;

public sealed class ReviewsMapping : IRegister
{
    public void Register(TypeAdapterConfig config) =>
        config.NewConfig<(Review Review, Guid? Viewer), ReviewResponse>().MapToConstructor(true)
            .Map(dest => dest.Id, source => source.Review.Id)
            .Map(dest => dest.Text, source => source.Review.Text)
            .Map(dest => dest.AuthorName, source => source.Review.Author.DisplayName)
            .Map(dest => dest.CreatedAtUtc, source => source.Review.CreatedAtUtc)
            .Map(dest => dest.ModifiedAtUtc, source => source.Review.ModifiedAtUtc)
            .Map(dest => dest.IsMine, source => source.Review.UserId == source.Viewer);

}

internal static class ReviewResponseMapping
{
    internal static ReviewResponse ToResponse(this Review review, Guid? userId) =>
        (review, userId).Adapt<ReviewResponse>(ResponseMapping.Config);
}
