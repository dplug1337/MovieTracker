namespace MovieTracker.Domain.Users;

public sealed class UserAccount
{
    private UserAccount() { }

    public UserAccount(string email, string displayName)
    {
        Id = Guid.NewGuid();
        Email = NormalizeEmail(email);
        DisplayName = displayName.Trim();
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = "";
    public string DisplayName { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";

    public void SetPasswordHash(string hash) => PasswordHash = hash;

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
