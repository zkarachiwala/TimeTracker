# Making TimeTracker resilient to a sleeping backend

## Context

The app runs on Azure App Service F1 + Azure SQL free offer. Both sleep independently: F1 unloads after ~20 min idle with no Always On available, and the database auto-pauses after 1h with a 60–90s resume. Neither can be kept warm — the 60 CPU-min/day quota forbids pinging the app (TD26), and the 100,000 vCore-second allowance forbids pinging the database (ADR-036). So the cold start can only be absorbed, never prevented.

Three symptoms follow from that: the app appears logged out, it takes minutes and several retries to become usable, and stopping a running timer fails to save.

The question this plan answers: **can the core workflow — open the app, start a timer, stop a timer — be made to work regardless of backend state, without gutting the application?**

Short answer: yes for the timer, at moderate cost, in stages that can be stopped at any point. The first stage is very small and fixes the most annoying symptom outright.

## What is actually broken

Investigation found one cause that had been misdiagnosed, and one that changes the shape of the work.

**1. Auth "breaking" is a client-side cache bug, not an auth problem.**

`TimeTracker.Client/Features/Auth/CookieAuthenticationStateProvider.cs` calls `/api/auth/user` and, on *any* exception, caches `Anonymous()` in a field:

```csharp
catch { return _cached = Anonymous(); }
```

Nothing ever calls `NotifyAuthenticationStateChanged`, and in WASM a scoped service lives for the whole app instance. So one failed call during a backend wake permanently poisons auth state for that page load — every `[Authorize]` page then sees an anonymous user until a hard reload. That is exactly the "I have to refresh / clear cache" symptom, and it has nothing to do with `SecurityStamp` revalidation, which is what I suggested earlier.

**2. The timer has no local existence.**

The active timer *is* a database row with `End = null`. `TimerPage.StartTimer` cannot start without a server round trip, and `CreateTimeEntry` returns `void` — the client re-reads state afterwards. The tick itself (`ElapsedDisplay`, driven by `PeriodicTimer`) is already client-side and survives fine.

So the clock is not the problem; the *lifecycle* is. Making the timer resilient means giving it a local record.

**3. There is no service worker.** `TimeTracker.Client/wwwroot/` contains only `images`. The app is hosted WASM — the host page is served by `TimeTracker.Web` — so with the backend asleep the app cannot even be loaded.

## The staged plan

Four stages, each independently valuable and independently shippable. Stop after any of them.

### Stage A — Don't lose the timer (small)

*Fixes: stopping a running timer fails; the app looks logged out.*

1. **Fix the auth cache.** Distinguish "server said anonymous" from "couldn't reach the server". Only cache a definitive answer; on a transport failure leave state uncached and retry on the next request. Optionally expose a `Refresh()` that calls `NotifyAuthenticationStateChanged` once connectivity returns.
   - `TimeTracker.Client/Features/Auth/CookieAuthenticationStateProvider.cs`

2. **Persist the pending stop.** When Stop is pressed, write `{entryId, stopUtc}` to local storage *before* attempting the save, and clear it on success. On page load, if a pending stop exists, retry it in the background.
   - `TimeTracker.Client/Features/Timer/Pages/TimerPage.razor`
   - A small `ILocalStore` wrapper over `localStorage` via `IJSRuntime` — no new package needed.

**No schema change.** Update-by-id (`PUT /api/timeentries/{id}` setting `End`) is naturally idempotent, so replaying it is safe.

**Gain:** stopping a timer never loses time or records a wrong duration, and a wake no longer logs you out. This is most of the daily pain, for a small change.

### Stage B — Start a timer offline (moderate)

*Fixes: cannot start tracking while the backend is cold.*

The active timer becomes a local record (`{clientId: Guid, projectId, startUtc}`) persisted locally. Start and stop are local, instant operations. A queue of mutations drains in the background.

This needs **idempotency**, the one real schema change:

- Add `Guid? ClientRequestId` to `TimeTracker.Shared/Entities/TimeEntry.cs` with a unique filtered index; EF migration on `TimeTrackerDataContext`.
- Add it to `TimeEntryCreateRequest` (`TimeTracker.Contracts/Features/TimeEntries/TimeEntryModels.cs`).
- `TimeEntryService.CreateTimeEntry` (`TimeTracker.Web/Features/TimeEntries/TimeEntryService.cs`) returns the existing row if the key is already present, instead of inserting a duplicate.

**Worth designing in:** when a start *and* its stop both happen offline, the queue should collapse them into a single create carrying both `Start` and `End`. That sidesteps local→server id mapping entirely for the common case, which is the fiddliest part of any sync engine.

**Gain:** the timer works with the backend completely down. Combined with Stage A, the whole core workflow stops depending on the free tier — provided the app is already open.

### Stage C — Open the app offline (moderate, higher risk)

*Fixes: cold start takes minutes; the ~5 MB payload on every visit.*

Add a PWA service worker caching the host page and WASM assets. Blazor has first-class support for this, but this is *hosted* WASM, so the service worker must also cache the server-rendered host document — less turnkey than the standalone template.

**Gain:** the app opens instantly from cache with everything asleep, and the bundle becomes a first-visit cost rather than a per-visit one. This is also the precondition for Stages A and B to help on a genuine cold start rather than only in an already-open tab.

**Risk:** the SSR `AuthorizeRouteView` gate documented in CLAUDE.md is server-side. A cached shell bypasses it, so the UI renders before any server auth check. For a single-user app on a personal device that is defensible — data stays local until sync, and sync re-authenticates — but it is a real change to the security posture and should be an explicit ADR, not a side effect.

### Stage D — Offline auth tolerance (small, only after C)

Let the cached shell render against last-known auth state rather than forcing anonymous when the server is unreachable. Mostly falls out of Stage A's fix; listed separately because it only matters once C exists.

## Azure Functions for the CRUDL layer: assessed, not recommended

It does not address the dominant cost. The 60–90 seconds is the **database** resume, and Functions on Consumption hit exactly the same paused SQL. You would trade an App Service cold start (seconds) for a Functions cold start (also seconds) and leave the large one untouched.

It would decouple API calls from the F1 CPU quota, which has some value, but it is a significant rework of the API layer for the smaller half of the problem — and Stages A–C deliver more, for less, without touching the API surface. If Functions are attractive for other reasons (cost model, learning), that is a separate decision.

## What this buys, and what it does not

Buys, after Stages A–C:

- Open the app: instant from cache, after first visit
- Start and stop a timer: instant, works fully offline, accurate durations
- The 60–90s resume moves out of the critical path and into background sync

Does not buy:

- First-ever visit still downloads the bundle
- Reports, project lists, admin, and anything needing fresh server data still wait for the wake
- Nothing about the deploy pipeline's migrate job

So the *daily* workflow becomes hosting-independent. The rest of the app still feels the free tier.

## Verdict

Stages A and B are worth doing on their merits — they are correct engineering for a network-dependent app regardless of where it is hosted, and they carry over intact if you later migrate platforms. Stage A in particular is small and fixes a genuine bug.

Stage C is the completionist path. It is where effort and risk climb, and it is reasonable to stop before it and simply keep the tab open.

If after Stage A the app still is not worth using over Clockify, that is a fair signal to park it or move platforms — and Stages A and B would not be wasted, because they are client-side and portable.

## Existing branch work

`claude/pull-request-review-nk0zur` carries two commits. Recommendation before starting:

- **Keep** `90f1b8d` — the bounded EF retry policy and the 40613 fix in `DatabaseWarmupMiddleware`. Small, tested, and it fixes the failing deploy.
- **Reset away** `54ca5f5` — `SchemaGuard` / `StartupVerificationService` and the UI additions. It is oversized, contains an unbounded retry loop, and this plan supersedes most of it. The two ideas worth salvaging (pinning the stop instant, not blocking boot on the database) return properly scoped in Stage A and a much smaller startup change.

Also outstanding and unrelated: the migrate job on `main` is still red and needs a re-run.

## Verification

Per stage, before anything is pushed:

- `dotnet test TimeTracker.Tests --filter "Category!=Container" && dotnet test TimeTracker.ComponentTests`
- **Stage A:** unit tests that a transport failure does not cache anonymous, and that a definitive anonymous response does; a component test that a failed stop persists `pendingStop` and a later retry sends the original timestamp.
- **Stage B:** service test that replaying a create with the same `ClientRequestId` inserts one row; container test for the unique index under RLS.
- **Manual, the only test that really counts:** let the database auto-pause (or stop the local SQL container), then start and stop a timer and confirm it syncs afterwards with the correct duration.
- Stage C additionally needs a real deployed check — load the app, go offline in devtools, reload, confirm the shell renders.
