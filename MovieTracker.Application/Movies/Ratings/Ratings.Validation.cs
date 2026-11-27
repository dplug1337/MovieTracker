using FluentValidation;
using MovieTracker.Domain.Movies;

namespace MovieTracker.Application.Movies.Ratings;

public sealed class RateMovieValidator : AbstractValidator<RateMovieRequest>
{
    public RateMovieValidator() => RuleFor(x => x.Rating).Must(Rating.IsValid).WithMessage("Rating must be between 1 and 10.");
}
