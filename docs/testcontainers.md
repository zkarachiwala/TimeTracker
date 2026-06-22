# Testcontainers in TimeTracker

## What is Testcontainers?

Testcontainers is a .NET library that starts real Docker containers from inside your test code. You write C#, call `container.StartAsync()`, and you get a fully working SQL Server (or Redis, or Postgres, or anything with a Docker image) running locally — no pre-installed service, no env vars, no manual setup.

The key insight: **the test controls the infrastructure, not the other way around.** You don't configure your machine to have SQL Server running; your test starts it on demand and tears it down when it's done.

---

## Why we added it

Two problems existed before this change:

1. **RLS tests were permanently skipped in CI.** `RlsIntegrationTests` required three env vars (`SQL_SERVER_RLS_TESTS=true`, admin connection string, app connection string) and only ran when you manually set them. This meant row-level security could silently break without anyone knowing.

2. **Broken migrations only surfaced at deploy time.** There was no automated check that EF Core migrations applied cleanly. A bad migration would reach Azure and fail during startup.

Both problems have the same root cause: the tests needed SQL Server but had no portable way to get it. Testcontainers solves this.

---

## How the fixture works

The entry point is `SqlServerFixture` in `TimeTracker.Tests/Infrastructure/SqlServerFixture.cs`.

```csharp
public class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithAutoRemove(true)
        .Build();

    public string AdminConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        AdminConnectionString = _container.GetConnectionString();
        await ApplyMigrationsAsync(AdminConnectionString);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
```

**`IAsyncLifetime`** is an xUnit interface. xUnit calls `InitializeAsync()` before any test in the collection runs, and `DisposeAsync()` after all of them finish. This is the lifecycle hook that lets a fixture own expensive resources.

**`MsSqlBuilder`** is the Testcontainers fluent API for SQL Server. Under the hood it:
1. Pulls the `mcr.microsoft.com/mssql/server` Docker image (cached after the first pull)
2. Starts a container with a randomly assigned host port
3. Waits until SQL Server is ready to accept connections (it polls the health endpoint)
4. Returns a connection string like `Server=localhost,54321;Database=master;User Id=sa;Password=...`

You don't need to know the port — `GetConnectionString()` gives it to you.

**`WithAutoRemove(true)`** tells Docker to delete the container when the process exits, even if `DisposeAsync` didn't run (e.g. the test runner crashed). Belt-and-suspenders cleanup.

---

## xUnit collection fixtures — how sharing works

The fixture is expensive to start (~10–20 seconds for SQL Server to initialise). You don't want to pay that cost once per test class. xUnit's **collection fixture** pattern lets multiple test classes share one fixture instance.

```csharp
[CollectionDefinition("SqlServer")]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture> { }
```

`CollectionDefinition` is a marker class — it has no tests, no body, just attributes. It declares that a collection called `"SqlServer"` exists and that its shared fixture is `SqlServerFixture`.

Any test class that opts into this collection gets the shared instance:

```csharp
[Collection("SqlServer")]
public class RlsIntegrationTests(SqlServerFixture fixture) { ... }

[Collection("SqlServer")]
public class MigrationSmokeTests(SqlServerFixture fixture) { ... }
```

xUnit creates one `SqlServerFixture`, calls `InitializeAsync` once, injects it into both test classes via primary constructor parameters, then calls `DisposeAsync` once when both classes finish. The container runs for the duration of the entire test session, not per test.

---

## The `[Trait]` filter

```csharp
[Trait("Category", "Container")]
```

This is xUnit metadata — a key/value tag you can filter on at the command line:

```bash
# Skip container tests (fast, no Docker needed)
dotnet test TimeTracker.Tests --filter "Category!=Container"

# Run only container tests
dotnet test TimeTracker.Tests --filter "Category=Container"
```

This is important because Testcontainers needs Docker. In remote Claude Code sessions, Docker isn't available — so the fast feedback command excludes container tests. CI (ubuntu-latest) has Docker, so CI runs everything.

The trait is on the class, not individual methods — all tests in the class inherit it.

---

## Migration smoke tests

`MigrationSmokeTests` verifies that both EF Core migration chains apply cleanly from scratch.

```csharp
[Fact]
public async Task TimeTrackerDataContext_migrations_apply_cleanly()
{
    var opts = new DbContextOptionsBuilder<TimeTrackerDataContext>()
        .UseSqlServer(fixture.CreateIsolatedConnectionString())
        .Options;
    await using var ctx = new TimeTrackerDataContext(opts);
    await ctx.Database.MigrateAsync();
}
```

**Why `CreateIsolatedConnectionString()`?** The shared fixture already applied migrations to the base database. If we just tested against that, we'd be verifying that migrations were already applied — not that they *can* apply. To test migrations properly, we need a fresh empty database. `CreateIsolatedConnectionString()` generates a connection string with a unique `InitialCatalog` name (a GUID), pointing to the same container. When `MigrateAsync()` runs against a non-existent database, EF Core creates it and applies every migration from the beginning.

```csharp
public string CreateIsolatedConnectionString()
{
    var builder = new SqlConnectionStringBuilder(AdminConnectionString)
    {
        InitialCatalog = $"isolated_{Guid.NewGuid():N}"
    };
    return builder.ConnectionString;
}
```

If a migration is malformed (bad SQL, missing column, wrong FK), `MigrateAsync` throws and the test fails. This catches migration bugs before they reach Azure.

---

## RLS tests — what changed

The original `RlsIntegrationTests` used:

```csharp
private static bool RlsTestsEnabled =>
    Environment.GetEnvironmentVariable("SQL_SERVER_RLS_TESTS") == "true";

[Fact]
public async Task TimeEntries_UserA_CannotSeeUserB_Entries()
{
    if (!RlsTestsEnabled) return; // silently skip
    ...
}
```

Silent skips are dangerous — the test always shows green in CI even though it never ran.

Now the test class takes the fixture as a constructor parameter. No guard, no env var. If Docker is available, the test runs and actually verifies something. If Docker isn't available (remote session), the `--filter "Category!=Container"` flag excludes it entirely — a skip that's explicit and intentional, not silent.

The test logic itself didn't change. The connection string now comes from the fixture:

```csharp
// Before: AdminConnection = Environment.GetEnvironmentVariable("SQL_SERVER_ADMIN_CONNECTION");
// After:
private string AppConnectionAsTestUser =>
    new SqlConnectionStringBuilder(fixture.AdminConnectionString) { ... }.ConnectionString;
```

The SA connection the fixture exposes has the same role as the old `SQL_SERVER_ADMIN_CONNECTION` — it bypasses RLS because SA is db_owner. The low-privilege login (`timetracker_rls_test`) is still created in `SetupAsync` using that SA connection, giving us a realistic representation of the production Managed Identity role.

---

## Docker image caching

The first time these tests run, Docker has to pull `mcr.microsoft.com/mssql/server` (~1.5 GB). This is slow. After the first pull, Docker caches the image locally and subsequent runs start in seconds.

In CI (GitHub Actions), the image is re-pulled on every run unless you configure Docker layer caching. For this project that's acceptable — the total CI time cost is modest and the simplicity is worth it.

---

## A subtle EF Core pitfall: stale navigation properties across contexts

During development, the `DisposeAsync` cleanup hit a `DbUpdateConcurrencyException` that illustrates a common EF Core trap worth understanding.

The original cleanup code passed entity objects across DbContext boundaries:

```csharp
// SetupAsync creates ProjectA/ProjectB in context #1
var projectA = new Project { ..., ProjectUsers = [new ProjectUser { UserId = UserA }] };
ctx.Projects.AddRange(projectA, projectB);
await ctx.SaveChangesAsync(); // context #1 tracks these

// DisposeAsync uses context #2 — but passes the stale entity from context #1
await using var ctx = new TimeTrackerDataContext(adminOptions); // context #2
ctx.Projects.RemoveRange(ProjectA, ProjectB); // ProjectA still has ProjectUsers loaded!
```

When you attach an entity to a new context, EF also attaches everything reachable via navigation properties. `ProjectA.ProjectUsers` still contained the `ProjectUser` object that was created in `SetupAsync`. EF generated a CASCADE DELETE for it — but we'd already explicitly deleted those `ProjectUsers` two lines earlier. The DELETE found 0 rows and EF threw `DbUpdateConcurrencyException`.

The fix: re-query the projects fresh in the cleanup context using `IgnoreAutoIncludes()`. This gives EF clean instances with no navigation state, so it generates a simple `DELETE WHERE Id = X` rather than trying to cascade:

```csharp
var projects = await ctx.Projects
    .IgnoreAutoIncludes()   // don't load ProjectUsers navigation
    .IgnoreQueryFilters()   // bypass soft-delete global filter
    .Where(p => p.Id == ProjectA.Id || p.Id == ProjectB.Id)
    .ToListAsync();
ctx.Projects.RemoveRange(projects);
```

**General rule:** never pass entity objects loaded by one `DbContext` into a different `DbContext` instance for mutation. Either keep the same context, or re-query what you need in the new context.

---

## When to reach for Testcontainers

Use it when:
- Your test needs a real database (not InMemory) to verify SQL Server-specific behaviour — RLS policies, indexed views, triggers, stored procedures
- You want to verify that migrations apply cleanly
- You're testing something that depends on database constraints (FK, unique indexes) that InMemory ignores

Don't use it when:
- You're testing business logic that doesn't depend on the database engine — InMemory is faster and simpler for that
- You need the test to run without Docker (remote sessions, restricted CI environments)

The cost of Testcontainers is startup time (~10–20s for SQL Server). Keep the container-dependent tests focused on things that genuinely require a real engine.
