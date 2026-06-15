# CLAUDE.md

## What this is

Two independent apps in one repo:

| App | Solution | Purpose |
|-----|----------|---------|
| **DuendeAuth** | `duende.sln` | Reusable auth server — run once, shared by all personal projects |
| **ScalarApi** | `ScalarApi.sln` | Sample client app showing how to connect to DuendeAuth |

DuendeAuth is the long-lived service. Every new personal project registers as a client in `Config.cs` and points its JWT authority at DuendeAuth's URL.

## Structure

```
src/
  DuendeAuth/              Standalone auth server (duende.sln)
    Admin/                 Admin API endpoints and models
      Models/              DTOs and request records
      AdminEndpoints.cs    Minimal API handlers (users, claims, clients)
    Common/Constants/      Named constants (no magic strings)
    Infrastructure/
      DbContextOptionsFactory.cs   DB provider abstraction
    Data/
      ApplicationDbContext.cs
      SeedData.cs
    Config.cs              In-memory client/scope/resource registration
    Program.cs

  ScalarApi/               Sample protected API (ScalarApi.sln)
    Features/Forecasts/    Vertical slice example
    Common/                Cross-cutting concerns
    Infrastructure/

  ScalarApi.Tests/         Tests for ScalarApi
```

## Commands

```bash
# Build auth server only
dotnet build duende.sln

# Build sample app only
dotnet build ScalarApi.sln

# Run auth server (start this first — other apps depend on it)
dotnet run --project src/DuendeAuth --launch-profile https

# Run sample API
dotnet run --project src/ScalarApi --launch-profile https

# Run tests
dotnet test ScalarApi.sln

# Verify auth server is healthy
curl https://localhost:5001/.well-known/openid-configuration
```

## Local setup (first time)

`appsettings.Development.json` files are gitignored. Create before running:

**`src/DuendeAuth/appsettings.Development.json`**
```json
{
  "Clients": {
    "ScalarClient": { "Secret": "dev-secret" },
    "AdminClient": { "Secret": "dev-admin-secret" },
    "WatcherClient": { "Secret": "dev-watcher-secret" }
  },
  "SeedUsers": { "AdminPassword": "Admin1234!" }
}
```

## Switching databases (DuendeAuth)

Set `Database:Provider` in config — the connection strings change but the code does not.

| Provider | Value |
|----------|-------|
| SQLite (default) | `"sqlite"` |
| PostgreSQL | `"postgres"` |
| SQL Server | `"sqlserver"` |

**Postgres example (`appsettings.json`):**
```json
{
  "Database": { "Provider": "postgres" },
  "ConnectionStrings": {
    "IdentityConnection": "Host=localhost;Database=duende_identity;Username=...;Password=...",
    "GrantsConnection":   "Host=localhost;Database=duende_grants;Username=...;Password=..."
  }
}
```

Put credentials in `appsettings.Development.json` (gitignored), never in `appsettings.json`.

## Registering a new client app

1. Add a `Client` entry to `src/DuendeAuth/Config.cs` → `GetClients()`
2. Add the client secret to `appsettings.Development.json`
3. In the new app, set `Auth:Authority` to `https://localhost:5001` (or the deployed URL)

## Conventions

Standards at `C:\Users\paulo.rodriguez\Paulo\standards` take precedence.
- API routes versioned: `/api/v1/...`
- Errors: `{ "error": { "code": "...", "message": "...", "details": [] } }`
- Named constants in `Common/Constants/` — no magic strings
- Commits: Conventional Commits — `feat(scope): description`

## Never do

- Hardcode secrets — use config or environment variables
- Commit `appsettings.Development.json` or `appsettings.Local.json`
- Push directly to `main`
- Add magic strings inline — use constants

---
*Maintained by paurodriguez0220 · Last updated: 2026-06-15*
*Standards: https://github.com/paurodriguez0220/standards*
