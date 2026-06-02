/* TimeTracker — reports, projects, entry sheet */

/* ============ REPORTS / YEAR ============ */
function ReportsScreen({ entries }) {
  const year = new Date().getFullYear();
  const chartData = TT.MONTHLY.map((m, i) => ({
    label: m.m, v: m.hrs, active: i === new Date().getMonth(),
  }));
  const ytdTotal = TT.YTD.reduce((s, p) => s + p.hrs, 0);
  const maxProj = Math.max(...TT.YTD.map((p) => p.hrs));
  const billable = TT.YTD.filter((p) => TT.projectById(p.projectId).rate > 0).reduce((s, p) => s + p.hrs, 0);

  return (
    <div className="mud-content">
      <div className="mud-h6 mb4">Reports</div>
      <div className="mud-body2 t-muted mb16">Year overview · {year}</div>

      {/* KPI cards */}
      <div className="kpi-grid mb16">
        <Card className="flat"><div className="mud-card-body" style={{ padding: 16 }}>
          <div className="mud-overline">YTD hours</div>
          <div className="tabnum t-secondary" style={{ fontSize: "1.8rem", fontWeight: 500 }}>{ytdTotal}h</div>
        </div></Card>
        <Card className="flat"><div className="mud-card-body" style={{ padding: 16 }}>
          <div className="mud-overline">Billable</div>
          <div className="tabnum t-primary" style={{ fontSize: "1.8rem", fontWeight: 500 }}>{billable}h</div>
        </div></Card>
      </div>

      <Card className="mb16">
        <div className="mud-card-body">
          <div className="row between mb8" style={{ alignItems: "baseline" }}>
            <span className="mud-subtitle1 fw-500">Hours by month</span>
            <span className="mud-caption">hrs</span>
          </div>
          <BarChart data={chartData} valueFmt={(v) => v || ""} />
        </div>
      </Card>

      <Card>
        <div className="mud-card-body">
          <span className="mud-subtitle1 fw-500">By project</span>
          <div className="mt16 col gap16">
            {TT.YTD.sort((a, b) => b.hrs - a.hrs).map((row) => {
              const p = TT.projectById(row.projectId);
              return (
                <div key={row.projectId}>
                  <div className="row between mb4" style={{ alignItems: "center" }}>
                    <span className="row gap8" style={{ alignItems: "center", minWidth: 0 }}>
                      <span className="dot" style={{ background: p.color }} />
                      <span className="mud-body2 fw-500" style={{ whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{p.client}</span>
                    </span>
                    <span className="mud-body2 tabnum fw-500">{row.hrs}h</span>
                  </div>
                  <div className="bartrack">
                    <div className="barfill" style={{ width: `${(row.hrs / maxProj) * 100}%`, background: p.color }} />
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </Card>
      <div style={{ height: 90 }} />
    </div>
  );
}

/* ============ PROJECTS ============ */
function ProjectsScreen({ entries, onOpen }) {
  const hoursByProject = (id) => {
    const ms = entries.filter((e) => e.projectId === id).reduce((s, e) => s + durationOf(e), 0);
    const ytd = (TT.YTD.find((y) => y.projectId === id) || {}).hrs || 0;
    return ytd;
  };
  const active = TT.PROJECTS.filter((p) => !p.endDate);
  const archived = TT.PROJECTS.filter((p) => p.endDate);

  const ProjectCard = (p) => (
    <Card key={p.id} className="mb12" onClick={() => onOpen(p.id)} style={{ cursor: "pointer" }}>
      <div className="mud-card-body" style={{ padding: 16 }}>
        <div className="row gap12" style={{ alignItems: "flex-start" }}>
          <span className="mud-avatar sq" style={{ background: p.color + "22", color: p.color, fontWeight: 700 }}>
            {p.client.slice(0, 2).toUpperCase()}
          </span>
          <div className="grow" style={{ minWidth: 0 }}>
            <div className="mud-body1 fw-500" style={{ lineHeight: 1.3 }}>{p.name}</div>
            <div className="mud-caption mt4 row gap8 wrap" style={{ alignItems: "center" }}>
              <span>{p.client}</span>
              {p.rate > 0
                ? <span className="mud-chip sm cyan">${p.rate}/h</span>
                : <span className="mud-chip sm outlined">internal</span>}
            </div>
          </div>
          <Icon name="chevron_right" className="t-muted3" />
        </div>
        <div className="row between mt12" style={{ alignItems: "center" }}>
          <span className="mud-caption">{hoursByProject(p.id)}h logged YTD</span>
          <span className="mud-caption t-muted3">since {new Date(p.startDate).toLocaleDateString("en-AU", { month: "short", year: "numeric" })}</span>
        </div>
      </div>
    </Card>
  );

  return (
    <div className="mud-content">
      <div className="row between mb16" style={{ alignItems: "center" }}>
        <div className="mud-h6">Projects</div>
        <span className="mud-chip primary">{active.length} active</span>
      </div>
      {active.map(ProjectCard)}
      {archived.length > 0 && (
        <>
          <div className="mud-overline mt16 mb8">Archived</div>
          <div style={{ opacity: .7 }}>{archived.map(ProjectCard)}</div>
        </>
      )}
      <div style={{ height: 90 }} />
    </div>
  );
}

function ProjectDetail({ projectId, entries, onBack, onEdit }) {
  const p = TT.projectById(projectId);
  const list = entries.filter((e) => e.projectId === projectId).sort((a, b) => b.start - a.start);
  const total = list.reduce((s, e) => s + durationOf(e), 0);
  const ytd = (TT.YTD.find((y) => y.projectId === projectId) || {}).hrs || 0;
  return (
    <div className="mud-content">
      <div className="row gap8 mb12" style={{ alignItems: "center" }}>
        <span className="mud-avatar sq" style={{ background: p.color + "22", color: p.color, fontWeight: 700, width: 44, height: 44 }}>
          {p.client.slice(0, 2).toUpperCase()}
        </span>
        <div className="grow" style={{ minWidth: 0 }}>
          <div className="mud-h6" style={{ lineHeight: 1.2 }}>{p.name}</div>
          <div className="mud-caption">{p.client}</div>
        </div>
      </div>
      <Card className="mb16"><div className="mud-card-body">
        <div className="mud-body2 t-muted">{p.description}</div>
        <div className="row gap16 mt16 wrap">
          <div className="col"><span className="mud-overline">YTD hours</span><span className="mud-h6 tabnum">{ytd}h</span></div>
          <div className="col"><span className="mud-overline">Rate</span><span className="mud-h6 tabnum">{p.rate > 0 ? "$" + p.rate : "—"}</span></div>
          <div className="col"><span className="mud-overline">Est. value</span><span className="mud-h6 tabnum">{p.rate > 0 ? "$" + (ytd * p.rate).toLocaleString() : "—"}</span></div>
        </div>
      </div></Card>
      <div className="mud-subtitle1 fw-500 mb8">Recent entries</div>
      <Card><div className="mud-list" style={{ padding: "4px 12px" }}>
        {list.slice(0, 8).map((e, i) => (
          <React.Fragment key={e.id}>
            <EntryRow e={e} onEdit={onEdit} showProject={false} />
            {i < Math.min(list.length, 8) - 1 && <hr className="mud-divider" />}
          </React.Fragment>
        ))}
      </div></Card>
      <div style={{ height: 90 }} />
    </div>
  );
}

/* ============ ADD / EDIT ENTRY SHEET ============ */
function timeStr(d) { return `${pad(d.getHours())}:${pad(d.getMinutes())}`; }
function dateStr(d) { return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`; }

function EntrySheet({ entry, onSave, onDelete, onClose }) {
  const editing = !!entry.id;
  const [projectId, setProjectId] = useState(entry.projectId || TT.PROJECTS[0].id);
  const [date, setDate] = useState(dateStr(entry.start || new Date()));
  const [start, setStart] = useState(timeStr(entry.start || new Date()));
  const [end, setEnd] = useState(entry.end ? timeStr(entry.end) : "");
  const [note, setNote] = useState(entry.note || "");

  function combine(dStr, tStr) {
    const [y, mo, da] = dStr.split("-").map(Number);
    const [h, mi] = tStr.split(":").map(Number);
    return new Date(y, mo - 1, da, h, mi, 0, 0);
  }
  const sDate = combine(date, start);
  const eDate = end ? combine(date, end) : null;
  let dur = eDate ? eDate - sDate : 0;
  if (dur < 0) dur += 86400000; // wrap past midnight
  const valid = !!end && dur > 0;

  function quickAdd(mins) {
    const e = new Date(sDate.getTime() + mins * 60000);
    setEnd(timeStr(e));
  }

  return (
    <>
      <div className="scrim" onClick={onClose} />
      <div className="sheet">
        <div className="sheet-grip" />
        <div className="sheet-head">
          <span className="mud-h6">{editing ? "Edit entry" : "New time entry"}</span>
          <IconBtn icon="close" className="dark" onClick={onClose} />
        </div>
        <div className="sheet-body">
          <Field label="Project" float>
            <Select value={projectId} onChange={(e) => setProjectId(+e.target.value)}>
              {TT.PROJECTS.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
            </Select>
          </Field>

          <Field label="Date" float>
            <input type="date" className="mud-input" value={date} onChange={(e) => setDate(e.target.value)} />
          </Field>

          <div className="row gap12">
            <div className="grow"><Field label="Start" float>
              <input type="time" className="mud-input" value={start} onChange={(e) => setStart(e.target.value)} />
            </Field></div>
            <div className="grow"><Field label="End" float help={!end ? "Set an end time" : null}>
              <input type="time" className="mud-input" value={end} onChange={(e) => setEnd(e.target.value)} />
            </Field></div>
          </div>

          <div className="row gap8 wrap mb16">
            <span className="mud-caption" style={{ width: "100%" }}>Quick duration from start</span>
            {[15, 30, 45, 60, 90, 120].map((m) => (
              <Chip key={m} variant="outlined" sm onClick={() => quickAdd(m)}>{m < 60 ? `${m}m` : `${m / 60}h`}</Chip>
            ))}
          </div>

          <Field label="Note (optional)" float>
            <input className="mud-input" value={note} placeholder=" " onChange={(e) => setNote(e.target.value)} />
          </Field>

          <div className="row between mb16" style={{ alignItems: "center", padding: "10px 14px",
            background: "var(--primary-tint-s)", borderRadius: 8 }}>
            <span className="mud-subtitle2 t-muted">Duration</span>
            <span className="tabnum fw-700 t-primary" style={{ fontSize: "1.25rem" }}>
              {valid ? fmtDur(dur) : "—"}
            </span>
          </div>

          <Btn block lg disabled={!valid} icon="check"
            onClick={() => onSave({ id: entry.id, projectId, start: sDate, end: eDate, note })}>
            {editing ? "Save changes" : "Add entry"}
          </Btn>
          {editing && (
            <button className="mud-btn text neutral block mt8" onClick={() => onDelete(entry.id)}
              style={{ color: "var(--mud-error)" }}>
              <Icon name="delete_outline" className="sm" /> Delete entry
            </button>
          )}
          <div style={{ height: 8 }} />
        </div>
      </div>
    </>
  );
}

/* ============ NEW / EDIT PROJECT SHEET ============ */
function ProjectSheet({ entry, onSave, onClose, onAddClient }) {
  const editing = !!entry.id;
  const firstClient = TT.CLIENTS.find((c) => !c.isArchived) || TT.CLIENTS[0];
  const [name, setName] = useState(entry.name || "");
  const [clientId, setClientId] = useState(entry.clientId || firstClient.id);
  const client = TT.clientById(clientId);
  const [billable, setBillable] = useState(entry.rate !== 0);
  // rate defaults to the client's default rate; override per project
  const [rate, setRate] = useState(
    entry.rate != null ? entry.rate : (client && client.defaultHourlyRate != null ? client.defaultHourlyRate : 150));
  const [rateTouched, setRateTouched] = useState(false);
  const [description, setDescription] = useState(entry.description || "");
  const valid = name.trim() && clientId;

  // when client changes, inherit its default rate unless the user has overridden
  function pickClient(id) {
    setClientId(id);
    const c = TT.clientById(id);
    if (c && !rateTouched) {
      if (c.defaultHourlyRate != null) { setBillable(true); setRate(c.defaultHourlyRate); }
      else { setBillable(false); setRate(0); }
    }
  }

  return (
    <>
      <div className="scrim" onClick={onClose} />
      <div className="sheet">
        <div className="sheet-grip" />
        <div className="sheet-head">
          <span className="mud-h6">{editing ? "Edit project" : "New project"}</span>
          <IconBtn icon="close" className="dark" onClick={onClose} />
        </div>
        <div className="sheet-body">
          <Field label="Project name" float>
            <input className="mud-input" value={name} placeholder=" " onChange={(e) => setName(e.target.value)} />
          </Field>

          <Field label="Client" float>
            <Select value={clientId} onChange={(e) => pickClient(+e.target.value)}>
              {TT.CLIENTS.filter((c) => !c.isArchived || c.id === clientId).map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}{c.defaultHourlyRate != null ? ` · $${c.defaultHourlyRate}/h` : " · no default rate"}{c.isArchived ? " (archived)" : ""}
                </option>
              ))}
            </Select>
          </Field>
          <button className="mud-btn text neutral" style={{ marginTop: -6, marginBottom: 8, paddingLeft: 4 }}
            onClick={onAddClient}>
            <Icon name="add_business" className="sm" /> New client
          </button>

          <div className="row gap8 wrap mb16">
            <span className="mud-caption" style={{ width: "100%" }}>Billing</span>
            <Chip variant={billable ? "" : "outlined"} active={billable}
              onClick={() => { setBillable(true); setRateTouched(true); if (!rate) setRate(client && client.defaultHourlyRate != null ? client.defaultHourlyRate : 150); }}>Billable</Chip>
            <Chip variant={!billable ? "" : "outlined"} active={!billable}
              onClick={() => { setBillable(false); setRateTouched(true); setRate(0); }}>Internal</Chip>
          </div>

          {billable && (
            <Field label="Hourly rate (AUD)" float
              help={client && client.defaultHourlyRate != null && rate === client.defaultHourlyRate ? `Default rate for ${client.name}` : "Overrides client default"}>
              <input type="number" className="mud-input" value={rate}
                onChange={(e) => { setRate(+e.target.value); setRateTouched(true); }} />
            </Field>
          )}

          <Field label="Description (optional)" float>
            <input className="mud-input" value={description} placeholder=" " onChange={(e) => setDescription(e.target.value)} />
          </Field>

          <Btn block lg disabled={!valid} icon="check"
            onClick={() => onSave({ id: entry.id, name, clientId, rate: billable ? rate : 0, description })}>
            {editing ? "Save changes" : "Create project"}
          </Btn>
          <div style={{ height: 8 }} />
        </div>
      </div>
    </>
  );
}

/* ============ CLIENTS ============ */
function ClientsScreen({ onEdit }) {
  const projectCount = (cid) => TT.PROJECTS.filter((p) => p.clientId === cid).length;
  const ytdHours = (cid) =>
    TT.PROJECTS.filter((p) => p.clientId === cid)
      .reduce((s, p) => s + ((TT.YTD.find((y) => y.projectId === p.id) || {}).hrs || 0), 0);

  const clients = [...TT.CLIENTS].sort((a, b) =>
    ((a.isArchived ? 1 : 0) - (b.isArchived ? 1 : 0)) || a.name.localeCompare(b.name));
  const activeCount = TT.CLIENTS.filter((c) => !c.isArchived).length;

  return (
    <div className="mud-content">
      <div className="row between mb16" style={{ alignItems: "center" }}>
        <div className="mud-h6">Clients</div>
        <span className="mud-chip primary">{activeCount} active</span>
      </div>

      {clients.map((c) => (
        <Card key={c.id} className="mb12" onClick={() => onEdit(c)} style={{ cursor: "pointer", opacity: c.isArchived ? .68 : 1 }}>
          <div className="mud-card-body" style={{ padding: 16 }}>
            <div className="row gap12" style={{ alignItems: "center" }}>
              <span className="mud-avatar" style={{ background: c.color, color: "#fff", fontWeight: 700, width: 44, height: 44 }}>
                {c.name.slice(0, 2).toUpperCase()}
              </span>
              <div className="grow" style={{ minWidth: 0 }}>
                <div className="mud-body1 fw-500" style={{ lineHeight: 1.3 }}>{c.name}</div>
                <div className="mud-caption mt4 row gap8 wrap" style={{ alignItems: "center" }}>
                  {c.contactName
                    ? <span className="row gap4" style={{ alignItems: "center" }}><Icon name="person" className="sm" style={{ color: "var(--mud-text-disabled)" }} />{c.contactName}</span>
                    : <span className="t-muted3">No contact</span>}
                  {c.isArchived && <span className="mud-chip sm outlined">archived</span>}
                  {c.defaultHourlyRate == null && <span className="mud-chip sm outlined">internal</span>}
                </div>
              </div>
              <div className="col" style={{ alignItems: "flex-end" }}>
                <span className="mud-body2 fw-700 tabnum t-primary">{c.defaultHourlyRate != null ? `$${c.defaultHourlyRate}` : "—"}</span>
                <span className="mud-caption t-muted3">/ hr default</span>
              </div>
            </div>
            <div className="row between mt12" style={{ alignItems: "center" }}>
              <span className="mud-caption">{projectCount(c.id)} project{projectCount(c.id) === 1 ? "" : "s"} · {ytdHours(c.id)}h YTD</span>
              <Icon name="chevron_right" className="t-muted3" />
            </div>
          </div>
        </Card>
      ))}
      <div style={{ height: 90 }} />
    </div>
  );
}

/* ============ NEW / EDIT CLIENT SHEET ============ */
function ClientSheet({ client, onSave, onClose }) {
  const editing = !!client.id;
  const [name, setName] = useState(client.name || "");
  const [rate, setRate] = useState(client.defaultHourlyRate != null ? String(client.defaultHourlyRate) : "");
  const [contactName, setContactName] = useState(client.contactName || "");
  const [contactEmail, setContactEmail] = useState(client.contactEmail || "");
  const [contactPhone, setContactPhone] = useState(client.contactPhone || "");
  const valid = name.trim();
  const rateValue = rate.trim() === "" ? null : Number(rate);

  return (
    <>
      <div className="scrim" onClick={onClose} />
      <div className="sheet">
        <div className="sheet-grip" />
        <div className="sheet-head">
          <span className="mud-h6">{editing ? "Edit client" : "New client"}</span>
          <IconBtn icon="close" className="dark" onClick={onClose} />
        </div>
        <div className="sheet-body">
          <Field label="Client name" float>
            <input className="mud-input" value={name} placeholder=" " onChange={(e) => setName(e.target.value)} />
          </Field>

          <Field label="Default hourly rate (AUD)" float help="Optional — pre-fills the rate on new projects. Leave blank if none.">
            <input type="number" className="mud-input" value={rate} placeholder=" "
              onChange={(e) => setRate(e.target.value)} />
          </Field>

          <div className="mud-overline mt8 mb8">Primary contact (optional)</div>
          <Field label="Contact name" float>
            <input className="mud-input" value={contactName} placeholder=" " onChange={(e) => setContactName(e.target.value)} />
          </Field>
          <Field label="Email" float>
            <input type="email" className="mud-input" value={contactEmail} placeholder=" " onChange={(e) => setContactEmail(e.target.value)} />
          </Field>
          <Field label="Phone" float>
            <input type="tel" className="mud-input" value={contactPhone} placeholder=" " onChange={(e) => setContactPhone(e.target.value)} />
          </Field>

          <Btn block lg disabled={!valid} icon="check"
            onClick={() => onSave({ id: client.id, name,
              defaultHourlyRate: rateValue, contactName, contactEmail, contactPhone,
              isArchived: !!client.isArchived })}>
            {editing ? "Save changes" : "Create client"}
          </Btn>
          {editing && (
            <button className="mud-btn text neutral block mt8"
              onClick={() => onSave({ ...client, defaultHourlyRate: rateValue, contactName, contactEmail, contactPhone,
                isArchived: !client.isArchived })}>
              <Icon name={client.isArchived ? "check_circle" : "event_busy"} className="sm" />
              {client.isArchived ? " Reactivate client" : " Archive client"}
            </button>
          )}
          <div style={{ height: 8 }} />
        </div>
      </div>
    </>
  );
}

Object.assign(window, { ReportsScreen, ProjectsScreen, ProjectDetail, EntrySheet, ProjectSheet, ClientsScreen, ClientSheet });
