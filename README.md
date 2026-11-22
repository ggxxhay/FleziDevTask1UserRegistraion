# Flezi Dev Auth — User Registration & Login

A small full-stack authentication exercise built with plain HTML/CSS/JavaScript, ASP.NET Core Minimal APIs on .NET 10, and a local JSON file. This repository contains the application, focused unit/API tests, task requirements summary, and Fibonacci estimate.

## Stack

- Frontend: plain HTML, CSS, and JavaScript
- Backend: ASP.NET Core Minimal APIs on .NET 10
- Local persistence: one JSON file; no database server required
- Authentication: ASP.NET Core Identity password hashing and an HttpOnly protected cookie

## Run

Install the .NET 10 SDK, then run from the repository root:

```powershell
dotnet run --project UserRegistrationLogin/UserRegistrationLogin.csproj
```

Use the local URL printed by the app. Register with a sample email and a password of at least 8 characters containing a letter and a number.

## Tests

Run the unit and API integration tests with:

```powershell
dotnet test UserRegistrationLogin.Tests/UserRegistrationLogin.Tests.csproj
```

## Fibonacci estimate

**Recommended story estimate: 8 points** (`1, 2, 3, 5, 8, 13, ...`).

| Dimension | Rating | Rationale |
| --- | ---: | --- |
| Complexity | 8 | Covers browser UI, HTTP endpoints, validation, password hashing, cookie sessions, and persistence. |
| Effort | 8 | Includes registration and login flows, error handling, a protected page, repository code, and acceptance tests. |
| Risk | 5 | Authentication has security-sensitive failure modes, and JSON storage has concurrency/deployment limits. |
| Uncertainty | 5 | Framework/security behavior needs validation; the task leaves storage and endpoint design open. |

The ratings describe separate dimensions and are not added together. The overall estimate is 8 because this is a bounded authentication slice with standard framework support, but it still involves meaningful security and integration work. The estimate assumes a single-instance learning app without password recovery, email verification, MFA, or production deployment. See [`docs/IMPLEMENTATION_ESTIMATE.md`](docs/IMPLEMENTATION_ESTIMATE.md) for the full assumptions.

## Project notes

- `docs/User_Registration_Login_Requirements.md` summarizes the selected task requirements.
- `docs/IMPLEMENTATION_ESTIMATE.md` contains the requested Fibonacci estimate and rationale.
- `UserRegistrationLogin/README.md` describes the API and local JSON storage.

The JSON repository is suitable for a single-instance learning exercise. Replace it with a database if the app needs multi-instance deployment or reliable concurrent writes. Use sample accounts only; local account data and Data Protection keys are excluded from Git.
