# Plan: Issue #137 — Award rate

**Branch:** `feature/issue-137-award-rate`
**Issue:** [#137](https://github.com/zkarachiwala/TimeTracker/issues/137)

## Decisions and tech debt to record

- **D025** (decisions.md): Use `PublicHoliday` NuGet for AU public holiday resolution (`Nager.Date` rejected — requires paid license key)
- **TD25** (technical-debt.md): Public holiday jurisdiction hardcoded to national AU — no user or client/project location tracked. Ambiguity unresolved: should the jurisdiction follow the user or the client/project? Needs external investigation before state-level holidays can be implemented.

---

## Phase 1 — Documentation

- [x] Add D025 to `docs/decisions.md`
- [x] Add TD25 to `docs/technical-debt.md`

## Phase 2 — Data model

- [ ] Add `AwardRate (decimal?)` to `TimeTracker.Shared/Entities/Client.cs`
- [ ] EF Core migration: `AddAwardRateToClient` (TimeTrackerDataContext)
- [ ] Add `AwardRate` to `ClientResponse`, `ClientRequest`, `ClientCreateRequest`, `ClientUpdateRequest` in `TimeTracker.Contracts/Features/Clients/ClientModels.cs`
- [ ] Update `ClientService.CreateClient` and `UpdateClient` in `TimeTracker.Web/Features/Clients/ClientService.cs`
- [ ] Add "Award rate (AUD)" `MudNumericField` to `TimeTracker.Client/Features/Clients/Components/ClientSheet.razor` (below "Default hourly rate")
- [ ] Update `Reset()` and `Save()` in `ClientSheet.razor` to handle `awardRate` field

## Phase 3 — Rate resolution service

- [ ] Add `Nager.Date` NuGet to `TimeTracker.Web`
- [ ] Create `TimeTracker.Web/Features/AwardRate/IAwardRateResolver.cs`:
  - `(decimal? EffectiveRate, bool IsAwardRate) Resolve(DateTime entryDate, decimal? projectRate, decimal? clientAwardRate)`
  - Weekend: `entryDate.DayOfWeek` is Saturday or Sunday → use `clientAwardRate` (if set), else `projectRate`
  - Public holiday: `DateSystem.IsPublicHoliday(entryDate, CountryCode.AU)` → same logic
  - Weekday non-holiday: return `(projectRate, false)`
- [ ] Create `TimeTracker.Web/Features/AwardRate/AwardRateResolver.cs` implementing `IAwardRateResolver`
- [ ] Register `IAwardRateResolver` as scoped in `Program.cs`

## Phase 4 — Enrich TimeEntryResponse

- [ ] Add `decimal? EffectiveRate = null` and `bool IsAwardRate = false` to `TimeEntryResponse` record in `TimeTracker.Contracts/Features/TimeEntries/TimeEntryModels.cs` (default params — all existing callers compile unchanged)
- [ ] Update `UserEntries` query in `TimeEntryService` to `.ThenInclude(p => p.Client)` so client data is loaded alongside each entry
- [ ] Inject `IAwardRateResolver` into `TimeEntryService`
- [ ] Add private helper `Enrich(TimeEntryResponse dto, TimeEntry entity)` that calls the resolver and returns the enriched record via `with { EffectiveRate = ..., IsAwardRate = ... }`
- [ ] Apply `Enrich` in all service methods that currently call `Adapt<TimeEntryResponse>()` or `Adapt<List<TimeEntryResponse>>()`

## Phase 5 — UI indicator

- [ ] In `TimeTracker.Client/Features/TimeEntries/Components/EntryRow.razor`: show a small "Award" `MudChip` or `MudTooltip` badge when `Entry.IsAwardRate == true`

## Phase 6 — Tests

- [ ] `TimeTracker.Tests/Features/AwardRate/AwardRateResolverTests.cs`:
  - Weekday, no holiday → returns `(projectRate, false)`
  - Saturday → returns `(clientAwardRate, true)` when award rate is set
  - Sunday → returns `(clientAwardRate, true)` when award rate is set
  - Public holiday (weekday) → returns `(clientAwardRate, true)` when award rate is set
  - Award rate not set, weekend → returns `(projectRate, false)`
- [ ] Update `TimeTracker.Tests/Features/Clients/ClientServiceTests.cs` to cover `AwardRate` field in create/update
- [ ] `ReportsCalculationsTests` — no changes needed (signature unchanged; `EffectiveRate` defaulted to null)

## Phase 7 — SESSION.md + commit

- [ ] Update `SESSION.md` with current state
- [ ] Commit all changes and raise PR closing #137
