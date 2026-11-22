using UserRegistrationLogin.Models;

namespace UserRegistrationLogin.Data;

public interface IUserRepository
{
    Task<UserRecord?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<bool> TryAddAsync(UserRecord user, CancellationToken cancellationToken);
}
