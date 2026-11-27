using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MovieTracker.Application.Movies.GetMovieList;

public static class GetMovieListEndpoint
{
    public static IEndpointConventionBuilder MapGetMovieListEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/api/movies", HandleAsync)
            .WithName("GetMovieList")
            .WithSummary("Gets a paged list of movies")
            .Produces<GetMovieListResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

    private static async Task<IResult> HandleAsync(
        [AsParameters] GetMovieListRequest request,
        GetMovieListHandler handler,
        CancellationToken cancellationToken) =>
        Results.Ok(await handler.HandleAsync(request, cancellationToken));
}
