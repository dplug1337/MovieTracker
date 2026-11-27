using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieTracker.Domain.Users;

namespace MovieTracker.Infrastructure.Persistence;

public sealed class UserConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.Email).HasMaxLength(254);
        builder.Property(x => x.DisplayName).HasMaxLength(80);
    }
}

public sealed class WatchlistConfiguration : IEntityTypeConfiguration<WatchlistItem>
{
    public void Configure(EntityTypeBuilder<WatchlistItem> builder)
    {
        builder.HasKey(x => new { x.UserId, x.MovieId });
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Movie).WithMany().HasForeignKey(x => x.MovieId);
    }
}
