# /add-feature

Scaffold a complete Vertical Slice feature in this .NET/Blazor project. Follow these steps in order.

## Usage

`/add-feature <FeatureName>`

Example: `/add-feature Invoices`

## Steps

### 1. Entity (TimeTracker.Shared)

Create `TimeTracker.Shared/Entities/<FeatureName>.cs` — a plain EF Core entity. Follow the soft-delete pattern if the entity should support deletion: inherit from `SoftDeleteableEntity` (see `Project.cs` as a reference). If ownership is per-user, add a `UserId string` property (not a navigation property — avoids cascade delete issues).

### 2. DbContext registration (TimeTracker.Web)

Add a `DbSet<FeatureName>` to `TimeTrackerDataContext`. If the entity needs an `app` schema prefix or table configuration, add it in `OnModelCreating`.

### 3. Contracts / DTOs (TimeTracker.Contracts)

Create `TimeTracker.Contracts/Features/<FeatureName>/` and define request/response record types. Never define DTOs inline in `TimeTracker.Web` or `TimeTracker.Wasm`.

### 4. Service interface and implementation (TimeTracker.Web)

Create `TimeTracker.Web/Features/<FeatureName>/I<FeatureName>Service.cs` — interface only.
Create `TimeTracker.Web/Features/<FeatureName>/<FeatureName>Service.cs` — implementation:
- Inject `IDbContextFactory<TimeTrackerDataContext>` (use `await using var ctx = await _contextFactory.CreateDbContextAsync(ct)` per method)
- Inject `IUserContextService` and call `_userContextService.GetUserIdAsync()` to scope all queries to the current user
- Use `Adapt<T>()` from Mapster for entity→DTO mapping

Register in `Program.cs`: `builder.Services.AddScoped<I<FeatureName>Service, <FeatureName>Service>();`

### 5. Mapster mapping config (TimeTracker.Web)

Create `TimeTracker.Web/Features/<FeatureName>/<FeatureName>MappingConfig.cs` implementing `IRegister`. It is scanned automatically at startup — no manual registration needed. Only add a mapping config if the entity→DTO mapping is non-trivial (e.g. DateTime UTC normalization, computed fields).

### 6. API endpoints (TimeTracker.Web)

Create `TimeTracker.Web/Features/<FeatureName>/<FeatureName>Endpoints.cs`:
```csharp
public static class <FeatureName>Endpoints
{
    public static void Map<FeatureName>Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/<featurename>").RequireAuthorization();
        // MapGet / MapPost / MapPut / MapDelete here
    }
}
```
Register in `Program.cs` with `app.Map<FeatureName>Endpoints();`

Apply rate limiting for expensive reads (`.RequireRateLimiting("all-entries")`) and all writes (`.RequireRateLimiting("write")`).

### 7. WASM page (TimeTracker.Wasm)

Create the Blazor component in the appropriate location under `TimeTracker.Wasm/`. All routed pages (`@page`) must carry `@attribute [Authorize]` so the SSR `AuthorizeRouteView` redirects unauthenticated users before WASM loads.

Data loading pattern:
```csharp
protected override async Task OnInitializedAsync()
{
    await LoadData();
}

private async Task LoadData()
{
    try
    {
        // call API via HttpClient
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
    {
        Nav.NavigateTo("/login", forceLoad: true);
    }
    catch (HttpRequestException)
    {
        // aborted request (StatusCode == null) — page navigated away, swallow silently
    }
}
```

Use `RendererInfo.IsInteractive` to disable controls during SSR prerender.

### 8. EF Core migration

Run `/add-ef-migration` to create the migration after completing steps 1–2.

### 9. Unit tests (TimeTracker.Tests)

Create `TimeTracker.Tests/Features/<FeatureName>/<FeatureName>ServiceTests.cs`. See `/add-playwright-test` and existing tests for patterns. Every service-layer change must include tests in the same commit.

## Gotchas

- Do NOT store a navigation property back to `ApplicationUser` on entities — use `UserId string` to avoid cascade delete issues.
- Every WASM `LoadData()` must catch all `HttpRequestException` (not just 401) to handle teardown aborts where `StatusCode == null`.
- Never define DTOs outside `TimeTracker.Contracts/`.
