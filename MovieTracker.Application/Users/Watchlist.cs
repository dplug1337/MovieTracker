using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MovieTracker.Application.Movies;
using MovieTracker.Application.Movies.GetMovieList;
using MovieTracker.Domain.Common.Results;
using MovieTracker.Domain.Users;

namespace MovieTracker.Application.Users;

public interface IWatchlistRepository
{
    Task<IReadOnlyList<WatchlistItem>> ListAsync(Guid userId, CancellationToken ct);
    Task<bool> SetAsync(WatchlistItem item, CancellationToken ct);
    Task<bool> RemoveAsync(Guid userId, Guid movieId, CancellationToken ct);
}

public sealed record SetWatchlistRequest(WatchStatus Status);
public sealed record WatchlistResponse(MovieListItemResponse Movie, string Status, DateTime? WatchedAtUtc);

public sealed class SetWatchlistValidator : AbstractValidator<SetWatchlistRequest>
{
    public SetWatchlistValidator() => RuleFor(x => x.Status).IsInEnum();
}

public sealed class WatchlistHandler(IWatchlistRepository watchlist, IMovieCatalogRepository movies)
{
    public async Task<IReadOnlyList<WatchlistResponse>> ListAsync(Guid userId, CancellationToken ct) =>
        (await watchlist.ListAsync(userId, ct)).Select(x => new WatchlistResponse(x.Movie.ToResponse(), x.Status.ToString(), x.WatchedAtUtc)).ToArray();

    public async Task<Result> SetAsync(Guid userId, Guid movieId, SetWatchlistRequest request, CancellationToken ct)
    {
        if (await movies.FindAsync(movieId, ct) is null) return Result.Failure(Error.NotFound);
        var saved = await watchlist.SetAsync(new WatchlistItem(userId, movieId, request.Status, DateTime.UtcNow), ct);
        return saved ? Result.Success() : Result.Failure(Error.NotFound);
    }

    public async Task<Result> RemoveAsync(Guid userId, Guid movieId, CancellationToken ct) =>
        await watchlist.RemoveAsync(userId, movieId, ct) ? Result.Success() : Result.Failure(Error.NotFound);
}

internal static class UserClaims
{
    public static Guid? FindUserId(this ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated == true && Guid.TryParse(principal.FindFirst("sub")?.Value, out var id)
            && id != Guid.Empty ? id : null;

    public static Guid UserId(this ClaimsPrincipal principal) => principal.FindUserId()!.Value;
}

public static class WatchlistEndpoints
{
    public static void MapWatchlist(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users/me/watchlist").RequireAuthorization();
        group.MapGet("", async (ClaimsPrincipal user, WatchlistHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.ListAsync(user.UserId(), ct)));

        group.MapPut("/{movieId:guid}", async (Guid movieId, SetWatchlistRequest request, ClaimsPrincipal user,
            IValidator<SetWatchlistRequest> validator, WatchlistHandler handler, CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            return (await handler.SetAsync(user.UserId(), movieId, request, ct)).ToHttp();
        });

        group.MapDelete("/{movieId:guid}", async (Guid movieId, ClaimsPrincipal user, WatchlistHandler handler, CancellationToken ct) =>
            (await handler.RemoveAsync(user.UserId(), movieId, ct)).ToHttp());
    }
}
