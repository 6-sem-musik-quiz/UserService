# UserService

Part of the **6-sem-musik-quiz** system. ASP.NET Core (.NET 10) Web API using MVC controllers and a layered architecture.

## Architecture

```
Api  ──►  Service  ──►  Repository
  └─────────┴──────────────┴──►  Domain
```

| Project | Responsibility |
|---|---|
| `src/UserService.Api` | Controllers, middleware, `Program.cs`. Thin: validate input, call a service, return HTTP result. |
| `src/UserService.Service` | Business logic. Talks to repositories through interfaces, returns DTOs. |
| `src/UserService.Repository` | Data access with EF Core (PostgreSQL). The only layer that knows about the database. |
| `src/UserService.Domain` | Entities and DTOs. No dependencies. |
| `tests/UserService.Service.Tests` | Unit tests for services (xUnit + NSubstitute). |
| `tests/UserService.Api.Tests` | Integration tests using `WebApplicationFactory`. |

`Example*` files are placeholders showing the pattern end to end. Replace them with real entities.

### Adding a new feature

1. Entity in `Domain/Entities`, request/response DTOs in `Domain/DTOs`
2. EF configuration in `Repository/Data/Configurations`, `DbSet` in `AppDbContext`
3. `I<Name>Repository` + implementation, register in `RepositoryLayerExtensions`
4. `I<Name>Service` + implementation + mappings, register in `ServiceLayerExtensions`
5. Controller in `Api/Controllers`
6. Tests

## Authentication — JWT

UserService issues HS256 tokens that all services can validate with the same parameters.

| Parameter | Value |
|---|---|
| Algorithm | HS256 |
| Issuer | `slopster-userservice` |
| Audience | `slopster` |
| Lifetime | 12 hours (configurable via `Jwt:LifetimeHours`) |
| Signing key | `Jwt:SigningKey` / env `Jwt__SigningKey` — min. 32 bytes, comes from OpenBao |

**Claims in every token**

| Claim | Content |
|---|---|
| `sub` | User ID (integer as string) |
| `unique_name` | User name |
| `role` | `Player` or `Admin` |

**Validating tokens in other services** — configure `JwtBearer` with issuer `slopster-userservice`, audience `slopster`, and the shared `Jwt__SigningKey` from OpenBao. The service will fail to start if the key is absent or shorter than 32 bytes.

## Running locally

Locally the API always runs against the **real** Postgres in the cluster
(`quiz-postgres.tail5a1bc5.ts.net`, Tailscale on). **This is live data.**
Migrations are turned off locally (`Database:MigrateOnStartup=false`), so a branch can never
change the shared schema; schema changes reach the database only through a deploy from `main`.

The password is a secret, so it is not in Git. Get it with
`bao kv get -field=userservice kv/quiz/postgres` (or ask Asger).

**Docker:** keep the password in a local `.env` file (in `.gitignore`). Compose stops with a
clear message if it is missing.

```powershell
Copy-Item .env.example .env      # once, then fill in DB_PASSWORD
docker compose up --build
```

**From your IDE:** `appsettings.Development.json` has the host and user but no password;
Npgsql reads it from `PGPASSWORD`.

```powershell
$env:PGPASSWORD = "<password>"
dotnet run --project src/UserService.Api
```

Without Tailscale: `kubectl -n quiz port-forward svc/postgresql 15432:5432`, then set
`DB_HOST=host.docker.internal` and `DB_PORT=15432` in `.env` (see `.env.example`).

- API: http://localhost:5001
- Swagger: http://localhost:5001/swagger
- Health: http://localhost:5001/health
- Ready (also checks Postgres): http://localhost:5001/health/ready

## Database migrations

The API applies pending migrations when it starts, so a deploy from `main` updates the schema by itself. EF skips migrations already listed in `__EFMigrationsHistory`, and locks the database while migrating, so several pods starting at once is fine. Turn it off with `Database__MigrateOnStartup=false`.

To add a migration:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add <Name> --project src/UserService.Repository --startup-project src/UserService.Api
```

Do **not** run `dotnet ef database update` locally: your connection string points at the real
database, so it would apply your branch's migrations to the shared schema. Merge to `main`
and the deploy applies them.

## Tests

```bash
dotnet test
```

## Database

Se [docs/database.md](docs/database.md) for vejledning om at forbinde med pgAdmin 4,
hente adgangskoden, oprette en testbruger og køre kontrol-scriptet
[docs/sql/check-users.sql](docs/sql/check-users.sql).

## Service ports

| Service | API |
|---|---|
| UserService | 5001 |
| MusicService | 5002 |
| StatisticService | 5003 |
| LobbyService | 5004 |
| GameService | 5005 |
