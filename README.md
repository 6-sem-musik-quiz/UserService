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

## Running locally

**Everything in Docker:**

```bash
docker compose up --build
```

**API from your IDE, database in Docker:**

```bash
docker compose up db
dotnet run --project src/UserService.Api
```

- API: http://localhost:5001
- Swagger: http://localhost:5001/swagger
- Health: http://localhost:5001/health
- Ready (also checks Postgres): http://localhost:5001/health/ready
- Postgres: `localhost:5441` (postgres / postgres)

## Database migrations

The API applies pending migrations when it starts, so a deploy from `main` updates the schema by itself. EF skips migrations already listed in `__EFMigrationsHistory`, and locks the database while migrating, so several pods starting at once is fine. Turn it off with `Database__MigrateOnStartup=false`.

To add a migration or apply one by hand:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add <Name> --project src/UserService.Repository --startup-project src/UserService.Api
dotnet ef database update --project src/UserService.Repository --startup-project src/UserService.Api
```

## Tests

```bash
dotnet test
```

## Service ports

| Service | API | Postgres |
|---|---|---|
| UserService | 5001 | 5441 |
| MusicService | 5002 | 5442 |
| StatisticService | 5003 | 5443 |
| LobbyService | 5004 | 5444 |
| GameService | 5005 | 5445 |
