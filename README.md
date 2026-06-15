# DuendeAuth

A self-hosted OpenID Connect / OAuth 2.0 auth server built on [Duende IdentityServer](https://duendesoftware.com/products/identityserver). Designed to run once and be reused across all personal projects.

---

## Getting Started

**Prerequisites:**
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- (Optional) PostgreSQL or SQL Server for production deployments

**Clone and run:**

```bash
git clone <repo-url>
cd duende
```

Create `src/DuendeAuth/appsettings.Development.json` before running (this file is gitignored — never commit it):

```json
{
  "Clients": {
    "ScalarClient": { "Secret": "dev-secret" },
    "AdminClient": { "Secret": "dev-admin-secret" }
  },
  "SeedUsers": { "AdminPassword": "Admin1234!" }
}
```

```bash
dotnet run --project src/DuendeAuth --launch-profile https
```

Verify the server is up:

```bash
curl https://localhost:5001/.well-known/openid-configuration
```

---

## Commands

| Command | What it does |
| --- | --- |
| `dotnet build duende.sln` | Build the auth server |
| `dotnet run --project src/DuendeAuth --launch-profile https` | Start on https://localhost:5001 |
| `curl https://localhost:5001/.well-known/openid-configuration` | Verify the server is healthy |

---

## Architecture

DuendeAuth is a standalone auth server that handles authentication and authorization for all personal projects. Client apps register in `Config.cs` and point their JWT authority at this server's URL.

Key components:
- **ASP.NET Core Identity** — user management (stored in SQLite by default, Postgres or SQL Server in production)
- **Duende IdentityServer** — OIDC and OAuth 2.0 endpoints
- **DbContextOptionsFactory** — Factory Method + Strategy pattern for switching database providers via config

---

## Configuration

Secrets go in `appsettings.Development.json` (gitignored) or environment variables — never in `appsettings.json`.

| Key | Source | Description |
| --- | --- | --- |
| `Database:Provider` | `appsettings.json` | Database provider: `sqlite` (default), `postgres`, `sqlserver` |
| `ConnectionStrings:IdentityConnection` | `appsettings.json` | Connection string for ASP.NET Core Identity tables |
| `ConnectionStrings:GrantsConnection` | `appsettings.json` | Connection string for Duende operational grants tables |
| `Clients:ScalarClient:Secret` | `appsettings.Development.json` | Client secret for the ScalarApi OAuth2 client |
| `Clients:AdminClient:Secret` | `appsettings.Development.json` | Client secret for the admin API OAuth2 client |
| `SeedUsers:AdminPassword` | `appsettings.Development.json` | Password for the seeded admin user |

---

## Architecture decisions

### Why Duende IdentityServer?

Rolling your own auth is one of the most reliable ways to introduce security vulnerabilities. Duende implements the OpenID Connect and OAuth 2.0 specs correctly, is actively maintained, and is the direct successor to the widely-used IdentityServer4. For personal/non-commercial use the community license is free.

---

### Why is the DB provider abstracted? (`DbContextOptionsFactory`)

**The problem it solves:**

DuendeAuth is meant to be reused across different environments:
- Local dev → SQLite (zero setup, file-based)
- Shared server / production → Postgres or SQL Server (proper concurrent access, backups, etc.)

Without abstraction, switching databases means editing `Program.cs` every time — coupling infrastructure decisions to application code.

**The pattern used:**

`DbContextOptionsFactory` combines two patterns:

- **Factory Method** — a single method (`UseConfiguredProvider`) is responsible for constructing the right `DbContextOptionsBuilder` based on input. The caller never knows which provider it gets.
- **Strategy** — the `Database:Provider` config key selects the algorithm (which EF Core provider to wire up) at runtime. Adding a new provider means adding one `case` to the switch — nothing else changes.

**What this looks like in practice:**

```csharp
// Program.cs — caller knows nothing about SQLite, Postgres, or SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseConfiguredProvider(config, "IdentityConnection"));
```

```csharp
// DbContextOptionsFactory.cs — all provider knowledge lives here
return provider switch
{
    "postgres"  => builder.UseNpgsql(connStr, ...),
    "sqlserver" => builder.UseSqlServer(connStr, ...),
    _           => builder.UseSqlite(connStr, ...)
};
```

**To switch databases you only touch config, never code:**

```json
{ "Database": { "Provider": "postgres" } }
```

**Why not just use environment-specific `appsettings` files?**

You could put `UseNpgsql(...)` directly in `Program.cs` and swap it per environment. But then the code has a direct dependency on a specific provider — anyone reading it has to know that SQLite is for dev and Postgres for prod, and any new environment requires a code change and a redeploy. The factory keeps that concern in one place.

---

### Why two separate SQLite files (`duende-identity.db` and `duende-grants.db`)?

EF Core's `EnsureCreated()` only creates a database if it does not exist yet. When two `DbContext` classes share the same file, the first one to run creates the file and its own tables — the second one sees the file already exists and does nothing, leaving its tables missing.

Separate files mean each `DbContext` owns its database file and `EnsureCreated()` works correctly for both.

> For production use with Postgres or SQL Server, use proper EF Core migrations (`dotnet ef migrations add`) and separate schemas or databases per context.

---

### Why `AddDeveloperSigningCredential()`?

Duende's automatic key management stores signing keys in the `Keys` table (in the grants database). This feature requires a paid Business or Enterprise license.

`AddDeveloperSigningCredential()` instead writes a signing key to a local `tempkey.jwk` file and reads it back on restart. It requires no database table, no license, and is appropriate for development and personal use. It is **not suitable for production** because the key is not shared across multiple instances.

---

## Admin API

DuendeAuth ships with a built-in admin API at `/admin`. All endpoints require a valid JWT with the `duende-admin` scope, obtained via the `admin-client` OAuth2 client.

**Try it:** Start the server and open `https://localhost:5001/scalar/v1`. Authenticate using `admin-client` / `dev-admin-secret`, then the admin endpoints are available under the Admin tag.

| Method | Route | Description |
|---|---|---|
| `GET` | `/admin/users` | List all users |
| `POST` | `/admin/users` | Create a user (body: `{ "userName", "email", "password" }`) |
| `DELETE` | `/admin/users/{id}` | Delete a user |
| `GET` | `/admin/users/{id}/claims` | List a user's custom claims |
| `POST` | `/admin/users/{id}/claims` | Add a claim (body: `{ "type", "value" }`) |
| `DELETE` | `/admin/users/{id}/claims/{type}` | Remove a claim by type |
| `GET` | `/admin/clients` | List registered OAuth2 clients |

---

## Registering a new client app

1. Add a `Client` entry in `src/DuendeAuth/Config.cs` → `GetClients()`
2. Add its secret to `appsettings.Development.json` (gitignored)
3. In the new app, point `Auth:Authority` at `https://localhost:5001`

---

## Switching databases

| Provider | `Database:Provider` value |
|---|---|
| SQLite (default, dev) | `"sqlite"` |
| PostgreSQL | `"postgres"` |
| SQL Server | `"sqlserver"` |

Connection strings and provider value go in `appsettings.json`. Credentials go in `appsettings.Development.json` (gitignored) or environment variables — never hardcoded.

---

## Links

- [Duende IdentityServer docs](https://docs.duendesoftware.com/identityserver/v7)
- [Standards](https://github.com/paurodriguez0220/standards)

---
*Maintained by paurodriguez0220 · Last updated: 2026-06-15*
*Standards: https://github.com/paurodriguez0220/standards*
