using MovieTracker.Domain.Movies;

namespace MovieTracker.Domain.Users;

public enum WatchStatus { WantToWatch = 1, Watching, Watched }

public sealed class WatchlistItem
{
    private WatchlistItem() { }

    public WatchlistItem(Guid userId, Guid movieId, WatchStatus status)
    {
        UserId = userId;
        MovieId = movieId;
        Status = status;
    }

    public Guid UserId { get; private set; }
    public Guid MovieId { get; private set; }
    public WatchStatus Status { get; private set; }
    public Movie Movie { get; private set; } = null!;
}
