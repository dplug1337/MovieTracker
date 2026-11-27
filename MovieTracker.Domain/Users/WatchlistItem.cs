using MovieTracker.Domain.Movies;

namespace MovieTracker.Domain.Users;

public enum WatchStatus { WantToWatch = 1, Watching, Watched }

public sealed class WatchlistItem
{
    private WatchlistItem() { }

    public WatchlistItem(Guid userId, Guid movieId, WatchStatus status, DateTime nowUtc)
    {
        UserId = userId;
        MovieId = movieId;
        Status = status;
        WatchedAtUtc = status == WatchStatus.Watched ? nowUtc : null;
    }

    public Guid UserId { get; private set; }
    public Guid MovieId { get; private set; }
    public WatchStatus Status { get; private set; }
    public DateTime? WatchedAtUtc { get; private set; }
    public Movie Movie { get; private set; } = null!;
}
