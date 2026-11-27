using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MovieTracker.Application.Users;

namespace MovieTracker.Application.Movies.Ratings;

public static class RatingsEndpoints
{
    public static void MapRatings(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/movies/{id:guid}", async (Guid id, ClaimsPrincipal user, RatingsHandler handler, CancellationToken ct) =>
            (await handler.DetailsAsync(id, user.FindUserId(), ct)).ToHttp());

        endpoints.MapPost("/api/movies/{id:guid}/ratings", async (Guid id, RateMovieRequest request, ClaimsPrincipal user,
            IValidator<RateMovieRequest> validator, RatingsHandler handler, CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            return (await handler.SetAsync(user.UserId(), id, request, ct)).ToHttp();
        }).RequireAuthorization();

        endpoints.MapGet("/api/users/me/ratings", async (ClaimsPrincipal user, RatingsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.ListAsync(user.UserId(), ct))).RequireAuthorization();

        endpoints.MapGet("/api/movies/top-rated", async (RatingsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.TopAsync(ct)));
    }
}
