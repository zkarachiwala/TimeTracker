# Dev Container Learning Guide

This document explains the concepts behind the dev container setup in this project. Read it alongside the implementation in `.devcontainer/` and `docker-compose.yml`.

---

## What problem does a dev container solve?

When multiple developers work on a project, "works on my machine" is a constant source of friction. One developer has .NET 8, another has .NET 10. One has SQL Server 2019, another has 2022. One is on Windows, another on macOS.

A dev container packages the entire development environment — runtime, database, tools, extensions — into a Docker image. When you open the project in VS Code, it offers to reopen it inside the container. From that point on, everyone who opens the project gets the exact same environment, regardless of what's installed on their machine.

For a single-developer project like this one, the benefit is different: **reproducibility and learning**. If you get a new machine, you open the project and it works. If you want to understand how real-world containerised development works, this is the pattern used across the industry.

---

## The two files that matter

### `docker-compose.yml`

Defines the *services* your application needs — in this case, the .NET app and SQL Server. Docker Compose manages starting them in the right order, connecting them on a shared network, and keeping data alive across restarts.

### `.devcontainer/devcontainer.json`

Tells VS Code (or GitHub Codespaces) how to use the Compose file as a development environment. It specifies:
- Which service is your workspace (the one you edit code in)
- Which ports to forward to your host machine
- What to run after the container is created (`postCreateCommand`)
- Which VS Code extensions to install automatically

---

## Container networking: why `db` not `localhost`

This is the most common source of confusion when first learning Docker.

When you run two services in Docker Compose, each gets its own network namespace — its own `localhost`. The app container's `localhost` is the app container. SQL Server's `localhost` is the SQL Server container. They cannot reach each other via `localhost`.

Docker Compose creates a shared internal network and gives each service a DNS name matching its service name. So the app reaches SQL Server at `db` (the service name in `docker-compose.yml`), port 1433.

```
Your machine
└── Docker network
    ├── app container  (localhost = this container)
    └── db container   (reachable as "db" from app container)
```

This is why the connection string inside the container is:
```
Server=db,1433;Database=...
```
Not `Server=localhost,1433`.

---

## Health checks: `service_healthy` vs `service_started`

SQL Server takes 20–30 seconds to be ready to accept connections after the container starts. Without a health check, Docker Compose would mark it as started the moment the process launched — before it can actually handle queries.

The `depends_on: condition: service_healthy` tells Docker Compose to wait until SQL Server passes its health check before starting the app container. The health check runs `sqlcmd -Q "SELECT 1"` — a trivial query that only succeeds when SQL Server is fully up.

Without this, EF Core migrations in `postCreateCommand` would fail because SQL Server isn't ready yet.

---

## Named volumes: keeping your data

```yaml
volumes:
  sqlserver_data:
```

A named volume is a persistent storage location managed by Docker. When you stop and restart containers (`docker compose down` then `docker compose up`), the data survives because it lives in the named volume, not inside the container itself.

Compare this to what happens without a volume: every time you `docker compose down`, the SQL Server container is destroyed and all your data goes with it.

**The one exception:** `docker compose down -v` destroys volumes too. Use this only when you want a completely clean slate.

---

## Environment variables replace User Secrets

.NET User Secrets are stored in your user profile on your local machine (`~/.microsoft/usersecrets/`). They work for local development but don't travel into containers — the container has a different filesystem.

Inside a container, environment variables fill the same role. ASP.NET Core reads them automatically and they take precedence over `appsettings.json`.

ASP.NET Core uses `__` (double underscore) as the hierarchy separator in env var names:

```yaml
# This env var...
ConnectionStrings__DefaultConnection: "Server=db,1433;..."

# ...is equivalent to this in appsettings.json:
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=db,1433;..."
  }
}
```

The `.env` file (gitignored) feeds these values into Docker Compose without hardcoding secrets in `docker-compose.yml`.

---

## HTTP only inside the container

The app runs on HTTP inside the container (`ASPNETCORE_URLS=http://+:5019`), not HTTPS.

This is intentional and correct. In production, TLS is terminated at the infrastructure edge — a load balancer, reverse proxy, or CDN — and plain HTTP flows inside the infrastructure. The container never handles TLS directly.

Running HTTPS inside a dev container is possible but requires generating and trusting a certificate inside the container. It teaches a workaround for a dev environment quirk, not the production pattern. HTTP inside the container *is* the production pattern.

Your browser sees the app at `http://localhost:5019`. That's fine for local development.

---

## Google OAuth with HTTP

Google OAuth has one explicit exception to its HTTPS requirement: `http://localhost` (any port). It is whitelisted for development use and will not trigger a security warning.

You must register `http://localhost:5019/signin-google` as an authorized redirect URI in Google Cloud Console alongside the production HTTPS URI. See `docs/google-oauth-setup.md` for how to do this.

This only applies to localhost. Never use HTTP on a real domain.

---

## `postCreateCommand`

This runs once, inside your workspace container, after the dev container is first created. It's the equivalent of the setup steps a new developer would follow from a README.

```json
"postCreateCommand": "dotnet restore && dotnet ef database update --context TimeTrackerDataContext && dotnet ef database update --context IdentityDataContext"
```

What each step does:
1. `dotnet restore` — downloads NuGet packages (the container may not have them cached)
2. `dotnet ef database update` — applies all pending EF migrations to the containerised SQL Server

This runs against `db` (the SQL Server container), not your local SQL Server.

---

## Common Docker Compose commands

```bash
# Start all services (detached — runs in background)
docker compose up -d

# Stop all services (data preserved in volumes)
docker compose down

# Stop and destroy volumes (complete clean slate)
docker compose down -v

# See running containers and their status
docker compose ps

# View logs from a specific service
docker compose logs db
docker compose logs app

# Open a shell inside a running container
docker compose exec app bash
docker compose exec db bash

# Rebuild the app image after a Dockerfile change
docker compose up -d --build app
```

---

## Claude Code with a dev container

When VS Code reopens inside the container, its integrated terminal runs inside the container too. Claude Code is not installed inside the container, so the VS Code extension won't work from there.

**Recommended workflow (WSL2 on Windows):**
- Keep a **Windows Terminal** tab open running WSL2 alongside VS Code
- Run `claude` from there — it reads the same repo files via the shared WSL2 filesystem
- VS Code (inside the container) handles editing, `dotnet run`, and EF migrations
- Claude Code (host terminal) handles code review, file edits, and AI assistance

Alternatively, keep a **second VS Code window** on the host (not reopened in the container) and use that window's Claude Code extension. Both windows operate on the same files.

The key insight: Claude Code is a host-side tool. It doesn't need to be inside the container to work on the project — the files are the same either way.

---

## Codespaces: same spec, different host

The `.devcontainer/devcontainer.json` works identically in GitHub Codespaces — that's the point of the dev container spec. VS Code (locally) and Codespaces both read the same file.

The main differences when using Codespaces for this project:

- **Ports** — Codespaces forwards ports automatically and gives you an HTTPS URL (e.g. `https://<codespace-name>-5019.app.github.dev`)
- **Google OAuth** — that URL changes if you delete and recreate the codespace, requiring a new redirect URI registration in Google Cloud Console each time
- **Free tier** — 120 core-hours/month on a personal account; SQL Server needs a 2-core machine, so ~60 hours/month before charges

For occasional use, Codespaces is seamless. For daily development, the local dev container avoids the OAuth friction and free tier limits.

---

## The three SQL Server instances in this project

This project ends up with three separate SQL Server instances, each serving a distinct purpose:

| Instance | When it runs | Purpose |
|----------|-------------|---------|
| Local SQL Server | Always (existing setup) | Local development outside the container |
| Dev container SQL Server | Inside the dev container | Isolated reproducible dev environment |
| Testcontainers SQL Server | During container tests only | RLS and migration smoke tests |

They never all run simultaneously in normal use. The fast unit tests (`Category!=Container`) use EF Core InMemory and touch no SQL Server at all.

This is objectively more infrastructure than a single-user timetracking app needs. It exists because each instance teaches something different: containerised service dependencies (dev container), test isolation and Testcontainers patterns (#161), and the baseline of knowing how to run a database locally.

---

## Common gotchas

| Gotcha | Fix |
|--------|-----|
| SQL Server container exits immediately | Weak `SA_PASSWORD` — must meet complexity rules (upper, lower, digit, symbol, 8+ chars) |
| App can't reach SQL Server | Use service name `db` in the connection string, not `localhost` |
| `https` or `http` launch profiles make app unreachable from host | Their `applicationUrl` binds to `localhost` only inside the container — Docker port forwarding can't reach it. Use `dotnet run --launch-profile container` which binds to `http://+:5019` (all interfaces). |
| Data lost on `docker compose down` | Use a named volume, not an anonymous volume |
| `docker compose down -v` wipes data | Only use `-v` when you want a completely clean slate |
| EF migrations fail in `postCreateCommand` | Ensure `--project` points to the correct `.csproj` and `depends_on: service_healthy` is set so SQL Server is ready |
| Claude Code not available in container terminal | Claude Code runs on the host — use a separate Windows Terminal tab. See the section above. |
| Google OAuth `ClientId` null / 500 on first request | .NET User Secrets don't exist inside the container. Add `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` to `.env` and wire them into docker-compose.yml as `Authentication__Google__ClientId` / `Authentication__Google__ClientSecret`. |

---

## Further reading

- [VS Code Dev Containers documentation](https://code.visualstudio.com/docs/devcontainers/containers)
- [Docker Compose reference](https://docs.docker.com/compose/compose-file/)
- [Dev container spec (devcontainer.json)](https://containers.dev/implementors/json_reference/)
- [GitHub Codespaces with dev containers](https://docs.github.com/en/codespaces/setting-up-your-project-for-codespaces/adding-a-dev-container-configuration)
- [ASP.NET Core configuration with environment variables](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
