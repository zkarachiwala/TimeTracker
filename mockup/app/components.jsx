/* TimeTracker — shared UI primitives (MudBlazor-styled) + helpers */
const { useState, useEffect, useRef } = React;

/* ---------- time helpers ---------- */
function pad(n) { return String(n).padStart(2, "0"); }
function fmtClock(d) {
  let h = d.getHours(); const m = d.getMinutes();
  const ap = h >= 12 ? "pm" : "am"; h = h % 12 || 12;
  return `${h}:${pad(m)} ${ap}`;
}
function fmtDur(ms) {
  const totalMin = Math.max(0, Math.round(ms / 60000));
  const h = Math.floor(totalMin / 60), m = totalMin % 60;
  return `${h}h ${pad(m)}m`;
}
function fmtDurShort(ms) {
  const totalMin = Math.max(0, Math.round(ms / 60000));
  const h = Math.floor(totalMin / 60), m = totalMin % 60;
  if (h === 0) return `${m}m`;
  return m === 0 ? `${h}h` : `${h}h ${m}m`;
}
function fmtTimer(ms) {
  const s = Math.floor(ms / 1000);
  return `${pad(Math.floor(s / 3600))}:${pad(Math.floor((s % 3600) / 60))}:${pad(s % 60)}`;
}
function durationOf(e) {
  const end = e.end || new Date();
  return end - e.start;
}
function sameDay(a, b) {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}
const DOW = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
const MON = ["January","February","March","April","May","June","July","August","September","October","November","December"];
function dayLabel(d) {
  const today = new Date();
  const yest = new Date(); yest.setDate(yest.getDate() - 1);
  if (sameDay(d, today)) return "Today";
  if (sameDay(d, yest)) return "Yesterday";
  return `${DOW[d.getDay()]}, ${d.getDate()} ${MON[d.getMonth()].slice(0,3)}`;
}

/* ---------- primitives ---------- */
const ICON_PATHS = {
  timer: "M15 1H9v2h6V1zm-4 13h2V8h-2v6zm8.03-6.61l1.42-1.42c-.43-.51-.9-.99-1.41-1.41l-1.42 1.42C16.07 4.74 14.12 4 12 4c-4.97 0-9 4.03-9 9s4.02 9 9 9 9-4.03 9-9c0-2.12-.74-4.07-1.97-5.61zM12 20c-3.87 0-7-3.13-7-7s3.13-7 7-7 7 3.13 7 7-3.13 7-7 7z",
  view_list: "M4 14h4v-4H4v4zm0 5h4v-4H4v4zM4 9h4V5H4v4zm5 5h12v-4H9v4zm0 5h12v-4H9v4zM9 5v4h12V5H9z",
  bar_chart: "M5 9.2h3V19H5V9.2zM10.6 5h2.8v14h-2.8V5zm5.6 8H19v6h-2.8v-6z",
  folder_open: "M20 6h-8l-2-2H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm0 12H4V8h16v10z",
  play_arrow: "M8 5v14l11-7z",
  stop: "M6 6h12v12H6z",
  bolt: "M11 21h-1l1-7H7.5c-.88 0-.33-.75-.31-.78C8.48 10.94 10.42 7.54 13 3h1l-1 7h3.5c.49 0 .56.33.47.51l-.07.15C12.96 17.55 11 21 11 21z",
  add: "M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z",
  chevron_right: "M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z",
  chevron_left: "M15.41 7.41L14 6l-6 6 6 6 1.41-1.41L10.83 12z",
  arrow_back: "M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20v-2z",
  close: "M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z",
  search: "M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z",
  logout: "M17 7l-1.41 1.41L18.17 11H8v2h10.17l-2.58 2.58L17 17l5-5zM4 5h8V3H4c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h8v-2H4V5z",
  vpn_key: "M12.65 10C11.83 7.67 9.61 6 7 6c-3.31 0-6 2.69-6 6s2.69 6 6 6c2.61 0 4.83-1.67 5.65-4H17v4h4v-4h2v-4H12.65zM7 14c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2z",
  check: "M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z",
  check_circle: "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z",
  play_circle: "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 14.5v-9l6 4.5-6 4.5z",
  delete_outline: "M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM8 9h8v10H8V9zm7.5-5l-1-1h-5l-1 1H5v2h14V4z",
  delete: "M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z",
  hourglass_empty: "M6 2v6h.01L6 8.01 10 12l-4 4 .01.01H6V22h12v-5.99h-.01L18 16l-4-4 4-3.99-.01-.01H18V2H6zm10 14.5V20H8v-3.5l4-4 4 4zm-4-5l-4-4V4h8v3.5l-4 4z",
  event_busy: "M9.31 17l2.44-2.44L14.19 17l1.06-1.06-2.44-2.44 2.44-2.44L14.19 10l-2.44 2.44L9.31 10l-1.06 1.06 2.44 2.44-2.44 2.44L9.31 17zM19 3h-1V1h-2v2H8V1H6v2H5c-1.11 0-1.99.9-1.99 2L3 19c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V8h14v11z",
  edit: "M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34a.9959.9959 0 0 0-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z",
  menu: "M3 18h18v-2H3v2zm0-5h18v-2H3v2zm0-7v2h18V6H3z",
  add_business: "M2 7V4h2V2h12v2h2v3h-2V6H4v1H2zm14 4h-2v2h-2v2h2v2h2v-2h2v-2h-2v-2zM2 9v11h8v-5h2v5h2v-6.5c0-.83.67-1.5 1.5-1.5H18V9H2zm6 8H4v-2h4v2zm0-4H4v-2h4v2z",
  business: "M12 7V3H2v18h20V7H12zM6 19H4v-2h2v2zm0-4H4v-2h2v2zm0-4H4V9h2v2zm0-4H4V5h2v2zm4 12H8v-2h2v2zm0-4H8v-2h2v2zm0-4H8V9h2v2zm0-4H8V5h2v2zm10 12h-8v-2h2v-2h-2v-2h2v-2h-2V9h8v10zm-2-8h-2v2h2v-2zm0 4h-2v2h2v-2z",
  email: "M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z",
  phone: "M6.62 10.79c1.44 2.83 3.76 5.14 6.59 6.59l2.2-2.2c.27-.27.67-.36 1.02-.24 1.12.37 2.33.57 3.57.57.55 0 1 .45 1 1V20c0 .55-.45 1-1 1-9.39 0-17-7.61-17-17 0-.55.45-1 1-1h3.5c.55 0 1 .45 1 1 0 1.25.2 2.45.57 3.57.11.35.03.74-.25 1.02l-2.2 2.2z",
  person: "M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z",
};
function iconSize(className = "") {
  if (/\bxl\b/.test(className)) return 44;
  if (/\blg\b/.test(className)) return 32;
  if (/\bsm\b/.test(className)) return 18;
  return 24;
}
function Icon({ name, className = "", style }) {
  const s = (style && style.fontSize) ? parseInt(style.fontSize) : iconSize(className);
  const rest = { ...style }; delete rest.fontSize;
  return (
    <svg className={`mi ${className}`} width={s} height={s} viewBox="0 0 24 24"
      fill="currentColor" style={rest} aria-hidden="true">
      <path d={ICON_PATHS[name] || ""} />
    </svg>
  );
}

function IconBtn({ icon, onClick, className = "", title, style }) {
  return (
    <button className={`mud-iconbtn ${className}`} onClick={onClick} title={title} style={style}>
      <Icon name={icon} />
    </button>
  );
}

function Btn({ children, variant = "filled", color = "", className = "", icon, onClick, disabled, lg, block }) {
  const cls = `mud-btn ${variant} ${color} ${lg ? "lg" : ""} ${block ? "block" : ""} ${className}`;
  return (
    <button className={cls} onClick={onClick} disabled={disabled}>
      {icon && <Icon name={icon} />}{children}
    </button>
  );
}

function Card({ children, className = "", onClick, style }) {
  return <div className={`mud-card ${className}`} onClick={onClick} style={style}>{children}</div>;
}

function Chip({ children, active, variant = "", className = "", icon, onClick, sm }) {
  return (
    <span className={`mud-chip ${active ? "active" : ""} ${variant} ${sm ? "sm" : ""} ${className}`} onClick={onClick}>
      {icon && <Icon name={icon} />}{children}
    </span>
  );
}

function Avatar({ children, img, className = "", style }) {
  return <span className={`mud-avatar ${className}`} style={style}>{img ? <img src={img} alt="" /> : children}</span>;
}

function Field({ label, children, float = true, help, error }) {
  return (
    <div className="mud-field" style={{ marginBottom: 18 }}>
      <div className={`mud-field ${float ? "float" : ""}`}>
        {label && <label className="fl">{label}</label>}
        {children}
      </div>
      {help && <div className={`mud-help ${error ? "error" : ""}`}>{help}</div>}
    </div>
  );
}

function Select({ value, onChange, children, style }) {
  return (
    <select className="mud-input" value={value} onChange={onChange} style={style}>{children}</select>
  );
}

function ProjectDot({ id, size = 12 }) {
  const p = TT.projectById(id);
  return <span className="dot" style={{ background: p ? p.color : "#999", width: size, height: size }} />;
}

/* simple bar chart (MudChart-style) */
function BarChart({ data, height = 170, color = "var(--mud-primary)", valueFmt }) {
  const max = Math.max(...data.map((d) => d.v), 1);
  return (
    <div className="row" style={{ alignItems: "flex-end", gap: 6, height, paddingTop: 8 }}>
      {data.map((d, i) => {
        const h = d.v === 0 ? 2 : Math.round((d.v / max) * (height - 34));
        return (
          <div key={i} className="col center grow" style={{ justifyContent: "flex-end", height: "100%" }}>
            <div className="mud-caption tabnum" style={{ fontSize: ".62rem", marginBottom: 3, color: d.v ? "var(--mud-text-2)" : "transparent" }}>
              {valueFmt ? valueFmt(d.v) : d.v}
            </div>
            <div style={{ width: "78%", maxWidth: 34, height: h, borderRadius: "4px 4px 0 0",
              background: d.active ? "var(--dzk-cyan)" : color, opacity: d.v ? 1 : .25, transition: "height .5s ease" }} />
            <div className="mud-caption" style={{ fontSize: ".65rem", marginTop: 5 }}>{d.label}</div>
          </div>
        );
      })}
    </div>
  );
}

/* expose to other babel scripts */
Object.assign(window, {
  pad, fmtClock, fmtDur, fmtDurShort, fmtTimer, durationOf, sameDay, dayLabel, DOW, MON,
  Icon, IconBtn, Btn, Card, Chip, Avatar, Field, Select, ProjectDot, BarChart,
});
