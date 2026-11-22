using System.Text.Json;
using UserRegistrationLogin.Models;

namespace UserRegistrationLogin.Data;

public sealed class JsonUserRepository : IUserRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonUserRepository(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "users.json");
    }

    public async Task<UserRecord?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var users = await ReadUsersAsync(cancellationToken);
            return users.FirstOrDefault(user => string.Equals(user.Email, normalizedEmail, StringComparison.Ordinal));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> TryAddAsync(UserRecord user, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var users = await ReadUsersAsync(cancellationToken);
            if (users.Any(existing => string.Equals(existing.Email, user.Email, StringComparison.Ordinal)))
                return false;

            users.Add(user);
            await WriteUsersAtomicallyAsync(users, cancellationToken);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<UserRecord>> ReadUsersAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath)) return [];

        await using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await JsonSerializer.DeserializeAsync<List<UserRecord>>(stream, JsonOptions, cancellationToken) ?? [];
    }

    private async Task WriteUsersAtomicallyAsync(List<UserRecord> users, CancellationToken cancellationToken)
    {
        var temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, users, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
