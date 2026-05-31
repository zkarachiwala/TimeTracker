/* TimeTracker — mock data (consulting timesheet domain) */
window.TT = (function () {
  // Mirrors the C# Client model: inherits SoftDeleteableEntity (IsDeleted handled by
  // the base — soft-deleted rows are filtered out and never reach the UI). Fields:
  // Name, DefaultHourlyRate (nullable), ContactName/Email/Phone, IsArchived.
  // `color` is a UI-only accent (not persisted).
  const CLIENTS = [
    { id: 1, name: "Acme Corp", defaultHourlyRate: 185, contactName: "Dana Whitfield",
      contactEmail: "dana@acmecorp.com", contactPhone: "+61 2 9000 1100", isArchived: false, color: "#0068CD" },
    { id: 2, name: "Zenith Health", defaultHourlyRate: 195, contactName: "Dr. Priya Nair",
      contactEmail: "p.nair@zenithhealth.org", contactPhone: "+61 3 9555 2200", isArchived: false, color: "#1FACF2" },
    { id: 3, name: "Northwind Logistics", defaultHourlyRate: 170, contactName: "Marco Reyes",
      contactEmail: "marco@northwind.io", contactPhone: "+61 7 3100 4400", isArchived: false, color: "#002F6F" },
    { id: 4, name: "DZK Consulting", defaultHourlyRate: null, contactName: "Zak Karachiwala",
      contactEmail: "zak@dzk.com.au", contactPhone: "+61 4 1200 8800", isArchived: false, color: "#6a7a8c" },
    { id: 5, name: "Meridian Bank", defaultHourlyRate: 210, contactName: "Helen Cho",
      contactEmail: "h.cho@meridianbank.com", contactPhone: "+61 2 8200 6600", isArchived: true, color: "#2e7d32" },
  ];

  const PROJECTS = [
    { id: 1, name: "Acme Corp — Platform Rebuild", clientId: 1, color: "#0068CD",
      description: "Migration of the legacy asset-management system to a .NET 10 + Blazor stack.",
      startDate: "2026-02-02", endDate: null, rate: 185 },
    { id: 2, name: "Zenith Health — Data Warehouse", clientId: 2, color: "#1FACF2",
      description: "Azure Synapse data warehouse + Power BI reporting layer for clinical metrics.",
      startDate: "2026-03-16", endDate: null, rate: 195 },
    { id: 3, name: "Northwind Logistics — Mobile App", clientId: 3, color: "#002F6F",
      description: "Driver-facing PWA for route tracking and proof-of-delivery capture.",
      startDate: "2026-01-12", endDate: null, rate: 170 },
    { id: 4, name: "Internal — DZK Ops", clientId: 4, color: "#6a7a8c",
      description: "Internal tooling, admin, business development and proposals.",
      startDate: "2026-01-01", endDate: null, rate: 0 },
    { id: 5, name: "Meridian Bank — Audit Portal", clientId: 5, color: "#2e7d32",
      description: "Compliance audit evidence portal. Phase 1 discovery complete.",
      startDate: "2025-11-03", endDate: "2026-04-30", rate: 210 },
  ];

  const clientById = (id) => CLIENTS.find((c) => c.id === id);
  const projectById = (id) => PROJECTS.find((p) => p.id === id);
  // convenience: a project's client display name (back-compat for `p.client`)
  const clientName = (p) => (clientById(p.clientId) || {}).name || "—";
  PROJECTS.forEach((p) => { Object.defineProperty(p, "client", { get() { return clientName(p); } }); });

  // Build a realistic set of entries across recent days.
  // Helper to make a date at h:m on an offset-from-today day.
  function at(dayOffset, h, m) {
    const d = new Date();
    d.setDate(d.getDate() + dayOffset);
    d.setHours(h, m, 0, 0);
    return d;
  }

  let _id = 100;
  function entry(dayOffset, projectId, sh, sm, eh, em, note) {
    return {
      id: ++_id, projectId,
      start: at(dayOffset, sh, sm),
      end: eh == null ? null : at(dayOffset, eh, em),
      note: note || "",
    };
  }

  const ENTRIES = [
    // Today
    entry(0, 1, 9, 0, 10, 30, "Standup + sprint planning"),
    entry(0, 1, 10, 45, 12, 30, "Auth service refactor"),
    entry(0, 2, 13, 30, 15, 0, "Synapse pipeline review"),
    // Yesterday
    entry(-1, 3, 8, 30, 11, 0, "PoD capture screen"),
    entry(-1, 1, 11, 30, 13, 0, "Code review"),
    entry(-1, 4, 14, 0, 15, 15, "Proposal — Lumen Retail"),
    entry(-1, 2, 15, 30, 17, 45, "Power BI dataset modelling"),
    // -2
    entry(-2, 1, 9, 0, 12, 30, "EF Core migration debugging"),
    entry(-2, 2, 13, 30, 16, 0, "Stakeholder workshop"),
    // -3
    entry(-3, 3, 9, 0, 12, 0, "Offline sync logic"),
    entry(-3, 3, 13, 0, 17, 0, "Map SDK integration"),
    // -4
    entry(-4, 1, 9, 30, 13, 0, "API endpoints — time entries"),
    entry(-4, 4, 14, 0, 16, 0, "Internal — recruiting"),
    // -5
    entry(-5, 2, 9, 0, 12, 30, "Clinical metrics ETL"),
    entry(-5, 1, 13, 30, 18, 0, "Blazor UI work"),
    // older spread for month/year totals
    entry(-8, 1, 9, 0, 17, 0, ""),
    entry(-9, 2, 9, 0, 16, 30, ""),
    entry(-10, 3, 9, 0, 17, 30, ""),
    entry(-12, 1, 9, 0, 17, 0, ""),
    entry(-15, 2, 9, 0, 15, 0, ""),
    entry(-18, 3, 9, 0, 17, 0, ""),
    entry(-22, 1, 9, 0, 16, 0, ""),
    entry(-26, 4, 10, 0, 14, 0, ""),
  ];

  // Monthly aggregate hours per project for the year chart (Jan–current).
  const MONTHLY = [
    { m: "Jan", hrs: 96 }, { m: "Feb", hrs: 142 }, { m: "Mar", hrs: 168 },
    { m: "Apr", hrs: 154 }, { m: "May", hrs: 131 }, { m: "Jun", hrs: 0 },
    { m: "Jul", hrs: 0 }, { m: "Aug", hrs: 0 }, { m: "Sep", hrs: 0 },
    { m: "Oct", hrs: 0 }, { m: "Nov", hrs: 0 }, { m: "Dec", hrs: 0 },
  ];

  // Year-to-date hours by project (for breakdown bars)
  const YTD = [
    { projectId: 1, hrs: 312 },
    { projectId: 2, hrs: 248 },
    { projectId: 3, hrs: 176 },
    { projectId: 4, hrs: 64 },
    { projectId: 5, hrs: 91 },
  ];

  return { PROJECTS, CLIENTS, ENTRIES, MONTHLY, YTD, projectById, clientById, clientName };
})();
