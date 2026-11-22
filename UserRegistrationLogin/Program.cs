using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using UserRegistrationLogin.Data;
using UserRegistrationLogin.Models;

var builder = WebApplication.CreateBuilder(args);

var dataProtectionKeyDirectory = builder.Configuration["Authentication:KeyDirectory"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeyDirectory))
{
    Directory.CreateDirectory(dataProtectionKeyDirectory);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyDirectory));
}

builder.Services.AddSingleton<IUserRepository, JsonUserRepository>();
builder.Services.AddSingleton<IPasswordHasher<UserRecord>, PasswordHasher<UserRecord>>();
var passwordHasherForTiming = new PasswordHasher<UserRecord>();
var timingUser = new UserRecord(Guid.Empty, "nobody@example.invalid", string.Empty, DateTimeOffset.UnixEpoch);
builder.Services.AddSingleton(new DummyPasswordCredential(
    timingUser,
    passwordHasherForTiming.HashPassword(timingUser, "DummyPasswordForTiming123!")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Flezi.Auth";
        options.LoginPath = "/";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", (ClaimsPrincipal principal, IWebHostEnvironment environment) =>
    principal.Identity?.IsAuthenticated == true
        ? Results.Redirect("/dashboard")
        : Results.File(Path.Combine(environment.WebRootPath, "index.html"), "text/html"));

app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    IUserRepository users,
    IPasswordHasher<UserRecord> passwordHasher,
    CancellationToken cancellationToken) =>
{
    var errors = AuthValidation.ValidateCredentials(request.Email, request.Password);
    if (errors.Count > 0)
        return Results.BadRequest(ApiError.Validation(errors));

    var normalizedEmail = AuthValidation.NormalizeEmail(request.Email!);
    var user = new UserRecord(Guid.NewGuid(), normalizedEmail, string.Empty, DateTimeOffset.UtcNow);
    var passwordHash = passwordHasher.HashPassword(user, request.Password!);
    user = user with { PasswordHash = passwordHash };

    if (!await users.TryAddAsync(user, cancellationToken))
        return Results.Conflict(ApiError.General("email_exists", "An account with this email already exists."));

    return Results.Created("/api/auth/me", UserResponse.From(user));
});

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    HttpContext httpContext,
    IUserRepository users,
    IPasswordHasher<UserRecord> passwordHasher,
    DummyPasswordCredential dummyCredential,
    CancellationToken cancellationToken) =>
{
    var errors = AuthValidation.ValidateCredentials(request.Email, request.Password, validatePasswordRules: false);
    if (errors.Count > 0)
        return Results.BadRequest(ApiError.Validation(errors));

    var user = await users.FindByEmailAsync(AuthValidation.NormalizeEmail(request.Email!), cancellationToken);
    var credentialToVerify = user ?? dummyCredential.User;
    var hashToVerify = user?.PasswordHash ?? dummyCredential.PasswordHash;
    var verification = passwordHasher.VerifyHashedPassword(credentialToVerify, hashToVerify, request.Password!);
    if (user is null || verification == PasswordVerificationResult.Failed)
        return Results.Json(ApiError.General("invalid_credentials", "Invalid email or password."), statusCode: StatusCodes.Status401Unauthorized);

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Email)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity),
        new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });

    return Results.Ok(new LoginResponse(
        new UserResponse(user.Id, user.Email, user.CreatedAt),
        (int)TimeSpan.FromHours(1).TotalSeconds));
});

app.MapGet("/api/auth/me", (ClaimsPrincipal principal) =>
{
    var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = principal.FindFirstValue(ClaimTypes.Email);
    if (!Guid.TryParse(idValue, out var id) || string.IsNullOrWhiteSpace(email))
        return Results.Unauthorized();

    return Results.Ok(new AuthenticatedUserResponse(id, email));
}).RequireAuthorization();

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
});

app.MapGet("/dashboard", (IWebHostEnvironment environment) =>
    Results.File(Path.Combine(environment.ContentRootPath, "Pages", "dashboard.html"), "text/html"))
    .RequireAuthorization();

app.Run();

public partial class Program;

public sealed record DummyPasswordCredential(UserRecord User, string PasswordHash);
