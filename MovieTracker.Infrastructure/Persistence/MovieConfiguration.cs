using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieTracker.Domain.Movies;

namespace MovieTracker.Infrastructure.Persistence;

public sealed class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.PosterUrl).HasMaxLength(2048);
        builder.HasIndex(x => new { x.Title, x.Id });
        builder.HasData(MovieSeed.Movies);
    }
}
