using MovieTracker.Domain.Users;
using Microsoft.EntityFrameworkCore;
using MovieTracker.Domain.Movies;

namespace MovieTracker.Infrastructure.Persistence;

public sealed class MovieTrackerDbContext(DbContextOptions<MovieTrackerDbContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();

    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<WatchlistItem> Watchlist => Set<WatchlistItem>();

    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MovieTrackerDbContext).Assembly);
}
