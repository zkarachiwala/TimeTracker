# TimeTracker — Active Plan

## Phase 4 — External OAuth ✅

Completed on `feature/google-auth`. See `docs/roadmap.md` for summary.

**Before merging:** set user secrets and smoke-test the full login/logout flow locally.

```bash
cd TimeTracker.Web
dotnet user-secrets set "Authentication:Google:ClientId" "<your-client-id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<your-client-secret>"
dotnet user-secrets set "Authentication:AllowedEmails:0" "zak.karachiwala@gmail.com"
```

See `docs/google-oauth-setup.md` for Google Cloud Console steps.

---

## Next: Phase 5 — MudBlazor UI uplift

**Branch:** `feature/mudblazor-ui`

Replace Tailwind + Radzen + QuickGrid with MudBlazor. Mobile-first responsive design.

- `MudLayout` + responsive `MudNavMenu` drawer (phone and desktop)
- `MudDataGrid` replaces QuickGrid
- `MudDialog`, `MudTextField`, `MudSelect`, `MudDatePicker` for forms
- MudBlazor Snackbar replaces any remaining toast usage
- `MudChart` evaluated as replacement for Radzen year chart
- Tailwind CSS and Radzen removed
