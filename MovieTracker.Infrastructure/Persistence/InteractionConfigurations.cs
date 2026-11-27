using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieTracker.Domain.Movies;

namespace MovieTracker.Infrastructure.Persistence;

public sealed class RatingConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> builder)
    {
        builder.HasKey(x => new { x.UserId, x.MovieId });
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Movie).WithMany().HasForeignKey(x => x.MovieId);
        builder.ToTable(table => table.HasCheckConstraint("CK_Ratings_Value", """ "Value" BETWEEN 1 AND 10 """));
    }
}

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.MovieId }).IsUnique();
        builder.HasIndex(x => new { x.MovieId, x.CreatedAtUtc });
        builder.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne<Movie>().WithMany().HasForeignKey(x => x.MovieId);
        builder.Property(x => x.Text).HasMaxLength(2000);
    }
}
