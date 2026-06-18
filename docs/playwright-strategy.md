# Playwright Test Strategy

Sources consulted: [Playwright Best Practices](https://playwright.dev/docs/best-practices), [Playwright .NET Writing Tests](https://playwright.dev/dotnet/docs/writing-tests), [Playwright Network - waitForResponse](https://playwright.dev/docs/network), [BrowserStack waitForResponse guide](https://www.browserstack.com/guide/playwright-waitforresponse), [Checkly waits and timeouts](https://www.checklyhq.com/docs/learn/playwright/waits-and-timeouts/)

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
| `Page.WaitForRequestFinishedAsync(predicate)` | An API call must **fully complete** (body downloaded) before the test proceeds. Register **before** the action. See below. |
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

**Use `WaitForRequestFinishedAsync` when waiting for data to load.** Using `WaitForResponseAsync` leaves a gap between headers arriving and the body being read by .NET's `ReadFromJsonAsync`. If the page tears down during that gap, Playwright fires `RequestFailed` for the request even though a response was received.

Both methods must be set up **before** the action that triggers the request. If set up after, the request may already have fired and the task will timeout (confirmed by [BrowserStack](https://www.browserstack.com/guide/playwright-waitforresponse)):

```csharp
// CORRECT — listener registered before navigation
var finishedTask = Page.WaitForRequestFinishedAsync(new() {
    Predicate = r => r.Url.Contains("/api/something")
});
await Page.GotoAsync("/page");
await finishedTask; // resolves only when body is fully downloaded

// WRONG — request may have already fired; will timeout
await Page.GotoAsync("/page");
var finishedTask = Page.WaitForRequestFinishedAsync(...);
await finishedTask; // hangs indefinitely
```

---

## Blazor WASM two-phase rendering

This app uses Blazor SSR + WASM with `InteractiveWebAssembly` render mode. Every page goes through two phases:

**Phase 1 — SSR prerender (server):** The server renders the component to HTML. No WASM, no browser-side HTTP calls. Some default text may already be visible. `RendererInfo.IsInteractive` is `false`.

**Phase 2 — WASM hydration (browser):** .NET WASM loads, the component re-initialises, `OnInitializedAsync` runs again, and real API calls fire. `RendererInfo.IsInteractive` becomes `true`. The FAB button (`Disabled="@(!RendererInfo.IsInteractive)"`) becoming enabled is the authoritative signal that WASM is interactive.

**FAB enabled ≠ data loaded.** API calls start _after_ WASM is interactive; they have not completed yet. Never rely on FAB-enabled as a "data loaded" gate — only use it as a "safe to interact" gate.

---

## The timer page race condition

`TimerPage` sets `_providersReady = true` in `OnAfterRenderAsync(firstRender)`. This fires after the first render, which happens concurrently with `LoadData()`. The text "Start a timer" renders as soon as `_providersReady = true` — **before `api/timeentries/active` has responded**.

When the test ends, the page tears down. Any in-flight request is aborted by the browser. Playwright fires `Page.RequestFailed` for the aborted request, which `AssertNoConsoleErrors` catches as a test failure.

**"Start a timer" and "Tracking now" are not reliable data-load signals.** They can appear before the API call completes. Always use `WaitForResponseAsync` for `api/timeentries/active` on the timer page.

---

## SetUp patterns

### Timer page (`/`)

`TimerPage.LoadData()` makes three sequential API calls: `GetAllProjects` → `GetActiveTimeEntry` (`api/timeentries/active`) → `GetTodaysTimeEntries` (`api/timeentries/today`). Wait for the **last** one to finish (body fully downloaded). Because they are sequential awaits, if `today` is done, the other two must also be done.

```csharp
[SetUp]
public async Task NavigateToTimer()
{
    // WaitForRequestFinishedAsync fires on 'requestfinished' (body fully downloaded), not
    // 'response' (headers only). Target the last sequential call in LoadData() — if today's
    // entries have finished, projects and active-timer must also be fully complete.
    var loadDataDone = Page.WaitForRequestFinishedAsync(new()
    {
        Predicate = r => r.Url.Contains("/api/timeentries/today"),
        Timeout = 15_000
    });
    await Page.GotoAsync("/");
    await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
    await loadDataDone;
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
| `WaitForRequestFinishedAsync` timeout | API call never completed | Server or network |
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

- [ ] Register `WaitForRequestFinishedAsync` for `api/timeentries/today` **before** `GotoAsync` (last call in LoadData — guarantees all three are done)
- [ ] Await the finished task after FAB enabled wait
- [ ] Do **not** use `GetByText("Start a timer")` or `GetByText("Tracking now")` as data-load gates
- [ ] Do **not** use `WaitForResponseAsync` — it resolves on headers, leaving body reads in-flight
- [ ] If the test needs auth-dependent nav links, add the "Clients" visible wait after the finished task

## Checklist: new test for any other page

- [ ] Wait for `.tt-fab button` enabled (WASM interactive) — or page-specific content if no FAB
- [ ] If the test triggers an action that fires an API call, use `WaitForRequestFinishedAsync` (not `WaitForResponseAsync`) for that call before the action
- [ ] Assertions use `Expect()` (web-first); never use `IsVisibleAsync()` in an `Assert.That`
