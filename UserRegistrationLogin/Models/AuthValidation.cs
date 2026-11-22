using System.ComponentModel.DataAnnotations;

namespace UserRegistrationLogin.Models;

public static class AuthValidation
{
    private const int MinimumPasswordLength = 8;
    private const int MaximumPasswordLength = 128;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static Dictionary<string, string[]> ValidateCredentials(
        string? email,
        string? password,
        bool validatePasswordRules = true)
    {
        var errors = new Dictionary<string, string[]>();
        var emailError = ValidateEmail(email);
        if (emailError is not null)
            errors["email"] = [emailError];

        var passwordError = ValidatePassword(password, validatePasswordRules);
        if (passwordError is not null)
            errors["password"] = [passwordError];

        return errors;
    }

    private static string? ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "Email is required.";
        var normalized = email.Trim();
        if (normalized.Length > 254 || !new EmailAddressAttribute().IsValid(normalized))
            return "Enter a valid email address.";
        return null;
    }

    private static string? ValidatePassword(string? password, bool validateRules)
    {
        if (string.IsNullOrEmpty(password)) return "Password is required.";
        if (password.Length > MaximumPasswordLength)
            return $"Password must be no more than {MaximumPasswordLength} characters.";
        if (!validateRules) return null;
        if (password.Length < MinimumPasswordLength)
            return $"Password must be at least {MinimumPasswordLength} characters.";
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return "Password must contain at least one letter and one number.";
        return null;
    }
}
