# /add-playwright-test

Add a Playwright E2E test following this project's established patterns. Read this in full before writing any code.

## Usage

`/add-playwright-test <description of what to test>`

## Failure triage — do this BEFORE touching any code

When a Playwright test fails, answer both questions before changing anything:

**1. Where is the error?**
- `"Request failed: <url>"` in console errors → browser console message from Blazor unhandled `HttpRequestException`. Fix is in the **app** (`LoadData()` catch block), not the test.
- `WaitForRequestFinishedAsync` timeout → the API call never completed. Check the server.
- Element not found / assertion failed → `InitializeAsync` navigation or wait is wrong.

**2. Is it teardown or mid-test?**
- If the failed URL is from a data-polling endpoint and the test doesn't interact with it, it's almost certainly a **teardown abort** — the page closed while a request was in flight. Fix: catch all `HttpRequestException` in `LoadData()`, not just 401. An aborted request has `StatusCode == null` and doesn't match a `when` guard.

**Never change a test to make a failing test pass. Fix the app or the SetUp.**

---

## Test class patterns

### Unauthenticated test

```csharp
[Collection("App")]
public class MyFeatureTests : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = TestConfig.BaseUrl,
        IgnoreHTTPSErrors = true,
    };
}
```

### Authenticated test (mobile viewport — default)

```csharp
[Collection("App")]
public class MyFeatureTests : AuthenticatedPageTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        // Wait for the API call the page makes on load BEFORE considering the page ready.
        // Register WaitForRequestFinishedAsync BEFORE GotoAsync — not after.
        await Page.RunAndWaitForRequestFinishedAsync(
            async () => await Page.GotoAsync("/my-route"),
            new() { Predicate = r => r.Url.Contains("/api/myfeature"), Timeout = 15_000 }
        );
        // Then wait for a visible element that signals interactive WASM has loaded.
        await Expect(Page.Locator(".some-element")).ToBeVisibleAsync(new() { Timeout = 30_000 });
    }
}
```

### Authenticated test (desktop viewport)

Extend `AuthenticatedDesktopPageTest` instead — it sets a 1280×800 viewport.

---

## Write tests (mutating data)

Tests that create, update, or delete data must be guarded:

```csharp
private static bool WriteTestsEnabled =>
    Environment.GetEnvironmentVariable("PLAYWRIGHT_WRITE_TESTS") == "true";

[SkippableFact]
public async Task CreateEntry_SavesSuccessfully()
{
    Skip.If(!WriteTestsEnabled, "Write tests disabled — set PLAYWRIGHT_WRITE_TESTS=true to run locally");
    // test body
}
```

Write tests are skipped in CI. Run locally with `PLAYWRIGHT_WRITE_TESTS=true`.

---

## Wait strategy

- Use `WaitForRequestFinishedAsync` wrapped around `GotoAsync` — never use text content like "Start a timer" as a data-load signal.
- After navigation inside a test, wait for a visible element that signals WASM has re-rendered with the new data.
- Playwright's actionability checks auto-wait for elements to be visible and enabled before interactions — no manual sleep needed.
- `Page.SetDefaultTimeout(30_000)` is set in `AuthenticatedPageTest.InitializeAsync()` — no need to repeat it per test.

---

## Console error assertion

`AuthenticatedPageTest.DisposeAsync()` calls `Assert.True(_consoleErrors.Count == 0, ...)` automatically. You do not need to add this to individual tests — just make sure the app doesn't log errors to the browser console.

---

## Run command (user runs this — do not trigger it yourself)

```bash
PLAYWRIGHT_WRITE_TESTS=true BROWSER= dotnet test TimeTracker.Playwright --logger "console;verbosity=normal" --blame-hang-timeout 60s
```
