using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MovieTracker.Application.Users;

namespace MovieTracker.Application.Movies.Reviews;

public static class ReviewsEndpoints
{
    public static void MapReviews(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/movies/{id:guid}/reviews",
            async (Guid id, ClaimsPrincipal user, ReviewsHandler handler, CancellationToken ct) =>
                (await handler.ListAsync(id, user.FindUserId(), ct)).ToHttp());

        endpoints.MapPost("/api/movies/{id:guid}/reviews", async (Guid id, WriteReviewRequest request, ClaimsPrincipal user,
            IValidator<WriteReviewRequest> validator, ReviewsHandler handler, CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            var result = await handler.CreateAsync(id, user.UserId(), request, ct);
            return result.IsSuccess ? Results.StatusCode(StatusCodes.Status201Created) : result.ToHttp();
        }).RequireAuthorization();

        endpoints.MapPut("/api/movies/{movieId:guid}/reviews/{reviewId:guid}",
            async (Guid movieId, Guid reviewId, WriteReviewRequest request, ClaimsPrincipal user,
                IValidator<WriteReviewRequest> validator, ReviewsHandler handler, CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
                return (await handler.UpdateAsync(movieId, reviewId, user.UserId(), request, ct)).ToHttp();
            }).RequireAuthorization();

        endpoints.MapDelete("/api/movies/{movieId:guid}/reviews/{reviewId:guid}",
            async (Guid movieId, Guid reviewId, ClaimsPrincipal user, ReviewsHandler handler, CancellationToken ct) =>
                (await handler.DeleteAsync(movieId, reviewId, user.UserId(), ct)).ToHttp()).RequireAuthorization();
    }
}
