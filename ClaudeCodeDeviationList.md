# Deviations from mockup design

  Now the honest deviation list. These are places where the implementation diverges from the mockup that I should have surfaced as design decisions:
  
  1. Reports bar chart — month labels missing
  The mockup shows Jan–Dec labels on the x-axis. I removed XAxisLabels because MudBlazor 9's analyzer flagged it. The chart renders but without month labels. Need to find the correct MudBlazor 9
  API for this.

  2. Reports bar chart — visual fidelity
  The mockup has a custom-styled bar chart with coloured bars. MudBlazor's MudChart is much more basic — single colour, different proportions, no custom styling. This is a fundamental MudBlazor
  limitation I should have flagged during planning.

  3. Project colour dots — not client-specific
  The mockup assigns specific brand colours per client (Acme = DZK blue, Zenith = cyan, etc.). My implementation assigns colours deterministically by project ID from a fixed palette. The colours
  will differ from the mockup.

  4. Bottom navigation — visual approximation
  The mockup's bottom nav has specific spacing, active-state animation (icon lifts slightly), and typography that I've approximated in CSS. It may not be pixel-perfect.

  5. Timer card gradient
  The mockup's running timer card has a specific navy → dark blue → DZK blue gradient. I applied this via a CSS class, but MudBlazor's MudCard may override background styles.

  6. MudProgressLinear colour override on Reports
  I attempted to override the bar colour per-project using a CSS variable in an inline style. This likely doesn't work — all bars will be the primary colour, not project-specific colours.

  7. Entry sheet / project sheet — slide-up animation
  The mockup shows a smooth sheet sliding up from the bottom. MudDrawer with Anchor.Bottom has its own animation that may look different.

  8. MudChip appearance
  The mockup's chips (rate chip, filter chips, billing toggle) have specific sizing and styling. MudBlazor's MudChip defaults may differ.

  ---
  These need to be stepped through as design decisions before I address them. Some are fixable (chart labels, gradient), some require choosing between fidelity and platform constraints (chart
  styling, MudDrawer animation). How do you want to proceed?
