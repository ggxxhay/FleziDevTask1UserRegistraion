using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using UserRegistrationLogin.Data;
using UserRegistrationLogin.Models;
using Xunit;

namespace UserRegistrationLogin.Tests;

public sealed class AuthFlowTests
{
    [Fact]
    public async Task Registration_normalizes_email_hashes_password_and_does_not_return_hash()
    {
        using var factory = new TestAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "  Alice@Example.com ",
            password = "Secret123"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", content, StringComparison.OrdinalIgnoreCase);
        using var responseBody = JsonDocument.Parse(content);
        var responseUser = responseBody.RootElement;
        Assert.True(responseUser.TryGetProperty("id", out var responseId));
        Assert.NotEqual(Guid.Empty, responseId.GetGuid());
        Assert.Equal("alice@example.com", responseUser.GetProperty("email").GetString());
        Assert.True(responseUser.GetProperty("createdAt").GetDateTimeOffset() > DateTimeOffset.UnixEpoch);
        var stored = Assert.Single(factory.Users.Users);
        Assert.Equal("alice@example.com", stored.Email);
        Assert.NotEqual("Secret123", stored.PasswordHash);

        var hasher = new PasswordHasher<UserRecord>();
        Assert.Equal(PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(stored, stored.PasswordHash, "Secret123"));
    }

    [Fact]
    public async Task Duplicate_email_returns_conflict()
    {
        using var factory = new TestAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var request = new { email = "alice@example.com", password = "Secret123" };

        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", request)).StatusCode);
        var duplicate = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = " ALICE@example.com ",
            password = "OtherPass456"
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var duplicateBody = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
        Assert.Equal("email_exists", duplicateBody.RootElement.GetProperty("code").GetString());
        Assert.Contains("already exists", duplicateBody.RootElement.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Single(factory.Users.Users);
    }

    [Fact]
    public async Task Invalid_email_and_password_return_field_errors()
    {
        using var factory = new TestAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "not-an-email",
            password = "short"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var fields = body.RootElement.GetProperty("fields");
        Assert.True(fields.TryGetProperty("email", out _));
        Assert.True(fields.TryGetProperty("password", out _));
    }

    [Fact]
    public async Task Correct_login_sets_http_only_cookie_and_protects_profile()
    {
        using var factory = new TestAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client.PostAsJsonAsync("/api/auth/register", new { email = "alice@example.com", password = "Secret123" });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = " ALICE@example.com ", password = "Secret123" });

        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected login success but received {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(3600, body.RootElement.GetProperty("expiresIn").GetInt32());
        Assert.False(body.RootElement.TryGetProperty("token", out _));
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), value =>
            value.Contains("Flezi.Auth=", StringComparison.Ordinal) && value.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var profile = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        var signedInUser = await profile.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();
        Assert.Equal("alice@example.com", signedInUser?.Email);
    }

    [Fact]
    public async Task Wrong_password_returns_generic_unauthorized_message()
    {
        using var factory = new TestAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client.PostAsJsonAsync("/api/auth/register", new { email = "alice@example.com", password = "Secret123" });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "alice@example.com", password = "WrongPass456" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password", content, StringComparison.Ordinal);
        Assert.DoesNotContain("alice@example.com", content, StringComparison.Ordinal);

        var unknownEmailResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nobody@example.com",
            password = "WrongPass456"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmailResponse.StatusCode);
        Assert.Equal(content, await unknownEmailResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Logout_clears_the_session()
    {
        using var factory = new TestAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client.PostAsJsonAsync("/api/auth/register", new { email = "alice@example.com", password = "Secret123" });
        await client.PostAsJsonAsync("/api/auth/login", new { email = "alice@example.com", password = "Secret123" });

        var logout = await client.PostAsync("/api/auth/logout", content: null);
        var profile = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
    }

    private sealed class TestAppFactory : WebApplicationFactory<Program>
    {
        public InMemoryUserRepository Users { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.AddDataProtection().PersistKeysToFileSystem(
                    new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "DataProtectionKeys")));
                services.RemoveAll<IUserRepository>();
                services.AddSingleton<IUserRepository>(Users);
            });
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly List<UserRecord> _users = [];
        public IReadOnlyList<UserRecord> Users => _users;

        public Task<UserRecord?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(_users.FirstOrDefault(user => user.Email == normalizedEmail));

        public Task<bool> TryAddAsync(UserRecord user, CancellationToken cancellationToken)
        {
            if (_users.Any(existing => existing.Email == user.Email)) return Task.FromResult(false);
            _users.Add(user);
            return Task.FromResult(true);
        }
    }
}
