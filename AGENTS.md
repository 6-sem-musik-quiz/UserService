# AGENTS.md — Slopster / UserService

This .NET 10 repository is part of the `6-sem-musik-quiz` exam project. Read `README.md` for current architecture, development commands and ports. Cross-service approved knowledge lives in `6-sem-musik-quiz/Platform` (when created); the separate `AgentBlackboard` provides only transient coordination.

## Layer boundaries

- `src/UserService.Api`: HTTP controllers, middleware, startup.
- `src/UserService.Service`: business logic and interfaces.
- `src/UserService.Repository`: EF Core/PostgreSQL, data configurations, persistence.
- `src/UserService.Domain`: entities and DTOs.
- Tests: `tests/UserService.Service.Tests` and `tests/UserService.Api.Tests`.

`Example*` classes are scaffolding and should be replaced when implementing actual features; do not treat them as the domain model.

## Build and tests

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

Run locally: `docker compose up --build` (needs a local `.env`, which must never be committed). It runs against the REAL cluster database with migrations off; see README "Running locally". Review the README before migrations, DB access or changing ports.

## Four-agent workflow

1. Read the assigned Jira issue in project `SCRUM`, its acceptance criteria and blocking relationships; do not grab unrelated tickets.
2. Read the latest injected Platform contract/ADR context **before** planning. Treat the Platform mirror as read-only. Submit contract changes in a separate Platform PR before editing dependent services.
3. Declare affected paths using `RepoName:path/**`, and have the **worker** claim them against the latest blackboard before edits; if any claim overlaps, stop. An LLM statement is not a lock.
4. Keep changed files inside your claim. Publish relevant questions via the worker's blackboard-update mechanism (if implemented). Do not edit another agent's active note.
5. Work on an isolated branch/worktree, run tests, and open a PR for human review. Do not push to `main`, auto-merge, or set Jira Done yourself.
6. Report test results honestly, including failures. Never commit secrets, private tokens, raw copyrighted songs or local environment files.

The board currently uses `To Do`, `In Progress`, `In Review`, `Done`; verify additional AI statuses are actually created before referring to them in worker configuration.
