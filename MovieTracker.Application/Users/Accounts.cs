using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using MovieTracker.Domain.Common.Results;
using MovieTracker.Domain.Users;

namespace MovieTracker.Application.Users;

public interface IUserRepository
{
    Task<UserAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken ct);
    Task<bool> TryAddAsync(UserAccount account, CancellationToken ct);
}

public interface IAccessTokenIssuer
{
    TokenResponse Issue(UserAccount user);
}

public sealed record RegisterRequest(string Email, string DisplayName, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record TokenResponse(string AccessToken, DateTime ExpiresAtUtc, string DisplayName);

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254).EmailAddress();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Password).NotEmpty().Length(8, 128);
    }
}

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254).EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class AccountsHandler(IUserRepository users, IPasswordHasher<UserAccount> passwords, IAccessTokenIssuer tokens)
{
    public async Task<Result<TokenResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var user = new UserAccount(request.Email, request.DisplayName);
        user.SetPasswordHash(passwords.HashPassword(user, request.Password));
        return await users.TryAddAsync(user, ct) ? Result<TokenResponse>.Success(tokens.Issue(user)) : Error.Exist;
    }

    public async Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(UserAccount.NormalizeEmail(request.Email), ct);
        if (user is null || passwords.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return new Error("Unauthorized", "Invalid credentials.");
        return Result<TokenResponse>.Success(tokens.Issue(user));
    }
}

public static class AccountsEndpoints
{
    public static void MapAccounts(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/register", async (RegisterRequest request, AccountsHandler handler, CancellationToken ct) =>
        {
            return (await handler.RegisterAsync(request, ct)).ToHttp(user => Results.Json(user, statusCode: 201));
        });

        endpoints.MapPost("/api/auth/login", async (LoginRequest request, AccountsHandler handler, CancellationToken ct) =>
        {
            return (await handler.LoginAsync(request, ct)).ToHttp();
        });
    }
}
