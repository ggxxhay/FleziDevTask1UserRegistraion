using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using UserRegistrationLogin.Data;
using UserRegistrationLogin.Models;
using Xunit;

namespace UserRegistrationLogin.Tests;

public sealed class JsonUserRepositoryTests
{
    [Fact]
    public async Task Added_user_is_persisted_and_can_be_read_by_a_new_repository()
    {
        var root = CreateTestDirectory();
        var user = CreateUser();
        var firstRepository = new JsonUserRepository(CreateEnvironment(root));

        Assert.True(await firstRepository.TryAddAsync(user, CancellationToken.None));

        var persistedJson = await File.ReadAllTextAsync(Path.Combine(root, "App_Data", "users.json"));
        var secondRepository = new JsonUserRepository(CreateEnvironment(root));
        var loaded = await secondRepository.FindByEmailAsync(user.Email, CancellationToken.None);

        Assert.DoesNotContain("Secret123", persistedJson, StringComparison.Ordinal);
        Assert.Equal(user, loaded);
    }

    [Fact]
    public async Task Duplicate_email_is_rejected_without_adding_a_second_user()
    {
        var root = CreateTestDirectory();
        var repository = new JsonUserRepository(CreateEnvironment(root));
        var user = CreateUser();

        Assert.True(await repository.TryAddAsync(user, CancellationToken.None));
        Assert.False(await repository.TryAddAsync(user with { Id = Guid.NewGuid() }, CancellationToken.None));

        var persistedJson = await File.ReadAllTextAsync(Path.Combine(root, "App_Data", "users.json"));
        var savedUsers = System.Text.Json.JsonSerializer.Deserialize<List<UserRecord>>(
            persistedJson,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Single(savedUsers!);
    }

    [Fact]
    public async Task Concurrent_adds_of_same_email_create_only_one_record()
    {
        var root = CreateTestDirectory();
        var repository = new JsonUserRepository(CreateEnvironment(root));
        var first = CreateUser();
        var second = first with { Id = Guid.NewGuid() };

        var results = await Task.WhenAll(
            repository.TryAddAsync(first, CancellationToken.None),
            repository.TryAddAsync(second, CancellationToken.None));

        Assert.Single(results, added => added);
        Assert.Single(results, added => !added);
    }

    private static string CreateTestDirectory()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "UnitTestData", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static UserRecord CreateUser() =>
        new(Guid.NewGuid(), "alice@example.com", "hashed-test-password", DateTimeOffset.UtcNow);

    private static IWebHostEnvironment CreateEnvironment(string root) => new TestEnvironment
    {
        ApplicationName = "UserRegistrationLogin.Tests",
        ContentRootPath = root,
        ContentRootFileProvider = new PhysicalFileProvider(root),
        EnvironmentName = "Development",
        WebRootPath = Path.Combine(root, "wwwroot"),
        WebRootFileProvider = new PhysicalFileProvider(root)
    };

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = string.Empty;
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
