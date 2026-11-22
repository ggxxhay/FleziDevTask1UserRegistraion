# User Registration & Login

A small full-stack authentication exercise: plain HTML/CSS/JavaScript in the browser, ASP.NET Core Minimal APIs on .NET 10, and a JSON file for local persistence.

## Requirements

- .NET 10 SDK
- No database server or JavaScript package install is needed.

## Run locally

From this folder:

```powershell
dotnet run
```

Open the HTTPS or HTTP address printed by ASP.NET Core. The app serves the sign-in/create-account screen at `/`. After sign-in, `/dashboard` is protected by the authentication cookie.

## API

| Method | Path | Purpose | Success | Main errors |
| --- | --- | --- | --- | --- |
| POST | `/api/auth/register` | Create an account | `201` | `400`, `409` |
| POST | `/api/auth/login` | Sign in and issue an HttpOnly cookie | `200` | `400`, `401` |
| GET | `/api/auth/me` | Read the signed-in user's safe profile | `200` | `401` |
| POST | `/api/auth/logout` | Clear the authentication cookie | `204` | — |

The browser and API are same-origin. The login response includes the safe user representation and `expiresIn`; the signed, protected cookie is the session credential and is not exposed to JavaScript.

## Tests

The test project contains focused unit tests for credential validation, email normalization, password hash verification, and JSON persistence/duplicate protection. It also contains six lightweight API integration tests for the HTTP status codes, cookie session, generic login errors, and logout flow. Browser-driven E2E tests are not required for this exercise.

## Local JSON storage

Accounts are stored in `App_Data/users.json`, outside the public web root. The file is created when the first account is registered. Passwords are hashed with ASP.NET Core Identity's adaptive `PasswordHasher`; only hashes are persisted. Writes use a process-local lock and an atomic temporary-file replacement to avoid partial files during ordinary single-instance use.

This JSON repository is intended for the exercise and local demo. It is not suitable for multiple app instances or concurrent multi-process writes; a relational database can replace `IUserRepository` later without changing the endpoint contract.

## Security notes

- Emails are trimmed and lowercased before lookup/persistence.
- Login failures use a generic `Invalid email or password` message.
- The authentication cookie is HttpOnly, SameSite Strict, and expires after one hour.
- No password or token is written to application logs or browser local storage.
- Repeated-login rate limiting is left as a documented design consideration for this exercise.
- Use sample accounts only. Do not commit real credentials or the generated `App_Data/users.json` file.
