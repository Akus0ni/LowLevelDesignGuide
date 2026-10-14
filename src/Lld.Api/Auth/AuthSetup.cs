using System.Text;
using System.Threading.RateLimiting;
using Lld.Api.Endpoints;
using Lld.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lld.Api.Auth;

public static class AuthorizationPolicies
{
    /// <summary>Signed in, even if the password still has to be changed (change-password, me).</summary>
    public const string PasswordChangePending = "PasswordChangePending";
}

internal static class AuthSetup
{
    public const int DefaultRequestsPerMinute = 20;

    public static IServiceCollection AddLldAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var requestsPerMinute = configuration.GetValue("Auth:RateLimitPerMinute", DefaultRequestsPerMinute);

        services.AddOptions<JwtOptions>()
            .Validate(o => Encoding.UTF8.GetByteCount(o.SigningKey) >= JwtOptions.MinimumKeyBytes,
                $"Jwt:SigningKey must be at least {JwtOptions.MinimumKeyBytes} bytes.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = jwt.Value.CreateKey(),
                    NameClaimType = LldClaims.Name,
                    RoleClaimType = LldClaims.Role,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        // Default policy: signed in AND not waiting on a forced password change.
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireAssertion(ctx => ctx.User.FindFirst(LldClaims.MustChangePassword)?.Value != "true")
                .Build())
            .AddPolicy(AuthorizationPolicies.PasswordChangePending, policy => policy.RequireAuthenticatedUser());

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy(AuthEndpoints.RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = requestsPerMinute, Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }
}
