# TimeTracker — UI Mockup (DZK Consulting)

A static, interactive design mockup for the MudBlazor-styled, mobile-first TimeTracker
UI. Built with React + Babel (in-browser) purely for design review — **no build step,
no backend**. All data is in-memory mock data (`app/data.js`); create/edit actions show
a confirmation toast but do not persist.

## View it

Open `TimeTracker App.html` in a browser (needs a static file server because it loads
sibling files — e.g. `npx serve` or VS Code "Live Server"). Opening directly via
`file://` may block the module/script loads in some browsers.

- **TimeTracker App.html** — the app itself (responsive: desktop fly-out rail + mobile
  bottom nav / FAB).
- **TimeTracker Mockups.html** — both layouts side-by-side in device frames (desktop +
  phone) for review.

## Structure

```
app/
  app.jsx         root: state, layout, navigation, sheets
  components.jsx  shared MudBlazor-styled primitives + icons
  screens.jsx     Login, Timer, Time Entries
  screens2.jsx    Reports, Projects, Project detail, Clients, entry/project/client sheets
  data.js         mock domain data
  theme.css       MudBlazor-flavoured design tokens + component styles
  assets/         logo
```

## Model alignment

The `Client` shape in `data.js` mirrors the intended C# entity:

```
Client : SoftDeleteableEntity
{
  string  Name
  decimal? DefaultHourlyRate   // null => no default rate ("internal")
  string? ContactName
  string? ContactEmail
  string? ContactPhone
  bool    IsArchived
  List<Project> Projects
}
```

`color` in the mock is a UI-only accent and is **not** part of the entity.

> This is a design artifact, not production code. The real implementation is a
> Blazor + MudBlazor app.
