using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lld.Application.Abstractions;
using Lld.Application.Auth;
using Lld.Domain.Common;
using Lld.Domain.Identity;
using Lld.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Lld.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; set; } = "lld-guide";

    public string Audience { get; set; } = "lld-guide-clients";

    /// <summary>HMAC-SHA256 key, at least 32 bytes. From user secrets / environment, never committed.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public SymmetricSecurityKey CreateKey()
    {
        var bytes = Encoding.UTF8.GetBytes(SigningKey);
        if (bytes.Length < MinimumKeyBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:SigningKey must be at least {MinimumKeyBytes} bytes. Set it via configuration (Aspire parameter or user secrets).");
        }

        return new SymmetricSecurityKey(bytes);
    }
}

/// <summary>Claim names shared by the issuer and the API's authorization policies.</summary>
public static class LldClaims
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
    public const string MustChangePassword = "must_change_password";
}

/// <summary>Strategy implementation of <see cref="ITokenIssuer"/>: HS256-signed JWT access tokens.</summary>
internal sealed class JwtTokenIssuer(JwtOptions options, AuthOptions authOptions) : ITokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();
    // Created on first use, so processes that never issue tokens (the admin console) need no signing key.
    private readonly Lazy<SigningCredentials> _credentials = new(() => new SigningCredentials(options.CreateKey(), SecurityAlgorithms.HmacSha256));

    public AccessToken Issue(UserAccount user, DateTimeOffset now)
    {
        var expires = now + authOptions.AccessTokenLifetime;
        var claims = new List<Claim>
        {
            new(LldClaims.Subject, user.Id),
            new(LldClaims.Email, user.Email.Value),
            new(LldClaims.Name, user.DisplayName),
            new(LldClaims.MustChangePassword, user.MustChangePassword ? "true" : "false"),
        };
        claims.AddRange(user.Roles.Select(role => new Claim(LldClaims.Role, role)));

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = _credentials.Value,
        });

        return new AccessToken(token, expires);
    }
}

/// <summary>256-bit random refresh tokens; the database only ever sees their SHA-256 hash.</summary>
internal sealed class RefreshTokenCodec : IRefreshTokenCodec
{
    public NewRefreshToken Generate()
    {
        var raw = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        return new NewRefreshToken(raw, Hash(raw));
    }

    public string Hash(string rawToken) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}

/// <summary>16 characters from an alphabet without look-alikes (no 0/O, 1/l/I) so it can be read out or typed.</summary>
internal sealed class CryptoTemporaryPasswordGenerator : ITemporaryPasswordGenerator
{
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
    private const int Length = 16;

    public string Generate() => new(RandomNumberGenerator.GetItems<char>(Alphabet, Length));
}

/// <summary>
/// Strategy for user-chosen passwords: length over complexity (NIST SP 800-63B style).
/// At least 12 characters, at most 128, and not the same as the email.
/// </summary>
public sealed class MinLengthPasswordPolicy : IPasswordPolicy
{
    public const int MinLength = 12;
    public const int MaxLength = 128;

    public Result Validate(string password, EmailAddress email)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
        {
            return Error.Validation("password.too_short", $"Use at least {MinLength} characters.");
        }

        if (password.Length > MaxLength)
        {
            return Error.Validation("password.too_long", $"Use at most {MaxLength} characters.");
        }

        if (string.Equals(password, email.Value, StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("password.equals_email", "Your password can't be your email address.");
        }

        return Result.Success();
    }
}

/// <summary>Plugs the password policy into ASP.NET Core Identity so every password path enforces it.</summary>
internal sealed class PolicyPasswordValidator(IPasswordPolicy policy) : IPasswordValidator<AppUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string? password)
    {
        var email = EmailAddress.FromTrusted(user.Email ?? string.Empty);
        var result = policy.Validate(password ?? string.Empty, email);
        return Task.FromResult(result.IsSuccess
            ? IdentityResult.Success
            : IdentityResult.Failed(new IdentityError { Code = result.Error!.Code, Description = result.Error.Message }));
    }
}
