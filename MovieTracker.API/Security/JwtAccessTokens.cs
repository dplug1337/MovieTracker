using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MovieTracker.Application.Users;
using MovieTracker.Domain.Users;

namespace MovieTracker.API.Security;

internal sealed class JwtAccessTokens(string issuer, string audience, SymmetricSecurityKey key, IReadOnlySet<Guid> administrators) : IAccessTokenIssuer
{
    public TokenResponse Issue(UserAccount user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddHours(1);
        var claims = new List<Claim>
            { new(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new(JwtRegisteredClaimNames.Name, user.DisplayName) };
        if (administrators.Contains(user.Id)) claims.Add(new Claim("role", "administrator"));
        var token = new JwtSecurityToken(issuer, audience, claims,
            now, expires, new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, user.DisplayName);
    }

    public static IServiceCollection AddMovieTrackerSecurity(IServiceCollection services, IConfiguration config)
    {
        var issuer = config["Jwt:Issuer"] ?? "MovieTracker.API";
        var audience = config["Jwt:Audience"] ?? "MovieTracker.Web";
        var secret = config["Jwt:SigningKey"];
        if (secret is null || Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("Set Jwt:SigningKey to a random key of at least 32 bytes.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        services.AddSingleton<IAccessTokenIssuer>(new JwtAccessTokens(issuer, audience, key,
            (config.GetSection("Jwt:AdministratorIds").Get<Guid[]>() ?? []).ToHashSet()));
        services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (!Guid.TryParse(context.Principal?.FindFirst("sub")?.Value, out var id) || id == Guid.Empty)
                        context.Fail("Invalid subject.");
                    return Task.CompletedTask;
                }
            };
        });
        services.AddAuthorizationBuilder().AddPolicy("CatalogueWrite", policy =>
            policy.RequireClaim("role", "administrator"));
        return services;
    }
}
