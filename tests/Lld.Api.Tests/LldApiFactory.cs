using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lld.Application.Abstractions;
using Lld.Application.Accounts;
using Lld.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lld.Api.Tests;

/// <summary>The real API on a throw-away SQLite file with a test signing key.</summary>
public class LldApiFactory : WebApplicationFactory<Program>
{
    public const string Password = "correct-horse-battery";

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), "lld-tests", $"{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:Path", _databasePath);
        builder.UseSetting("Jwt:SigningKey", "test-signing-key-that-is-long-enough-1234567890");
        builder.UseSetting("Auth:RateLimitPerMinute", "10000");
    }

    /// <summary>Client using the app transport (refresh token in the body).</summary>
    public HttpClient CreateAppClient() => CreateClient();

    /// <summary>Client using the browser transport (refresh token in an httpOnly, Secure cookie).</summary>
    public HttpClient CreateWebClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });
        client.DefaultRequestHeaders.Add(ClientHeaders.Client, ClientHeaders.WebClient);
        return client;
    }

    /// <summary>Creates an account through the application layer (as the admin console would).</summary>
    public async Task<IssuedCredentials> CreateUserAsync(string email, string role = Roles.Learner)
    {
        await using var scope = Services.CreateAsyncScope();
        var created = await scope.ServiceProvider.GetRequiredService<CreateUser>()
            .ExecuteAsync(new CreateUserCommand(email, null, role), CancellationToken.None);
        return created.Value;
    }

    /// <summary>Creates a user, signs in with the temporary password and sets <see cref="Password"/>.</summary>
    public async Task<AuthResponse> CreateActiveUserAsync(HttpClient client, string email)
    {
        var credentials = await CreateUserAsync(email);
        var first = await LoginAsync(client, email, credentials.TemporaryPassword);
        using var change = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password")
        {
            Content = JsonContent.Create(new ChangePasswordRequest(credentials.TemporaryPassword, Password)),
        };
        change.Headers.Authorization = new AuthenticationHeaderValue("Bearer", first.AccessToken);
        var response = await client.SendAsync(change);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public static async Task<AuthResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var file in new[] { _databasePath, $"{_databasePath}-wal", $"{_databasePath}-shm" })
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
                // Best effort: temp files are cleaned up by the OS eventually.
            }
        }
    }
}
