# Playwright Test Strategy

Sources consulted: [Playwright Best Practices](https://playwright.dev/docs/best-practices), [Playwright .NET Writing Tests](https://playwright.dev/dotnet/docs/writing-tests), [Playwright Network - waitForResponse](https://playwright.dev/docs/network), [BrowserStack waitForResponse guide](https://www.browserstack.com/guide/playwright-waitforresponse), [Checkly waits and timeouts](https://www.checklyhq.com/docs/learn/playwright/waits-and-timeouts/)

---

## How local authentication works

Production auth is Google OAuth — there is no username/password. Playwright cannot drive a real Google OAuth flow (it would require browser automation of Google's own pages, which Google actively blocks). Instead, the test suite uses a **dev-only bypass endpoint** that signs in directly via ASP.NET Identity's `SignInManager`, skipping OAuth entirely.

### Prerequisites — things that must be true on the dev machine

1. **`ASPNETCORE_ENVIRONMENT=Development`** — the `https` launch profile in `launchSettings.json` sets this automatically. The dev endpoint is gated behind `app.Environment.IsDevelopment()` in `Program.cs` and is **never compiled into a production build**.

2. **An admin user must exist in the database.** On first run (or after a DB wipe), `Program.cs` reads `Authentication:AdminEmail` from config and seeds a user with the `Admin` role. This value is stored in .NET User Secrets:
   ```
   Authentication:AdminEmail = zak@dzk.com.au
   ```
   Set it with:
   ```bash
   cd TimeTracker.Web
   dotnet user-secrets set "Authentication:AdminEmail" "your@email.com"
   ```

3. **DB credentials** must also be in user secrets (`DbUser`, `DbPassword`) — the app needs to reach the database to sign in.

### What GlobalSetup does

`GlobalSetup.cs` runs once before all tests. It:

1. Starts the app on port `7007` using `dotnet run --launch-profile https --urls https://localhost:7007` (a dedicated port so it never collides with a running dev instance on 7006)
2. Calls `GET /api/dev/login` — this endpoint finds the first `Admin` user in the database and calls `SignInManager.SignInAsync`, which issues an ASP.NET Identity auth cookie
3. Saves the entire browser storage state (cookies) to `playwright/.auth/user.json` via Playwright's `StorageStateAsync`

### What AuthenticatedPageTest / AuthenticatedDesktopPageTest do

Both base classes pass `StorageStatePath = TestConfig.AuthStatePath` in `ContextOptions()`. Playwright loads `user.json` and injects its cookies into every new browser context — each test starts already authenticated, no login flow required per test.

`TestConfig.AuthStatePath` resolves to `<build-output>/playwright/.auth/user.json`. This file is generated fresh on each test run (GlobalSetup always recreates it) and is not committed to source control.

### The dev login endpoint (`/api/dev/login`)

```csharp
// TimeTracker.Web/Infrastructure/DevEndpointExtensions.cs
app.MapGet("/api/dev/login", async (UserManager<User> userManager, SignInManager<User> signInManager) =>
{
    var admins = await userManager.GetUsersInRoleAsync("Admin");
    var user = admins.FirstOrDefault();
    if (user is null)
        return Results.Problem("No admin user found. Run /api/dev/seed first.");
    await signInManager.SignInAsync(user, isPersistent: true);
    return Results.Content($"<html><body>Signed in as {user.Email}</body></html>", "text/html");
});
```

Only registered in `Program.cs` when `IsDevelopment()` is true:
```csharp
if (app.Environment.IsDevelopment())
{
    app.MapDevEndpoints(); // includes /api/dev/login
}
```

### Common failure modes

| Symptom | Cause | Fix |
|---------|-------|-----|
| `Dev login failed (404)` | App not running as `Development` | Use the `https` launch profile; check `ASPNETCORE_ENVIRONMENT` |
| `No admin user found` | DB empty or `Authentication:AdminEmail` not set | Set the user secret and restart the app so the seed runs |
| `user.json` missing / auth redirects to `/login` in tests | GlobalSetup didn't run or was skipped | Always run the full test project via `dotnet test`, not individual test classes |
| App startup timeout (180s exceeded) | DB not running or cold-start pause | Ensure SQL Server container is running; visit the app manually first |

---

## Core principle

Playwright auto-waits before every action and web-first assertion. The official guidance is:

> "There is no need to wait for anything prior to performing an action: Playwright automatically waits for the wide range of actionability checks to pass."

Add explicit waits only at specific synchronisation points that Playwright cannot infer from element state — primarily API responses that must complete before the test proceeds.

---

## Wait hierarchy (most to least preferred)

| Mechanism | Use when |
|-----------|----------|
| Web-first assertions (`Expect(...).ToBeVisibleAsync()`) | Confirming element state after an action. Auto-retries. Always prefer this over checking `.IsVisibleAsync()` manually. |
| `Page.RunAndWaitForRequestFinishedAsync(action, options)` | An API call must **fully complete** (body downloaded) before the test proceeds. Couples the wait to the triggering action — if the action throws, the wait cancels immediately. **Always use this, never the standalone `WaitForRequestFinishedAsync`.** See below. |
| `Page.WaitForResponseAsync(predicate)` | Only when you need to inspect response headers or status — fires on headers only, body may still be downloading. |
| `WaitForLoadState(DOMContentLoaded / Load)` | Waiting for basic page load when no specific element signals readiness. |
| `WaitForLoadState(NetworkIdle)` | **Discouraged** (Checkly, Playwright docs). Unreliable in apps with polling, analytics, or websockets. Only use when no other signal exists. |
| `Task.Delay` / hard sleeps | **Never.** Creates flakiness and masks real synchronisation issues. |

---

## `WaitForResponseAsync` vs `WaitForRequestFinishedAsync` — critical distinction

These two methods fire on different Playwright network events:

| Method | Event | Fires when |
|--------|-------|-----------|
| `WaitForResponseAsync` | `response` | Response **headers** received — body may still be downloading |
| `WaitForRequestFinishedAsync` | `requestfinished` | Response **body fully downloaded** — request is completely done |

From [Playwright docs](https://playwright.dev/docs/api/class-request):

> **`response` event** — emitted when/if the response status and headers are received  
> **`requestfinished` event** — emitted when the response body is downloaded and the request is complete

**Use `RunAndWaitForRequestFinishedAsync` when waiting for data to load.** Using `WaitForResponseAsync` leaves a gap between headers arriving and the body being read by .NET's `ReadFromJsonAsync`. If the page tears down during that gap, Playwright fires `RequestFailed` for the request even though a response was received.

---

## `RunAndWaitForRequestFinishedAsync` — the only correct pattern

### Why `WaitForRequestFinishedAsync` (standalone) is broken

The standalone `WaitForRequestFinishedAsync` registers an event listener, then you call the action separately:

```csharp
// BROKEN — do NOT use this pattern
var loadDataDone = Page.WaitForRequestFinishedAsync(new()
{
    Predicate = r => r.Url.Contains("/api/timeentries/today"),
    Timeout = 15_000
});
await Page.GotoAsync("/");
await loadDataDone;
```

**Problem (playwright-dotnet issue #2530):** If an exception is thrown before the monitored request fires, the waiter does not cancel — it ignores the exception and continues waiting until the full timeout expires. This leaves an orphaned event listener on the `Page` object. When `PageTest` teardown later tries to dispose the page, it cannot until that listener resolves, causing a hang of up to `Timeout` milliseconds (15–30 seconds in our SetUp methods).

This is a confirmed framework bug. The workaround is `RunAndWaitForRequestFinishedAsync`.

### The correct pattern

`RunAndWaitForRequestFinishedAsync` couples the wait to the action as a single atomic operation. If the action throws, the wait cancels immediately — no orphaned listener.

```csharp
// CORRECT — use this in all SetUp methods
await Page.RunAndWaitForRequestFinishedAsync(
    async () => await Page.GotoAsync("/"),
    new() { Predicate = r => r.Url.Contains("/api/timeentries/today"), Timeout = 15_000 }
);
```

**Rule: never use the standalone `WaitForRequestFinishedAsync` register-before-action pattern. Always use `RunAndWaitForRequestFinishedAsync`.**

---

## Blazor WASM two-phase rendering

This app uses Blazor SSR + WASM with `InteractiveWebAssembly` render mode. Every page goes through two phases:

**Phase 1 — SSR prerender (server):** The server renders the component to HTML. No WASM, no browser-side HTTP calls. Some default text may already be visible. `RendererInfo.IsInteractive` is `false`.

**Phase 2 — WASM hydration (browser):** .NET WASM loads, the component re-initialises, `OnInitializedAsync` runs again, and real API calls fire. `RendererInfo.IsInteractive` becomes `true`. The FAB button (`Disabled="@(!RendererInfo.IsInteractive)"`) becoming enabled is the authoritative signal that WASM is interactive.

**FAB enabled ≠ data loaded.** API calls start _after_ WASM is interactive; they have not completed yet. Never rely on FAB-enabled as a "data loaded" gate — only use it as a "safe to interact" gate.

---

## The timer page race condition

`TimerPage.LoadData()` makes three sequential API calls: `GetAllProjects` → `GetActiveTimeEntry` (`api/timeentries/active`) → `GetTodaysTimeEntries` (`api/timeentries/today`). The text "Start a timer" renders as soon as `_providersReady = true` in `OnAfterRenderAsync(firstRender)` — **before `api/timeentries/active` has responded**.

When the test ends, the page tears down. Any in-flight request is aborted by the browser. Playwright fires `Page.RequestFailed` for the aborted request, which `AssertNoConsoleErrors` catches as a test failure.

**"Start a timer" and "Tracking now" are not reliable data-load signals.** They can appear before the API call completes. Always wait for `api/timeentries/today` (the last sequential call) — if it's done, all three calls are done.

---

## SetUp patterns

### Timer page (`/`)

```csharp
[SetUp]
public async Task NavigateToTimer()
{
    // RunAndWaitForRequestFinishedAsync fires on 'requestfinished' (body fully downloaded).
    // Target the last sequential call in LoadData() — if today's entries have finished,
    // projects and active-timer must also be fully complete.
    // The coupled pattern ensures the wait cancels immediately if GotoAsync throws,
    // preventing orphaned listeners that cause teardown hangs.
    await Page.RunAndWaitForRequestFinishedAsync(
        async () => await Page.GotoAsync("/"),
        new() { Predicate = r => r.Url.Contains("/api/timeentries/today"), Timeout = 15_000 }
    );
    await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
}
```

If the test also needs auth-protected nav links (e.g. "Users", "Clients") to be present, add after the response wait — auth state resolves on a separate call:

```csharp
await Expect(Page.Locator(".bottom-nav").GetByText("Clients"))
    .ToBeVisibleAsync(new() { Timeout = 15_000 });
```

### Non-timer pages

Wait for the FAB to be enabled (WASM interactive), then use a page-specific content assertion that only renders after data loads:

```csharp
[SetUp]
public async Task NavigateToProjects()
{
    await Page.GotoAsync("/projects");
    await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
    // No extra wait needed — test assertions use Expect() which auto-retries
}
```

For pages without a FAB, wait for a heading or content element that only renders after data:

```csharp
await Page.GotoAsync("/reports");
await Expect(Page.GetByText("YTD hours")).ToBeVisibleAsync(new() { Timeout = 30_000 });
```

---

## Failure triage — do this before touching any code

### Step 1: identify the error source

| Error text | Source | Fix location |
|------------|--------|--------------|
| `"Request failed: <url>"` in `AssertNoConsoleErrors` | Blazor logged an unhandled `HttpRequestException` to the browser console | App — `LoadData()` catch block |
| Other text in `AssertNoConsoleErrors` | Real Blazor/JS console error | App — component error handling |
| `RunAndWaitForRequestFinishedAsync` timeout | API call never completed | Server or network |
| Element not found / assertion timeout | SetUp navigation/wait wrong | Test SetUp |

### Step 2: is it teardown or mid-test?

A **teardown abort** happens when the page closes while an API request is still in-flight. Signs:
- The failing URL is from the timer page (`/api/timeentries/active`, `/api/timeentries/today`)
- The test itself doesn't interact with the timer
- Error appears in `TearDown`, not during a test action

Teardown aborts surface as `"Request failed: <url>"` in the browser console because:
1. Page closes → browser cancels the fetch
2. .NET `HttpClient` throws `HttpRequestException` with `StatusCode == null`
3. `LoadData()` catch only handles `Unauthorized` (`StatusCode == 401`) — the abort falls through
4. Blazor catches the unhandled exception and writes it to the browser console

**Fix:** catch all `HttpRequestException` in `LoadData()`, not just `Unauthorized`:

```csharp
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
{
    Nav.NavigateTo("login", forceLoad: true);
}
catch (HttpRequestException)
{
    // Swallow — request aborted during page teardown, not a real error
}
```

### `RequestFailed` Playwright event vs browser console errors

These are distinct:
- **`Page.RequestFailed` event** — Playwright fires this for network-level failures and aborts. HTTP 4xx/5xx do NOT trigger it (they complete with `requestfinished`).
- **Browser console `"Request failed: <url>"`** — written by Blazor when `HttpRequestException` goes unhandled. This IS caught by `AssertNoConsoleErrors`.

`AuthenticatedPageTest` monitors console errors only. `Page.RequestFailed` is not tracked — teardown aborts that cause Blazor console errors are fixed in the app's catch blocks, not by filtering the console monitor.

---

## Selector guidance

From [Playwright Best Practices](https://playwright.dev/docs/best-practices): prefer user-facing locators in this order:

1. `GetByRole()` — best; mirrors how users and assistive tech see the page
2. `GetByLabel()` — for form fields
3. `GetByText()` — for non-interactive content
4. `GetByTestId()` — when semantic selectors are not practical
5. CSS class selectors (`.mud-card`, `.tt-fab`) — only when the above don't work; acceptable for stable component-level selectors in MudBlazor

Avoid XPath. Avoid selectors tied to implementation details that change with refactors.

---

## Checklist: new test that navigates to `/` (timer page)

- [ ] Use `RunAndWaitForRequestFinishedAsync` wrapping `GotoAsync("/")`, predicate `api/timeentries/today` (last call in LoadData — guarantees all three are done)
- [ ] Await FAB enabled after `RunAndWaitForRequestFinishedAsync`
- [ ] Do **not** use `GetByText("Start a timer")` or `GetByText("Tracking now")` as data-load gates
- [ ] Do **not** use `WaitForResponseAsync` — it resolves on headers, leaving body reads in-flight
- [ ] Do **not** use standalone `WaitForRequestFinishedAsync` — orphans listener on failure, causes teardown hang
- [ ] If the test needs auth-dependent nav links, add the "Clients" visible wait after the finished task

## Checklist: new test for any other page

- [ ] Wait for `.tt-fab button` enabled (WASM interactive) — or page-specific content if no FAB
- [ ] If the test triggers an action that fires an API call, use `RunAndWaitForRequestFinishedAsync` (not `WaitForResponseAsync`, not standalone `WaitForRequestFinishedAsync`)
- [ ] Assertions use `Expect()` (web-first); never use `IsVisibleAsync()` in an `Assert.That`
