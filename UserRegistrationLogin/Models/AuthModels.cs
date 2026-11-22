namespace UserRegistrationLogin.Models;

public sealed record RegisterRequest(string? Email, string? Password);
public sealed record LoginRequest(string? Email, string? Password);
public sealed record UserRecord(Guid Id, string Email, string PasswordHash, DateTimeOffset CreatedAt);
public sealed record UserResponse(Guid Id, string Email, DateTimeOffset CreatedAt)
{
    public static UserResponse From(UserRecord user) => new(user.Id, user.Email, user.CreatedAt);
}
public sealed record AuthenticatedUserResponse(Guid Id, string Email);
public sealed record LoginResponse(UserResponse User, int ExpiresIn);
public sealed record ApiError(string Code, string Message, IReadOnlyDictionary<string, string[]>? Fields = null)
{
    public static ApiError General(string code, string message) => new(code, message);
    public static ApiError Validation(Dictionary<string, string[]> fields) =>
        new("validation_failed", "Please correct the highlighted fields.", fields);
}
