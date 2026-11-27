using Mapster;
using MovieTracker.Application.Common;
using MovieTracker.Domain.Movies;

namespace MovieTracker.Application.Movies.GetMovieList;

public sealed class GetMovieListMapping : IRegister
{
    public void Register(TypeAdapterConfig config) =>
        config.NewConfig<Movie, MovieListItemResponse>().MapToConstructor(true)
            .Map(dest => dest.Genre, source => source.Genre.ToString());

}

internal static class MovieListResponseMapping
{
    internal static MovieListItemResponse ToResponse(this Movie movie) =>
        movie.Adapt<MovieListItemResponse>(ResponseMapping.Config);
}
