# /new-api-endpoint

Add a new minimal API endpoint to an existing feature in this project.

## Usage

`/new-api-endpoint <FeatureName> <HTTP method> <route>`

Example: `/new-api-endpoint Projects GET /api/projects/{id}/summary`

## Steps

### 1. Add the handler to the existing Endpoints class

Open `TimeTracker.Web/Features/<FeatureName>/<FeatureName>Endpoints.cs` and add inside the `Map<FeatureName>Endpoints` method:

```csharp
group.MapGet("/{id:int}/summary", async (int id, I<FeatureName>Service svc, CancellationToken ct) =>
{
    var result = await svc.GetSummary(id, ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
});
```

Always inject the service interface, not the concrete class. Always accept `CancellationToken ct` as the last parameter.

### 2. Add the method to the service interface and implementation

`I<FeatureName>Service.cs`:
```csharp
Task<SomeResponse?> GetSummary(int id, CancellationToken ct = default);
```

`<FeatureName>Service.cs`:
```csharp
public async Task<SomeResponse?> GetSummary(int id, CancellationToken ct = default)
{
    var userId = await GetUserIdAsync();
    await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
    // scope to userId — never return other users' data
    var entity = await ctx.<Entities>
        .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);
    return entity?.Adapt<SomeResponse>();
}
```

`IUserContextService` scopes all queries to the current user. Never return data without filtering by `userId`.

### 3. Define the DTO in Contracts (if new)

`TimeTracker.Contracts/Features/<FeatureName>/SomeResponse.cs`:
```csharp
public record SomeResponse(int Id, string Name /* ... */);
```

Never define DTOs inline in `TimeTracker.Web` or `TimeTracker.Wasm`.

### 4. Rate limiting

Apply rate limiting where appropriate:
- `RequireRateLimiting("all-entries")` — expensive reads that return large datasets
- `RequireRateLimiting("write")` — all POST / PUT / DELETE handlers

### 5. Wire up the WASM consumer

In the relevant Blazor page, add an `HttpClient` call in `LoadData()`:

```csharp
var response = await Http.GetFromJsonAsync<SomeResponse>($"/api/<featurename>/{id}/summary");
```

Always catch `HttpRequestException` in `LoadData()`:
- `StatusCode == HttpStatusCode.Unauthorized` → redirect to `/login`
- Any other `HttpRequestException` (including `StatusCode == null` for aborted requests) → swallow silently

### 6. CSP check

If the new endpoint calls an external API or loads resources from a new origin, update `SecurityHeadersMiddleware.cs` with the appropriate CSP directive. The current policy is strict (`connect-src 'self'`) and will silently block unlisted origins.

### 7. Add a unit test

Add a test in `TimeTracker.Tests/Features/<FeatureName>/<FeatureName>ServiceTests.cs` for the new service method. Every service-layer change must include tests in the same commit.
