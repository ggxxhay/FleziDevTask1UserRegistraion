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

## Project notes

- `docs/User_Registration_Login_Requirements.md` summarizes the selected task requirements.
- `docs/IMPLEMENTATION_ESTIMATE.md` contains the requested Fibonacci estimate and rationale.
- `UserRegistrationLogin/README.md` describes the API and local JSON storage.

The JSON repository is suitable for a single-instance learning exercise. Replace it with a database if the app needs multi-instance deployment or reliable concurrent writes. Use sample accounts only; local account data and Data Protection keys are excluded from Git.