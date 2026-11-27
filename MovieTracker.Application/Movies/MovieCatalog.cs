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

public sealed record CreateMovieRequest(string Title, string Description, MovieGenre Genre, int ReleaseYear, string PosterUrl);
public sealed record UpdateMovieRequest(string Title, string Description, MovieGenre Genre, int ReleaseYear, string PosterUrl);

public sealed class CreateMovieValidator : AbstractValidator<CreateMovieRequest>
{
    public CreateMovieValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Genre).IsInEnum();
        RuleFor(x => x.ReleaseYear).GreaterThanOrEqualTo(1888);
        RuleFor(x => x.PosterUrl).NotEmpty().MaximumLength(2048);
    }
}

public sealed class UpdateMovieValidator : AbstractValidator<UpdateMovieRequest>
{
    public UpdateMovieValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Genre).IsInEnum();
        RuleFor(x => x.ReleaseYear).GreaterThanOrEqualTo(1888);
        RuleFor(x => x.PosterUrl).NotEmpty().MaximumLength(2048);
    }
}

public sealed class MovieCatalogHandler(IMovieCatalogRepository repository)
{
    public async Task<Result<MovieListItemResponse>> GetAsync(Guid id, CancellationToken ct)
    {
        var movie = await repository.FindAsync(id, ct);
        return movie is null ? Error.NotFound : Result<MovieListItemResponse>.Success(movie.ToResponse());
    }

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
        endpoints.MapGet("/api/movies/{id:guid}", async (Guid id, MovieCatalogHandler handler, CancellationToken ct) =>
            (await handler.GetAsync(id, ct)).ToHttp());

        endpoints.MapPost("/api/movies", async (CreateMovieRequest request, IValidator<CreateMovieRequest> validator,
            MovieCatalogHandler handler, CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            return (await handler.CreateAsync(request, ct)).ToHttp(movie => Results.Created($"/api/movies/{movie.Id}", movie));
        }).RequireAuthorization("CatalogueWrite");

        endpoints.MapPut("/api/movies/{id:guid}", async (Guid id, UpdateMovieRequest request, IValidator<UpdateMovieRequest> validator,
            MovieCatalogHandler handler, CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            return (await handler.UpdateAsync(id, request, ct)).ToHttp();
        }).RequireAuthorization("CatalogueWrite");

        endpoints.MapDelete("/api/movies/{id:guid}", async (Guid id, MovieCatalogHandler handler, CancellationToken ct) =>
            (await handler.DeleteAsync(id, ct)).ToHttp()).RequireAuthorization("CatalogueWrite");
    }
}
