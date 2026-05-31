/* TimeTracker — root app: state, layout, navigation */

const NAV = [
  { id: "timer", label: "Timer", icon: "timer" },
  { id: "entries", label: "Entries", icon: "view_list" },
  { id: "reports", label: "Reports", icon: "bar_chart" },
  { id: "projects", label: "Projects", icon: "folder_open" },
  { id: "clients", label: "Clients", icon: "business" },
];

function App() {
  const params = new URLSearchParams(location.search);
  const [authed, setAuthed] = useState(params.get("auth") === "1");
  const [tab, setTab] = useState(params.get("screen") || "timer");
  const [entries, setEntries] = useState(() => TT.ENTRIES.slice());
  const [running, setRunning] = useState(
    params.get("running") === "1" ? { projectId: 1, start: new Date(Date.now() - 4145000) } : null
  );
  const [sheet, setSheet] = useState(null);      // entry object or null
  const [snack, setSnack] = useState(null);
  const [openProject, setOpenProject] = useState(params.get("project") ? +params.get("project") : null);
  const [drawerOpen, setDrawerOpen] = useState(() =>
    typeof window !== "undefined" && window.matchMedia("(min-width: 900px)").matches);
  const [projectSheet, setProjectSheet] = useState(null);
  const [clientSheet, setClientSheet] = useState(null);

  // entries filter state
  const [filter, setFilter] = useState("Day");
  const [cursor, setCursor] = useState(new Date());
  const [projFilter, setProjFilter] = useState(0);

  const lastProject = useRef(TT.PROJECTS[0].id);
  const safe = params.get("safe") === "1";

  useEffect(() => {
    if (params.get("sheet") === "1") {
      const start = new Date(); start.setMinutes(0, 0, 0);
      setSheet({ projectId: lastProject.current, start, end: null, note: "" });
    }
  }, []);

  function toast(msg, icon = "check_circle") {
    setSnack({ msg, icon });
    clearTimeout(toast._t);
    toast._t = setTimeout(() => setSnack(null), 2600);
  }

  function startTimer(projectId, fixedMins) {
    lastProject.current = projectId;
    if (fixedMins) {
      const end = new Date();
      const start = new Date(end.getTime() - fixedMins * 60000);
      const e = { id: Date.now(), projectId, start, end, note: "" };
      setEntries((x) => [e, ...x]);
      toast(`Logged ${fixedMins < 60 ? fixedMins + "m" : fixedMins / 60 + "h"} block`);
      return;
    }
    setRunning({ projectId, start: new Date() });
    toast("Timer started", "play_circle");
  }

  function stopTimer() {
    if (!running) return;
    const e = { id: Date.now(), projectId: running.projectId, start: running.start, end: new Date(), note: "" };
    setEntries((x) => [e, ...x]);
    setRunning(null);
    toast(`Saved ${fmtDurShort(e.end - e.start)}`);
  }

  function saveEntry(data) {
    if (data.id) {
      setEntries((x) => x.map((e) => (e.id === data.id ? { ...e, ...data } : e)));
      toast("Entry updated");
    } else {
      setEntries((x) => [{ ...data, id: Date.now() }, ...x]);
      toast("Entry added");
    }
    lastProject.current = data.projectId;
    setSheet(null);
  }

  function deleteEntry(id) {
    setEntries((x) => x.filter((e) => e.id !== id));
    setSheet(null);
    toast("Entry deleted", "delete");
  }

  function openNew() {
    const start = new Date(); start.setMinutes(0, 0, 0);
    setSheet({ projectId: lastProject.current, start, end: null, note: "" });
  }

  function openNewProject() {
    setProjectSheet({ name: "", clientId: null, rate: null, description: "" });
  }

  function saveProject() {
    setProjectSheet(null);
    toast("Project created");
  }

  function openNewClient() {
    setClientSheet({ name: "", defaultHourlyRate: 150, isArchived: false, contactName: "", contactEmail: "", contactPhone: "" });
  }

  function saveClient(data) {
    setClientSheet(null);
    toast(data.id ? "Client updated" : `Added ${data.name || "client"}`);
  }

  // navigate + auto-close the drawer on phones
  const isMobile = () => !window.matchMedia("(min-width: 900px)").matches;
  function go(id) {
    setTab(id);
    setOpenProject(null);
    if (isMobile()) setDrawerOpen(false);
  }

  function createOnTab() {
    if (tab === "projects") openNewProject();
    else if (tab === "clients") openNewClient();
    else openNew();
  }

  if (!authed) return <LoginScreen onLogin={() => setAuthed(true)} />;

  const titleMap = { timer: "TimeTracker", entries: "Time Entries", reports: "Reports", projects: "Projects", clients: "Clients" };

  function renderScreen() {
    if (openProject) return <ProjectDetail projectId={openProject} entries={entries} onBack={() => setOpenProject(null)} onEdit={setSheet} />;
    switch (tab) {
      case "timer": return <TimerScreen entries={entries} running={running} defaultProject={lastProject.current} onStart={startTimer} onStop={stopTimer} onEdit={setSheet} />;
      case "entries": return <EntriesScreen entries={entries} filter={filter} setFilter={setFilter} cursor={cursor} setCursor={setCursor} projFilter={projFilter} setProjFilter={setProjFilter} onEdit={setSheet} />;
      case "reports": return <ReportsScreen entries={entries} />;
      case "projects": return <ProjectsScreen entries={entries} onOpen={setOpenProject} />;
      case "clients": return <ClientsScreen onEdit={setClientSheet} />;
    }
  }

  const showFab = !openProject && (tab === "entries" || tab === "projects" || tab === "timer" || tab === "clients");

  return (
    <div className="app-shell">
      {/* ---- Fly-out rail / drawer ---- */}
      <aside className={`mud-drawer ${drawerOpen ? "open" : "closed"}`}>
        <div className="drawer-brand">
          <img src="app/assets/dzk-icon.png" alt="DZK" />
          <div className="col">
            <span className="fw-700" style={{ color: "var(--dzk-navy)", lineHeight: 1.1 }}>TimeTracker</span>
            <span className="mud-caption" style={{ lineHeight: 1.1 }}>DZK Consulting</span>
          </div>
        </div>
        <div style={{ padding: "10px 0", flex: 1 }}>
          {NAV.map((n) => (
            <div key={n.id} className={`nav-link ${tab === n.id && !openProject ? "active" : ""}`}
              onClick={() => go(n.id)}>
              <Icon name={n.icon} /> {n.label}
            </div>
          ))}
        </div>
        <div style={{ padding: 12, borderTop: "1px solid var(--mud-divider-l)" }}>
          <div className="row gap12" style={{ alignItems: "center", padding: "4px 6px" }}>
            <Avatar>ZK</Avatar>
            <div className="col grow" style={{ minWidth: 0 }}>
              <span className="mud-body2 fw-500" style={{ lineHeight: 1.2 }}>Zak Karachiwala</span>
              <span className="mud-caption" style={{ lineHeight: 1.2 }}>zak@dzk.com.au</span>
            </div>
            <IconBtn icon="logout" className="dark" onClick={() => setAuthed(false)} title="Sign out" />
          </div>
        </div>
      </aside>
      {drawerOpen && <div className="drawer-scrim" onClick={() => setDrawerOpen(false)} />}

      {/* ---- Main column ---- */}
      <div className="mud-layout grow">
        <header className="mud-appbar">
          {openProject ? (
            <IconBtn icon="arrow_back" onClick={() => setOpenProject(null)} />
          ) : (
            <IconBtn icon="menu" onClick={() => setDrawerOpen((o) => !o)} title="Menu" />
          )}
          <span className="title grow" style={{ marginLeft: 4 }}>{openProject ? "Project" : titleMap[tab]}</span>
          {running && (
            <span className="row gap4 show-mobile" style={{ alignItems: "center", marginRight: 4,
              background: "rgba(255,255,255,.18)", padding: "4px 10px", borderRadius: 20, fontSize: ".8rem" }}
              onClick={() => go("timer")}>
              <span className="pulse-dot" /> <RunningMini running={running} />
            </span>
          )}
          <IconBtn icon="search" />
          <Avatar className="show-desktop" style={{ marginLeft: 6 }}>ZK</Avatar>
        </header>

        <main className="mud-main">
          {renderScreen()}
        </main>

        {showFab && (
          <button className="mud-fab" onClick={createOnTab} title={tab === "projects" ? "New project" : tab === "clients" ? "New client" : "New entry"}><Icon name="add" /></button>
        )}
        {snack && (
          <div className="snackbar">
            <Icon name={snack.icon} className="sm" style={{ color: "var(--dzk-cyan)" }} />
            <span className="grow">{snack.msg}</span>
          </div>
        )}

        {/* ---- Mobile bottom nav ---- */}
        <nav className="bottom-nav show-mobile">
          {NAV.map((n) => (
            <div key={n.id} className={`item ${tab === n.id && !openProject ? "active" : ""}`}
              onClick={() => go(n.id)}>
              <Icon name={n.icon} /><span className="lbl">{n.label}</span>
            </div>
          ))}
        </nav>
      </div>

      {sheet && <EntrySheet entry={sheet} onSave={saveEntry} onDelete={deleteEntry} onClose={() => setSheet(null)} />}
      {projectSheet && <ProjectSheet entry={projectSheet} onSave={saveProject} onClose={() => setProjectSheet(null)} onAddClient={() => { setProjectSheet(null); openNewClient(); }} />}
      {clientSheet && <ClientSheet client={clientSheet} onSave={saveClient} onClose={() => setClientSheet(null)} />}
    </div>
  );
}

function RunningMini({ running }) {
  const [now, setNow] = useState(Date.now());
  useEffect(() => { const t = setInterval(() => setNow(Date.now()), 1000); return () => clearInterval(t); }, []);
  return <span className="tabnum">{fmtTimer(now - running.start.getTime())}</span>;
}

ReactDOM.createRoot(document.getElementById("root")).render(<App />);
