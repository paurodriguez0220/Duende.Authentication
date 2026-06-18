# DuendeAuth

A self-hosted OpenID Connect / OAuth 2.0 identity server built on [Duende IdentityServer](https://duendesoftware.com/products/identityserver) and ASP.NET Core Identity, used as the shared authentication authority for all personal projects.

---

## Getting Started

**Prerequisites:**

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- (Optional) PostgreSQL or SQL Server for production deployments — SQLite works out of the box for local dev

**Clone and first-time setup:**

```bash
git clone <repo-url>
cd duende-auth
```

`appsettings.Development.json` is gitignored. Create it before running — the app will throw on startup without it:

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

**Run:**

```bash
dotnet run --project src/DuendeAuth --launch-profile https
```

**Verify the server is up:**

```bash
curl https://localhost:5001/.well-known/openid-configuration
```

**Explore the API interactively:**

Open `https://localhost:5001/scalar/v1` in your browser. Authenticate using the `admin-client` / `dev-admin-secret` credentials to access the admin endpoints.

---

## Commands

| Command | What it does |
| --- | --- |
| `dotnet build duende.sln` | Build the solution |
| `dotnet run --project src/DuendeAuth --launch-profile https` | Start the auth server on https://localhost:5001 |
| `dotnet test duende.sln` | Run all unit tests |
| `curl https://localhost:5001/.well-known/openid-configuration` | Verify OIDC discovery endpoint is healthy |
| `cd tests/e2e && npx playwright test` | Run Playwright end-to-end tests |

---

## Architecture

DuendeAuth is a standalone auth server that runs once and is shared by all personal projects. Client apps register in `Config.cs` and point their JWT authority at this server's URL (`https://localhost:5001` locally, or the deployed Azure App Service URL in production).

### Design patterns

**Options pattern** — All secrets and provider settings are read from `IConfiguration` using strongly typed keys in `Common/Constants/ConfigKeys.cs`. No inline strings appear in application code.

**Strategy + Factory Method (`DbContextOptionsFactory`)** — A single extension method `UseConfiguredProvider` reads the `Database:Provider` config key at runtime and selects the appropriate EF Core provider (SQLite, PostgreSQL, or SQL Server). Adding a new database backend requires only one new `case` in the switch — `Program.cs` and all three `DbContext` registrations remain unchanged.

**Minimal API with endpoint grouping** — All admin routes live in `AdminEndpoints.cs` and are registered as a single call (`app.MapAdminEndpoints()`). Routes are grouped by resource (`/api/v1/users`, `/api/v1/clients`), with shared middleware (authorization policy, rate limiter) applied at the group level.

**Middleware pipeline** — Custom security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`) and structured request logging via Serilog are added as middleware in `Program.cs`.

**Seed pattern** — `SeedData.InitializeAsync` runs on startup to create database schemas and seed the admin user and OIDC configuration idempotently (checks before inserting).

**Cursor-based pagination** — The `GET /api/v1/users` endpoint uses opaque Base64 cursors rather than page offsets, making pagination stable under concurrent inserts.

### Key components

| Component | Location | Purpose |
| --- | --- | --- |
| `Program.cs` | `src/DuendeAuth/` | App composition root — service registration and middleware pipeline |
| `Config.cs` | `src/DuendeAuth/` | In-memory client, scope, and resource registration |
| `DbContextOptionsFactory.cs` | `Infrastructure/` | Strategy/Factory for database provider selection |
| `AdminEndpoints.cs` | `Admin/` | Minimal API handlers for users, claims, and clients |
| `SeedData.cs` | `Data/` | Startup seeder for schemas, OIDC config, and admin user |
| `Common/Constants/` | `Common/Constants/` | Named constants — no magic strings anywhere in the codebase |
| `infra/duende/` | `infra/duende/` | Azure Bicep templates (App Service, PostgreSQL, Key Vault, App Insights) |
| `tests/e2e/` | `tests/e2e/` | Playwright end-to-end tests covering auth, users, and clients |

### External dependencies

| Package | Purpose |
| --- | --- |
| `Duende.IdentityServer.AspNetIdentity` | OIDC/OAuth 2.0 protocol implementation |
| `Duende.IdentityServer.EntityFramework` | Persisted configuration and operational stores |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | User and role management |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL EF Core provider |
| `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server EF Core provider |
| `Microsoft.EntityFrameworkCore.Sqlite` | SQLite EF Core provider (default for dev) |
| `Serilog.AspNetCore` | Structured logging with compact JSON formatter |
| `Scalar.AspNetCore` | Interactive API reference UI at `/scalar/v1` |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT validation for the admin API |

### Signing credentials

In development, `AddDeveloperSigningCredential()` writes a signing key to `tempkey.jwk` (no database or license required). In production, Duende's automatic key management stores and rotates keys via the operational store.

### Three separate databases

Three `DbContext` classes map to three databases (or three schemas/databases in Postgres/SQL Server):

| Database | Purpose |
| --- | --- |
| `IdentityConnection` | ASP.NET Core Identity — users and roles |
| `GrantsConnection` | Duende operational store — persisted grants and device codes |
| `ConfigConnection` | Duende configuration store — clients, scopes, and resources |

Keeping them separate ensures `EnsureCreated()` works correctly for each context in SQLite, and maps cleanly to separate databases in PostgreSQL production.

---

## Configuration

Secrets go in `appsettings.Development.json` (gitignored) or environment variables. Never commit secrets to `appsettings.json`.

| Key | Default | Description |
| --- | --- | --- |
| `Database:Provider` | `sqlite` | Database provider: `sqlite`, `postgres`, or `sqlserver` |
| `ConnectionStrings:IdentityConnection` | `Data Source=duende-identity.db` | Connection string for ASP.NET Core Identity tables |
| `ConnectionStrings:GrantsConnection` | `Data Source=duende-grants.db` | Connection string for Duende operational grant tables |
| `ConnectionStrings:ConfigConnection` | `Data Source=duende-config.db` | Connection string for Duende configuration tables |
| `Auth:Authority` | `https://localhost:5001` | OIDC authority URL used for JWT validation in the admin API |
| `Clients:ScalarClient:Secret` | *(required)* | Client secret for the ScalarApi OAuth2 client |
| `Clients:AdminClient:Secret` | *(required)* | Client secret for the admin management OAuth2 client |
| `Clients:WatcherClient:Secret` | *(required)* | Client secret for the read-only watcher OAuth2 client |
| `SeedUsers:AdminPassword` | *(required)* | Password for the seeded admin user (created on first startup) |

**PostgreSQL example (`appsettings.json`):**

```json
{
  "Database": { "Provider": "postgres" },
  "ConnectionStrings": {
    "IdentityConnection": "Host=...;Database=duende-identity;Username=...;Password=...;SslMode=Require;",
    "GrantsConnection":   "Host=...;Database=duende-grants;Username=...;Password=...;SslMode=Require;",
    "ConfigConnection":   "Host=...;Database=duende-config;Username=...;Password=...;SslMode=Require;"
  }
}
```

Credentials always go in `appsettings.Development.json` or environment variables — never in the committed `appsettings.json`.

### Registering a new client app

1. Add a `Client` entry in `src/DuendeAuth/Config.cs` → `Config.GetClients()`
2. Add its secret to `appsettings.Development.json` (gitignored) and expose it via Key Vault in production
3. In the new app, point `Auth:Authority` at `https://localhost:5001` (dev) or the deployed URL (prod)

### Admin API

All endpoints require a JWT with the `duende-manage` scope. Obtain a token via the `admin-client` OAuth2 client using the Client Credentials flow.

| Method | Route | Scope required | Description |
| --- | --- | --- | --- |
| `GET` | `/api/v1/users` | `duende-read` or `duende-manage` | List users (cursor-paginated) |
| `POST` | `/api/v1/users` | `duende-manage` | Create a user |
| `DELETE` | `/api/v1/users/{id}` | `duende-manage` | Delete a user |
| `GET` | `/api/v1/users/{id}/claims` | `duende-read` or `duende-manage` | List a user's custom claims |
| `POST` | `/api/v1/users/{id}/claims` | `duende-manage` | Add a claim to a user |
| `DELETE` | `/api/v1/users/{id}/claims/{type}` | `duende-manage` | Remove a claim by type |
| `GET` | `/api/v1/clients` | `duende-read` or `duende-manage` | List registered OAuth2 clients |
| `POST` | `/api/v1/clients` | `duende-manage` | Register a new OAuth2 client |
| `DELETE` | `/api/v1/clients/{clientId}` | `duende-manage` | Delete a client |

Rate limit: 100 requests per minute per client (fixed window). Exceeding the limit returns HTTP 429.

---

## Links

- [Duende IdentityServer docs](https://docs.duendesoftware.com/identityserver/v7)
- [ASP.NET Core Identity docs](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [Scalar API reference](https://scalar.com)
- [Standards](https://github.com/paurodriguez0220/standards)

---
*Maintained by paurodriguez0220 · Last updated: 2026-06-18*
*Standards: https://github.com/paurodriguez0220/standards-docs*
