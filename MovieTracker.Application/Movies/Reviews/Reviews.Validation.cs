using FluentValidation;
using MovieTracker.Domain.Movies;

namespace MovieTracker.Application.Movies.Reviews;

public sealed class WriteReviewValidator : AbstractValidator<WriteReviewRequest>
{
    public WriteReviewValidator() => RuleFor(x => x.Text).Must(Review.IsValid)
        .WithMessage("Review text must contain 20 to 2000 characters after trimming.");
}
