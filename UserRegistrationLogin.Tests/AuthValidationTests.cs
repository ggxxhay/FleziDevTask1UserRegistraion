using Microsoft.AspNetCore.Identity;
using UserRegistrationLogin.Models;
using Xunit;

namespace UserRegistrationLogin.Tests;

public sealed class AuthValidationTests
{
    [Fact]
    public void Valid_registration_credentials_are_accepted_and_email_is_normalized()
    {
        var errors = AuthValidation.ValidateCredentials("  Alice@Example.com ", "Secret123");

        Assert.Empty(errors);
        Assert.Equal("alice@example.com", AuthValidation.NormalizeEmail("  Alice@Example.com "));
    }

    [Theory]
    [InlineData("short1A", "at least 8 characters")]
    [InlineData("passwordonly", "at least one letter and one number")]
    [InlineData("12345678", "at least one letter and one number")]
    public void Registration_rejects_passwords_that_break_password_rules(string password, string expectedMessage)
    {
        var errors = AuthValidation.ValidateCredentials("alice@example.com", password);

        Assert.Contains("password", errors.Keys);
        Assert.Contains(expectedMessage, errors["password"][0]);
    }

    [Fact]
    public void Registration_accepts_password_at_minimum_length()
    {
        var errors = AuthValidation.ValidateCredentials("alice@example.com", "A1234567");

        Assert.Empty(errors);
    }

    [Fact]
    public void Registration_accepts_password_at_maximum_length()
    {
        var password = new string('A', 127) + "1";

        var errors = AuthValidation.ValidateCredentials("alice@example.com", password);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Registration_rejects_missing_or_invalid_email(string? email)
    {
        var errors = AuthValidation.ValidateCredentials(email, "Secret123");

        Assert.Contains("email", errors.Keys);
    }

    [Fact]
    public void Registration_rejects_email_longer_than_254_characters()
    {
        var email = new string('a', 243) + "@example.com";

        var errors = AuthValidation.ValidateCredentials(email, "Secret123");

        Assert.Contains("email", errors.Keys);
        Assert.Contains("valid email", errors["email"][0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Registration_accepts_email_at_maximum_length()
    {
        var domain = string.Join(".", new string('a', 63), new string('b', 63), new string('c', 63), new string('d', 60));
        var email = $"a@{domain}";

        Assert.Equal(254, email.Length);
        Assert.Empty(AuthValidation.ValidateCredentials(email, "Secret123"));
    }

    [Fact]
    public void Registration_rejects_password_longer_than_128_characters()
    {
        var password = new string('A', 128) + "1";

        var errors = AuthValidation.ValidateCredentials("alice@example.com", password);

        Assert.Contains("password", errors.Keys);
        Assert.Contains("no more than 128 characters", errors["password"][0]);
    }

    [Fact]
    public void Login_requires_password_but_does_not_apply_registration_complexity_rules()
    {
        var errors = AuthValidation.ValidateCredentials("alice@example.com", "simple", validatePasswordRules: false);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Login_rejects_missing_password(string? password)
    {
        var errors = AuthValidation.ValidateCredentials("alice@example.com", password, validatePasswordRules: false);

        Assert.Contains("password", errors.Keys);
    }

    [Fact]
    public void Password_hash_verifies_correct_password_and_rejects_wrong_password()
    {
        var user = new UserRecord(Guid.NewGuid(), "alice@example.com", string.Empty, DateTimeOffset.UtcNow);
        var hasher = new PasswordHasher<UserRecord>();
        var hash = hasher.HashPassword(user, "Secret123");

        Assert.NotEqual("Secret123", hash);
        Assert.Equal(PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(user, hash, "Secret123"));
        Assert.Equal(PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(user, hash, "WrongPass456"));
        Assert.NotEqual(hash, hasher.HashPassword(user, "Secret123"));
    }
}
