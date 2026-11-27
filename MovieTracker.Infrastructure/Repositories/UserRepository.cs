using Microsoft.EntityFrameworkCore;
using MovieTracker.Application.Users;
using MovieTracker.Domain.Users;
using MovieTracker.Infrastructure.Persistence;
using Npgsql;

namespace MovieTracker.Infrastructure.Repositories;

public sealed class UserRepository(MovieTrackerDbContext db) : IUserRepository
{
    public Task<UserAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Email == normalizedEmail, ct);

    public async Task<bool> TryAddAsync(UserAccount account, CancellationToken ct)
    {
        db.Users.Add(account);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Users_Email" })
        {
            db.Entry(account).State = EntityState.Detached;
            return false;
        }
    }
}
