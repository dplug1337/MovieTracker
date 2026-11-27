using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MovieTracker.Application.Movies.GetMovieList;
using MovieTracker.Domain.Common.Results;
using MovieTracker.Domain.Movies;

namespace MovieTracker.Application.Movies;

public interface IMovieCatalogRepository
{
    Task<Movie?> FindAsync(Guid id, CancellationToken ct);
    Task AddAsync(Movie movie, CancellationToken ct);
    Task<bool> UpdateAsync(Movie movie, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public sealed record CreateMovieRequest(string Title, string Description, MovieGenre Genre, int ReleaseYear, string PosterUrl) : IMovieWriteRequest;
public sealed record UpdateMovieRequest(string Title, string Description, MovieGenre Genre, int ReleaseYear, string PosterUrl) : IMovieWriteRequest;

public interface IMovieWriteRequest
{
    string Title { get; }
    string Description { get; }
    MovieGenre Genre { get; }
    int ReleaseYear { get; }
    string PosterUrl { get; }
}

public abstract class MovieWriteValidator<TRequest> : AbstractValidator<TRequest> where TRequest : IMovieWriteRequest
{
    protected MovieWriteValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Genre).IsInEnum();
        RuleFor(x => x.ReleaseYear).GreaterThanOrEqualTo(1888);
        RuleFor(x => x.PosterUrl).NotEmpty().MaximumLength(2048);
    }
}

public sealed class CreateMovieValidator : MovieWriteValidator<CreateMovieRequest>;
public sealed class UpdateMovieValidator : MovieWriteValidator<UpdateMovieRequest>;

public sealed class MovieCatalogHandler(IMovieCatalogRepository repository)
{
    public async Task<Result<MovieListItemResponse>> CreateAsync(CreateMovieRequest request, CancellationToken ct)
    {
        var movie = Movie.Create(Guid.NewGuid(), request.Title, request.Description, request.Genre, request.ReleaseYear, request.PosterUrl);
        if (movie.IsFailure) return movie.Error;
        await repository.AddAsync(movie.Value!, ct);
        return Result<MovieListItemResponse>.Success(movie.Value!.ToResponse());
    }

    public async Task<Result<MovieListItemResponse>> UpdateAsync(Guid id, UpdateMovieRequest request, CancellationToken ct)
    {
        var movie = Movie.Create(id, request.Title, request.Description, request.Genre, request.ReleaseYear, request.PosterUrl);
        if (movie.IsFailure) return movie.Error;
        return await repository.UpdateAsync(movie.Value!, ct)
            ? Result<MovieListItemResponse>.Success(movie.Value!.ToResponse())
            : Error.NotFound;
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct) =>
        await repository.DeleteAsync(id, ct) ? Result.Success() : Result.Failure(Error.NotFound);
}

public static class MovieCatalogEndpoints
{
    public static void MapMovieCatalog(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/movies", async (CreateMovieRequest request, MovieCatalogHandler handler, CancellationToken ct) =>
        {
            return (await handler.CreateAsync(request, ct)).ToHttp(movie => Results.Created($"/api/movies/{movie.Id}", movie));
        }).RequireAuthorization("CatalogueWrite");

        endpoints.MapPut("/api/movies/{id:guid}", async (Guid id, UpdateMovieRequest request, MovieCatalogHandler handler, CancellationToken ct) =>
        {
            return (await handler.UpdateAsync(id, request, ct)).ToHttp();
        }).RequireAuthorization("CatalogueWrite");

        endpoints.MapDelete("/api/movies/{id:guid}", async (Guid id, MovieCatalogHandler handler, CancellationToken ct) =>
            (await handler.DeleteAsync(id, ct)).ToHttp()).RequireAuthorization("CatalogueWrite");
    }
}
