/* TimeTracker — screens (login, timer/today, entries, reports, projects, entry sheet) */

/* ============ LOGIN ============ */
function LoginScreen({ onLogin }) {
  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", alignItems: "center",
      justifyContent: "center", padding: 28, background:
      "radial-gradient(120% 90% at 50% 0%, #eef4fb 0%, #f4f6f9 55%)", textAlign: "center" }}>
      <div style={{ maxWidth: 360, width: "100%" }}>
        <img src="app/assets/dzk-logo.png" alt="DZK Consulting"
          style={{ height: 92, objectFit: "contain", marginBottom: 10 }} />
        <div className="mud-h5 fw-500" style={{ color: "var(--dzk-navy)" }}>TimeTracker</div>
        <div className="mud-body2 t-muted mt4" style={{ marginBottom: 30 }}>
          Timesheets for DZK Consulting engagements.
        </div>
        <Card className="e4" style={{ padding: "28px 24px", textAlign: "left" }}>
          <div className="mud-overline">Sign in</div>
          <div className="mud-body2 t-muted mb16" style={{ marginTop: 2 }}>
            Use your DZK Google Workspace account.
          </div>
          <button className="mud-btn outlined neutral block lg" onClick={onLogin}
            style={{ justifyContent: "center", gap: 12, textTransform: "none", fontSize: ".95rem" }}>
            <svg width="20" height="20" viewBox="0 0 48 48"><path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/><path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/><path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"/><path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/></svg>
            Sign in with Google
          </button>
          <div className="mud-divider mt24" style={{ marginBottom: 16 }} />
          <button className="mud-btn text block neutral" onClick={onLogin} style={{ justifyContent: "center" }}>
            <Icon name="vpn_key" className="sm" /> Continue with email
          </button>
        </Card>
        <div className="mud-caption" style={{ marginTop: 22 }}>© 2026 DZK Consulting · v0.5</div>
      </div>
    </div>
  );
}

/* ============ TIMER / TODAY (primary mobile screen) ============ */
function TimerHero({ running, defaultProject, onStart, onStop }) {
  const [now, setNow] = useState(Date.now());
  const [proj, setProj] = useState(defaultProject);
  useEffect(() => {
    if (!running) return;
    const t = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(t);
  }, [running]);

  if (running) {
    const p = TT.projectById(running.projectId);
    const elapsed = now - running.start.getTime();
    return (
      <Card className="e4" style={{ background: "linear-gradient(135deg, var(--dzk-navy) 0%, #013a86 60%, var(--dzk-blue) 130%)",
        color: "#fff", borderRadius: 16 }}>
        <div className="mud-card-body" style={{ padding: 20 }}>
          <div className="row between" style={{ alignItems: "flex-start" }}>
            <div className="row gap8" style={{ alignItems: "center" }}>
              <span className="dot" style={{ background: p.color, boxShadow: "0 0 0 3px rgba(255,255,255,.25)" }} />
              <span className="mud-overline" style={{ color: "rgba(255,255,255,.8)" }}>Tracking now</span>
            </div>
            <span className="row gap4" style={{ alignItems: "center", fontSize: ".7rem", color: "rgba(255,255,255,.8)" }}>
              <span className="pulse-dot" /> LIVE
            </span>
          </div>
          <div className="fw-500" style={{ fontSize: "1.05rem", margin: "10px 0 2px" }}>{p.name}</div>
          <div className="tabnum" style={{ fontSize: "3.2rem", fontWeight: 300, letterSpacing: "1px", lineHeight: 1.1 }}>
            {fmtTimer(elapsed)}
          </div>
          <div className="mud-caption" style={{ color: "rgba(255,255,255,.7)", marginBottom: 16 }}>
            Started {fmtClock(running.start)}
          </div>
          <button className="mud-btn block lg" onClick={onStop}
            style={{ background: "#fff", color: "var(--dzk-navy)", boxShadow: "var(--e4)" }}>
            <Icon name="stop" /> Stop &amp; save
          </button>
        </div>
      </Card>
    );
  }

  return (
    <Card className="e4" style={{ borderRadius: 16 }}>
      <div className="mud-card-body" style={{ padding: 20 }}>
        <div className="mud-overline">Start a timer</div>
        <div style={{ marginTop: 12 }}>
          <Field label="Project" float>
            <Select value={proj} onChange={(e) => setProj(+e.target.value)}>
              {TT.PROJECTS.filter((p) => !p.endDate).map((p) => (
                <option key={p.id} value={p.id}>{p.name}</option>
              ))}
            </Select>
          </Field>
        </div>
        <button className="mud-btn filled block lg" onClick={() => onStart(proj)}
          style={{ marginTop: 2, height: 52 }}>
          <Icon name="play_arrow" /> Start timer
        </button>
        <div className="row gap8 wrap" style={{ marginTop: 14, justifyContent: "center" }}>
          <span className="mud-caption" style={{ width: "100%", textAlign: "center", marginBottom: 2 }}>or log a fixed block</span>
          {[15, 30, 60, 90].map((m) => (
            <Chip key={m} variant="outlined" onClick={() => onStart(proj, m)}>
              <Icon name="bolt" className="sm" />{m < 60 ? `${m}m` : `${m / 60}h`}
            </Chip>
          ))}
        </div>
      </div>
    </Card>
  );
}

function EntryRow({ e, onEdit, showProject = true }) {
  const p = TT.projectById(e.projectId);
  return (
    <div className="mud-list-item" onClick={() => onEdit(e)} style={{ padding: "12px 4px" }}>
      <span className="dot" style={{ background: p.color, width: 10, height: 10 }} />
      <div className="grow" style={{ minWidth: 0 }}>
        <div className="mud-body2 fw-500" style={{ whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
          {showProject ? p.name : (e.note || "Time entry")}
        </div>
        <div className="mud-caption">
          {showProject && e.note ? e.note + " · " : ""}{fmtClock(e.start)} – {e.end ? fmtClock(e.end) : "now"}
        </div>
      </div>
      <div className="tabnum fw-500" style={{ fontSize: ".95rem" }}>{fmtDurShort(durationOf(e))}</div>
      <Icon name="chevron_right" className="t-muted3" />
    </div>
  );
}

function TimerScreen({ entries, running, defaultProject, onStart, onStop, onEdit }) {
  const today = new Date();
  const todays = entries.filter((e) => sameDay(e.start, today)).sort((a, b) => b.start - a.start);
  const total = todays.reduce((s, e) => s + durationOf(e), 0);
  const goal = 8 * 3600000;
  return (
    <div className="mud-content">
      <TimerHero running={running} defaultProject={defaultProject} onStart={onStart} onStop={onStop} />

      <div className="row between mt24 mb8" style={{ alignItems: "baseline" }}>
        <span className="mud-h6">Today</span>
        <span className="mud-body2 t-muted tabnum">{fmtDur(total)} / 8h</span>
      </div>
      <div className="bartrack mb16" style={{ height: 6 }}>
        <div className="barfill" style={{ width: `${Math.min(100, (total / goal) * 100)}%` }} />
      </div>

      <Card>
        <div className="mud-list" style={{ padding: "4px 12px" }}>
          {todays.length === 0 && (
            <div className="col center" style={{ padding: "36px 0", color: "var(--mud-text-3)" }}>
              <Icon name="hourglass_empty" className="lg" />
              <span className="mud-body2 mt8">No time logged yet today</span>
            </div>
          )}
          {todays.map((e, i) => (
            <React.Fragment key={e.id}>
              <EntryRow e={e} onEdit={onEdit} />
              {i < todays.length - 1 && <hr className="mud-divider" />}
            </React.Fragment>
          ))}
        </div>
      </Card>
      <div style={{ height: 90 }} />
    </div>
  );
}

/* ============ ENTRIES (filterable list) ============ */
const FILTERS = ["Day", "Month", "Year", "Project"];
function EntriesScreen({ entries, filter, setFilter, cursor, setCursor, projFilter, setProjFilter, onEdit }) {
  // determine range
  const ref = cursor;
  let inRange, rangeLabel;
  if (filter === "Day") {
    inRange = (e) => sameDay(e.start, ref);
    rangeLabel = dayLabel(ref) + ` · ${ref.getDate()} ${MON[ref.getMonth()].slice(0,3)}`;
  } else if (filter === "Month") {
    inRange = (e) => e.start.getMonth() === ref.getMonth() && e.start.getFullYear() === ref.getFullYear();
    rangeLabel = `${MON[ref.getMonth()]} ${ref.getFullYear()}`;
  } else if (filter === "Year") {
    inRange = (e) => e.start.getFullYear() === ref.getFullYear();
    rangeLabel = `${ref.getFullYear()}`;
  } else {
    inRange = (e) => projFilter === 0 || e.projectId === projFilter;
    rangeLabel = projFilter === 0 ? "All projects" : TT.projectById(projFilter).name;
  }
  const list = entries.filter(inRange).sort((a, b) => b.start - a.start);
  const total = list.reduce((s, e) => s + durationOf(e), 0);

  // group by day
  const groups = [];
  list.forEach((e) => {
    const key = e.start.toDateString();
    let g = groups.find((x) => x.key === key);
    if (!g) { g = { key, date: e.start, items: [] }; groups.push(g); }
    g.items.push(e);
  });

  function step(dir) {
    const d = new Date(cursor);
    if (filter === "Day") d.setDate(d.getDate() + dir);
    else if (filter === "Month") d.setMonth(d.getMonth() + dir);
    else if (filter === "Year") d.setFullYear(d.getFullYear() + dir);
    setCursor(d);
  }

  return (
    <div>
      <div className="mud-tabs">
        {FILTERS.map((f) => (
          <div key={f} className={`mud-tab ${filter === f ? "active" : ""}`} onClick={() => setFilter(f)}>{f}</div>
        ))}
      </div>

      <div className="mud-content" style={{ paddingTop: 12 }}>
        {/* range stepper / project picker */}
        {filter === "Project" ? (
          <div className="mb16">
            <Field label="Project" float>
              <Select value={projFilter} onChange={(e) => setProjFilter(+e.target.value)}>
                <option value={0}>All projects</option>
                {TT.PROJECTS.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
              </Select>
            </Field>
          </div>
        ) : (
          <Card className="flat mb16" style={{ borderRadius: 10 }}>
            <div className="row between" style={{ padding: "6px 6px 6px 16px", alignItems: "center" }}>
              <IconBtn icon="chevron_left" className="dark" onClick={() => step(-1)} />
              <span className="mud-subtitle1 fw-500" style={{ textAlign: "center", flex: 1 }}>{rangeLabel}</span>
              <IconBtn icon="chevron_right" className="dark" onClick={() => step(1)} />
            </div>
          </Card>
        )}

        {/* summary card */}
        <Card className="mb16" style={{ background: "var(--dzk-navy)", color: "#fff", borderRadius: 12 }}>
          <div className="row between mud-card-body" style={{ padding: "14px 18px", alignItems: "center" }}>
            <div className="col">
              <span className="mud-overline" style={{ color: "rgba(255,255,255,.7)" }}>Total tracked</span>
              <span className="tabnum" style={{ fontSize: "1.9rem", fontWeight: 300 }}>{fmtDur(total)}</span>
            </div>
            <div className="col" style={{ alignItems: "flex-end" }}>
              <span className="mud-h6">{list.length}</span>
              <span className="mud-caption" style={{ color: "rgba(255,255,255,.7)" }}>entries</span>
            </div>
          </div>
        </Card>

        {list.length === 0 && (
          <div className="col center" style={{ padding: "48px 0", color: "var(--mud-text-3)" }}>
            <Icon name="event_busy" className="xl" />
            <span className="mud-body2 mt8">No entries in this range</span>
          </div>
        )}

        {groups.map((g) => {
          const gtot = g.items.reduce((s, e) => s + durationOf(e), 0);
          return (
            <div key={g.key} className="mb16">
              {filter !== "Day" && (
                <div className="row between" style={{ padding: "2px 4px 6px" }}>
                  <span className="mud-subtitle2 t-muted">{dayLabel(g.date)}</span>
                  <span className="mud-caption tabnum fw-500">{fmtDurShort(gtot)}</span>
                </div>
              )}
              <Card>
                <div className="mud-list" style={{ padding: "4px 12px" }}>
                  {g.items.map((e, i) => (
                    <React.Fragment key={e.id}>
                      <EntryRow e={e} onEdit={onEdit} />
                      {i < g.items.length - 1 && <hr className="mud-divider" />}
                    </React.Fragment>
                  ))}
                </div>
              </Card>
            </div>
          );
        })}
        <div style={{ height: 90 }} />
      </div>
    </div>
  );
}

Object.assign(window, { LoginScreen, TimerScreen, TimerHero, EntriesScreen, EntryRow });
