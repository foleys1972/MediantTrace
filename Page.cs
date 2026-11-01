namespace MediantTrace;

/// <summary>The single web page, served from memory. No external fonts or scripts: it works offline.</summary>
public static class Page
{
    public const string Html = """"
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>MediantTrace</title>
<link rel="icon" href="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 32 32'%3E%3Crect width='32' height='32' rx='8' fill='%232563eb'/%3E%3Cpath d='M5 17h5l3-8 5 15 3-9h6' stroke='white' stroke-width='2.6' fill='none' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E">
<style>
/* Fonts are served from inside MediantTrace.exe (SIL Open Font License, /fonts/*-OFL.txt), so this works offline. */
@font-face{font-family:"IBM Plex Sans";src:url(/fonts/IBMPlexSans.woff2) format("woff2");font-weight:100 700;font-display:swap}
@font-face{font-family:"JetBrains Mono";src:url(/fonts/JetBrainsMono.woff2) format("woff2");font-weight:100 800;font-display:swap}
/* Design system from the ui-ux-pro-max skill: dark first, slate, IBM Plex Sans + JetBrains Mono.
   Blue is the accent; green, amber and red mean line and call state only. */
:root{
  --bg:#f1f5f9;--surface:#ffffff;--surface-2:#f8fafc;--sunken:#e9eff8;--border:#e2e8f0;--border-strong:#cbd5e1;
  --text:#0f172a;--text-2:#334155;--text-3:#475569;
  --accent:#2563eb;--accent-soft:#dbeafe;--accent-text:#1d4ed8;--accent-solid:#2563eb;
  --ok:#059669;--ok-soft:#e3f6ef;--warn:#d97706;--warn-soft:#fdf2e1;--bad:#dc2626;--bad-soft:#fde8e8;--info:#7c3aed;
  --ok-text:#047857;--warn-text:#a14a06;--bad-text:#b91c1c;--bad-solid:#dc2626;
  --req:#2563eb;--prov:#8391a5;--succ:#059669;--fail:#dc2626;
  --shadow:0 1px 2px rgba(15,23,42,.04),0 2px 8px rgba(15,23,42,.06);
  --shadow-lg:0 12px 40px rgba(15,23,42,.18);
  --mono:"JetBrains Mono","Cascadia Mono","Cascadia Code",Consolas,ui-monospace,monospace;
  --sans:"IBM Plex Sans","Segoe UI Variable Text","Segoe UI",system-ui,-apple-system,Roboto,Arial,sans-serif;
}
:root[data-theme="dark"]{
  --bg:#0f172a;--surface:#1b2336;--surface-2:#212b40;--sunken:#141c2e;--border:#334155;--border-strong:#475569;
  --text:#f8fafc;--text-2:#cbd5e1;--text-3:#94a3b8;
  --accent:#3b82f6;--accent-soft:#1e3a5f;--accent-text:#93c5fd;--accent-solid:#2563eb;
  --ok:#22c55e;--ok-soft:#0f2a1c;--warn:#f59e0b;--warn-soft:#3a2a0b;--bad:#f87171;--bad-soft:#3b1219;--info:#a78bfa;
  --ok-text:#86efac;--warn-text:#fcd34d;--bad-text:#fca5a5;--bad-solid:#dc2626;
  --req:#60a5fa;--prov:#94a3b8;--succ:#22c55e;--fail:#f87171;
  --shadow:none;--shadow-lg:0 12px 40px rgba(0,0,0,.55);
}
@media (prefers-color-scheme:dark){:root:not([data-theme="light"]){
  --bg:#0f172a;--surface:#1b2336;--surface-2:#212b40;--sunken:#141c2e;--border:#334155;--border-strong:#475569;
  --text:#f8fafc;--text-2:#cbd5e1;--text-3:#94a3b8;
  --accent:#3b82f6;--accent-soft:#1e3a5f;--accent-text:#93c5fd;--accent-solid:#2563eb;
  --ok:#22c55e;--ok-soft:#0f2a1c;--warn:#f59e0b;--warn-soft:#3a2a0b;--bad:#f87171;--bad-soft:#3b1219;--info:#a78bfa;
  --ok-text:#86efac;--warn-text:#fcd34d;--bad-text:#fca5a5;--bad-solid:#dc2626;
  --req:#60a5fa;--prov:#94a3b8;--succ:#22c55e;--fail:#f87171;
  --shadow:none;--shadow-lg:0 12px 40px rgba(0,0,0,.55);
}}
*{box-sizing:border-box}
html,body{height:100%}
body{margin:0;background:var(--bg);color:var(--text);font:14px/1.5 var(--sans);-webkit-font-smoothing:antialiased}
a{color:var(--accent-text);text-decoration:none}
a:hover{text-decoration:underline}
code,.mono{font-family:var(--mono);font-size:12.5px}
button{font:inherit;color:inherit}
.app{display:grid;grid-template-columns:232px minmax(0,1fr);min-height:100vh;width:100%;min-width:0}

/* sidebar */
.side{background:var(--surface);border-right:1px solid var(--border);display:flex;flex-direction:column;position:sticky;top:0;height:100vh}
.brand{display:flex;align-items:center;gap:10px;padding:18px 18px 14px;font-weight:700;font-size:16px;letter-spacing:-.01em}
.brand svg{flex:none}
.brand small{display:block;font-weight:500;font-size:12px;color:var(--text-3);letter-spacing:.02em}
.nav{padding:6px 10px;display:flex;flex-direction:column;gap:2px;flex:1;min-height:0;overflow:auto}
.nav-label{padding:12px 10px 3px;font-size:12px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:var(--text-3)}
.nav-label:first-child{padding-top:2px}
.nav button{display:flex;align-items:center;gap:10px;width:100%;padding:8px 10px;border:0;background:none;border-radius:8px;cursor:pointer;color:var(--text-2);font-weight:550;text-align:left}
.nav button:hover{background:var(--sunken);color:var(--text)}
.nav button.on{background:var(--accent-soft);color:var(--accent-text)}
.nav .count{margin-left:auto;font-size:12px;font-weight:600;color:var(--text-3);font-variant-numeric:tabular-nums}
.nav button.on .count{color:var(--accent-text)}
.side-section{padding:14px 18px 6px;font-size:12px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:var(--text-3)}
.sbc-mini{padding:0 10px;display:flex;flex-direction:column;gap:1px;overflow:auto}
.sbc-mini div{display:flex;align-items:center;gap:8px;padding:6px 8px;border-radius:7px;font-size:13px;color:var(--text-2)}
.sbc-mini div span:nth-child(2){overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.side-foot{margin-top:auto;padding:12px 12px 12px 16px;border-top:1px solid var(--border);display:flex;gap:8px;align-items:center}
.avatar{flex:none;width:30px;height:30px;border-radius:50%;background:var(--accent-soft);color:var(--accent-text);display:inline-flex;align-items:center;justify-content:center;font-weight:700;font-size:13px}
.who-box{flex:1;min-width:0;line-height:1.25}
#who{font-weight:600;font-size:13.5px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
#ver{font-size:12px;color:var(--text-3)}
/* top bar: what is arriving from the SBCs, at a glance from any page */
.feeds{display:flex;flex-wrap:wrap;gap:6px}
.feed-chip{display:inline-flex;align-items:center;gap:8px;height:32px;padding:0 11px;border-radius:7px;border:1px solid var(--border);background:var(--surface-2);font-size:13px;white-space:nowrap}
.feed-chip b{font-weight:600}
.feed-chip .mono{color:var(--text-3);font-size:12.5px}

/* main */
.main{min-width:0;display:flex;flex-direction:column}
.top{display:flex;align-items:center;gap:12px;padding:14px 24px;background:var(--surface);border-bottom:1px solid var(--border);position:sticky;top:0;z-index:20}
.top h1{font-size:18px;margin:0;font-weight:650;letter-spacing:-.01em}
.top .sub{color:var(--text-3);font-size:13px}
.grow{flex:1}
#recBox{display:flex;align-items:center;gap:8px}
#who{font-size:13px;line-height:1}
.rec{display:flex;align-items:center;gap:8px;height:34px;padding:0 12px;border-radius:999px;background:var(--bad-soft);color:var(--bad-text);font-weight:650;font-variant-numeric:tabular-nums}
.rec .pulse{width:9px;height:9px;border-radius:50%;background:var(--bad);animation:pulse 1.4s ease-in-out infinite}
.rec small{color:var(--text-2);font-weight:500}
/* always-on logging is the normal state: calm, not the red of a packet capture */
.rec.log{background:var(--sunken);color:var(--text-2)}
.rec.log .pulse{background:var(--ok);box-shadow:0 0 0 3px var(--ok-soft);animation:none}
@keyframes pulse{0%,100%{opacity:1;transform:scale(1)}50%{opacity:.35;transform:scale(.8)}}
.content{padding:20px 24px 40px;max-width:1680px;width:100%;min-width:0}

/* controls */
.btn{display:inline-flex;align-items:center;gap:7px;height:34px;padding:0 14px;border-radius:8px;border:1px solid var(--border-strong);background:var(--surface);color:var(--text);font-weight:600;font-size:13.5px;cursor:pointer;white-space:nowrap;text-decoration:none}
.btn:hover{background:var(--sunken);text-decoration:none}
.btn.primary{background:var(--accent-solid);border-color:var(--accent-solid);color:#fff}
.btn.primary:hover{filter:brightness(1.08)}
.btn.danger{background:var(--bad-solid);border-color:var(--bad-solid);color:#fff}
.btn.ghost{border-color:transparent;background:none;color:var(--text-2)}
.btn.ghost:hover{background:var(--sunken);color:var(--text)}
.btn.sm{height:28px;padding:0 10px;font-size:12.5px}
.btn.icon{width:34px;padding:0;justify-content:center}
.btn:disabled{opacity:.5;cursor:default}
.btn[aria-busy="true"]{opacity:.7;cursor:progress}
.btn:focus-visible,.nav button:focus-visible,input:focus-visible,select:focus-visible,.chip:focus-visible,.row:focus-visible{outline:2px solid var(--accent);outline-offset:2px}
input[type=text],input[type=password],input[type=number],input[type=search],select{height:34px;padding:0 11px;border-radius:8px;border:1px solid var(--border-strong);background:var(--surface);color:var(--text);font:inherit;font-size:13.5px}
input::placeholder{color:var(--text-3)}
.search{position:relative;flex:1;min-width:220px;max-width:460px}
.search input{width:100%;padding-left:34px}
.search svg{position:absolute;left:11px;top:9px;color:var(--text-3)}
.search kbd{position:absolute;right:8px;top:7px}
kbd{font:600 11px var(--mono);padding:2px 6px;border-radius:5px;border:1px solid var(--border-strong);background:var(--surface-2);color:var(--text-3)}
.chips{display:flex;gap:4px;padding:3px;background:var(--sunken);border-radius:9px;flex-wrap:wrap}
.chip{border:0;background:none;padding:5px 11px;border-radius:7px;font-weight:600;font-size:12.5px;color:var(--text-2);cursor:pointer;display:inline-flex;gap:6px;align-items:center}
.chip:hover{color:var(--text)}
.chip.on{background:var(--surface);color:var(--text);box-shadow:var(--shadow)}
.chip .n{font-variant-numeric:tabular-nums;color:var(--text-3);font-weight:600}
.toolbar{display:flex;gap:10px;align-items:center;flex-wrap:wrap;margin-bottom:14px}

/* cards & kpis */
.card{background:var(--surface);border:1px solid var(--border);border-radius:12px;box-shadow:var(--shadow)}
.card-h{display:flex;align-items:center;gap:10px;padding:14px 16px;border-bottom:1px solid var(--border)}
.card-h h2{font-size:14px;margin:0;font-weight:650}
.card-b{padding:16px}
.kpis{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:12px;margin-bottom:16px}
.kpi{padding:14px 16px;background:var(--surface);border:1px solid var(--border);border-radius:12px;box-shadow:var(--shadow);cursor:pointer;text-align:left}
.kpi:hover{border-color:var(--border-strong)}
.kpi .l{font-size:12px;color:var(--text-2);font-weight:600;display:flex;align-items:center;gap:6px}
.kpi .v{font-size:26px;font-weight:700;letter-spacing:-.02em;margin-top:2px;font-variant-numeric:tabular-nums}
.kpi.bad .v{color:var(--bad)}.kpi.warn .v{color:var(--warn)}.kpi.ok .v{color:var(--ok)}

/* alerts */
.alerts{display:flex;flex-direction:column;gap:8px;margin-bottom:16px}
.alerts:empty{display:none}
.alert{display:flex;gap:12px;align-items:flex-start;padding:11px 14px;border-radius:10px;border:1px solid;font-size:13.5px}
.alert.warn{background:var(--warn-soft);border-color:color-mix(in srgb,var(--warn) 35%,transparent)}
.alert.bad{background:var(--bad-soft);border-color:color-mix(in srgb,var(--bad) 45%,transparent);padding:15px 18px;font-size:14px}
.alert.bad .ic{width:36px;height:36px;margin:0;border-radius:9px;display:inline-flex;align-items:center;justify-content:center;background:color-mix(in srgb,var(--bad) 18%,transparent)}
.alert.bad b:first-child{display:block;font-size:15.5px;margin-bottom:2px}
.alert.info{background:var(--accent-soft);border-color:color-mix(in srgb,var(--accent) 30%,transparent)}
.alert .ic{flex:none;margin-top:1px}
.alert.warn .ic{color:var(--warn)}.alert.bad .ic{color:var(--bad)}.alert.info .ic{color:var(--accent)}
.alert b{font-weight:650}

/* status dots / badges */
.dot{display:inline-block;width:8px;height:8px;border-radius:50%;flex:none;background:var(--border-strong)}
.dot.ok{background:var(--ok);box-shadow:0 0 0 3px var(--ok-soft)}
.dot.warn{background:var(--warn);box-shadow:0 0 0 3px var(--warn-soft)}
.dot.bad{background:var(--bad);box-shadow:0 0 0 3px var(--bad-soft)}
.badge{display:inline-flex;align-items:center;gap:5px;height:22px;padding:0 8px;border-radius:6px;font-size:12px;font-weight:650;background:var(--sunken);color:var(--text-2);white-space:nowrap}
.badge.ok{background:var(--ok-soft);color:var(--ok-text)}
.badge.warn{background:var(--warn-soft);color:var(--warn-text)}
.badge.bad{background:var(--bad-soft);color:var(--bad-text)}
.badge.accent{background:var(--accent-soft);color:var(--accent-text)}
.muted{color:var(--text-3)}
.t2{color:var(--text-2)}
.num{font-variant-numeric:tabular-nums}

/* calls split view */
.split{display:grid;grid-template-columns:minmax(380px,40%) 1fr;gap:16px;align-items:start}
.list{overflow:hidden}
.list-h{display:flex;align-items:center;justify-content:space-between;padding:10px 14px;border-bottom:1px solid var(--border);font-size:12px;color:var(--text-3);font-weight:600}
.rows{max-height:calc(100vh - 330px);min-height:200px;overflow:auto}
.row{display:grid;grid-template-columns:4px 1fr auto;gap:12px;padding:11px 14px 11px 0;border-bottom:1px solid var(--border);cursor:pointer;outline:none}
.row:hover{background:var(--surface-2)}
.row.sel{background:var(--accent-soft)}
.row .sev{border-radius:0 3px 3px 0;background:transparent}
.row .sev.bad{background:var(--bad)}.row .sev.warn{background:var(--warn)}
.row .who{font-weight:650;display:flex;gap:6px;align-items:center;min-width:0}
.row .who span{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.row .meta{font-size:12.5px;color:var(--text-3);display:flex;gap:8px;align-items:center;margin-top:3px;flex-wrap:wrap}
.row .issue{font-size:12.5px;margin-top:5px;color:var(--bad);display:flex;gap:6px;align-items:flex-start}
.row .issue.warn{color:var(--warn)}
.row .issue span{display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden}
.row .side-r{text-align:right;font-size:12px;color:var(--text-3);display:flex;flex-direction:column;align-items:flex-end;gap:4px}
.flow{display:inline-flex;gap:8px;font-variant-numeric:tabular-nums;font-weight:600;color:var(--text-2)}
.flow .z{color:var(--bad)}
.arrow-in,.arrow-out{display:inline-flex;align-items:center;gap:2px}

/* detail */
.detail{min-width:0}
.d-head{padding:18px 20px 0}
.d-title{display:flex;align-items:center;gap:10px;flex-wrap:wrap}
.d-title h2{margin:0;font-size:20px;font-weight:700;letter-spacing:-.015em}
.d-sub{display:flex;gap:14px;flex-wrap:wrap;color:var(--text-3);font-size:12.5px;margin-top:6px;align-items:center}
.d-sub span{display:inline-flex;align-items:center;gap:5px}
.d-actions{display:flex;gap:6px;margin-left:auto}
.verdict{display:flex;gap:12px;align-items:center;margin:16px 20px 0;padding:13px 16px;border-radius:10px;font-weight:600}
.verdict.ok{background:var(--ok-soft);color:var(--ok-text)}
.verdict.warn{background:var(--warn-soft);color:var(--warn-text)}
.verdict.bad{background:var(--bad-soft);color:var(--bad-text)}
.verdict span{color:var(--text)}
.tabs{display:flex;gap:2px;padding:0 20px;margin-top:16px;border-bottom:1px solid var(--border);overflow:auto}
.tabs button{border:0;background:none;padding:10px 12px;font-weight:600;color:var(--text-3);cursor:pointer;border-bottom:2px solid transparent;margin-bottom:-1px;white-space:nowrap}
.tabs button:hover{color:var(--text)}
.tabs button.on{color:var(--accent-text);border-bottom-color:var(--accent)}
.tab-b{padding:18px 20px 20px}
.facts{display:grid;grid-template-columns:repeat(auto-fill,minmax(150px,1fr));border:1px solid var(--border);border-radius:10px;overflow:hidden;margin-bottom:18px;background:var(--surface)}
.facts > div{padding:10px 12px;border-right:1px solid var(--border);border-bottom:1px solid var(--border);margin:0 -1px -1px 0}
.facts .l{font-size:12px;color:var(--text-3);font-weight:600;text-transform:uppercase;letter-spacing:.04em}
.facts .v{font-weight:650;margin-top:2px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
h3{font-size:12px;margin:22px 0 10px;font-weight:700;color:var(--text-2);text-transform:uppercase;letter-spacing:.06em}
h3:first-child{margin-top:0}
.checks{display:flex;flex-direction:column;gap:8px}
.check{display:flex;gap:10px;align-items:flex-start;padding:10px 12px;border-radius:9px;background:var(--surface-2);border:1px solid var(--border)}
.check .ic{flex:none;margin-top:1px}
.check.bad .ic{color:var(--bad)}.check.warn .ic{color:var(--warn)}.check.ok .ic{color:var(--ok)}.check.note .ic{color:var(--accent)}

/* media */
.paths{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:12px}
.path{border:1px solid var(--border);border-radius:10px;padding:14px;background:var(--surface-2)}
.path-h{display:flex;justify-content:space-between;align-items:center;font-weight:650;margin-bottom:10px;gap:8px}
.dir{display:grid;grid-template-columns:110px 1fr 80px;gap:10px;align-items:center;font-size:13px;margin:6px 0}
.dir .bar{height:8px;border-radius:99px;background:var(--sunken);overflow:hidden}
.dir .bar i{display:block;height:100%;border-radius:99px;background:var(--s1)}
.dir .bar i.rx{background:var(--s2)}
.dir .bar i.zero{background:var(--bad);width:100%!important;opacity:.25}
.dir .n{text-align:right;font-weight:650;font-variant-numeric:tabular-nums}
.dir .n.zero{color:var(--bad)}
.path-f{display:flex;gap:14px;flex-wrap:wrap;font-size:12px;color:var(--text-3);margin-top:10px}
.timeline{border:1px solid var(--border);border-radius:10px;padding:12px 14px;background:var(--surface-2)}
.tl-row{display:grid;grid-template-columns:260px 1fr 70px;gap:12px;align-items:center;padding:6px 0;font-size:12.5px}
.tl-row .lbl{overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-family:var(--mono);font-size:12px}
.tl-row svg{width:100%;height:30px;display:block;background:var(--sunken);border-radius:5px}
.tl-row .r{text-align:right}
.tl-axis{display:grid;grid-template-columns:260px 1fr 70px;gap:12px;font-size:12px;color:var(--text-3)}
.tl-axis div:nth-child(2){display:flex;justify-content:space-between}

/* tables */
.tbl{width:100%;border-collapse:collapse;font-size:13px}
.tbl th{text-align:left;font-size:12px;text-transform:uppercase;letter-spacing:.04em;color:var(--text-3);font-weight:650;padding:9px 12px;border-bottom:1px solid var(--border);white-space:nowrap;background:var(--surface-2);position:sticky;top:0}
.tbl td{padding:10px 12px;border-bottom:1px solid var(--border);white-space:nowrap}
.tbl tr:last-child td{border-bottom:0}
.tbl .r{text-align:right;font-variant-numeric:tabular-nums}
.scroll{overflow:auto}

/* ladder */
.ladder-tools{display:flex;gap:10px;align-items:center;margin-bottom:12px;flex-wrap:wrap}
.ladder-wrap{border:1px solid var(--border);border-radius:10px;overflow:auto;background:var(--surface-2);max-height:62vh}
.ladder svg text{fill:var(--text);font-family:var(--mono);font-size:12px}
.ladder .colh rect{fill:var(--surface);stroke:var(--border-strong)}
.ladder .colh .t1{font-family:var(--sans);font-weight:700;font-size:12.5px}
.ladder .colh .t2{fill:var(--text-3);font-size:11px}
.ladder .life{stroke:var(--border-strong);stroke-width:1.5;stroke-dasharray:4 4}
.ladder .msg{cursor:pointer}
.ladder .msg .hit{fill:transparent}
.ladder .msg:hover .hit{fill:var(--sunken)}
.ladder .msg.sel .hit{fill:var(--accent-soft)}
.ladder .msg .time{fill:var(--text-3);font-size:11px}
.ladder .msg .delta{fill:var(--text-3);font-size:10.5px}
.ladder .msg .sdp{fill:var(--info);font-size:10.5px;font-weight:700}
.sip{font:12.5px/1.55 var(--mono);white-space:pre-wrap;word-break:break-all;margin:0;padding:14px 16px;background:var(--sunken);border-radius:10px;max-height:60vh;overflow:auto;border:1px solid var(--border)}
.sip .s1{color:var(--accent-text);font-weight:700}
.sip .s1.fail{color:var(--bad)}.sip .s1.succ{color:var(--ok)}
.sip .hn{color:var(--info);font-weight:600}
.sip .sdp{color:var(--ok)}
.sip .sdpk{color:var(--warn);font-weight:700}
.msg-panel{margin-top:14px}
.msg-panel .bar{display:flex;align-items:center;gap:10px;margin-bottom:8px;flex-wrap:wrap}
details.m{margin-bottom:8px;border:1px solid var(--border);border-radius:10px;background:var(--surface-2)}
details.m summary{cursor:pointer;padding:9px 12px;display:flex;gap:8px;align-items:center;flex-wrap:wrap}
details.m .sip{border:0;border-top:1px solid var(--border);border-radius:0 0 10px 10px}

/* sdp compare */
.sdp-pair{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-bottom:14px}
.sdp-card{border:1px solid var(--border);border-radius:10px;padding:12px 14px;background:var(--surface-2)}
.sdp-card .h{display:flex;justify-content:space-between;font-weight:650;margin-bottom:8px;gap:8px}
.kv{display:grid;grid-template-columns:110px 1fr;gap:6px 10px;font-size:13px;align-items:center}
.kv .k{color:var(--text-3)}
.codec{display:inline-block;padding:1px 7px;border-radius:5px;background:var(--sunken);font:12px var(--mono);margin:0 4px 4px 0}
.codec.common{background:var(--ok-soft);color:var(--ok);font-weight:700}

/* syslog */
.log{font:12.5px/1.6 var(--mono);background:var(--surface);border:1px solid var(--border);border-radius:12px;max-height:calc(100vh - 230px);min-height:200px;overflow:auto;padding:6px 0;box-shadow:var(--shadow)}
.log > div{padding:1px 14px;white-space:pre-wrap;word-break:break-all;display:grid;grid-template-columns:100px 110px 1fr;gap:10px}
.log > div:hover{background:var(--surface-2)}
.log .tm{color:var(--text-3)}
.log .sb{color:var(--accent-text);overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.log .banner{color:var(--info);font-weight:600}
.log .sid{color:var(--warn);cursor:pointer}
.log .lv-err{background:color-mix(in srgb,var(--bad) 10%,transparent)}
.log .lv-warn{background:color-mix(in srgb,var(--warn) 9%,transparent)}
.log mark{background:color-mix(in srgb,var(--warn) 35%,transparent);color:inherit;border-radius:3px}
.log .empty{display:block}
.log.viewer > div{grid-template-columns:58px auto 120px 1fr;cursor:default}
.log.viewer .tm{white-space:nowrap}
.log .ln{color:var(--text-3);text-align:right;cursor:pointer;user-select:none;opacity:.7}
.log .ln:hover{color:var(--accent);opacity:1}
.log > div.mk{background:color-mix(in srgb,#d946ef 18%,transparent)}
.log > div.mk .ln{color:#d946ef;opacity:1;font-weight:700}
.log > div.sel{box-shadow:inset 3px 0 0 var(--accent);background:color-mix(in srgb,var(--accent) 9%,transparent)}
.log > div.gap{color:#c026d3;font-weight:600;background:color-mix(in srgb,#d946ef 9%,transparent)}
.log .bin{color:var(--req);font-weight:650}
.log .bout{color:var(--succ);font-weight:650}
.log .sreq{color:var(--req);font-weight:650}
.log .sok{color:var(--succ);font-weight:650}
.log .sprov{color:var(--prov);font-weight:650}
.log .sfail{color:var(--fail);font-weight:650}
.log .hdr{color:var(--text-3)}
.log .cdr{color:var(--info);font-weight:650}
.log .kw{border-radius:3px;color:inherit}
.log .mk2{background:color-mix(in srgb,var(--warn) 35%,transparent);border-radius:3px}
.log .fd{background:#facc15;color:#111;border-radius:3px}
.sl-tabs{display:flex;gap:4px;flex-wrap:wrap;margin-bottom:10px}
.sl-tabs button{height:28px;padding:0 12px;border-radius:14px;border:1px solid var(--border);background:var(--surface);color:var(--text-2);font:600 12.5px var(--sans);cursor:pointer}
.sl-tabs button.on{background:var(--accent-solid);border-color:var(--accent-solid);color:#fff}
.sl-status{display:flex;gap:6px 18px;flex-wrap:wrap;align-items:center;font-size:12.5px;color:var(--text-3);margin-top:8px}
.findbar{display:flex;gap:8px;align-items:center;flex-wrap:wrap;margin:0 0 8px;padding:8px 10px;border:1px solid var(--border);border-radius:10px;background:var(--surface-2)}
.findbar input[type=search]{min-width:240px}
.ctx{position:fixed;z-index:60;background:var(--surface);border:1px solid var(--border-strong);border-radius:10px;box-shadow:var(--shadow-lg);padding:4px;min-width:230px;max-width:360px}
.ctx button{display:block;width:100%;text-align:left;background:none;border:0;padding:7px 10px;border-radius:6px;color:var(--text);font:13px var(--sans);cursor:pointer;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.ctx button:hover,.ctx button:focus{background:var(--sunken);outline:none}
.ctx hr{border:0;border-top:1px solid var(--border);margin:4px 0}
.dlg-back{position:fixed;inset:0;background:rgba(15,23,42,.5);z-index:70;display:flex;align-items:center;justify-content:center;padding:16px}
.dlg{background:var(--surface);color:var(--text);border-radius:14px;box-shadow:var(--shadow-lg);max-width:700px;width:100%;max-height:88vh;overflow:auto;padding:20px}
.dlg-f{display:flex;gap:8px;justify-content:flex-end;margin-top:16px}
.hl-row{display:flex;gap:8px;align-items:center;margin-bottom:8px}
.hl-row .hl-t{flex:1}
.drop{border:2px dashed var(--border-strong);border-radius:12px;padding:20px;text-align:center;color:var(--text-3)}
body.dropping .main::after{content:"Drop the file to open it";position:fixed;inset:12px;border:3px dashed var(--accent);border-radius:16px;background:color-mix(in srgb,var(--accent) 12%,transparent);display:flex;align-items:center;justify-content:center;font:700 22px var(--sans);color:var(--accent-text);z-index:80;pointer-events:none}

/* sbc cards */
.grid-cards{display:grid;grid-template-columns:repeat(auto-fill,minmax(330px,1fr));gap:14px}
.sbc-card .card-b{display:flex;flex-direction:column;gap:8px}
.feed{display:flex;align-items:center;gap:10px;font-size:13px;padding:8px 10px;border-radius:8px;background:var(--surface-2);border:1px solid var(--border)}
.feed .l{font-weight:650;width:128px}
.feed .s{color:var(--text-3);margin-left:auto;font-size:12px;font-variant-numeric:tabular-nums}
.form{display:grid;grid-template-columns:1fr;gap:14px}
.form label{font-size:12px;font-weight:650;color:var(--text-2);display:block;margin-bottom:5px}
.form input{width:100%}
.steps{counter-reset:s;list-style:none;padding:0;margin:0;display:flex;flex-direction:column;gap:14px}
.steps li{counter-increment:s;display:grid;grid-template-columns:28px 1fr;gap:10px;line-height:1.9}
.steps li::before{content:counter(s);width:24px;height:24px;border-radius:50%;background:var(--accent-soft);color:var(--accent-text);font-weight:700;font-size:12.5px;display:flex;align-items:center;justify-content:center;margin-top:2px}
.copy{display:inline-flex;align-items:center;gap:6px;padding:1px 3px 1px 8px;border-radius:6px;background:var(--sunken);font:12.5px var(--mono);border:1px solid var(--border);line-height:1.6;max-width:100%;word-break:break-all}
.copy button{border:0;background:none;cursor:pointer;color:var(--text-3);padding:2px 4px;border-radius:4px;display:inline-flex;flex:none}
.copy button:hover{color:var(--text);background:var(--surface)}
.toggle{display:inline-flex;align-items:center;gap:8px;cursor:pointer;font-weight:550;font-size:13.5px}
.toggle input{width:16px;height:16px;accent-color:var(--accent)}
.sbc-edit{display:grid;grid-template-columns:1fr 1fr 1.4fr auto;gap:8px;margin-bottom:8px}

/* empty state */
.empty{padding:48px 24px;text-align:center;color:var(--text-2)}
/* line board */
.lb-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(188px,1fr));gap:10px}
.lt{position:relative;display:flex;flex-direction:column;gap:5px;text-align:left;border:1px solid var(--border);border-radius:10px;background:var(--surface);padding:10px 12px 10px 17px;cursor:pointer;font:inherit;color:inherit;min-height:112px}
.lt:hover{border-color:var(--border-strong)}
.lt:focus-visible,.lt.sel{outline:2px solid var(--accent);outline-offset:1px}
.lt::before{content:"";position:absolute;left:0;top:0;bottom:0;width:6px;border-radius:10px 0 0 10px;background:var(--border-strong)}
.lt.up::before{background:var(--ok)} .lt.warn::before{background:var(--warn)} .lt.down::before{background:var(--bad)} .lt.call::before{background:var(--accent)}
.lt.down{background:var(--bad-soft)}
.lt .nm{display:flex;align-items:center;gap:6px;font-weight:650;font-size:13.5px}
.lt .nm b{flex:1;min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.lt .num{font:12px var(--mono);color:var(--text-3)}
.lt .st{display:flex;align-items:center;gap:5px;font-size:12.5px;font-weight:650}
.lt .st.up{color:var(--ok)} .lt .st.warn{color:var(--warn)} .lt .st.down{color:var(--bad)} .lt .st.call{color:var(--accent-text)} .lt .st.idle,.lt .st.unknown{color:var(--text-3)}
.lt .au{display:flex;gap:10px;align-items:center;font-size:12px;color:var(--text-2);margin-top:auto}
.lt .au span{display:inline-flex;align-items:center;gap:2px}
.lt .au .zero{color:var(--warn);font-weight:650}
.lb-group{margin-bottom:16px}
.lb-group .card-h .badge{height:20px}
.lb-kv{display:grid;grid-template-columns:130px 1fr;gap:6px 12px;font-size:13px}
.lb-kv dt{color:var(--text-3)} .lb-kv dd{margin:0}
.lb-table td{padding:7px 12px}
.lb-table tbody tr{cursor:pointer}
.lb-table tbody tr:hover td{background:var(--surface-2)}
.lb-table tr.sel td{background:var(--accent-soft)}
.lb-table tr.down td{background:var(--bad-soft)}
.lb-table th.sortable{cursor:pointer;user-select:none}
.lb-table th.sortable:hover{color:var(--text)}
.lb-table td.dt{max-width:420px;overflow:hidden;text-overflow:ellipsis;color:var(--text-2)}
.lb-table .zero{color:var(--warn);font-weight:650}
.lb-table .bad-t,.lt .bad-t{color:var(--bad);font-weight:700}
.lb-ev{display:grid;grid-template-columns:150px 150px 1fr;gap:10px;padding:7px 16px;border-bottom:1px solid var(--border);font-size:13px}
.lb-ev:last-child{border-bottom:0}
/* folder picker */
dialog.fp{border:1px solid var(--border);border-radius:14px;padding:0;width:min(640px,calc(100vw - 32px));background:var(--surface);color:var(--text);box-shadow:0 20px 60px rgba(0,0,0,.35)}
dialog.fp::backdrop{background:rgba(10,15,25,.45)}
.fp-h{display:flex;align-items:center;gap:10px;padding:14px 16px;border-bottom:1px solid var(--border)}
.fp-h h2{font-size:15px;margin:0}
.fp-path{display:flex;gap:4px;flex-wrap:wrap;align-items:center;padding:10px 16px;border-bottom:1px solid var(--border);font:12.5px var(--mono)}
.fp-path button{background:none;border:0;padding:2px 4px;border-radius:5px;color:var(--accent-text);cursor:pointer;font:inherit}
.fp-path button:hover{background:var(--accent-soft)}
.fp-list{max-height:min(46vh,380px);overflow:auto}
.fp-item{display:flex;align-items:center;gap:10px;width:100%;text-align:left;padding:9px 16px;border:0;border-bottom:1px solid var(--border);background:none;color:inherit;font:inherit;cursor:pointer}
.fp-item:hover,.fp-item:focus-visible{background:var(--surface-2)}
.fp-item .grow{min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.fp-drive .meter{width:120px;height:6px;border-radius:3px;background:var(--sunken);overflow:hidden}
.fp-drive .meter i{display:block;height:100%;background:var(--accent)}
.fp-f{display:flex;gap:8px;align-items:center;padding:12px 16px;border-top:1px solid var(--border);flex-wrap:wrap}
.ask-b{padding:16px;display:flex;flex-direction:column;gap:12px;font-size:13.5px}
.ask-b p{margin:0}
.ask-b .kv{grid-template-columns:90px 1fr}
.ask-b .sip{max-height:min(34vh,260px)}
/* timeline */
.tl-strip{position:relative;height:18px;margin:2px 0 0;border-top:1px dashed var(--border)}
.tl-strip i{position:absolute;top:4px;width:9px;height:9px;border-radius:50%;transform:translateX(-50%);border:2px solid var(--surface);cursor:pointer}
.tl-strip i.bad{background:var(--bad)} .tl-strip i.warn{background:var(--warn)} .tl-strip i.info{background:var(--accent)} .tl-strip i.ok{background:var(--ok)}
.tl-day{padding:8px 16px;background:var(--surface-2);border-bottom:1px solid var(--border);font-size:12px;font-weight:700;color:var(--text-2);text-transform:uppercase;letter-spacing:.04em}
.tl-ev{display:grid;grid-template-columns:70px 22px 92px 1fr;gap:10px;padding:9px 16px;border-bottom:1px solid var(--border);font-size:13.5px;align-items:start}
.tl-ev[data-link]{cursor:pointer} .tl-ev[data-link]:hover{background:var(--surface-2)}
.tl-ev .i{display:flex;padding-top:1px} .tl-ev.bad .i{color:var(--bad)} .tl-ev.warn .i{color:var(--warn)} .tl-ev.ok .i{color:var(--ok)} .tl-ev.info .i{color:var(--accent)}
.tl-ev .t{font-variant-numeric:tabular-nums;color:var(--text-2)}
.tl-ev.flash{animation:tlflash 1.6s}
@keyframes tlflash{from{background:var(--accent-soft)}to{background:transparent}}
@media (max-width:820px){.tl-ev{grid-template-columns:60px 22px 1fr}.tl-ev .k{display:none}}
/* upgrades */
.up-steps{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px;margin-bottom:16px}
.up-step{border:1px solid var(--border);border-radius:10px;padding:12px 14px;background:var(--surface)}
.up-step h3{margin:0 0 6px;font-size:13.5px;display:flex;gap:8px;align-items:center}
.up-step .n{display:inline-flex;width:22px;height:22px;border-radius:50%;align-items:center;justify-content:center;background:var(--sunken);font-size:12px;font-weight:700}
.up-step.done .n{background:var(--ok);color:#fff} .up-step.now .n{background:var(--accent-solid);color:#fff}
.up-step p{margin:0;font-size:13px;color:var(--text-2)}
.up-chg{display:grid;grid-template-columns:22px 130px 1fr;gap:10px;padding:8px 16px;border-bottom:1px solid var(--border);font-size:13.5px}
.up-chg:last-child{border-bottom:0}
.up-chg.bad .i{color:var(--bad)} .up-chg.warn .i{color:var(--warn)} .up-chg.info .i{color:var(--text-3)}
.up-diff{font:12.5px/1.55 var(--mono);margin:0;padding:0}
.up-diff div{padding:0 12px;white-space:pre-wrap;word-break:break-all}
.up-diff .add{background:var(--ok-soft)} .up-diff .del{background:var(--bad-soft)} .up-diff .sec{background:var(--surface-2);color:var(--text-3);padding:4px 12px;border-top:1px solid var(--border)}
@media (max-width:820px){.up-steps{grid-template-columns:1fr}.up-chg{grid-template-columns:22px 1fr}.up-chg .t{grid-column:2}}
/* sbc health */
.hl-kpis{display:grid;grid-template-columns:repeat(6,minmax(0,1fr));gap:10px;margin-bottom:14px}
.hl-kpi{border:1px solid var(--border);border-radius:10px;padding:10px 12px;background:var(--surface-2)}
.hl-kpi .l{font-size:12px;color:var(--text-3);font-weight:600}
.hl-kpi .v{font-size:19px;font-weight:700;margin-top:2px;font-variant-numeric:tabular-nums}
.hl-kpi.warn .v{color:var(--warn)} .hl-kpi.bad .v{color:var(--bad)} .hl-kpi.ok .v{color:var(--ok)}
.hl-meter{height:5px;border-radius:3px;background:var(--sunken);margin-top:6px;overflow:hidden}
.hl-meter i{display:block;height:100%;background:var(--accent)}
.hl-kpi.warn .hl-meter i{background:var(--warn)} .hl-kpi.bad .hl-meter i{background:var(--bad)}
.hl-two{display:grid;grid-template-columns:1fr 1fr;gap:14px;margin-bottom:14px}
.hl-sub{border:1px solid var(--border);border-radius:10px;overflow:hidden}
.hl-sub h3{font-size:13px;margin:0;padding:9px 12px;background:var(--surface-2);border-bottom:1px solid var(--border);display:flex;gap:8px;align-items:center}
.hl-sub .tbl td{padding:7px 12px}
@media (max-width:1180px){.hl-kpis{grid-template-columns:repeat(3,minmax(0,1fr))}.hl-two{grid-template-columns:1fr}}
/* ready check */
.sr-only{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0 0 0 0);white-space:nowrap}
.rd-verdict{display:flex;align-items:center;gap:16px;padding:20px 22px;border-radius:12px;border:1px solid var(--border);background:var(--surface);margin-bottom:16px;flex-wrap:wrap}
.rd-verdict .big{display:flex;align-items:center;gap:12px;font-size:24px;font-weight:750;letter-spacing:-.01em}
.rd-verdict.ready{background:var(--ok-soft);border-color:color-mix(in srgb,var(--ok) 40%,transparent)} .rd-verdict.ready .big{color:var(--ok)}
.rd-verdict.warnings{background:var(--warn-soft);border-color:color-mix(in srgb,var(--warn) 40%,transparent)} .rd-verdict.warnings .big{color:var(--warn)}
.rd-verdict.notready{background:var(--bad-soft);border-color:color-mix(in srgb,var(--bad) 40%,transparent)} .rd-verdict.notready .big{color:var(--bad)}
.rd-item{display:grid;grid-template-columns:22px 260px 1fr;gap:10px;padding:9px 16px;border-bottom:1px solid var(--border);font-size:13.5px;align-items:start}
.rd-item:last-child{border-bottom:0}
.rd-item .ic{display:flex;padding-top:1px} .rd-item.ok .ic{color:var(--ok)} .rd-item.warn .ic{color:var(--warn)} .rd-item.bad .ic{color:var(--bad)} .rd-item.skip .ic,.rd-item.running .ic{color:var(--text-3)}
.rd-item .nm{font-weight:650} .rd-item .dt{color:var(--text-2)}
.rd-item.bad .nm{color:var(--bad)}
.rd-hist{display:grid;grid-template-columns:1fr auto;gap:4px 10px;padding:9px 16px;border-bottom:1px solid var(--border);cursor:pointer;font-size:13px}
.rd-hist:hover{background:var(--surface-2)} .rd-hist.sel{background:var(--accent-soft)}
@media (max-width:820px){.rd-item{grid-template-columns:22px 1fr}.rd-item .dt{grid-column:2}}
.empty .ic{width:52px;height:52px;border-radius:14px;background:var(--accent-soft);color:var(--accent);display:flex;align-items:center;justify-content:center;margin:0 auto 14px}
.empty h4{color:var(--text);font-size:15px;margin:0 0 6px}
.empty p{margin:0 auto;max-width:420px;font-size:13.5px}

/* toast */
.toasts{position:fixed;right:20px;bottom:20px;display:flex;flex-direction:column;gap:8px;z-index:100}
.toast{background:var(--text);color:var(--bg);padding:10px 14px;border-radius:9px;box-shadow:var(--shadow-lg);font-weight:550;font-size:13.5px;animation:tin .18s ease-out;max-width:380px}
.toast.bad{background:var(--bad-solid);color:#fff}
@keyframes tin{from{opacity:0;transform:translateY(8px)}to{opacity:1;transform:none}}

/* chart tokens (validated: dataviz validator, both themes) */
:root{--s1:#2a78d6;--s2:#eb6834;--good:#0ca30c;--warning:#fab219;--critical:#d03b3b;--grid:#e8ebf0;--axis:#c3c9d3}
:root[data-theme="dark"]{--s1:#3987e5;--s2:#d95926;--grid:#2a3448;--axis:#475569}
@media (prefers-color-scheme:dark){:root:not([data-theme="light"]){--s1:#3987e5;--s2:#d95926;--grid:#2a3448;--axis:#475569}}
.chart{position:relative;padding:4px 0 0 40px}
.chart .plot{position:relative;height:var(--h,180px)}
.chart svg.p{position:absolute;inset:0;width:100%;height:100%;overflow:visible}
.chart .yl{position:absolute;left:-40px;width:34px;text-align:right;font-size:12px;color:var(--text-3);transform:translateY(-50%);font-variant-numeric:tabular-nums}
.chart .xl{display:flex;justify-content:space-between;font-size:12px;color:var(--text-3);margin-top:6px;font-variant-numeric:tabular-nums}
.chart .xh{position:absolute;top:0;bottom:0;width:1px;background:var(--text-3);pointer-events:none;display:none}
.chart .mk{position:absolute;width:9px;height:9px;border-radius:50%;border:2px solid var(--surface);transform:translate(-50%,-50%);pointer-events:none;display:none}
.chart .ref{position:absolute;right:0;font-size:12px;color:var(--text-3);transform:translateY(-110%)}
.legend{display:flex;gap:14px;flex-wrap:wrap;font-size:12.5px;color:var(--text-2);margin-bottom:8px}
.legend span{display:inline-flex;align-items:center;gap:6px}
.legend i{display:inline-block;width:14px;height:3px;border-radius:2px}
.legend i.box{width:10px;height:10px;border-radius:3px}
.tip{position:fixed;z-index:200;pointer-events:none;background:var(--surface);color:var(--text);border:1px solid var(--border-strong);border-radius:9px;box-shadow:var(--shadow-lg);padding:8px 10px;font-size:12.5px;display:none;min-width:150px}
.tip .t{color:var(--text-3);font-size:12px;margin-bottom:4px}
.tip .r{display:flex;align-items:center;gap:8px;justify-content:space-between}
.tip .r b{font-variant-numeric:tabular-nums}
.tip .k{display:inline-flex;align-items:center;gap:6px;color:var(--text-2)}
.tip .k i{display:inline-block;width:12px;height:3px;border-radius:2px}
.dash{display:grid;grid-template-columns:repeat(12,minmax(0,1fr));gap:16px}
.span-2{grid-column:span 2}.span-3{grid-column:span 3}.span-4{grid-column:span 4}.span-5{grid-column:span 5}.span-6{grid-column:span 6}.span-7{grid-column:span 7}.span-8{grid-column:span 8}.span-12{grid-column:span 12}
.stat{padding:16px 18px}
.stat .l{font-size:12.5px;color:var(--text-2);font-weight:600;display:flex;align-items:center;gap:7px}
.stat .v{font:700 28px/1.2 var(--mono);letter-spacing:-.02em;margin-top:4px;display:flex;align-items:baseline;gap:8px;font-variant-numeric:tabular-nums}
.stat .v small{font-family:var(--sans)}
/* overview line board: one square per line, so 1,000 lines still fit one panel */
.lm{display:flex;flex-wrap:wrap;align-items:stretch}
.lm-main{flex:3 1 560px;min-width:0;padding:8px 0}
.lm-row{display:grid;grid-template-columns:150px 104px minmax(0,1fr);gap:12px;align-items:start;padding:7px 16px}
.lm-name{display:flex;flex-direction:column;line-height:1.25;min-width:0}
.lm-name b{font-weight:650;font-size:13.5px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.lm-name span{font:12px var(--mono);color:var(--text-3)}
.lm-sum{font:600 12px var(--mono);padding:2px 8px;border-radius:5px;justify-self:start;background:var(--ok-soft);color:var(--ok-text)}
.lm-sum.bad{background:var(--bad-soft);color:var(--bad-text)} .lm-sum.warn{background:var(--warn-soft);color:var(--warn-text)}
.lm-cells{display:flex;flex-wrap:wrap;gap:3px;padding-top:3px}
.lm-c{width:11px;height:11px;border-radius:2px;border:0;padding:0;cursor:pointer;background:var(--lm-up,#166534)}
.lm-c.down{background:var(--bad)} .lm-c.warn{background:var(--warn)} .lm-c.call{background:var(--accent)}
.lm-c.idle{background:var(--border-strong)} .lm-c.unknown{background:transparent;box-shadow:inset 0 0 0 2px var(--text-3)}
.lm-c:hover{outline:2px solid var(--text);outline-offset:1px}
.lm-key{display:flex;flex-wrap:wrap;gap:14px;padding:10px 16px 4px;margin-top:4px;border-top:1px solid var(--border);font-size:12.5px;color:var(--text-3)}
.lm-key span{display:inline-flex;align-items:center;gap:6px}
.lm-key .lm-c{cursor:default}
.lm-key .lm-c:hover{outline:0}
.lm-side{flex:1 1 300px;min-width:0;border-left:1px solid var(--border)}
.lm-side h3{margin:0;font-size:14px;text-transform:none;letter-spacing:0;color:var(--text)}
.lm-att{display:grid;grid-template-columns:10px minmax(0,1fr) auto;gap:10px;align-items:center;padding:8px 16px;width:100%;border:0;background:none;color:inherit;font:inherit;text-align:left;cursor:pointer}
.lm-att:hover{background:var(--surface-2)}
.lm-att .t{font-weight:600;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.lm-att .s{font-size:12.5px;color:var(--text-3);overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
:root[data-theme="light"]{--lm-up:#86efac}
@media (max-width:820px){.lm-row{grid-template-columns:1fr}.lm-side{border-left:0;border-top:1px solid var(--border)}}
.stat .v small{font-size:13px;font-weight:600;color:var(--text-3);letter-spacing:0}
.stat .s{font-size:12.5px;color:var(--text-3);margin-top:2px}
.mos{display:inline-flex;align-items:center;gap:5px;height:22px;padding:0 8px;border-radius:6px;font-size:12px;font-weight:700;font-variant-numeric:tabular-nums;white-space:nowrap;background:var(--sunken);color:var(--text-2)}
.mos::before{content:"";width:7px;height:7px;border-radius:50%;background:currentColor}
.mos.good{color:var(--ok)}.mos.fair{color:var(--warn)}.mos.poor,.mos.bad{color:var(--bad)}
.feed-list{display:flex;flex-direction:column}
.feed-item{display:grid;grid-template-columns:auto 1fr auto;gap:10px;align-items:center;padding:10px 16px;border-bottom:1px solid var(--border);cursor:pointer}
.feed-item:last-child{border-bottom:0}
.feed-item:hover{background:var(--surface-2)}
.feed-item .tt{font-weight:600}
.feed-item .ss{font-size:12.5px;color:var(--text-3);overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.sbc-health{display:grid;grid-template-columns:repeat(auto-fill,minmax(220px,1fr));gap:12px;padding:16px}
.sbc-health > div{border:1px solid var(--border);border-radius:10px;padding:12px 14px;background:var(--surface-2)}
.sbc-health .n{font-weight:700;display:flex;align-items:center;gap:8px}
.sbc-health .m{display:grid;grid-template-columns:repeat(3,1fr);gap:6px;margin-top:10px;font-size:12px;color:var(--text-3)}
.sbc-health .m b{display:block;font-size:16px;color:var(--text);font-variant-numeric:tabular-nums}
.tests{display:grid;grid-template-columns:minmax(340px,38%) 1fr;gap:16px;align-items:start}
.test-card{padding:14px 16px;border-bottom:1px solid var(--border);display:grid;grid-template-columns:1fr auto;gap:8px 12px;cursor:pointer}
.test-card:hover{background:var(--surface-2)}
.test-card.sel{background:var(--accent-soft)}
.test-card .nm{font-weight:700}
.test-card .tg{font-size:12.5px;color:var(--text-3)}
.test-card .act{display:flex;gap:6px;align-items:center}
.spark{width:120px;height:26px;display:block}
.stepper{display:flex;gap:0;margin:6px 0 14px}
.stepper div{flex:1;text-align:center;font-size:12px;color:var(--text-3);position:relative;padding-top:18px}
.stepper div::before{content:"";position:absolute;top:4px;left:50%;width:10px;height:10px;border-radius:50%;background:var(--border-strong);transform:translateX(-50%)}
.stepper div::after{content:"";position:absolute;top:8px;left:calc(50% + 8px);right:calc(-50% + 8px);height:2px;background:var(--border)}
.stepper div:last-child::after{display:none}
.stepper div.done{color:var(--text-2)}.stepper div.done::before{background:var(--ok)}
.stepper div.now{color:var(--accent-text);font-weight:700}.stepper div.now::before{background:var(--accent);box-shadow:0 0 0 4px var(--accent-soft);animation:pulse 1.4s infinite}
.form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px 14px}
.form-grid .full{grid-column:1/-1}
.form-grid label{font-size:12px;font-weight:650;color:var(--text-2);display:block;margin-bottom:5px}
.form-grid input,.form-grid select{width:100%}
.help{font-size:12px;color:var(--text-3);margin-top:4px}
.bulk-plan{display:flex;gap:8px;flex-wrap:wrap;padding:10px 16px;border-bottom:1px solid var(--border)}
/* config compare */
.cmp-files{display:grid;grid-template-columns:minmax(0,1fr) auto minmax(0,1fr);gap:12px;align-items:stretch}
.cmp-drop{display:flex;flex-direction:column;gap:10px;padding:14px;border:1.5px dashed var(--border-strong);border-radius:12px;background:var(--surface-2);min-width:0;transition:border-color .15s,background-color .15s}
.cmp-drop:hover{border-color:var(--accent)}
.cmp-drop.over{border-color:var(--accent);background:var(--accent-soft)}
.cmp-drop.loaded{border-style:solid;border-color:var(--border);background:var(--surface)}
.cmp-drop input[type=text]{width:100%}
.cmp-pick{position:relative;display:flex;gap:12px;align-items:center;cursor:pointer;min-width:0}
.cmp-pick input[type=file]{position:absolute;width:1px;height:1px;opacity:0;overflow:hidden}
.cmp-pick:focus-within{outline:2px solid var(--accent);outline-offset:4px;border-radius:8px}
.cmp-txt{display:flex;flex-direction:column;min-width:0}
.cmp-fn{font-weight:650;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.cmp-meta{font-size:12px;color:var(--text-3)}
.cmp-tag{flex:none;display:inline-flex;align-items:center;justify-content:center;width:30px;height:30px;border-radius:8px;font-weight:750;font-size:13.5px;color:#fff;background:var(--accent-solid);letter-spacing:0;text-transform:none}
.cmp-tag.b{background:var(--info)}
.cmp-tag.sm{width:18px;height:18px;border-radius:5px;font-size:10.5px;margin-right:6px;vertical-align:-4px}
.cmp-swap{align-self:center}
.cmp-std{display:flex;gap:10px;align-items:center;flex-wrap:wrap;margin-bottom:14px}
.cmp-std select{height:34px;min-width:220px;max-width:100%}
.cmp-up{position:relative;overflow:hidden}
.cmp-up input{position:absolute;inset:0;opacity:0;cursor:pointer}
.cmp-bar{position:sticky;top:73px;z-index:5;display:flex;gap:10px;align-items:center;flex-wrap:wrap;padding:10px 0;margin-bottom:6px;background:var(--bg)}
.cmp-legend{display:inline-flex;align-items:center;gap:6px;font-size:12.5px;color:var(--text-2)}
.cmp-legend i{width:14px;height:14px;border-radius:4px;background:var(--warn-soft);box-shadow:inset 3px 0 0 var(--warn)}
button.stat{font:inherit;text-align:left;cursor:pointer;color:inherit}
button.stat:hover{border-color:var(--border-strong)}
.cmp-verdict.warn .v{color:var(--warn-text)}.cmp-verdict.ok .v{color:var(--ok)}
.cmp-verdict .s{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.cmp-card{margin-bottom:12px;scroll-margin-top:130px;overflow:hidden;min-width:0;max-width:100%}
.cmp-card .scroll{max-width:100%}
.cmp-tbl{min-width:640px}
details.cmp-card > summary{cursor:pointer;list-style:none}
details.cmp-card > summary::-webkit-details-marker{display:none}
details.cmp-card > summary svg:first-child{flex:none;color:var(--text-3);transition:transform .15s}
details.cmp-card[open] > summary svg:first-child{transform:rotate(90deg)}
details.cmp-card:not([open]) > summary{border-bottom:0}
.cmp-tbl{table-layout:fixed}
.cmp-tbl th{white-space:normal}
.cmp-tbl td{white-space:normal;overflow-wrap:anywhere;vertical-align:top}
.cmp-tbl td.v{font-family:var(--mono);font-size:12.5px}
/* A difference is something to look at, not an error: full-contrast text on a light tint of the column's own colour
   (A blue, B purple, as the A/B tags), the changed part a stronger tint and underlined so it does not rely on colour. */
.cmp-tbl td.v.d{color:var(--text)}
.cmp-tbl td.v.d.a{background:color-mix(in srgb,var(--accent) 8%,var(--surface))}
.cmp-tbl td.v.d.b{background:color-mix(in srgb,var(--info) 8%,var(--surface))}
.cmp-tbl td.v.d .cmp-none{color:var(--text-2)}
.cmp-tbl td.lead.d{box-shadow:inset 3px 0 0 var(--warn)}
.cmp-tbl mark{color:inherit;font-weight:700;border-radius:3px;padding:0 2px;text-decoration:underline 2px;text-underline-offset:3px}
.cmp-tbl td.a mark{background:color-mix(in srgb,var(--accent) 24%,transparent);text-decoration-color:var(--accent)}
.cmp-tbl td.b mark{background:color-mix(in srgb,var(--info) 24%,transparent);text-decoration-color:var(--info)}
.cmp-tbl tr.sec td{background:var(--surface-2);font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:.04em;color:var(--text-2);padding:7px 12px}
.cmp-none{font-family:var(--sans);font-style:italic;color:var(--text-3)}
.cmp-rl{display:flex;flex-direction:column}
.cmp-rl .mono{color:var(--text-3)}
.cmp-toc{display:flex;flex-wrap:wrap;gap:6px;margin:0 0 12px}
.cmp-toc a{display:inline-flex;gap:6px;align-items:center;padding:3px 9px;border-radius:7px;background:var(--surface);border:1px solid var(--border);font:12px var(--mono);color:var(--text-2)}
.cmp-toc a b{color:var(--warn-text);font-family:var(--sans)}
.cmp-toc a:hover{border-color:var(--warn);text-decoration:none}
.gauge{display:flex;align-items:center;gap:16px}
.gauge svg{flex:none}
@media (max-width:1180px){.span-2{grid-column:span 4}.span-3,.span-4,.span-5{grid-column:span 6}.span-6,.span-7,.span-8{grid-column:span 12}.tests{grid-template-columns:1fr}}
@media (max-width:820px){.dash > *{grid-column:span 12!important}.form-grid{grid-template-columns:1fr}.cmp-files{grid-template-columns:1fr}.cmp-swap{justify-self:center;transform:rotate(90deg)}.cmp-bar{position:static}}
@media print{.side,.top,.alerts,.tabs,.d-actions,.toolbar{display:none!important}.app{display:block}}
.hidden{display:none!important}
body.locked .app{display:none}
.gate{min-height:100vh;display:flex;align-items:center;justify-content:center;padding:24px}
.gate-card{width:min(400px,100%);background:var(--surface);border:1px solid var(--border);border-radius:16px;box-shadow:var(--shadow-lg);padding:28px 28px 24px}
.gate-card h1{margin:14px 0 6px;font-size:22px;letter-spacing:-.02em}
.gate-card p{margin:0;color:var(--text-2);font-size:13.5px;line-height:1.45}
.gate-card label{display:block;margin:14px 0 6px;font-size:12px;font-weight:650;color:var(--text-2)}
.gate-card input{width:100%}
.gate-card .btn{width:100%;justify-content:center;margin-top:18px;height:38px}
.gate-err{min-height:1.2em;margin-top:10px;color:var(--bad);font-size:13px}
@media (max-width:1180px){.split{grid-template-columns:1fr}.rows{max-height:42vh}.kpis{grid-template-columns:repeat(3,minmax(0,1fr))}}
@media (max-width:820px){
  .app{grid-template-columns:minmax(0,1fr)}
  .side{position:static;height:auto;border-right:0;border-bottom:1px solid var(--border)}
  .nav{flex-direction:row;flex:none;overflow:auto;align-items:center}.nav button{width:auto;white-space:nowrap}.nav .count{display:none}
  .nav-label{flex:none;padding:0 4px 0 16px;align-self:center}
  .side-section,.sbc-mini{display:none}
  .feeds{display:none}
  .top{padding:12px 16px;flex-wrap:wrap}.content{padding:16px}
  .kpis{grid-template-columns:repeat(2,minmax(0,1fr))}
  .sdp-pair{grid-template-columns:1fr}
  .tl-row,.tl-axis{grid-template-columns:1fr}
  .log > div,.log.viewer > div{grid-template-columns:1fr}
  .sbc-edit{grid-template-columns:1fr}
}
@media (prefers-reduced-motion:reduce){*{animation:none!important}}
</style>
</head>
<body class="locked">
<div id="gate" class="gate">
  <form id="gateForm" class="gate-card" autocomplete="on">
    <svg width="36" height="36" viewBox="0 0 32 32" aria-hidden="true"><rect width="32" height="32" rx="8" fill="var(--accent)"/><path d="M5 17h5l3-8 5 15 3-9h6" stroke="#fff" stroke-width="2.6" fill="none" stroke-linecap="round" stroke-linejoin="round"/></svg>
    <h1 id="gateTitle">Sign in</h1>
    <p id="gateHelp">Enter the username and password to open MediantTrace.</p>
    <label for="gateUser">Username</label>
    <input id="gateUser" name="username" autocomplete="username" required>
    <label for="gatePass">Password</label>
    <input id="gatePass" name="password" type="password" autocomplete="current-password" required>
    <div id="gatePass2wrap" class="hidden">
      <label for="gatePass2">Confirm password</label>
      <input id="gatePass2" type="password" autocomplete="new-password">
    </div>
    <button class="btn primary" id="gateBtn" type="submit">Sign in</button>
    <p id="gateErr" class="gate-err" role="alert"></p>
  </form>
</div>
<div class="app">
  <aside class="side">
    <div class="brand">
      <svg width="30" height="30" viewBox="0 0 32 32" aria-hidden="true"><rect width="32" height="32" rx="8" fill="var(--accent)"/><path d="M5 17h5l3-8 5 15 3-9h6" stroke="#fff" stroke-width="2.6" fill="none" stroke-linecap="round" stroke-linejoin="round"/></svg>
      <div>MediantTrace<small>SBC call troubleshooting</small></div>
    </div>
    <nav class="nav" id="nav">
      <div class="nav-label">Watch</div>
      <button data-view="dashboard" class="on"><span data-i="grid"></span>Dashboard</button>
      <button data-view="ready"><span data-i="ready"></span>Ready check<span class="count" id="nc-ready"></span></button>
      <button data-view="health"><span data-i="pulse"></span>SBC health<span class="count" id="nc-health"></span></button>
      <button data-view="lines"><span data-i="board"></span>Line board<span class="count" id="nc-lines"></span></button>
      <button data-view="calls"><span data-i="phone"></span>Calls<span class="count" id="nc-calls"></span></button>
      <div class="nav-label">Test</div>
      <button data-view="tests"><span data-i="beaker"></span>Call tests<span class="count" id="nc-tests"></span></button>
      <button data-view="bulk"><span data-i="tasks"></span>Line tests<span class="count" id="nc-bulk"></span></button>
      <div class="nav-label">Look up</div>
      <button data-view="timeline"><span data-i="clock"></span>Timeline</button>
      <button data-view="streams"><span data-i="wave"></span>Audio streams<span class="count" id="nc-streams"></span></button>
      <button data-view="syslog"><span data-i="term"></span>Syslog<span class="count" id="nc-syslog"></span></button>
      <button data-view="captures"><span data-i="file"></span>Captures</button>
      <div class="nav-label">Set up</div>
      <button data-view="sbcs"><span data-i="server"></span>SBCs<span class="count" id="nc-sbcs"></span></button>
      <button data-view="compare"><span data-i="diff"></span>Config compare</button>
      <button data-view="upgrades"><span data-i="out"></span>Upgrades</button>
      <button data-view="changes"><span data-i="shield"></span>Changes<span class="count" id="nc-changes"></span></button>
      <button data-view="settings"><span data-i="gear"></span>Settings</button>
    </nav>
    <div class="side-section">SBC feeds</div>
    <div class="sbc-mini" id="sbcMini"></div>
    <div class="side-foot">
      <span class="avatar" id="avatar" aria-hidden="true"></span>
      <div class="who-box"><div id="who"></div><div id="ver"></div></div>
      <button class="btn ghost icon sm" id="theme" title="Switch light / dark" aria-label="Switch light or dark theme"><span data-i="moon"></span></button>
      <button class="btn ghost icon sm" id="signOut" type="button" title="Sign out" aria-label="Sign out"><span data-i="out"></span></button>
    </div>
  </aside>

  <div class="main">
    <header class="top">
      <div><h1 id="title">Calls</h1><div class="sub" id="subtitle"></div></div>
      <div class="grow"></div>
      <div class="feeds" id="feeds" aria-label="Feeds from the SBCs"></div>
      <div id="sbcLinks"></div>
      <div id="recBox"></div>
    </header>
    <div class="content">
      <div class="alerts" id="alerts"></div>

      <section id="v-dashboard"><div id="dashBox"></div></section>

      <section id="v-ready" class="hidden"><div id="readyBox"></div></section>

      <section id="v-health" class="hidden"><div id="healthBox"></div></section>

      <section id="v-upgrades" class="hidden"><div id="upBox"></div></section>

      <section id="v-timeline" class="hidden"><div id="tlBox"></div></section>

      <section id="v-lines" class="hidden"><div id="linesBox"></div></section>

      <section id="v-tests" class="hidden"><div id="testsBox"></div></section>

      <section id="v-bulk" class="hidden"><div id="bulkBox"></div></section>

      <section id="v-calls" class="hidden">
        <div class="kpis" id="kpis"></div>
        <div class="toolbar">
          <div class="search"><span data-i="search"></span><input type="search" id="q" placeholder="Search number, Call-ID or SID" aria-label="Search calls"><kbd>/</kbd></div>
          <div class="chips" id="fchips" role="tablist"></div>
          <select id="sbcFilter" aria-label="SBC"></select>
          <label class="toggle" title="SIP OPTIONS keep-alives are hidden by default"><input type="checkbox" id="showOptions"> Show OPTIONS</label>
          <button class="btn sm" id="callCsv" title="Download the calls in this list">CSV</button>
        </div>
        <div id="player" class="card" style="display:none;margin:0 0 12px"><div class="card-b" style="display:flex;gap:12px;align-items:center;flex-wrap:wrap"><audio id="playAudio" controls style="height:36px"></audio><span id="playLabel" class="muted"></span><button class="btn sm" id="playStop">Close</button></div></div>
        <div class="split">
          <div class="card list">
            <div class="list-h"><span id="listCount"></span><span>↑ ↓ to move</span></div>
            <div class="rows" id="rows"></div>
          </div>
          <div class="card detail" id="detail"></div>
        </div>
      </section>

      <section id="v-streams" class="hidden">
        <div class="toolbar"><select id="sbcFilter2" aria-label="SBC"></select><span class="muted" id="streamNote"></span></div>
        <div class="card scroll" id="streamsBox"></div>
      </section>

      <section id="v-syslog" class="hidden">
        <div class="sl-tabs" id="slTabs" role="tablist" aria-label="Devices"></div>
        <div class="toolbar">
          <div class="search"><span data-i="search"></span><input type="search" id="sq" placeholder="Display filter: number, Call-ID, SID, word" aria-label="Display filter"></div>
          <label class="toggle" title="The filter is a regular expression"><input type="checkbox" id="slRegex"> RegExp</label>
          <label class="toggle" title="Match case"><input type="checkbox" id="slCase"> Match case</label>
          <label class="toggle"><input type="checkbox" id="slWord"> Whole words</label>
          <label class="toggle" title="Hide the lines that match instead of showing them"><input type="checkbox" id="slExclude"> Filter out</label>
          <select id="slLevel" aria-label="Severity"><option value="">All severities</option><option value="problems">Errors &amp; warnings</option><option value="errors">Errors</option><option value="warnings">Warnings</option></select>
          <select id="slKind" aria-label="Which lines"><option value="">All lines</option><option value="calls">Call lines (with a SID)</option><option value="noncalls">Non-call lines</option><option value="cdr">CDRs</option></select>
          <select id="slLimit" aria-label="Lines shown"><option value="2000">2,000 lines</option><option value="5000">5,000 lines</option><option value="20000">20,000 lines</option></select>
        </div>
        <div class="toolbar">
          <span id="slSid"></span>
          <label class="toggle"><input type="checkbox" id="follow" checked> Auto-scroll</label>
          <button class="btn sm" id="slFreeze" aria-pressed="false" title="Stop updating the screen; logging carries on">Freeze</button>
          <button class="btn sm" id="slFind" title="Find in text (Ctrl+F, F3)">Find</button>
          <button class="btn sm" id="slGoto" title="Go to line (Ctrl+G)">Go to line</button>
          <button class="btn sm" id="slMarks" title="Next marked line (F2, Shift+F2 back; Ctrl+F2 marks)">Next mark</button>
          <button class="btn sm" id="slHl">Highlight keywords</button>
          <span class="grow"></span>
          <label class="toggle"><input type="checkbox" id="slDate"> Show date</label>
          <label class="toggle" title="Times in UTC instead of this PC's time"><input type="checkbox" id="slUtc"> UTC</label>
          <button class="btn sm" id="slOpen" title="Open a saved log or capture (or drop it on the page)">Open…</button>
          <button class="btn sm" id="slPaste" title="Paste a log (Ctrl+V)">Paste</button>
          <a class="btn sm" id="slSave" href="/api/syslog/save" title="Save the syslog buffer as text">Save as…</a>
          <button class="btn sm" id="slClean" title="Clean the display">Clean</button>
          <button class="btn sm" id="slWrite" title="Write every syslog line to text files (Settings → Syslog)">Write log</button>
        </div>
        <div class="findbar hidden" id="slFindBar" role="search">
          <input type="search" id="slFindQ" placeholder="Find in the lines shown" aria-label="Find">
          <label class="toggle"><input type="checkbox" id="slFindRegex"> RegExp</label>
          <label class="toggle"><input type="checkbox" id="slFindCase"> Match case</label>
          <label class="toggle"><input type="checkbox" id="slFindWord"> Whole words</label>
          <button class="btn sm" id="slFindPrev" title="Shift+F3">Previous</button>
          <button class="btn sm primary" id="slFindNext" title="F3">Next</button>
          <span class="muted" id="slFindN"></span><span class="grow"></span>
          <button class="btn sm ghost" id="slFindClose" aria-label="Close find">Close</button>
        </div>
        <div class="log viewer" id="syslogBox" tabindex="0" aria-label="Syslog lines"></div>
        <div class="sl-status" id="slStatus" aria-live="polite"></div>
      </section>

      <section id="v-captures" class="hidden"><div id="capBox"></div></section>
      <section id="v-sbcs" class="hidden"><div id="sbcBox"></div></section>
      <section id="v-compare" class="hidden"><div id="cmpBox"></div></section>
      <section id="v-changes" class="hidden"><div id="chgBox"></div></section>
      <section id="v-settings" class="hidden"><div id="setBox"></div></section>
    </div>
  </div>
</div>
<div class="toasts" id="toasts" aria-live="polite"></div>
<input type="file" id="fileOpen" hidden aria-hidden="true">

<script>
"use strict";
/* ------------------------------------------------------------ helpers */
const $ = id => document.getElementById(id);
const esc = s => String(s ?? "").replace(/[&<>"']/g, c => ({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"}[c]));
const fmtN = v => (v ?? 0).toLocaleString();
const tm = s => s ? new Date(s).toLocaleTimeString([], {hour:"2-digit", minute:"2-digit", second:"2-digit"}) : "";
const tms = s => { if (!s) return ""; const d = new Date(s); return tm(s) + "." + String(d.getMilliseconds()).padStart(3, "0"); };
const dtm = s => s ? new Date(s).toLocaleString([], {day:"2-digit", month:"short", hour:"2-digit", minute:"2-digit", second:"2-digit"}) : "";
const dur = secs => { secs = Math.max(0, Math.round(secs || 0)); const h = Math.floor(secs / 3600), m = Math.floor(secs % 3600 / 60), s = secs % 60; return h ? `${h}:${String(m).padStart(2,"0")}:${String(s).padStart(2,"0")}` : `${m}:${String(s).padStart(2,"0")}`; };
const bytes = b => b < 1024 ? b + " B" : b < 1048576 ? (b / 1024).toFixed(1) + " KB" : b < 1073741824 ? (b / 1048576).toFixed(1) + " MB" : (b / 1073741824).toFixed(1) + " GB";
const host = ep => { if (!ep) return ""; const i = ep.lastIndexOf(":"); return i > 0 ? ep.slice(0, i) : ep; };
const peerName = addr => {
  const h = host(addr || ""); if (!h) return "";
  const hits = (status?.labels || []).filter(n => n.ip === h);
  return (hits.find(n => n.kind === "manual") || hits[0])?.name || "";
};
const namedText = text => esc(text).replace(/\b(\d{1,3}(?:\.\d{1,3}){3})\b/g, ip => { const n = peerName(ip); return n ? `<b>${esc(n)}</b> (${ip})` : ip; });
const whoEp = ep => { const n = peerName(ep); return n ? `${n} (${ep})` : (ep || "SBC"); };
const addrCell = addr => { const n = peerName(addr); return n ? `<b>${esc(n)}</b><div class="muted mono" style="font-size:11.5px">${esc(addr)}</div>` : `<span class="mono">${esc(addr)}</span>`; };
const store = { get(k, d) { try { const v = localStorage.getItem("mt." + k); return v === null ? d : JSON.parse(v); } catch { return d; } }, set(k, v) { try { localStorage.setItem("mt." + k, JSON.stringify(v)); } catch {} } };
function setHTML(el, html) { if (el && el.__h !== html) { el.innerHTML = html; el.__h = html; return true; } return false; }

const ICONS = {
  phone:'<path d="M5 4h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 6a2 2 0 0 1 2-2"/>',
  wave:'<path d="M2 12h2M6 8v8M10 5v14M14 9v6M18 7v10M22 12h-2"/>',
  list:'<path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01"/>',
  file:'<path d="M14 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z"/><path d="M14 3v6h6"/>',
  server:'<rect x="3" y="4" width="18" height="7" rx="2"/><rect x="3" y="13" width="18" height="7" rx="2"/><path d="M7 7.5h.01M7 16.5h.01"/>',
  gear:'<circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M4.2 4.2l2.1 2.1M17.7 17.7l2.1 2.1M2 12h3M19 12h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1"/>',
  search:'<circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/>',
  moon:'<path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z"/>',
  alert:'<path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z"/><path d="M12 9v4M12 17h.01"/>',
  x:'<circle cx="12" cy="12" r="9"/><path d="m15 9-6 6M9 9l6 6"/>',
  check:'<circle cx="12" cy="12" r="9"/><path d="m8 12 3 3 5-6"/>',
  info:'<circle cx="12" cy="12" r="9"/><path d="M12 16v-4M12 8h.01"/>',
  ext:'<path d="M14 4h6v6M20 4l-9 9M18 14v5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1h5"/>',
  copy:'<rect x="9" y="9" width="12" height="12" rx="2"/><path d="M5 15H4a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1h10a1 1 0 0 1 1 1v1"/>',
  down:'<path d="M12 4v12M6 10l6 6 6-6M4 20h16"/>',
  link:'<path d="M10 14a5 5 0 0 0 7 0l3-3a5 5 0 0 0-7-7l-1 1"/><path d="M14 10a5 5 0 0 0-7 0l-3 3a5 5 0 0 0 7 7l1-1"/>',
  rec:'<circle cx="12" cy="12" r="7" fill="currentColor"/>',
  stop:'<rect x="6" y="6" width="12" height="12" rx="2" fill="currentColor"/>',
  plus:'<path d="M12 5v14M5 12h14"/>',
  trash:'<path d="M4 7h16M10 11v6M14 11v6M5 7l1 13h12l1-13M9 7V4h6v3"/>',
  in:'<path d="M12 4v14M6 12l6 6 6-6"/>',
  out:'<path d="M12 20V6M6 12l6-6 6 6"/>',
  shield:'<path d="M12 3 4 6v6c0 5 3.4 8 8 9 4.6-1 8-4 8-9V6z"/>',
  diff:'<path d="M8 3v12M4 11l4 4 4-4M16 21V9M12 13l4-4 4 4"/>',
  grid:'<rect x="3" y="3" width="7" height="9" rx="1.5"/><rect x="14" y="3" width="7" height="5" rx="1.5"/><rect x="14" y="12" width="7" height="9" rx="1.5"/><rect x="3" y="16" width="7" height="5" rx="1.5"/>',
  beaker:'<path d="M9 3h6M10 3v6L4.5 18.5A2 2 0 0 0 6.2 21h11.6a2 2 0 0 0 1.7-2.5L14 9V3"/><path d="M7 15h10"/>',
  play:'<path d="M7 4v16l13-8z" fill="currentColor"/>',
  edit:'<path d="M4 20h4L19 9l-4-4L4 16z"/><path d="m13.5 6.5 4 4"/>',
  report:'<path d="M14 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z"/><path d="M14 3v6h6M8 13h8M8 17h5"/>',
  clock:'<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
  print:'<path d="M6 9V3h12v6M6 18H4a1 1 0 0 1-1-1v-6a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v6a1 1 0 0 1-1 1h-2"/><rect x="6" y="14" width="12" height="7" rx="1"/>',
  swap:'<path d="M7 4 3 8l4 4M3 8h14M17 20l4-4-4-4M21 16H7"/>',
  chev:'<path d="m9 6 6 6-6 6"/>',
  board:'<rect x="3" y="3" width="8" height="5" rx="1.5"/><rect x="13" y="3" width="8" height="5" rx="1.5"/><rect x="3" y="10" width="8" height="5" rx="1.5"/><rect x="13" y="10" width="8" height="5" rx="1.5"/><rect x="3" y="17" width="8" height="4" rx="1.5"/><rect x="13" y="17" width="8" height="4" rx="1.5"/>',
  ready:'<circle cx="12" cy="12" r="9"/><path d="M8 12.5l2.5 2.5L16 9.5"/>',
  pulse:'<path d="M3 12h4l2.5-6 5 12 2.5-6h4"/>',
  term:'<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M7 9l3 3-3 3M12 15h5"/>',
  tasks:'<path d="M3 6l1.5 1.5L7 5M3 12l1.5 1.5L7 11M3 18l1.5 1.5L7 17M11 6h10M11 12h10M11 18h10"/>',

};
const ic = (n, size = 16) => `<svg width="${size}" height="${size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${ICONS[n] || ""}</svg>`;
function paintIcons(root = document) { root.querySelectorAll("[data-i]").forEach(e => { if (!e.firstChild) e.innerHTML = ic(e.dataset.i, e.closest(".search") ? 15 : 17); }); }

function toast(text, bad) {
  const t = document.createElement("div");
  t.className = "toast" + (bad ? " bad" : ""); t.textContent = text;
  if (bad) t.setAttribute("role", "alert");   // errors are announced at once, and stay longer to be read
  $("toasts").appendChild(t); setTimeout(() => t.remove(), bad ? 7000 : 3800);
}
// The button that started an action shows it is busy until the server answers, so it cannot be pressed twice.
let pressed = null, pressedAt = 0;
document.addEventListener("click", e => { pressed = e.target.closest("button"); pressedAt = Date.now(); }, true);
async function copyText(text) {
  try { await navigator.clipboard.writeText(text); }
  catch { const a = document.createElement("textarea"); a.value = text; document.body.appendChild(a); a.select(); try { document.execCommand("copy"); } catch {} a.remove(); }
  toast("Copied");
}
const copyChip = text => `<span class="copy">${esc(text)}<button data-copy="${esc(text)}" title="Copy" aria-label="Copy">${ic("copy", 13)}</button></span>`;
document.addEventListener("click", e => { const b = e.target.closest("[data-copy]"); if (b) { e.preventDefault(); copyText(b.dataset.copy); } });
document.addEventListener("click", e => { const g = e.target.closest("[data-go]"); if (g) { e.preventDefault(); go(g.dataset.go); } });
const emptyState = (i, h, p) => `<div class="empty"><div class="ic">${ic(i, 24)}</div><h4>${h}</h4><p>${p}</p></div>`;

async function api(path, body) {
  const opt = body === undefined ? {} : {method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify(body)};
  // Only a button the handler has not already disabled itself, pressed just now.
  const btn = body !== undefined && pressed?.isConnected && !pressed.disabled && Date.now() - pressedAt < 1000 ? pressed : null;
  if (btn) { btn.disabled = true; btn.setAttribute("aria-busy", "true"); }
  try {
    let r;
    // fetch only throws when nothing answered: the program stopped, is restarting, or the network dropped.
    try { r = await fetch(path, opt); }
    catch { throw new Error("MediantTrace is not answering. It may be restarting: try again in a few seconds."); }
    const ct = r.headers.get("content-type") || "";
    const data = ct.includes("json") ? await r.json() : await r.text();
    if (!r.ok) throw new Error(data.message || data || r.statusText);
    return data;
  } finally {
    if (btn?.getAttribute("aria-busy") === "true") { btn.disabled = false; btn.removeAttribute("aria-busy"); }
  }
}

/* ------------------------------------------------------------ theme */
function applyTheme(t) { if (t) document.documentElement.dataset.theme = t; else delete document.documentElement.dataset.theme; }
applyTheme(store.get("theme", "dark"));   // dark first; the moon button switches to light
$("theme").onclick = () => {
  const cur = document.documentElement.dataset.theme;
  const dark = cur ? cur === "dark" : matchMedia("(prefers-color-scheme:dark)").matches;
  const next = dark ? "light" : "dark"; applyTheme(next); store.set("theme", next);
};

/* ------------------------------------------------------------ state */
let view = store.get("view", "dashboard"), status = null, calls = [], selected = null, detail = null;
let filter = store.get("filter", "all"), dtab = "overview", bothLegs = true, openMsg = null, legDetails = {}, extraLegs = new Set(), lastFlow = null;
const sbcName = ip => { const s = (status?.sbcs || []).find(x => x.ip === ip && x.listed); return s ? s.name : ip; };
const titles = {dashboard:"Dashboard", ready:"Ready check", health:"SBC health", upgrades:"Upgrades", timeline:"Timeline", lines:"Line board", calls:"Calls", tests:"Call tests", bulk:"Line tests", streams:"Audio streams", syslog:"Syslog", captures:"Captures", sbcs:"SBCs", compare:"Config compare", changes:"Changes", settings:"Settings"};
const sbcQ = el => el.value ? "sbc=" + encodeURIComponent(el.value) : "";

// The address bar follows the view (#view=sbcs, or #call= / #result= set by the views), so Back works and links can be shared.
const hashView = h => { const p = new URLSearchParams(h.slice(1)); return p.get("call") ? "calls" : p.get("result") ? "tests" : p.get("view"); };
// how: "push" (a click), "replace" (first open) or "none" (Back / Forward already moved the address).
function go(v, how = "push") {
  if (!titles[v]) v = "calls";
  if (v !== "calls") stopPlayer();
  view = v; store.set("view", v);
  if (how !== "none" && hashView(location.hash) !== v) history[how === "replace" ? "replaceState" : "pushState"](null, "", "#view=" + v);
  document.querySelectorAll("#nav button").forEach(b => {
    b.classList.toggle("on", b.dataset.view === v);
    if (b.dataset.view === v) b.setAttribute("aria-current", "page"); else b.removeAttribute("aria-current");
  });
  document.querySelectorAll(".content > section").forEach(s => s.classList.toggle("hidden", s.id !== "v-" + v));
  $("title").textContent = titles[v];
  setDocTitle();
  refresh();
}
window.addEventListener("popstate", () => {
  const p = new URLSearchParams(location.hash.slice(1));
  if (p.get("call") && +p.get("call") !== selected) { selected = +p.get("call"); detail = null; }
  if (p.get("result")) selectedResult = p.get("result");
  go(hashView(location.hash) || "dashboard", "none");
});
// The browser tab shows the number of problems, so a tab in the background still says when something is wrong.
let problemCount = 0;
function setDocTitle() { document.title = (problemCount ? `(${problemCount}) ` : "") + (titles[view] || "MediantTrace") + " · MediantTrace"; }
document.querySelectorAll("#nav button").forEach(b => b.onclick = () => go(b.dataset.view));
$("playStop").onclick = stopPlayer;

/* ------------------------------------------------------------ header: capture + sbc links */
// "12 s", "4 min", "3 h", "4 d": how long ago, short enough for a chip.
const agoText = sec => sec < 60 ? Math.round(sec) + " s" : sec < 3600 ? Math.round(sec / 60) + " min" : sec < 172800 ? Math.round(sec / 3600) + " h" : Math.round(sec / 86400) + " d";
function feedChip(s, label, port, key) {
  const latest = Math.min(...s.sbcs.map(x => ago(s, x[key])), Infinity);
  const [cls, text] = latest < 60 ? ["ok", "live"] : latest < Infinity ? ["bad", "silent " + agoText(latest)] : ["", "nothing yet"];
  return `<span class="feed-chip" title="${esc(label)} on UDP ${port}"><span class="dot ${cls}"></span><b>${label}</b><span class="mono">:${port} ${text}</span></span>`;
}
function renderTop(s) {
  const who = $("who"); if (who && s.signedInAs) who.textContent = s.signedInAs;
  const av = $("avatar"); if (av) av.textContent = (s.signedInAs || who?.textContent || "?").slice(0, 1).toUpperCase();
  const lis = s.settings?.listening || {};
  const extra = (lis.syslogTcp ? ` TCP ${lis.syslogTcp}` : "") + (lis.syslogTls ? ` TLS ${lis.syslogTls}` : "");
  setHTML($("feeds"), s.settings ? feedChip(s, "Syslog", s.settings.syslogPort + extra, "syslog") + feedChip(s, "Debug Rec", s.settings.drPort, "dr") : "");
  const rec = s.recording;
  let html;
  if (rec) {
    const secs = (new Date(s.now) - new Date(s.startedAt)) / 1000;
    const left = s.stopsAt ? Math.ceil((new Date(s.stopsAt) - new Date(s.now)) / 60000) : null;
    html = `<div class="rec"><span class="pulse"></span>REC ${dur(secs)}${left !== null ? `<small>auto-stop in ${left} min</small>` : ""}<button class="btn danger sm" id="stopBtn">${ic("stop", 12)} Stop</button></div>`;
  } else if (s.opened) html = `<div class="rec log" title="${esc(s.opened)}"><span class="pulse" style="background:var(--info)"></span>FILE<small>${esc(s.opened.length > 40 ? s.opened.slice(0, 40) + "…" : s.opened)}</small></div><button class="btn primary" id="liveBtn">Back to live</button>`;
  else html = `<div class="rec log"><span class="pulse"></span>LOG<small>${esc(s.settings?.logFile || "starting")}</small></div><button class="btn" id="startBtn">${ic("rec", 13)} Packet capture</button>`;
  if (setHTML($("recBox"), html)) {
    const sb = $("startBtn"), st = $("stopBtn");
    if (sb) sb.onclick = async () => { try { await api("/api/start", {}); toast("Capture started"); selected = null; detail = null; } catch (e) { toast(e.message, true); } refresh(); };
    if (st) st.onclick = async () => { try { await api("/api/stop", {}); toast("Capture stopped: the files are under Captures"); } catch (e) { toast(e.message, true); } refresh(); };
    const lb = $("liveBtn");
    if (lb) lb.onclick = async () => { try { const r = await api("/api/files/close", {}); toast(r.message); afterOpen(); } catch (e) { toast(e.message, true); } };
  }
  $("subtitle").textContent = rec ? `Packet capture since ${tm(s.startedAt)} · ${fmtN(s.sipMessages)} SIP messages · ${fmtN(s.rtpPackets)} audio packets`
    : s.opened ? "Opened capture · " + s.opened : `Logging to ${s.settings?.logFolder || "splunk"} · ${fmtN(s.sipMessages)} SIP messages · ${fmtN(s.rtpPackets)} audio packets`;

  const links = s.sbcs.filter(x => x.listed && x.url);
  const html2 = links.length === 1
    ? `<a class="btn" href="${esc(links[0].url)}" target="_blank" rel="noopener">${ic("ext", 15)} Open ${esc(links[0].name)}</a>`
    : links.length ? `<select id="sbcOpen" aria-label="Open an SBC's web page"><option value="">Open SBC…</option>${links.map(x => `<option value="${esc(x.url)}">${esc(x.name)} (${esc(x.ip)})</option>`).join("")}</select>` : "";
  if (setHTML($("sbcLinks"), html2)) {
    const so = $("sbcOpen"); if (so) so.onchange = () => { if (so.value) window.open(so.value, "_blank", "noopener"); so.value = ""; };
  }
}

/* ------------------------------------------------------------ sidebar + alerts */
const ago = (s, x) => x ? (new Date(s.now) - new Date(x.lastSeen)) / 1000 : Infinity;
function renderSide(s) {
  setHTML($("sbcMini"), s.sbcs.length ? s.sbcs.map(x => {
    const a = Math.min(ago(s, x.syslog), ago(s, x.dr));
    const cls = a < 60 ? "ok" : a < Infinity ? "warn" : "";
    const tip = `syslog: ${x.syslog ? fmtN(x.syslog.packets) + " packets, last " + tm(x.syslog.lastSeen) : "none"} · Debug Recording: ${x.dr ? fmtN(x.dr.packets) + " packets, last " + tm(x.dr.lastSeen) : "none"}`;
    return `<div title="${esc(tip)}"><span class="dot ${cls}"></span><span>${esc(x.listed ? x.name : x.ip)}</span></div>`;
  }).join("") : `<div class="muted">No SBCs yet</div>`);
  $("nc-calls").textContent = s.calls || ""; $("nc-streams").textContent = s.streams || "";
  $("nc-syslog").textContent = s.syslogLines ? fmtN(s.syslogLines) : ""; $("nc-sbcs").textContent = s.sbcs.filter(x => x.listed).length || "";
  $("ver").textContent = "v" + (s.version || "");
  $("nc-tests").textContent = s.tester?.running ? "●" : "";
  $("nc-bulk").textContent = s.bulkRunning ? "●" : "";
  const nc = $("nc-changes"); nc.textContent = s.changesPending || ""; nc.style.color = s.changesPending ? "var(--warn)" : "";
}
function renderAlerts(s) {
  const a = [];
  s.listenErrors.forEach(e => a.push(["bad", "alert", esc(e)]));
  if (s.firewall && s.firewall.rule === true && s.firewall.portsOk === false)
    a.push(["warn", "shield", `<b>Windows Firewall allows MediantTrace on UDP ${esc(s.firewall.rulePorts)} only,</b> but it now listens on ${s.settings.listening.syslog} and ${s.settings.listening.dr}, so logs from the SBCs may be blocked. Run <span class="mono">MediantTrace.exe install</span> as administrator, or this in an administrator command prompt:<div style="margin-top:6px">${copyChip(s.firewall.command)}</div>`]);
    if (s.firewall && s.firewall.rule === false)
    a.push(["warn", "shield", `<b>Windows Firewall has no rule for MediantTrace,</b> so logs from the SBCs may be blocked. Run <span class="mono">MediantTrace.exe install</span> as administrator, or this command:<div style="margin-top:6px">${copyChip(s.firewall.command)}</div>`]);
  // An SBC that used to send logs and stopped is the first thing to fix: it leads the page.
  s.sbcs.filter(x => x.listed && (x.syslog || x.dr)).forEach(x => {
    const quiet = Math.min(ago(s, x.syslog), ago(s, x.dr));
    if (quiet < 300) return;
    const last = [x.syslog, x.dr].filter(Boolean).map(f => f.lastSeen).sort().pop();
    a.push(["bad", "alert", `<b>${esc(x.name)} has gone quiet</b>No syslog or Debug Recording from <span class="mono">${esc(x.ip)}</span> since ${esc(dtm(last))}, ${agoText(quiet)} ago. Check the SBC is powered and on the network, then its logging settings. <a href="#" data-go="health">SBC health</a>`]);
  });
  const live = s.sbcs.some(x => ago(s, x.syslog) < 60 || ago(s, x.dr) < 60);
  if (!live && !s.listenErrors.length && !a.some(([c]) => c === "bad"))
    a.push(["info", "info", `<b>Nothing is arriving from any SBC.</b> Point each SBC's syslog and Debug Recording at <b>${esc((s.serverAddresses || []).join(" or "))}</b> (UDP ${s.settings.syslogPort} and ${s.settings.drPort}). <a href="#" data-go="sbcs">How to connect an SBC</a>`]);
  // Notes that ask for no action now stay on the pages they are about, so the banner on other pages means a problem.
  const notes = view === "dashboard" || view === "sbcs";
  const unlisted = notes ? s.sbcs.filter(x => !x.listed) : [];
  if (unlisted.length) a.push(["info", "server", `Logs are arriving from ${unlisted.map(x => `<b>${esc(x.ip)}</b>`).join(", ")}, not in the SBC list. <a href="#" data-go="sbcs">Add it on the SBCs page</a>`]);
  if (notes && s.ignored) a.push(["info", "info", `${fmtN(s.ignored)} packets ignored from addresses not in the SBC list.`]);
  if (s.changesPending) a.push(["warn", "shield", `<b>${s.changesPending} SBC change${s.changesPending > 1 ? "s" : ""} waiting for approval.</b> <a href="#" data-go="changes">Review on the Changes page</a>`]);
  setHTML($("alerts"), a.map(([c, i, h]) => `<div class="alert ${c}"><span class="ic">${ic(i, 18)}</span><div>${h}</div></div>`).join(""));
  problemCount = a.filter(([c]) => c !== "info").length;
  setDocTitle();
}
function fillSbcSelect(el, s) {
  const cur = el.value;
  const html = `<option value="">All SBCs</option>` + s.sbcs.map(x => `<option value="${esc(x.ip)}">${esc(x.listed ? x.name : x.ip + " (unlisted)")}</option>`).join("");
  if (setHTML(el, html)) el.value = cur;
}

/* ------------------------------------------------------------ calls */
const FILTERS = [["all", "All"], ["issues", "With issues"], ["oneway", "One-way audio"], ["failed", "Failed"], ["connected", "Connected"]];
const match = (c, f) => f === "all" || (f === "issues" && c.warnings > 0) || (f === "oneway" && c.oneWay) || (f === "failed" && c.state === "failed") || (f === "connected" && (c.state === "connected" || c.state === "ended"));
$("q").oninput = () => renderCalls();
$("sbcFilter").onchange = () => { selected = null; detail = null; refresh(); };
$("showOptions").onchange = () => refresh();
$("callCsv").onclick = () => {
  const list = visibleCalls();
  if (!list.length) { toast("No calls to export", true); return; }
  const when = s => { if (!s) return ""; const d = new Date(s), p = n => String(n).padStart(2, "0"); return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}:${p(d.getSeconds())}`; };
  const audio = c => (c.audioIn || c.audioOut) ? [c.audioIn, c.audioOut] : [c.sbcIn > 0 ? c.sbcIn : "", c.sbcOut > 0 ? c.sbcOut : ""];
  const rows = ["Started,SBC,From,To,State,SIP status,Talk (s),Codec,MOS,Audio in,Audio out,Issue,Call-ID,Session"];
  list.forEach(c => { const [inn, out] = audio(c); rows.push([when(c.started), sbcName(c.sbc), c.from, c.to, c.state, c.status || "", c.seconds || "", c.codec || "", c.mos ?? "", inn, out, c.topIssue || "", c.callId, c.session || ""].map(csvCell).join(",")); });
  downloadText("calls.csv", rows.join("\r\n") + "\r\n");
};
document.addEventListener("keydown", e => {
  const typing = /INPUT|TEXTAREA|SELECT/.test(document.activeElement?.tagName || "");
  if (e.key === "/" && !typing) { e.preventDefault(); go("calls"); $("q").focus(); return; }
  if (e.key === "Escape" && typing) { document.activeElement.blur(); return; }
  if (view !== "calls" || typing || !["ArrowDown", "ArrowUp", "j", "k"].includes(e.key)) return;
  const list = visibleCalls(); if (!list.length) return;
  e.preventDefault();
  let i = list.findIndex(c => c.id === selected);
  i = (e.key === "ArrowDown" || e.key === "j") ? Math.min(list.length - 1, i + 1) : Math.max(0, i - 1);
  const inList = $("rows").contains(document.activeElement);
  select(list[i].id);
  const row = document.querySelector(`.row[data-id="${list[i].id}"]`);
  row?.scrollIntoView({block:"nearest"});
  if (inList) row?.focus({preventScroll: true});
});
function visibleCalls() {
  const q = $("q").value.trim().toLowerCase();
  return calls.filter(c => match(c, filter) && (!q || [c.from, c.to, c.callId, c.session, sbcName(c.sbc)].some(v => String(v || "").toLowerCase().includes(q))));
}
function stateBadge(c) {
  const m = {connected:["ok", "Connected"], ended:["", "Ended"], failed:["bad", "Failed"], ringing:["accent", "Ringing"], trying:["", "Trying"], cancelled:["", "Cancelled"]}[c.state] || ["", c.state];
  return `<span class="badge ${m[0]}">${m[1]}${c.status && c.state === "failed" ? " " + c.status : ""}</span>`;
}
function renderKpis() {
  const k = [
    ["all", "Calls", calls.length, ""],
    ["connected", "Connected", calls.filter(c => match(c, "connected")).length, "ok"],
    ["failed", "Failed", calls.filter(c => c.state === "failed").length, "bad"],
    ["issues", "With issues", calls.filter(c => c.warnings > 0).length, "warn"],
    ["oneway", "One-way audio", calls.filter(c => c.oneWay).length, "bad"],
  ];
  if (setHTML($("kpis"), k.map(([f, l, v, c]) => `<button class="kpi ${v ? c : ""}" data-f="${f}"><div class="l">${l}</div><div class="v">${fmtN(v)}</div></button>`).join("")))
    $("kpis").querySelectorAll("[data-f]").forEach(b => b.onclick = () => setFilter(b.dataset.f));
  if (setHTML($("fchips"), FILTERS.map(([f, l]) => `<button class="chip ${filter === f ? "on" : ""}" data-f="${f}" role="tab" aria-selected="${filter === f}">${l}<span class="n">${calls.filter(c => match(c, f)).length}</span></button>`).join("")))
    $("fchips").querySelectorAll("[data-f]").forEach(b => b.onclick = () => setFilter(b.dataset.f));
}
function setFilter(f) { filter = f; store.set("filter", f); renderCalls(); }
function renderCalls() {
  renderKpis();
  const list = visibleCalls();
  $("listCount").textContent = `${list.length} of ${calls.length} call${calls.length === 1 ? "" : "s"}`;
  let html;
  if (!calls.length) html = emptyState("phone", status?.recording ? "Waiting for calls" : status?.opened ? "No calls in this capture" : "No capture running",
      status?.recording ? "Calls appear here as soon as an SBC logs SIP for them. Make a test call." : status?.opened ? "This file has SIP messages that did not form a call, or it is empty." : "Press <b>Start capture</b>, or open a saved one from <b>Captures</b>.");
  else if (!list.length) html = emptyState("search", "No calls match", "Try another filter or search.");
  else html = list.map(c => {
    const issue = c.topIssue ? `<div class="issue ${c.severity === "bad" ? "" : "warn"}">${ic(c.severity === "bad" ? "x" : "alert", 14)}<span>${namedText(c.topIssue)}</span></div>` : "";
    const aIn = c.audioIn || c.audioOut ? c.audioIn : c.sbcIn, aOut = c.audioIn || c.audioOut ? c.audioOut : c.sbcOut;
    const audio = (aIn > 0 || aOut > 0) ? `<span class="flow" title="Audio packets received / sent by the SBC${c.audioIn || c.audioOut ? "" : " (from its call record)"}"><span class="arrow-in ${aIn ? "" : "z"}">${ic("in", 12)}${fmtN(aIn)}</span><span class="arrow-out ${aOut ? "" : "z"}">${ic("out", 12)}${fmtN(aOut)}</span></span>` : "";
    return `<div class="row ${c.id === selected ? "sel" : ""}" data-id="${c.id}" role="button" tabindex="${c.id === selected ? 0 : -1}">
      <div class="sev ${c.severity}"></div>
      <div style="min-width:0">
        <div class="who"><span>${esc(c.from || "unknown")}</span><span class="muted">→</span><span>${esc(c.to || "unknown")}</span></div>
        <div class="meta">${stateBadge(c)}${mosBadge(c.mos)}<span>${esc(sbcName(c.sbc))}</span>${c.seconds ? `<span>${dur(c.seconds)}</span>` : ""}${c.codec ? `<span>${esc(c.codec)}</span>` : ""}</div>
        ${issue}
      </div>
      <div class="side-r"><span>${tm(c.started)}</span>${audio}</div>
    </div>`;
  }).join("");
  // One row is in the Tab order (the selected one); keep the focus on it when the list is redrawn.
  const hadFocus = $("rows").contains(document.activeElement);
  if (setHTML($("rows"), html)) {
    $("rows").querySelectorAll(".row").forEach(r => {
      r.onclick = () => select(+r.dataset.id);
      r.onkeydown = e => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); select(+r.dataset.id); } };
    });
    if (hadFocus) $("rows").querySelector(".row.sel")?.focus({preventScroll: true});
  }
  if (selected === null && list.length) select(list[0].id, true);
}
async function playStream(id, btn) {
  btn.disabled = true;
  try {
    const q = new URLSearchParams({ssrc: btn.dataset.audio, src: btn.dataset.src, dst: btn.dataset.dst});
    const r = await fetch("/api/calls/" + id + "/audio?" + q);
    if (!r.ok) { toast(await r.text(), true); return; }
    const url = URL.createObjectURL(await r.blob());
    const audio = $("playAudio");
    if (audio.src.startsWith("blob:")) URL.revokeObjectURL(audio.src);
    audio.src = url;
    $("playLabel").textContent = (btn.dataset.src || "") + " → " + (btn.dataset.dst || "");
    $("player").style.display = "";
    try { await audio.play(); } catch { /* the controls are there if the browser held the sound */ }
  } catch (e) { toast(e.message, true); }
  finally { btn.disabled = false; }
}
function stopPlayer() {
  const audio = $("playAudio");
  if (audio) { audio.pause(); if (audio.src.startsWith("blob:")) URL.revokeObjectURL(audio.src); audio.removeAttribute("src"); }
  const box = $("player"); if (box) box.style.display = "none";
}
function select(id, quiet) {
  if (selected === id) return;
  stopPlayer();
  selected = id; openMsg = null; legDetails = {}; extraLegs = new Set();
  if (!quiet) history.replaceState(null, "", "#call=" + id);
  document.querySelectorAll(".row").forEach(r => { const on = +r.dataset.id === id; r.classList.toggle("sel", on); r.tabIndex = on ? 0 : -1; });
  loadDetail();
}

/* ------------------------------------------------------------ call detail */
async function loadDetail() {
  if (selected === null) { setHTML($("detail"), emptyState("phone", "Select a call", "Pick a call on the left to see its SIP flow, checks and audio.")); return; }
  const want = selected;
  let d;
  try { d = await api("/api/calls/" + want); }
  catch { if (want === selected) { detail = null; setHTML($("detail"), emptyState("phone", "Call not found", "It may belong to an earlier capture.")); } return; }
  const legs = {};
  if (bothLegs) for (const leg of d.otherLegs) { try { legs[leg.id] = await api("/api/calls/" + leg.id); } catch {} }
  // Other calls added to this flow by hand (Syslog Viewer's SIP flow of several sessions).
  for (const id of extraLegs) if (id !== d.id && !legs[id]) { try { legs[id] = await api("/api/calls/" + id); } catch {} }
  if (want !== selected) return; // the user moved on while this was loading
  if (dtab === "sbclog" && d.session) {
    try { d.sbcLog = await api(`/api/syslog?limit=3000&sbc=${encodeURIComponent(d.sbc)}&q=${encodeURIComponent("SID=" + d.session)}`); } catch { d.sbcLog = []; }
    if (want !== selected) return;
  }
  detail = d; legDetails = legs;
  renderDetail();
}
function verdict(d) {
  const bad = d.checks.filter(c => c.level === "bad"), warn = d.checks.filter(c => c.level === "warn");
  const findings = bad.length + warn.length;
  const more = n => n > 0 ? `${n} more finding${n > 1 ? "s" : ""} below` : "";
  if (bad.length) return ["bad", "x", bad[0].text, more(findings - 1)];
  if (warn.length) return ["warn", "alert", warn[0].text, more(findings - 1)];
  if (d.state === "trying" || d.state === "ringing") return ["ok", "info", "Call in progress: nothing wrong so far.", ""];
  return ["ok", "check", "No problems found in the SIP, SDP or audio.", ""];
}
async function downloadCallPcap(id) {
  try {
    const r = await fetch("/api/calls/" + id + "/pcap");
    if (!r.ok) { toast((await r.text()) || "No packets for this call", true); return; }
    const name = /filename="([^"]+)"/.exec(r.headers.get("content-disposition") || "")?.[1] || ("call-" + id + ".pcap");
    const a = document.createElement("a");
    a.href = URL.createObjectURL(await r.blob());
    a.download = name; a.click();
    setTimeout(() => URL.revokeObjectURL(a.href), 2000);
  } catch (e) { toast(e.message, true); }
}
function renderDetail() {
  const d = detail; if (!d) return;
  const [vc, vi, vt, vs] = verdict(d);
  const setup = d.answered ? Math.round(new Date(d.answered) - new Date(d.started)) : null;
  const flowCount = d.messages.length + (d.dtmf || []).length + Object.values(legDetails).reduce((n, l) => n + l.messages.length + (l.dtmf || []).length, 0);
  const tabs = [["overview", "Overview"], ["flow", `Call flow · ${flowCount}`], ["media", `Media · ${d.streams.length || (d.sbcMedia || []).length}`], ["sdp", `SDP · ${d.sdps.length}`], ["cdr", `SBC report · ${d.cdrs.length}`], ["sbclog", "SBC log"], ["messages", "Messages"]];
  const head = `<div class="d-head">
      <div class="d-title"><h2>${esc(d.from || "unknown")} <span class="muted">→</span> ${esc(d.to || "unknown")}</h2>${stateBadge(d)}
        <div class="d-actions">
          <button class="btn sm" data-copy="${esc(location.origin + location.pathname + "#call=" + d.id)}" title="Copy a link to this call">${ic("link", 14)} Link</button>
          <a class="btn sm" href="/api/calls/${d.id}/text" title="Download this call's SIP messages">${ic("down", 14)} .txt</a>
          ${status?.pcap ? `<button class="btn sm" id="pcapBtn" title="Download the packets in this capture that belong to this call">${ic("down", 14)} .pcap</button>` : ""}
          <button class="btn sm primary" id="reportBtn" title="A report to send to a carrier or AudioCodes">${ic("report", 14)} Report</button>
        </div></div>
      <div class="d-sub"><span>${ic("server", 13)} ${esc(sbcName(d.sbc))}</span><span>Started ${dtm(d.started)}</span>
        <span class="mono">${esc(d.callId)} <button class="btn ghost sm icon" style="height:22px;width:22px" data-copy="${esc(d.callId)}" title="Copy Call-ID" aria-label="Copy Call-ID">${ic("copy", 12)}</button></span>
        ${d.session ? `<span class="mono">SID ${esc(d.session)}</span>` : ""}</div>
    </div>
    <div class="verdict ${vc}">${ic(vi, 20)}<div><span>${esc(vt)}</span>${vs ? `<div class="muted" style="font-weight:500;font-size:12.5px">${vs}</div>` : ""}</div></div>
    <div class="tabs" role="tablist">${tabs.map(([k, l]) => `<button class="${dtab === k ? "on" : ""}" data-t="${k}" role="tab" aria-selected="${dtab === k}">${l}</button>`).join("")}</div>`;
  const body = dtab === "overview" ? overview(d, setup) : dtab === "flow" ? flowTab(d) : dtab === "media" ? mediaTab(d) : dtab === "sdp" ? sdpTab(d)
    : dtab === "cdr" ? cdrTab(d) : dtab === "sbclog" ? sbcLogTab(d) : messagesTab(d);
  const el = $("detail");
  const w0 = el.querySelector(".ladder-wrap"), keep = w0 ? [w0.scrollTop, w0.scrollLeft] : null;
  const opened = [...el.querySelectorAll("details.m[open]")].map(x => x.dataset.i);
  if (setHTML(el, head + `<div class="tab-b">${body}</div>`)) {
    el.querySelectorAll("[data-t]").forEach(b => b.onclick = () => { dtab = b.dataset.t; if (dtab === "sbclog") loadDetail(); else renderDetail(); });
    el.querySelectorAll(".ladder .msg").forEach(g => g.onclick = () => { openMsg = openMsg === g.dataset.k ? null : g.dataset.k; renderDetail(); });
    const lg = el.querySelector("#bothLegs"); if (lg) lg.onchange = () => { bothLegs = lg.checked; loadDetail(); };
    const al = el.querySelector("#addLeg"); if (al) al.onchange = () => { if (al.value) { extraLegs.add(+al.value); loadDetail(); } };
    const cg = el.querySelector("#clearLegs"); if (cg) cg.onclick = () => { extraLegs = new Set(); legDetails = {}; loadDetail(); };
    const ft = el.querySelector("#flowTxt"); if (ft) ft.onclick = () => downloadText(`sip-flow-${d.id}.txt`, flowText(), "text/plain");
    const fh = el.querySelector("#flowHtml"); if (fh) fh.onclick = () => downloadText(`sip-flow-${d.id}.html`, flowHtml(), "text/html");
    const rb = el.querySelector("#reportBtn"); if (rb) rb.onclick = () => callReport(detail);
    const pb = el.querySelector("#pcapBtn"); if (pb) pb.onclick = () => downloadCallPcap(d.id);
    el.querySelectorAll("[data-audio]").forEach(b => b.onclick = () => playStream(d.id, b));
    el.querySelectorAll("[data-leg]").forEach(a => a.onclick = e => { e.preventDefault(); select(+a.dataset.leg); });
    opened.forEach(i => { const x = el.querySelector(`details.m[data-i="${i}"]`); if (x) x.open = true; });
    const w = el.querySelector(".ladder-wrap"); if (w && keep) { w.scrollTop = keep[0]; w.scrollLeft = keep[1]; }
  }
}
function overview(d, setup) {
  const codec = (d.sdps.filter(s => !s.ownerIsMediant).pop()?.codecs || [])[0];
  const facts = [
    ["Result", d.status ? `${d.status} · ${d.state}` : d.state], ["Call setup", setup !== null ? (setup / 1000).toFixed(2) + " s" : "—"],
    ["Talk time", d.seconds ? dur(d.seconds) : "—"], ["Codec", codec || "—"],
    ["SBC", sbcName(d.sbc)], ["Answered", d.answered ? tm(d.answered) : "—"], ["Ended", d.ended ? tm(d.ended) : "—"],
  ];
  if (d.signalling === "TLS") facts.push(["Signalling", "TLS"]);
  if (d.mediaProtection) facts.push(["Audio", d.mediaProtection]);
  facts.push(
    ["Legs", String(1 + d.otherLegs.length)],
    [d.mosSource === "sbc" ? "MOS" : "Estimated MOS", d.mos != null ? `${d.mos.toFixed(2)} · ${mosGrade(d.mos)}${d.mosSource === "sbc" ? " · SBC" : d.mosSource === "counters" ? " · SBC counters" : ""}` : "—"],
  );
  if ((d.dtmf || []).length) facts.push(["Digits", d.dtmf.map(t => t.digit).join(" ")]);
  const checkIcon = c => c.level === "bad" ? "x" : c.level === "note" ? "shield" : "alert";
  const checks = d.checks.length ? d.checks.map(c => `<div class="check ${c.level}"><span class="ic">${ic(checkIcon(c), 17)}</span><div>${namedText(c.text)}</div></div>`).join("")
    : `<div class="check ok"><span class="ic">${ic("check", 17)}</span><div>No problems found.</div></div>`;
  const legs = d.otherLegs.length ? `<h3>Other legs of this call</h3><div class="checks">${d.otherLegs.map(o => `<div class="check"><span class="ic muted">${ic("phone", 16)}</span><div><a href="#" data-leg="${o.id}">${esc(o.from)} → ${esc(o.to)}</a> <span class="muted">· ${esc(o.state)}</span></div></div>`).join("")}</div>` : "";
  return `<div class="facts">${facts.map(([l, v]) => `<div><div class="l">${l}</div><div class="v" title="${esc(v)}">${esc(v)}</div></div>`).join("")}</div>
    <h3>Checks</h3><div class="checks">${checks}</div>${mediaPaths(d)}${legs}`;
}
function mediaPaths(d) {
  if (!d.media.length && (d.sbcMedia || []).length) return sbcMediaPaths(d);
  if (!d.media.length) return `<h3>Audio</h3><p class="muted" style="margin:0">${d.streams.length ? "Audio packets are present, but no far-end SDP has been seen yet." : "No audio measured yet. Debug Recording with <b>Signaling &amp; Media</b> shows it live. Without that, the SBC's MEDIA_END record in syslog scores the call when it ends."}</p>`;
  return `<h3>Audio by far end</h3><div class="paths">${d.media.map(m => {
    const max = Math.max(m.toPeer, m.fromPeer, 1);
    const dir = (label, v, rx) => `<div class="dir"><span class="t2">${label}</span><div class="bar"><i class="${v ? (rx ? "rx" : "") : "zero"}" style="width:${Math.max(2, v / max * 100)}%"></i></div><span class="n ${v ? "" : "zero"}">${fmtN(v)}</span></div>`;
    return `<div class="path"><div class="path-h"><span class="mono">${esc(m.sdpAddress)}</span><span class="badge">a=${esc(m.direction)}</span></div>
      ${dir("SBC → far end", m.toPeer)}${dir("Far end → SBC", m.fromPeer, true)}
      <div class="path-f"><span>MOS SBC → far end ${mosBadge(m.mosTo)}</span><span>MOS far end → SBC ${mosBadge(m.mosFrom)}</span></div>
      <div class="path-f"><span>Lost ${fmtN(m.lost)}</span><span>Jitter ${m.jitterMs} ms</span><span>Longest gap ${fmtN(m.maxGapMs)} ms</span>${m.actualFrom ? `<span>Actually from ${esc(m.actualFrom)}</span>` : ""}</div></div>`;
  }).join("")}</div>`;
}
function sbcMediaPaths(d) {
  const rows = d.sbcMedia || [];
  const note = rows.every(m => m.mosFromSbc)
    ? "The SBC scored this audio itself, so Debug Recording was not needed."
    : rows.some(m => m.mosFromSbc)
      ? "Where the SBC sent a MOS, that score is shown. The rest is estimated from its packet counts."
      : "Estimated from the packet counts in the SBC's call record, so Debug Recording was not needed.";
  const known = v => v != null && v >= 0;
  const card = m => {
    const ip = m.remoteIp || "";
    const named = ip && peerName(ip);
    const addr = ip ? `${named ? `<b>${esc(named)}</b> ` : ""}<span class="mono">${esc(ip)}${m.remotePort && m.remotePort !== "0" ? ":" + esc(m.remotePort) : ""}</span>` : "the far end";
    const nums = [m.outPackets, m.inPackets].filter(known);
    const max = Math.max(1, ...nums, 1);
    const dir = (label, v, rx) => `<div class="dir"><span class="t2">${label}</span><div class="bar"><i class="${known(v) && v ? (rx ? "rx" : "") : "zero"}" style="width:${known(v) ? Math.max(2, v / max * 100) : 2}%"></i></div><span class="n ${known(v) && v ? "" : "zero"}">${known(v) ? fmtN(v) : "—"}</span></div>`;
    const latched = m.latchedIp && m.latchedIp !== "0.0.0.0" && m.latchedIp !== ip
      ? `<div class="path-f"><span>Latched ${esc(peerName(m.latchedIp) || m.latchedIp)}${m.latchedPort && m.latchedPort !== "0" ? ":" + esc(m.latchedPort) : ""}</span></div>` : "";
    const pct = v => v == null ? "—" : v + "%";
    return `<div class="path"><div class="path-h"><span>${addr}</span>${m.codec ? `<span class="badge">${esc(m.codec)}</span>` : ""}</div>
      ${dir("SBC → far end", m.outPackets)}${dir("Far end → SBC", m.inPackets, true)}
      <div class="path-f"><span>MOS SBC → far end ${mosBadge(m.mosOut)}</span><span>MOS far end → SBC ${mosBadge(m.mosIn)}</span></div>
      <div class="path-f"><span>Loss out ${pct(m.outLoss)}</span><span>Loss in ${pct(m.inLoss)}</span><span>Jitter ${m.jitterMs == null ? "—" : m.jitterMs + " ms"}</span>${m.delayMs != null ? `<span>Delay ${m.delayMs} ms</span>` : ""}</div>
      ${latched}</div>`;
  };
  return `<h3>Audio measured by the SBC</h3><p class="muted" style="margin:0 0 8px">${note}</p><div class="paths">${rows.map(card).join("")}</div>`;
}

/* Call flow: the first leg's far end | the SBC | the other legs' far ends */
function flowTab(d) {
  const legs = [{leg: 0, d}].concat(Object.values(legDetails).filter(l => bothLegs || extraLegs.has(l.id)).map((l, i) => ({leg: i + 1, d: l})));
  const msgs = [];
  legs.forEach(({leg, d: ld}) => {
    ld.messages.forEach(m => msgs.push({...m, leg, key: leg + ":" + m.index}));
    (ld.dtmf || []).forEach((t, i) => msgs.push({...t, leg, dtmf: true, first: "DTMF " + t.digit, key: "dtmf:" + leg + ":" + i}));
  });
  msgs.sort((a, b) => new Date(a.time) - new Date(b.time) || (a.dtmf ? 1 : 0) - (b.dtmf ? 1 : 0));
  // The SBC's addresses: where inbound messages arrive, and where outbound ones leave from.
  const sbcHosts = new Set();
  msgs.forEach(m => { if (m.inbound === true && m.dst) sbcHosts.add(host(m.dst)); if (m.inbound === false && m.src) sbcHosts.add(host(m.src)); });
  const SBC = "§sbc";
  const side = (ep, isSbc) => (isSbc || !ep || sbcHosts.has(host(ep))) ? SBC : host(ep);
  const left = [], right = [];
  msgs.forEach(m => [side(m.src, m.inbound === false), side(m.dst, m.inbound === true)].forEach(p => {
    if (p !== SBC && !left.includes(p) && !right.includes(p)) (m.leg === 0 ? left : right).push(p);
  }));
  if (!left.length && !right.length) left.push("far end");
  const cols = [...left, SBC, ...right];
  // Fit the columns to the panel; scroll sideways only when there are too many to fit.
  const avail = Math.max(600, ($("detail").clientWidth || 900) - 44);
  const pad = 112, H = 36, top = 66;
  const W = Math.max(170, Math.min(260, Math.floor((avail - pad) / cols.length)));
  const width = pad + cols.length * W, height = top + msgs.length * H + 14;
  const cx = i => pad + i * W + W / 2;
  let svg = `<svg width="${width}" height="${height}" role="img" aria-label="SIP call flow">`;
  cols.forEach((c, i) => {
    const isS = c === SBC;
    const named = peerName(c);
    const t1 = isS ? sbcName(d.sbc) : named || (left.includes(c) ? (right.length ? "Far end · leg A" : "Far end") : "Far end · leg B");
    const t2 = isS ? ([...sbcHosts].join(", ") || "SBC") : c;
    svg += `<line class="life" x1="${cx(i)}" y1="54" x2="${cx(i)}" y2="${height}"/>
      <g class="colh"><rect x="${cx(i) - W / 2 + 14}" y="8" width="${W - 28}" height="44" rx="8" ${isS ? 'style="stroke:var(--accent);stroke-width:1.5"' : ""}/>
      <text class="t1" x="${cx(i)}" y="26" text-anchor="middle">${esc(t1)}</text><text class="t2" x="${cx(i)}" y="43" text-anchor="middle">${esc(t2)}</text></g>`;
  });
  let prev = null;
  msgs.forEach((m, i) => {
    const y = top + i * H + 18;
    const a = cols.indexOf(side(m.src, m.inbound === false));
    let b = cols.indexOf(side(m.dst, m.inbound === true));
    if (a === b) b = a === cols.indexOf(SBC) ? (m.leg === 0 ? 0 : cols.length - 1) : cols.indexOf(SBC);
    const x1 = cx(a), x2 = cx(b), dir = x2 >= x1 ? 1 : -1;
    const other = m.kind && m.kind !== "SIP";
    const color = m.dtmf ? "var(--accent)" : other ? "var(--info)" : /^SIP\/2\.0 [3-6]/.test(m.first) ? "var(--fail)" : /^SIP\/2\.0 2/.test(m.first) ? "var(--succ)" : /^SIP\/2\.0 1/.test(m.first) ? "var(--prov)" : "var(--req)";
    const label = (m.dtmf ? `DTMF ${m.digit}${m.durationMs ? " · " + m.durationMs + " ms" : ""}` : m.first.replace(/^SIP\/2\.0 /, "").replace(/ SIP\/2\.0$/, "").replace(/sips?:([^@ ;>]*)@\S*/, "$1").slice(0, 30)) + (!m.dtmf && m.transport === "TLS" ? " · TLS" : "");
    const delta = prev ? new Date(m.time) - new Date(prev) : null; prev = m.time;
    svg += `<g class="msg ${openMsg === m.key ? "sel" : ""}" data-k="${m.key}">
      <rect class="hit" x="0" y="${y - 21}" width="${width}" height="${H}"/>
      <text class="time" x="12" y="${y - 2}">${tms(m.time)}</text>
      ${delta !== null ? `<text class="delta" x="12" y="${y + 12}">+${delta < 1000 ? delta + " ms" : (delta / 1000).toFixed(2) + " s"}</text>` : ""}
      <line x1="${x1}" y1="${y}" x2="${x2 - dir * 8}" y2="${y}" stroke="${color}" stroke-width="1.8"${other ? ' stroke-dasharray="5 4"' : ""}/>
      <path d="M${x2 - dir * 11},${y - 5} L${x2},${y} L${x2 - dir * 11},${y + 5} z" fill="${color}"/>
      <text x="${(x1 + x2) / 2}" y="${y - 6}" text-anchor="middle" style="fill:${color};font-weight:600">${esc(label)}</text>
      ${m.hasSdp ? `<text class="sdp" x="${(x1 + x2) / 2}" y="${y + 13}" text-anchor="middle">SDP</text>` : ""}
    </g>`;
  });
  svg += "</svg>";
  lastFlow = {d, msgs, cols, SBC, side, left, right, sbcHosts, svg};
  const legToggle = (d.otherLegs.length ? `<label class="toggle"><input type="checkbox" id="bothLegs" ${bothLegs ? "checked" : ""}> Show both legs through the SBC</label>`
    : `<span class="muted">One leg: no other leg with the same SID.</span>`)
    + ` <select id="addLeg" aria-label="Add another call to this flow"><option value="">Add a call to this flow…</option>${calls.filter(c => c.id !== d.id && !d.otherLegs.some(o => o.id === c.id) && !extraLegs.has(c.id)).slice(0, 300).map(c => `<option value="${c.id}">${esc(tm(c.started))} ${esc(c.from || "?")} → ${esc(c.to || "?")}</option>`).join("")}</select>`
    + (extraLegs.size ? ` <button class="btn sm ghost" id="clearLegs">Remove added calls (${extraLegs.size})</button>` : "")
    + ` <button class="btn sm" id="flowTxt" title="The ladder as text">${ic("down", 13)} .txt</button> <button class="btn sm" id="flowHtml" title="The ladder and messages as a web page">${ic("down", 13)} .html</button>`;
  const sel = openMsg ? msgs.find(m => m.key === openMsg) : null;
  const panel = sel?.dtmf ? `<div class="msg-panel"><div class="bar"><b>DTMF ${esc(sel.digit)}</b><span class="muted">${tms(sel.time)} · held ${sel.durationMs} ms · ${esc(whoEp(sel.src))} → ${esc(whoEp(sel.dst))}${sel.ended ? "" : " · still held"}</span></div></div>`
    : sel ? `<div class="msg-panel"><div class="bar"><b>${esc(sel.first)}</b><span class="muted">${tms(sel.time)} · ${esc(whoEp(sel.src))} → ${esc(whoEp(sel.dst))} · via ${sel.source === "DR" ? "Debug Recording" : "syslog"}</span><span class="grow"></span><button class="btn sm" data-copy="${esc(sel.text)}">${ic("copy", 13)} Copy</button></div>${sipHtml(sel.text)}</div>`
    : `<p class="muted" style="margin:10px 0 0">Click a message to read it in full.</p>`;
  return `<div class="ladder-tools">${legToggle}<span class="grow"></span><span class="badge accent">Request</span><span class="badge">1xx</span><span class="badge ok">2xx</span><span class="badge bad">3xx–6xx</span><span class="badge">DTMF</span>${msgs.some(m => m.kind && m.kind !== "SIP") ? `<span class="badge" style="color:var(--info)">HTTP / DNS</span>` : ""}${msgs.some(m => m.transport === "TLS") ? `<span class="badge">TLS</span>` : ""}</div>
    <div class="ladder-wrap ladder">${svg}</div>${panel}`;
}
// The ladder as plain text (Syslog Viewer's "Export flow diagram as TXT").
function flowText() {
  const f = lastFlow; if (!f) return "";
  const W = 26, T = 14;
  const name = c => c === f.SBC ? sbcName(f.d.sbc) : (peerName(c) || c);
  const center = i => T + i * W + Math.floor(W / 2);
  const width = T + f.cols.length * W;
  const blank = () => { const a = Array(width).fill(" "); f.cols.forEach((_, i) => a[center(i)] = "|"); return a; };
  const put = (a, at, text) => { for (let k = 0; k < text.length && at + k < width; k++) if (at + k >= 0) a[at + k] = text[k]; };
  const out = [`SIP flow: ${f.d.from || "?"} -> ${f.d.to || "?"}   SBC ${sbcName(f.d.sbc)}   Call-ID ${f.d.callId}${f.d.session ? "   SID " + f.d.session : ""}`, ""];
  const head = Array(width).fill(" ");
  f.cols.forEach((c, i) => { const t = name(c).slice(0, W - 2); put(head, center(i) - Math.floor(t.length / 2), t); });
  out.push(head.join("").trimEnd());
  const head2 = Array(width).fill(" ");
  f.cols.forEach((c, i) => { const t = (c === f.SBC ? [...f.sbcHosts].join(",") : c).slice(0, W - 2); put(head2, center(i) - Math.floor(t.length / 2), t); });
  out.push(head2.join("").trimEnd());
  f.msgs.forEach(m => {
    let a = f.cols.indexOf(f.side(m.src, m.inbound === false)), b = f.cols.indexOf(f.side(m.dst, m.inbound === true));
    if (a === b) b = a === f.cols.indexOf(f.SBC) ? (m.leg === 0 ? 0 : f.cols.length - 1) : f.cols.indexOf(f.SBC);
    const line = blank();
    put(line, 0, tms(m.time));
    const x1 = center(a), x2 = center(b), lo = Math.min(x1, x2), hi = Math.max(x1, x2);
    for (let x = lo + 1; x < hi; x++) line[x] = (m.kind && m.kind !== "SIP") ? "." : "-";
    if (x2 > x1) line[hi - 1] = ">"; else line[lo + 1] = "<";
    const label = (m.dtmf ? `DTMF ${m.digit}` : m.first.replace(/^SIP\/2\.0 /, "").replace(/ SIP\/2\.0$/, "").replace(/sips?:([^@ ;>]*)@\S*/, "$1")) + (m.hasSdp ? " (SDP)" : "");
    const room = hi - lo - 3, t = label.slice(0, Math.max(4, room));
    const lab = Array(width).fill(" "); f.cols.forEach((_, i) => lab[center(i)] = "|");
    put(lab, lo + 1 + Math.max(0, Math.floor((hi - lo - t.length) / 2)), t);
    out.push(lab.join("").trimEnd(), line.join("").trimEnd());
  });
  out.push("", "Messages", "");
  f.msgs.filter(m => !m.dtmf).forEach(m => out.push(`===== ${tms(m.time)}  ${m.src || "SBC"} -> ${m.dst || "SBC"}${m.kind && m.kind !== "SIP" ? "  (" + m.kind + ")" : ""}`, m.text.replace(/\r\n/g, "\n"), ""));
  return out.join("\r\n").replace(/\n/g, "\r\n").replace(/\r\r\n/g, "\r\n");
}
// The ladder and every message as one web page (Syslog Viewer's "Export flow diagram as HTML").
function flowHtml() {
  const f = lastFlow; if (!f) return "";
  const css = document.querySelector("style").textContent;
  const title = `${f.d.from || "?"} → ${f.d.to || "?"} · SIP flow`;
  return `<!doctype html><html lang="en" data-theme="light"><head><meta charset="utf-8"><title>${esc(title)}</title><style>${css}
    body{padding:24px;background:var(--surface)} .ladder-wrap{max-height:none}</style></head><body>
    <h1 style="margin:0 0 4px">${esc(title)}</h1><p class="muted">${esc(sbcName(f.d.sbc))} · Call-ID <span class="mono">${esc(f.d.callId)}</span>${f.d.session ? ` · SID <span class="mono">${esc(f.d.session)}</span>` : ""} · ${esc(dtm(f.d.started))}</p>
    <div class="ladder-wrap ladder">${f.svg}</div><h2>Messages</h2>
    ${f.msgs.filter(m => !m.dtmf).map(m => `<details class="m" open><summary><b>${esc(m.first)}</b><span class="muted">${tms(m.time)} · ${esc(m.src || "SBC")} → ${esc(m.dst || "SBC")}</span></summary>${sipHtml(m.text)}</details>`).join("")}
    <p class="muted">Exported from MediantTrace ${esc(status?.version || "")}.</p></body></html>`;
}
function sipHtml(text) {
  const lines = String(text).replace(/\r\n/g, "\n").split("\n");
  let body = false;
  const out = lines.map((l, i) => {
    if (i === 0) { const cls = /^SIP\/2\.0 [3-6]/.test(l) ? "fail" : /^SIP\/2\.0 2/.test(l) ? "succ" : ""; return `<span class="s1 ${cls}">${esc(l)}</span>`; }
    if (!body && l.trim() === "") { body = true; return ""; }
    if (body) { const m = /^([a-z])=(.*)$/.exec(l); return m ? `<span class="sdpk">${m[1]}=</span><span class="sdp">${esc(m[2])}</span>` : esc(l); }
    const c = l.indexOf(":"); return c > 0 ? `<span class="hn">${esc(l.slice(0, c))}</span>:${esc(l.slice(c + 1))}` : esc(l);
  });
  return `<pre class="sip">${out.join("\n")}</pre>`;
}

/* Media: per-second timeline of every stream on one time axis */
function mediaTab(d) {
  let html = mediaPaths(d);
  if (!d.streams.length) return html;
  const start = Math.min(...d.streams.map(s => s.startSecond)), end = Math.max(...d.streams.map(s => s.startSecond + s.perSecond.length));
  const span = Math.max(1, end - start);
  const peak = Math.max(10, ...d.streams.flatMap(s => s.perSecond));
  const rows = d.streams.map(s => {
    const off = s.startSecond - start;
    const fill = s.inbound === false ? "var(--s1)" : "var(--s2)";
    const bars = s.perSecond.map((v, i) => { if (!v) return ""; const h = Math.max(2, v / peak * 28); return `<rect x="${((off + i) / span * 1000).toFixed(1)}" y="${(30 - h).toFixed(1)}" width="${Math.max(1, 1000 / span - .6).toFixed(2)}" height="${h.toFixed(1)}" fill="${fill}"/>`; }).join("");
    const who = a => { const n = peerName(a); return n ? `${n} (${a})` : a; };
    const label = `${s.inbound === false ? "SBC →" : s.inbound === true ? "→ SBC" : ""} ${who(s.src)} → ${who(s.dst)}`;
    return `<div class="tl-row"><div class="lbl" title="${esc(label)}">${esc(label)}</div><svg viewBox="0 0 1000 30" preserveAspectRatio="none" role="img" aria-label="Audio packets per second">${bars}</svg><div class="r num">${fmtN(s.packets)}</div></div>`;
  }).join("");
  return html + `<h3>Audio timeline</h3>
    <div class="legend"><span><i class="box" style="background:var(--s1)"></i>Sent by the SBC</span><span><i class="box" style="background:var(--s2)"></i>Received by the SBC</span><span class="muted">Packets per second. A gap means audio stopped in that direction.</span></div>
    <div class="timeline"><div class="tl-axis"><div></div><div><span>${tm(start * 1000)}</span><span>${dur(span)}</span><span>${tm(end * 1000)}</span></div><div></div></div>${rows}</div>
    ${qualityCharts(d, start, span)}
    <h3>Streams</h3><p class="muted" style="margin:0 0 8px">${d.streams.some(s => s.encrypted) ? "Encrypted audio cannot be played. Loss and jitter still come from the RTP header." : status?.pcap ? "Play listens to G.711 audio from the capture. A missing packet is silence." : "This capture has no .pcap, so the audio cannot be played."}</p><div class="card scroll">${streamTable(d.streams.map(s => ({stream: s})), false, true)}</div>`;
}
function streamTable(items, withSbc, play) {
  const canPlay = s => play && status?.pcap && !s.encrypted && (s.payloadType === 0 || s.payloadType === 8);
  return `<table class="tbl"><thead><tr>${withSbc ? "<th>SBC</th>" : ""}${play ? "<th></th>" : ""}<th>From</th><th>To</th><th>SSRC</th><th class="r">PT</th><th class="r">Packets</th><th class="r">Lost</th><th class="r">Loss</th><th class="r">Jitter</th><th class="r">Longest gap</th><th>MOS</th><th>First</th><th>Last</th>${withSbc ? "<th>Call</th>" : ""}</tr></thead><tbody>` +
    items.map(x => { const s = x.stream; const lc = s.lossPercent > 2 ? "bad" : s.lossPercent > 0.5 ? "warn" : "";
      return `<tr>${withSbc ? `<td>${esc(sbcName(x.sbc))}</td>` : ""}${play ? `<td>${canPlay(s) ? `<button class="btn sm" data-audio="${esc(s.ssrc)}" data-src="${esc(s.src)}" data-dst="${esc(s.dst)}" title="Play this direction">Play</button>` : s.encrypted ? `<span class="badge" title="The audio is encrypted, so it cannot be played">Encrypted</span>` : ""}</td>` : ""}<td>${addrCell(s.src)}</td><td>${addrCell(s.dst)}</td><td class="mono">${esc(s.ssrc)}</td><td class="r">${s.payloadType}</td><td class="r">${fmtN(s.packets)}</td><td class="r">${fmtN(s.lost)}</td><td class="r">${lc ? `<span class="badge ${lc}">${s.lossPercent}%</span>` : s.lossPercent + "%"}</td><td class="r">${s.jitterMs != null ? s.jitterMs + " ms" : "—"}</td><td class="r">${fmtN(s.maxGapMs)} ms</td><td>${mosBadge(s.mos)}</td><td>${tm(s.first)}</td><td>${tm(s.last)}</td>${withSbc ? `<td>${x.matched ? '<span class="badge ok">matched</span>' : '<span class="badge">no call</span>'}</td>` : ""}</tr>`; }).join("") +
    "</tbody></table>";
}
function sdpTab(d) {
  if (!d.sdps.length) return emptyState("info", "No SDP in this call", "None of the SIP messages so far carries a session description.");
  const pairs = [];
  for (let i = 0; i < d.sdps.length; i++) {
    const o = d.sdps[i], a = d.sdps[i + 1];
    if (a && a.ownerIsMediant !== o.ownerIsMediant) { pairs.push([o, a]); i++; } else pairs.push([o, null]);
  }
  const card = (s, other) => {
    if (!s) return `<div class="sdp-card muted">No answer seen.</div>`;
    const common = other ? s.codecs.filter(c => other.codecs.some(x => x.split("/")[0].toUpperCase() === c.split("/")[0].toUpperCase())) : [];
    const from = s.ownerIsMediant ? "the SBC" : (peerName(s.owner) || s.owner || "the far end");
    const mediaName = peerName(s.ip);
    return `<div class="sdp-card"><div class="h"><span>${esc(s.label)} <span class="muted" style="font-weight:500">from ${esc(from)}</span></span><span class="muted" style="font-weight:500">${tms(s.time)}</span></div>
      <div class="kv"><span class="k">Media address</span><span>${mediaName ? `<b>${esc(mediaName)}</b> ` : ""}<span class="mono">${esc(s.ip)}:${s.port}</span></span>
      <span class="k">Direction</span><span>${s.direction === "sendrecv" ? "sendrecv" : `<span class="badge warn">${esc(s.direction)}</span>`}</span>
      ${s.protection ? `<span class="k">Protection</span><span><span class="badge">${esc(s.protection)}</span>${s.profile ? ` <span class="mono muted">${esc(s.profile)}</span>` : ""}</span>` : ""}
      <span class="k">Codecs</span><span>${s.codecs.map(c => `<span class="codec ${common.includes(c) ? "common" : ""}">${esc(c)}</span>`).join("") || "—"}</span></div></div>`;
  };
  return `<p class="muted" style="margin-top:0">Each offer next to its answer. Codecs both sides support are highlighted.</p>` + pairs.map(([o, a]) => `<div class="sdp-pair">${card(o, a)}${card(a, o)}</div>`).join("");
}
/* The SBC's own call records (CDRs): the key facts first, every column below. */
const CDR_KEYS = [["TrmReason", "End reason"], ["TrmReasonCategory", "End category"], ["SIPTrmReason", "SIP end reason"], ["Durat", "Duration (s)"], ["Coder", "Codec"],
  ["InPackets", "Audio packets in"], ["OutPackets", "Audio packets out"], ["PackLoss", "Packets lost (in)"], ["LocalPackLoss", "Packets lost (in)"], ["RemotePackLoss", "Packets lost (far end)"], ["RTPjitter", "Jitter (ms)"], ["RemoteRTPjitter", "Jitter at far end (ms)"], ["RTPdelay", "Delay (ms)"],
  ["LocalMOSCQ", "MOS far end → SBC"], ["RemoteMOSCQ", "MOS SBC → far end"], ["LocalMOS", "MOS far end → SBC"], ["RemoteMOS", "MOS SBC → far end"], ["LocalMOSLQ", "Listen MOS in"], ["RemoteMOSLQ", "Listen MOS out"],
  ["RtpIp", "Far-end media IP"], ["RemoteRtpIp", "Far-end media IP"], ["Port", "Far-end media port"], ["RemoteRtpPort", "Far-end media port"], ["LocalRtpIp", "SBC media IP"], ["LocalRtpPort", "SBC media port"], ["LatchedRtpIp", "Latched media IP"], ["LatchedRtpPort", "Latched media port"],
  ["IPG", "IP group"], ["IPGroup", "IP group"], ["SIPInterfaceId", "SIP interface"], ["ProxySetId", "Proxy set"], ["IpProfileId", "IP profile"], ["MediaRealmId", "Media realm"], ["SrdId", "SRD"],
  ["SrcPhoneNum", "Calling"], ["DstPhoneNum", "Called"], ["Caller", "Caller"], ["Callee", "Callee"], ["SetupTime", "Set up"], ["ConnectTime", "Connected"], ["ReleaseTime", "Released"], ["TrmSd", "Ended by"]];
function cdrTab(d) {
  if (!d.cdrs.length) return emptyState("report", "No SBC call record yet", "The SBC sends its record (CDR) in syslog when the call ends. Syslog must be on, at debug level <b>Detailed</b>.");
  return d.cdrs.map(r => {
    const f = Object.fromEntries(r.fields.map(x => [x.k, x.v]));
    const inP = +f.InPackets, outP = +f.OutPackets;
    const key = CDR_KEYS.filter(([k]) => f[k] !== undefined && f[k] !== "" && f[k] !== "-1").map(([k, l]) => {
      const warn = (k === "InPackets" && inP >= 0 && outP > 500 && inP < outP / 20) || (k === "TrmReason" && /BROKEN|ABNORMAL|FAIL/i.test(f[k]));
      const shown = /^\d{1,3}(?:\.\d{1,3}){3}$/.test(f[k]) && peerName(f[k]) ? `${peerName(f[k])} (${f[k]})` : /^\d+$/.test(f[k]) && +f[k] > 999 ? fmtN(+f[k]) : f[k];
      return `<div><div class="l">${l}</div><div class="v" title="${esc(f[k])}" ${warn ? 'style="color:var(--bad)"' : ""}>${esc(shown)}</div></div>`;
    }).join("");
    return `<div class="card" style="margin-bottom:14px"><div class="card-h"><h2>${esc(r.kind)}</h2><span class="badge">${r.family === "SBC" ? "SBC record" : "Gateway record"}</span><span class="grow"></span><span class="muted">${tms(r.received)}</span></div>
      <div class="card-b"><div class="facts">${key}</div>
      <details class="m"><summary><b>All ${r.fields.length} columns</b></summary><div class="scroll"><table class="tbl"><tbody>${r.fields.filter(x => x.v !== "").map(x => `<tr><td class="muted">${esc(x.k)}</td><td class="mono" style="white-space:normal;word-break:break-all">${esc(x.v)}</td></tr>`).join("")}</tbody></table></div></details></div></div>`;
  }).join("") + `<p class="muted" style="font-size:12.5px">Counters come from the SBC itself: "in" is audio it received from the far end, "out" what it sent.</p>`;
}
function sbcLogTab(d) {
  if (!d.session) return emptyState("list", "No session ID for this call", "The SBC log is matched by the SID, which comes with syslog or Debug Recording.");
  const lines = d.sbcLog || [];
  if (!lines.length) return emptyState("list", "No SBC log lines for this call", `Nothing in the syslog with SID ${esc(d.session)}. Syslog at debug level <b>Detailed</b> logs every step: classification, routing, manipulation, media.`);
  const sev = t => { const m = /^<(\d{1,3})>/.exec(t); return m ? (+m[1]) & 7 : 9; };
  return `<p class="muted" style="margin-top:0">The SBC's own log for SID <span class="mono">${esc(d.session)}</span>: ${fmtN(lines.length)} line${lines.length === 1 ? "" : "s"}, oldest first. Warnings and errors are tinted.</p>
    <div class="log" style="max-height:62vh">${lines.slice().reverse().map(l => `<div class="${sev(l.text) <= 3 ? "lv-err" : sev(l.text) === 4 ? "lv-warn" : ""}"><span class="tm">${tms(l.time)}</span><span class="sb">${esc(sbcName(l.source))}</span><span>${esc(l.text.replace(/^<\d{1,3}>\s*/, ""))}</span></div>`).join("")}</div>`;
}
function messagesTab(d) {
  return d.messages.map(m => `<details class="m" data-i="${m.index}"><summary><b>${esc(m.first)}</b><span class="muted">${tms(m.time)} · ${esc(m.src || "SBC")} → ${esc(m.dst || "SBC")}</span>${m.hasSdp ? '<span class="badge accent">SDP</span>' : ""}</summary>${sipHtml(m.text)}</details>`).join("")
    + (d.droppedMessages ? `<p class="muted">${d.droppedMessages} more messages were not kept (limit per call).</p>` : "");
}

/* ------------------------------------------------------------ streams view */
async function loadStreams() {
  const items = await api("/api/streams?" + sbcQ($("sbcFilter2")));
  $("streamNote").textContent = items.length ? `${items.length} stream${items.length === 1 ? "" : "s"}` : "";
  setHTML($("streamsBox"), items.length ? streamTable(items, true) : emptyState("wave", "No audio streams", "Audio streams need Debug Recording with <b>Signaling &amp; Media</b> on the SBC."));
}
$("sbcFilter2").onchange = () => refresh();

/* ------------------------------------------------------------ syslog view (a log viewer like AudioCodes Syslog Viewer) */
// Device tabs, display filter (text or regular expression, case, whole word, filter out), severity and line kind,
// session filter, freeze, auto-scroll, find, go to line, marks (F2), keyword highlighting, right-click actions,
// open / paste / follow a file, save, clean and write log. Line numbers are the server's line ids: they stay the
// same whatever the filter, so "Go to line" and marks keep working.
const SL = {sbc: "", sid: "", lines: [], lastId: 0, key: "", frozen: false, marks: new Set(), sel: null, info: null, seen: new Set(), temp: [], find: {hits: [], at: -1}};
let slHl = store.get("slHl", []);   // [{text, regex, color}]
const HL_COLORS = {yellow:"#facc15", green:"#4ade80", cyan:"#22d3ee", orange:"#fb923c", pink:"#f472b6", red:"#f87171", violet:"#a78bfa", blue:"#60a5fa"};
const slSev = t => { const m = /^<(\d{1,3})>/.exec(t); return m ? (+m[1]) & 7 : 9; };
const slGap = t => /^<\d{1,3}>MediantTrace: \d+ syslog messages? (?:is|are) missing/.test(t);
const slTime = s => {
  if (!s) return "";
  const d = new Date(s), p = (n, w = 2) => String(n).padStart(w, "0");
  const utc = $("slUtc").checked;
  const [Y, M, D, h, m, sec, ms] = utc ? [d.getUTCFullYear(), d.getUTCMonth() + 1, d.getUTCDate(), d.getUTCHours(), d.getUTCMinutes(), d.getUTCSeconds(), d.getUTCMilliseconds()]
    : [d.getFullYear(), d.getMonth() + 1, d.getDate(), d.getHours(), d.getMinutes(), d.getSeconds(), d.getMilliseconds()];
  return ($("slDate").checked ? `${Y}-${p(M)}-${p(D)} ` : "") + `${p(h)}:${p(m)}:${p(sec)}.${p(ms, 3)}`;
};
function slParams() {
  return {q: $("sq").value.trim(), regex: $("slRegex").checked ? 1 : 0, case: $("slCase").checked ? 1 : 0, word: $("slWord").checked ? 1 : 0,
    exclude: $("slExclude").checked ? 1 : 0, level: $("slLevel").value, kind: $("slKind").value, sbc: SL.sbc, sid: SL.sid, limit: $("slLimit").value};
}
function slRe(text, regex, cs, word) {
  if (!text) return null;
  let src = regex ? text : text.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  if (word) src = `\\b(?:${src})\\b`;
  try { return new RegExp(src, "g" + (cs ? "" : "i")); } catch { return null; }
}
// Paints a line: finds every rule's matches in the raw text, keeps the first rule's span where they overlap, escapes the rest.
function slPaint(raw, rules) {
  const spans = [];
  for (const r of rules) {
    if (!r.re) continue;
    r.re.lastIndex = 0;
    let m, guard = 0;
    while ((m = r.re.exec(raw)) && guard++ < 400) {
      if (!m[0].length) { r.re.lastIndex++; continue; }
      const s = m.index, e = s + m[0].length;
      if (!spans.some(x => s < x.e && e > x.s)) spans.push({s, e, open: r.open(m)});
    }
  }
  spans.sort((a, b) => a.s - b.s);
  let out = "", at = 0;
  for (const x of spans) { out += esc(raw.slice(at, x.s)) + x.open + esc(raw.slice(x.s, x.e)) + "</span>"; at = x.e; }
  return out + esc(raw.slice(at));
}
function slRules() {
  const p = slParams(), rules = [];
  SL.temp.forEach(t => rules.push({re: slRe(t.text, false, true, false), open: () => `<span class="kw" style="background:${HL_COLORS.violet}55">`}));
  slHl.forEach(h => rules.push({re: slRe(h.text, h.regex, false, false), open: () => `<span class="kw" style="background:${(HL_COLORS[h.color] || HL_COLORS.yellow)}55">`}));
  const fq = $("slFindQ")?.value.trim();
  if (fq && !$("slFindBar").classList.contains("hidden")) rules.push({re: slRe(fq, $("slFindRegex").checked, $("slFindCase").checked, $("slFindWord").checked), open: () => `<span class="fd">`});
  if (p.q && !p.exclude) rules.push({re: slRe(p.q, !!p.regex, !!p.case, !!p.word), open: () => `<span class="mk2">`});
  rules.push({re: /-{3,} *Incoming (?:SIP|HTTP|DNS|VoiceAI) Message[^\n]*?(?:-{3,}|$)/gm, open: () => `<span class="bin">`});
  rules.push({re: /-{3,} *Outgoing (?:SIP|HTTP|DNS|VoiceAI) Message[^\n]*?(?:-{3,}|$)/gm, open: () => `<span class="bout">`});
  rules.push({re: /\[SID[=:]([^\]]*)\]/g, open: m => `<span class="sid" data-sid="${esc(m[1])}" title="Show only this session">`});
  rules.push({re: /^(?:INVITE|ACK|BYE|CANCEL|OPTIONS|REGISTER|PRACK|UPDATE|INFO|REFER|NOTIFY|SUBSCRIBE|MESSAGE|PUBLISH) \S+ SIP\/2\.0$/gm, open: () => `<span class="sreq">`});
  rules.push({re: /^SIP\/2\.0 [12]\d\d[^\n]*/gm, open: m => `<span class="${/^SIP\/2\.0 2/.test(m[0]) ? "sok" : "sprov"}">`});
  rules.push({re: /^SIP\/2\.0 [3-6]\d\d[^\n]*/gm, open: () => `<span class="sfail">`});
  rules.push({re: /^[A-Za-z][A-Za-z0-9\-]*:(?= )/gm, open: () => `<span class="hdr">`});
  rules.push({re: /\|(?:CALL_START|CALL_CONNECT|CALL_END|MEDIA_START|MEDIA_UPDATE|MEDIA_END)\b/g, open: () => `<span class="cdr">`});
  return rules;
}
function slLine(l, rules) {
  const sev = slSev(l.text), gap = slGap(l.text);
  const cls = [gap ? "gap" : sev <= 3 ? "lv-err" : sev === 4 ? "lv-warn" : "", SL.marks.has(l.id) ? "mk" : "", SL.sel === l.id ? "sel" : ""].join(" ").trim();
  return `<div class="${cls}" data-id="${l.id}"><span class="ln" data-mark="${l.id}" title="Mark or unmark this line (Ctrl+F2)">${l.id}</span><span class="tm">${slTime(l.time)}</span><span class="sb" title="${esc(l.source)}">${esc(sbcName(l.source))}</span><span class="tx">${slPaint(l.text.replace(/^<\d{1,3}>\s*/, ""), rules)}</span></div>`;
}
async function loadSyslog(force) {
  if (SL.frozen && !force) { slStatus(); return; }
  const p = slParams(), key = JSON.stringify(p);
  let reset = force === "reset" || key !== SL.key;
  const get = after => api("/api/syslog/view?" + new URLSearchParams({...p, after}));
  let r = await get(reset ? 0 : SL.lastId);
  if (!reset && r.last < SL.lastId) { reset = true; r = await get(0); }   // the buffer was cleaned or a file opened
  SL.info = r; SL.key = key; SL.lastId = r.last;
  const limit = +p.limit;
  let added = r.lines;
  if (reset) SL.lines = r.lines.slice(-limit);
  else if (added.length) SL.lines = SL.lines.concat(added).filter(l => l.id >= r.first).slice(-limit);
  else if (SL.lines.length && SL.lines[0].id < r.first) { SL.lines = SL.lines.filter(l => l.id >= r.first); reset = true; }
  SL.lines.forEach(l => SL.seen.add(l.source));
  slTabs();
  const box = $("syslogBox"), rules = slRules();
  const atBottom = box.scrollHeight - box.scrollTop - box.clientHeight < 40;
  if (reset || box.__count === undefined) {
    box.innerHTML = SL.lines.length ? SL.lines.map(l => slLine(l, rules)).join("")
      : emptyState("list", "No syslog lines", r.error ? esc(r.error) : status?.opened ? "Nothing in the opened file matches." : "Nothing logged yet, or nothing matches the filter. Point the SBC's syslog at this server, or open a saved log.");
    box.__count = SL.lines.length;
    if ($("follow").checked || !box.__init) { box.scrollTop = box.scrollHeight; box.__init = true; }
  } else if (added.length) {
    if (!box.__count) box.innerHTML = "";
    box.insertAdjacentHTML("beforeend", added.map(l => slLine(l, rules)).join(""));
    while (box.childElementCount > SL.lines.length) box.firstElementChild.remove();
    box.__count = SL.lines.length;
    if ($("follow").checked && atBottom) box.scrollTop = box.scrollHeight;
  }
  slStatus();
}
function slRepaint() { $("syslogBox").__count = undefined; SL.key = ""; loadSyslog("reset"); }
function slTabs() {
  const sources = new Set(SL.seen);
  (status?.sbcs || []).forEach(x => { if (x.listed || x.syslog) sources.add(x.ip); });
  const list = [...sources].filter(Boolean).sort((a, b) => sbcName(a).localeCompare(sbcName(b)));
  const tab = (v, label, title) => `<button role="tab" class="${SL.sbc === v ? "on" : ""}" data-tab="${esc(v)}" aria-selected="${SL.sbc === v}" title="${esc(title || label)}">${esc(label)}</button>`;
  const html = tab("", "All devices") + list.map(ip => tab(ip, sbcName(ip) === ip ? ip : `${sbcName(ip)}`, ip)).join("");
  if (setHTML($("slTabs"), html)) $("slTabs").querySelectorAll("[data-tab]").forEach(b => b.onclick = () => { SL.sbc = b.dataset.tab; loadSyslog("reset"); });
}
function slStatus() {
  const r = SL.info || {}, s = status || {};
  const errs = SL.lines.filter(l => slSev(l.text) <= 3).length, warns = SL.lines.filter(l => slSev(l.text) === 4).length;
  const seq = (s.sbcs || []).filter(x => x.sequence && (x.sequence.missing || x.sequence.outOfOrder)).map(x =>
    `<span title="Syslog [S=n] numbers from ${esc(x.ip)}: ${fmtN(x.sequence.received)} received"><b>${esc(x.listed ? x.name : x.ip)}</b> missing <b style="color:${x.sequence.missing ? "var(--bad)" : "inherit"}">${fmtN(x.sequence.missing)}</b> · out of order ${fmtN(x.sequence.outOfOrder)}</span>`).join("");
  const st = s.settings || {}, lis = st.listening || {};
  const listen = `Syslog: UDP ${lis.syslog || "–"}${lis.syslogTcp ? ` · TCP ${lis.syslogTcp}` : ""}${lis.syslogTls ? ` · TLS ${lis.syslogTls}` : ""}`;
  const parts = [
    `Shown <b>${fmtN(SL.lines.length)}</b>${r.limited ? " (newest)" : ""} of ${fmtN(r.total || 0)} in buffer (keeps ${fmtN(r.capacity || 0)})`,
    `Errors <b style="color:${errs ? "var(--bad)" : "inherit"}">${fmtN(errs)}</b> · Warnings <b style="color:${warns ? "var(--warn)" : "inherit"}">${fmtN(warns)}</b>`,
    SL.marks.size ? `Marked ${SL.marks.size}` : "",
    seq,
    s.opened ? `<span class="badge accent">Opened: ${esc(s.opened)}</span>` : listen,
    s.watching ? `Following <span class="mono">${esc(s.watching)}</span> (${fmtN(s.watchLines)} lines${s.watchError ? ", " + esc(s.watchError) : ""}) <button class="btn sm ghost" id="slUnwatch">Stop</button>` : "",
    st.writeLog ? `<span style="color:var(--bad)">●</span> Writing log to <span class="mono">${esc(st.writeLogCurrent || st.writeLogFolderFull)}</span>${st.writeLogError ? ` <b style="color:var(--bad)">${esc(st.writeLogError)}</b>` : ""}` : "",
    SL.frozen ? `<b style="color:var(--warn)">Frozen</b>` : "",
    r.error ? `<b style="color:var(--bad)">${esc(r.error)}</b>` : "",
  ].filter(Boolean);
  if (setHTML($("slStatus"), parts.map(x => `<span>${x}</span>`).join(""))) {
    const u = $("slUnwatch"); if (u) u.onclick = async () => { try { await api("/api/watch/stop", {}); refresh(); } catch (e) { toast(e.message, true); } };
  }
  const sidBox = SL.sid ? `<span class="badge accent">Session ${esc(SL.sid)}</span> <button class="btn sm" id="slSidCall">SIP flow</button> <button class="btn sm ghost" id="slSidClear">Show all sessions</button>` : "";
  if (setHTML($("slSid"), sidBox)) {
    const c = $("slSidCall"); if (c) c.onclick = () => openSessionCall(SL.sid, SL.sbc);
    const x = $("slSidClear"); if (x) x.onclick = () => { SL.sid = ""; loadSyslog("reset"); };
  }
  const w = $("slWrite"); if (w) { w.classList.toggle("danger", !!st.writeLog); w.textContent = st.writeLog ? "Stop writing log" : "Write log"; }
  const f = $("slFreeze"); f.classList.toggle("primary", SL.frozen); f.setAttribute("aria-pressed", SL.frozen); f.textContent = SL.frozen ? "Unfreeze" : "Freeze";
}
// Opens the call with this SID (the ladder), as Syslog Viewer's SIP Flow Diagram does from a session.
async function openSessionCall(sid, sbc) {
  try {
    const list = await api("/api/calls?options=1" + (sbc ? "&sbc=" + encodeURIComponent(sbc) : ""));
    const hit = list.find(c => c.session === sid) || list.find(c => (c.session || "").startsWith(sid));
    if (!hit) { toast(`No call with session ${sid} yet. Its SIP messages may not be in the log.`, true); return; }
    dtab = "flow"; selected = null; detail = null; go("calls"); select(hit.id);
  } catch (e) { toast(e.message, true); }
}
let syslogTimer;
["sq"].forEach(id => $(id).oninput = () => { clearTimeout(syslogTimer); syslogTimer = setTimeout(() => loadSyslog(), 300); });
["slRegex", "slCase", "slWord", "slExclude", "slLevel", "slKind", "slLimit"].forEach(id => $(id).onchange = () => loadSyslog());
["slDate", "slUtc"].forEach(id => $(id).onchange = () => { store.set(id, $(id).checked); slRepaint(); });
["slDate", "slUtc"].forEach(id => $(id).checked = store.get(id, false));
$("follow").onchange = () => { if ($("follow").checked) { const b = $("syslogBox"); b.scrollTop = b.scrollHeight; } };
$("slFreeze").onclick = () => { SL.frozen = !SL.frozen; if (!SL.frozen) loadSyslog(); else slStatus(); };
$("slClean").onclick = async () => {
  if (!confirm("Clean the syslog view? The lines leave the screen; files already written are kept.")) return;
  try { await api("/api/syslog/clean", {}); SL.marks.clear(); SL.seen.clear(); SL.sel = null; slRepaint(); toast("Cleaned"); } catch (e) { toast(e.message, true); }
};
$("slSave").onclick = e => { e.preventDefault(); location.href = "/api/syslog/save?" + new URLSearchParams({sbc: SL.sbc, utc: $("slUtc").checked ? 1 : 0}); };
$("slWrite").onclick = async () => {
  const on = !(status?.settings?.writeLog);
  try { const r = await api("/api/settings", {writeLog: on}); toast(r.message); refresh(); } catch (e) { toast(e.message, true); }
};
$("slOpen").onclick = () => $("fileOpen").click();
$("slPaste").onclick = () => pasteDialog();
$("slMarks").onclick = () => slNextMark(1);
$("slGoto").onclick = () => slGoto();
$("slFind").onclick = () => slOpenFind();
$("slHl").onclick = () => hlDialog();
$("slFindClose").onclick = () => { $("slFindBar").classList.add("hidden"); slRepaint(); };
$("slFindNext").onclick = () => slFindStep(1);
$("slFindPrev").onclick = () => slFindStep(-1);
$("slFindQ").onkeydown = e => { if (e.key === "Enter") { e.preventDefault(); slFindStep(e.shiftKey ? -1 : 1); } };
["slFindQ", "slFindRegex", "slFindCase", "slFindWord"].forEach(id => $(id)[id === "slFindQ" ? "oninput" : "onchange"] = () => { SL.find.at = -1; slFindCount(); clearTimeout(syslogTimer); syslogTimer = setTimeout(slRepaint, 300); });
function slOpenFind() { $("slFindBar").classList.remove("hidden"); const q = $("slFindQ"); const sel = String(getSelection() || "").trim(); if (sel && sel.length < 200 && !sel.includes("\n")) q.value = sel; q.focus(); q.select(); slFindCount(); }
function slFindHits() {
  const re = slRe($("slFindQ").value.trim(), $("slFindRegex").checked, $("slFindCase").checked, $("slFindWord").checked);
  if (!re) return [];
  return SL.lines.filter(l => { re.lastIndex = 0; return re.test(l.text); }).map(l => l.id);
}
function slFindCount() { const hits = slFindHits(); $("slFindN").textContent = $("slFindQ").value.trim() ? (hits.length ? `${SL.find.at >= 0 ? SL.find.at + 1 : 0} of ${fmtN(hits.length)}` : "not found") : ""; return hits; }
function slFindStep(dir) {
  const hits = slFindHits();
  if (!hits.length) { $("slFindN").textContent = "not found"; return; }
  // From the selected line onwards (or backwards), wrapping round.
  const from = SL.sel ?? (dir > 0 ? -Infinity : Infinity);
  let i = dir > 0 ? hits.findIndex(id => id > from) : hits.map((id, k) => [id, k]).filter(([id]) => id < from).pop()?.[1] ?? -1;
  if (i < 0) i = dir > 0 ? 0 : hits.length - 1;
  SL.find.at = i;
  slSelect(hits[i], true);
  $("slFindN").textContent = `${i + 1} of ${fmtN(hits.length)}`;
}
function slSelect(id, scroll) {
  SL.sel = id;
  const box = $("syslogBox");
  box.querySelectorAll(".sel").forEach(x => x.classList.remove("sel"));
  const row = box.querySelector(`[data-id="${id}"]`);
  if (row) { row.classList.add("sel"); if (scroll) row.scrollIntoView({block: "center"}); }
  return row;
}
function slGoto() {
  const v = prompt(`Go to line (${fmtN(SL.lines[0]?.id || 0)} to ${fmtN(SL.lines.at(-1)?.id || 0)} are shown):`);
  if (!v) return;
  const id = parseInt(v.replace(/\D/g, ""), 10);
  const near = SL.lines.find(l => l.id >= id);
  if (!near) { toast("That line is not in the lines shown. Clear the filter or show more lines.", true); return; }
  $("follow").checked = false;
  slSelect(near.id, true);
  if (near.id !== id) toast(`Line ${id} is filtered out; showing line ${near.id}.`);
}
function slToggleMark(id) {
  if (SL.marks.has(id)) SL.marks.delete(id); else SL.marks.add(id);
  $("syslogBox").querySelector(`[data-id="${id}"]`)?.classList.toggle("mk", SL.marks.has(id));
  slStatus();
}
function slNextMark(dir) {
  const marks = SL.lines.map(l => l.id).filter(id => SL.marks.has(id));
  if (!marks.length) { toast("No marked lines. Click a line number, or press Ctrl+F2, to mark a line."); return; }
  const from = SL.sel ?? (dir > 0 ? -Infinity : Infinity);
  let next = dir > 0 ? marks.find(id => id > from) : marks.filter(id => id < from).pop();
  if (next === undefined) next = dir > 0 ? marks[0] : marks.at(-1);
  $("follow").checked = false;
  slSelect(next, true);
}
$("syslogBox").addEventListener("click", e => {
  const mark = e.target.closest("[data-mark]");
  if (mark) { slToggleMark(+mark.dataset.mark); return; }
  const sid = e.target.closest("[data-sid]");
  if (sid && !String(getSelection() || "").trim()) { SL.sid = sid.dataset.sid; loadSyslog("reset"); return; }
  const row = e.target.closest("[data-id]");
  if (row) slSelect(+row.dataset.id);
});
$("syslogBox").addEventListener("contextmenu", e => {
  const row = e.target.closest("[data-id]");
  if (!row) return;
  e.preventDefault();
  const id = +row.dataset.id, line = SL.lines.find(l => l.id === id);
  if (!line) return;
  slSelect(id);
  const text = String(getSelection() || "").trim().slice(0, 300);
  const sid = (/\[SID[=:]([^\]]*)\]/.exec(line.text) || [])[1] || "";
  const party = h => (new RegExp(`^(?:${h}):.*?sips?:([^@;>\\s]+)`, "mi").exec(line.text) || [])[1] || "";
  const from = party("From|f"), to = party("To|t");
  const items = [
    sid && ["Filter this session", () => { SL.sid = sid; loadSyslog("reset"); }],
    sid && ["SIP flow of this session", () => openSessionCall(sid, line.source)],
    sid && ["Highlight this session", () => { SL.temp.push({text: `SID=${sid}`}); slRepaint(); }],
    text && "-",
    text && [`Filter "${text.slice(0, 30)}"`, () => { $("sq").value = text; $("slExclude").checked = false; $("slRegex").checked = false; loadSyslog(); }],
    text && [`Filter out "${text.slice(0, 30)}"`, () => { $("sq").value = text; $("slExclude").checked = true; $("slRegex").checked = false; loadSyslog(); }],
    text && [`Highlight "${text.slice(0, 30)}"`, () => { SL.temp.push({text}); slRepaint(); }],
    text && [`Find "${text.slice(0, 30)}"`, () => slOpenFind()],
    SL.temp.length && ["Clear highlight", () => { SL.temp = []; slRepaint(); }],
    "-",
    [SL.marks.has(id) ? "Unmark line" : "Mark line", () => slToggleMark(id)],
    ["Copy line", () => copyText(line.text.replace(/^<\d{1,3}>\s*/, ""))],
    sid && ["Copy SID", () => copyText(sid)],
    from && [`Copy From (${from})`, () => copyText(from)],
    to && [`Copy To (${to})`, () => copyText(to)],
    "-",
    ["Show only this device", () => { SL.sbc = line.source; loadSyslog("reset"); }],
    ["Ignore this device on arrival…", async () => {
      if (!confirm(`Drop all syslog from ${line.source} from now on? (Settings → Syslog → IP filters)`)) return;
      try { const r = await api("/api/settings", {ignoreSyslogFrom: [...(status?.settings?.ignoreSyslogFrom || []), line.source]}); toast(r.message); } catch (e2) { toast(e2.message, true); }
    }],
  ].filter(Boolean);
  ctxMenu(e.clientX, e.clientY, items);
});
function ctxMenu(x, y, items) {
  document.querySelectorAll(".ctx").forEach(m => m.remove());
  const m = document.createElement("div");
  m.className = "ctx"; m.setAttribute("role", "menu");
  items.forEach((it, i) => {
    if (it === "-") { if (i && items[i - 1] !== "-") m.appendChild(document.createElement("hr")); return; }
    const b = document.createElement("button");
    b.type = "button"; b.setAttribute("role", "menuitem"); b.textContent = it[0];
    b.onclick = () => { m.remove(); it[1](); };
    m.appendChild(b);
  });
  document.body.appendChild(m);
  const r = m.getBoundingClientRect();
  m.style.left = Math.min(x, innerWidth - r.width - 8) + "px";
  m.style.top = Math.min(y, innerHeight - r.height - 8) + "px";
  m.querySelector("button")?.focus();
  const close = ev => { if (!m.contains(ev.target) || ev.key === "Escape") { m.remove(); document.removeEventListener("mousedown", close, true); document.removeEventListener("keydown", close, true); } };
  setTimeout(() => { document.addEventListener("mousedown", close, true); document.addEventListener("keydown", close, true); });
}
function dialog(title, body, buttons) {
  const back = document.createElement("div");
  back.className = "dlg-back";
  back.innerHTML = `<div class="dlg" role="dialog" aria-modal="true" aria-label="${esc(title)}"><h2 style="margin:0 0 12px">${esc(title)}</h2><div class="dlg-b">${body}</div><div class="dlg-f"></div></div>`;
  const f = back.querySelector(".dlg-f");
  const close = () => back.remove();
  buttons.forEach(([label, fn, cls]) => { const b = document.createElement("button"); b.className = "btn " + (cls || ""); b.textContent = label; b.onclick = async () => { if (!fn || (await fn(back)) !== false) close(); }; f.appendChild(b); });
  back.onmousedown = e => { if (e.target === back) close(); };
  back.onkeydown = e => { if (e.key === "Escape") close(); };
  document.body.appendChild(back);
  back.querySelector("input,textarea,button")?.focus();
  return back;
}
function hlDialog() {
  const row = (h, i) => `<div class="hl-row" data-i="${i}"><input class="hl-t" value="${esc(h.text)}" placeholder="word or pattern" aria-label="Keyword"><label class="chk"><input type="checkbox" class="hl-r" ${h.regex ? "checked" : ""}> RegExp</label>
    <select class="hl-c" aria-label="Colour">${Object.keys(HL_COLORS).map(c => `<option ${h.color === c ? "selected" : ""}>${c}</option>`).join("")}</select><button class="btn sm ghost hl-x" aria-label="Remove">${ic("trash", 13)}</button></div>`;
  const items = slHl.length ? slHl : [{text: "", regex: false, color: "yellow"}];
  const d = dialog("Highlight keywords", `<p class="muted" style="margin-top:0">Matching text is coloured in the syslog view (case does not matter). Kept in this browser.</p><div id="hlRows">${items.map(row).join("")}</div><button class="btn sm" id="hlAdd">${ic("plus", 13)} Add keyword</button>`,
    [["Cancel"], ["Save", back => {
      const list = [...back.querySelectorAll(".hl-row")].map(r => ({text: r.querySelector(".hl-t").value.trim(), regex: r.querySelector(".hl-r").checked, color: r.querySelector(".hl-c").value})).filter(h => h.text);
      const bad = list.find(h => h.regex && !slRe(h.text, true, false, false));
      if (bad) { toast(`"${bad.text}" is not a valid regular expression`, true); return false; }
      slHl = list; store.set("slHl", list); slRepaint(); toast("Highlighting saved");
    }, "primary"]]);
  const bind = () => d.querySelectorAll(".hl-x").forEach(b => b.onclick = () => b.closest(".hl-row").remove());
  d.querySelector("#hlAdd").onclick = () => { d.querySelector("#hlRows").insertAdjacentHTML("beforeend", row({text: "", regex: false, color: Object.keys(HL_COLORS)[d.querySelectorAll(".hl-row").length % 8]}, 0)); bind(); };
  bind();
}
function pasteDialog() {
  dialog("Paste a log", `<p class="muted" style="margin-top:0">Paste syslog copied from anywhere: Syslog Viewer, acSyslog, a syslog server, Splunk, Azure, Excel. It is shown like an opened file; <b>Back to live</b> returns to what the SBCs are sending.</p>
    <textarea id="pasteText" rows="14" style="width:100%;font:12px var(--mono)" placeholder="Paste here (Ctrl+V)"></textarea>`,
    [["Cancel"], ["Show", async back => {
      const text = back.querySelector("#pasteText").value;
      if (!text.trim()) { toast("Nothing pasted", true); return false; }
      try { const r = await api("/api/paste", {text}); toast(r.message); afterOpen(); } catch (e) { toast(e.message, true); return false; }
    }, "primary"]]);
}
function formatsDialog() {
  dialog("Files MediantTrace opens", `<ul style="margin:0;padding-left:18px;line-height:1.7">
    <li><b>Packet captures</b> from MediantTrace, Wireshark or tcpdump (.pcap, .pcapng): syslog, Debug Recording, or plain SIP on any UDP port.</li>
    <li><b>MediantTrace .txt</b> captures, and the plain <b>write log</b> files.</li>
    <li><b>Syslog Viewer</b> standard log: <span class="mono">12:28:52.794  10.1.1.1  local0.notice  [S=…] …</span>, with or without a date.</li>
    <li><b>acSyslog</b> and raw syslog: <span class="mono">&lt;157&gt;2023-03-17T13:25:58.962Z 10.1.1.1 [S=…] …</span> (RFC 3164 and 5424).</li>
    <li><b>CSV</b> and <b>TSV</b> with a header row: a column called Message (or Msg, Text, SyslogMessage, _raw), and optionally Time / Timestamp / TimeGenerated, Host / IP / HostName, Facility, Severity, Priority, DeviceName.</li>
    <li><b>SL4NT</b> and other CSV without a header: date, time, facility, severity, host, message.</li>
    <li><b>Excel</b> (.xlsx): the first sheet, with the same column names as CSV.</li>
    <li><b>JSON</b>: Azure Log Analytics exports (v1 and v2), Elastic search results, an array of objects, or one object per line.</li>
    <li>The device's <b>legacy web</b> format: lines that end <span class="mono">[Time:14-11@12:28:52.794]</span>.</li></ul>
    <p class="muted">A line that does not start a new message continues the one before, so SIP messages over several lines stay together. Gaps in the SBC's [S=n] numbers are shown as warnings.</p>`, [["Close", null, "primary"]]);
}
async function openLogFile(file) {
  if (!file) return;
  if (status?.recording) { toast("Stop the packet capture before opening a file.", true); return; }
  toast(`Opening ${file.name}…`);
  try {
    const r = await fetch("/api/import?name=" + encodeURIComponent(file.name), {method: "POST", headers: {"Content-Type": "application/octet-stream"}, body: file});
    const ct = r.headers.get("content-type") || "";
    const data = ct.includes("json") ? await r.json() : {message: await r.text()};
    if (!r.ok) throw new Error(data.message || r.statusText);
    toast(data.message); afterOpen();
  } catch (e) { toast(e.message, true); }
}
function afterOpen() { selected = null; detail = null; SL.marks.clear(); SL.seen.clear(); SL.sid = ""; SL.sbc = ""; SL.sel = null; $("syslogBox").__count = undefined; SL.key = ""; refresh(); }
$("fileOpen").onchange = e => { openLogFile(e.target.files[0]); e.target.value = ""; };
// Drop a file anywhere on the page to open it.
document.addEventListener("dragover", e => { if ([...(e.dataTransfer?.types || [])].includes("Files")) { e.preventDefault(); document.body.classList.add("dropping"); } });
document.addEventListener("dragleave", e => { if (!e.relatedTarget) document.body.classList.remove("dropping"); });
document.addEventListener("drop", e => { if (e.dataTransfer?.files?.length) { e.preventDefault(); document.body.classList.remove("dropping"); openLogFile(e.dataTransfer.files[0]); } });
document.addEventListener("keydown", e => {
  if (view !== "syslog" || document.body.classList.contains("locked")) return;
  const typing = /INPUT|TEXTAREA|SELECT/.test(document.activeElement?.tagName || "");
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "f") { e.preventDefault(); slOpenFind(); return; }
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "g") { e.preventDefault(); slGoto(); return; }
  if (e.key === "F3") { e.preventDefault(); slFindStep(e.shiftKey ? -1 : 1); return; }
  if (e.key === "F2" && (e.ctrlKey || e.metaKey)) { e.preventDefault(); if (SL.sel !== null) slToggleMark(SL.sel); return; }
  if (e.key === "F2") { e.preventDefault(); slNextMark(e.shiftKey ? -1 : 1); return; }
  if (typing) return;
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "v") { pasteDialog(); return; }
  if (["ArrowDown", "ArrowUp"].includes(e.key) && SL.sel !== null) {
    e.preventDefault();
    const i = SL.lines.findIndex(l => l.id === SL.sel), j = Math.max(0, Math.min(SL.lines.length - 1, i + (e.key === "ArrowDown" ? 1 : -1)));
    if (SL.lines[j]) slSelect(SL.lines[j].id, false)?.scrollIntoView({block: "nearest"});
  }
});

/* ------------------------------------------------------------ captures */
let capQuery = "", capHits = null;
function capHitRows() {
  if (!capHits) return "";
  const hits = capHits.hits || [];
  if (!hits.length) return `<div class="empty" style="padding:8px 0 16px"><p>No saved capture has <b>${esc(capQuery)}</b>.</p></div>`;
  const when = t => t ? new Date(t).toLocaleString([], {day:"2-digit", month:"short", hour:"2-digit", minute:"2-digit", second:"2-digit"}) : "";
  return `<div class="scroll" style="margin:12px 0"><table class="tbl"><thead><tr><th>When</th><th>From</th><th>To</th><th>Call-ID</th><th>Session</th><th class="r">Open</th></tr></thead><tbody>${hits.map(h => `<tr>
      <td><b>${esc(when(h.started))}</b><div class="muted">${esc(h.sbcName || sbcName(h.sbc) || h.sbc)} · ${esc(h.stem)}</div></td>
      <td>${esc(h.from || "—")}</td><td>${esc(h.to || "—")}</td>
      <td class="mono" style="max-width:220px;overflow:hidden;text-overflow:ellipsis" title="${esc(h.callId)}">${esc(h.callId)}</td>
      <td class="mono">${esc(h.session || "")}</td>
      <td class="r"><button class="btn sm primary" data-openhit="${esc(h.stem)}" ${h.recording ? "disabled title=\"Stop the capture first\"" : ""}>Open</button></td>
    </tr>`).join("")}</tbody></table></div>${capHits.limited ? `<p class="muted">Showing the first ${hits.length} matches. Narrow the search to see more.</p>` : ""}`;
}
function bindCapHits(root) {
  root.querySelectorAll("[data-openhit]").forEach(b => b.onclick = async () => {
    if (b.disabled) return;
    b.disabled = true;
    try {
      const r = await api("/api/files/open", {id: b.dataset.openhit});
      toast(r.message); selected = null; detail = null;
      const q = $("q"); if (q) q.value = capQuery;
      go("calls");
    } catch (e) { toast(e.message, true); b.disabled = false; }
  });
}
const capAge = started => {
  if (!started) return "—";
  const s = (Date.now() - started.getTime()) / 1000;
  if (s < 60) return "under 1 min";
  if (s < 3600) return Math.round(s / 60) + " min";
  if (s < 86400) return Math.round(s / 3600) + " h";
  const d = Math.round(s / 86400);
  return d === 1 ? "1 day" : d + " days";
};
async function loadCaptures() {
  const box = $("capBox");
  if (box.contains(document.activeElement) && document.activeElement.tagName === "INPUT") return;
  const info = await api("/api/files");
  const files = info.files || [];
  const groups = {};
  files.forEach(f => { const k = f.name.replace(/\.(pcap|txt)$/i, ""); (groups[k] = groups[k] || {name: k, files: []}).files.push(f); });
  const list = Object.values(groups);
  const stamp = k => { const m = /mediant-(\d{4})(\d{2})(\d{2})-(\d{2})(\d{2})(\d{2})/.exec(k); return m ? new Date(+m[1], m[2] - 1, +m[3], +m[4], +m[5], +m[6]) : null; };
  const n = list.length;
  const free = info.free == null ? "" : ` · ${bytes(info.free)} free on ${esc(info.drive || "this disk")}`;
  const table = list.length ? `<div class="scroll"><table class="tbl"><thead><tr><th>Captured</th><th>Age</th><th>Files</th><th class="r">Size</th><th class="r">Open</th><th></th></tr></thead><tbody>${list.map(g => {
      const active = g.files.some(f => f.active), started = stamp(g.name);
      return `<tr><td><b>${started ? started.toLocaleString([], {weekday:"short", day:"2-digit", month:"short", hour:"2-digit", minute:"2-digit", second:"2-digit"}) : esc(g.name)}</b> ${active ? '<span class="badge bad">recording</span>' : ""}<div class="muted mono">${esc(g.name)}</div></td>
        <td>${capAge(started)}</td>
        <td>${g.files.map(f => f.name.toLowerCase().endsWith(".pcap") ? '<span class="badge accent">pcap</span>' : '<span class="badge">txt</span>').join(" ")}</td>
        <td class="r">${bytes(g.files.reduce((n, f) => n + f.size, 0))}</td>
        <td class="r"><button class="btn sm primary" data-open="${esc(g.name)}" ${active ? "disabled title=\"Stop the capture first\"" : ""}>Open</button> ${g.files.map(f => `<a class="btn sm" href="/files/${encodeURIComponent(f.name)}">${ic("down", 13)} ${f.name.toLowerCase().endsWith(".pcap") ? ".pcap" : ".txt"}</a>`).join(" ")}</td>
        <td class="r"><button class="btn sm" data-delcap="${esc(g.name)}" ${active ? "disabled title=\"Stop the capture first\"" : ""}>${ic("trash", 13)} Delete</button></td></tr>`;
    }).join("")}</tbody></table></div>` : `<div class="card-b">${emptyState("file", "No captures yet", "Press <b>Start capture</b>. Each capture saves a .pcap and a .txt here.")}</div>`;
  const hitRows = capHitRows();
  const opened = status?.opened;
  const openCard = `<div class="card" style="margin-bottom:16px"><div class="card-h"><h2>Open a log or capture</h2><span class="grow"></span><button class="btn sm ghost" id="capFormats">${ic("info", 13)} Which files?</button></div>
    <div class="card-b">
      <div class="drop" id="capDrop"><b>Drop a file here</b>, or <button class="btn sm primary" id="capPick">Choose a file…</button> <button class="btn sm" id="capPaste">Paste a log…</button>
        <div class="muted" style="margin-top:8px">Syslog Viewer and acSyslog logs, CSV, TSV, Excel, JSON (Azure, Elastic), SL4NT, the SBC's web format, Wireshark .pcap / .pcapng, MediantTrace .txt. Calls, SIP ladders and the syslog view are rebuilt from it.</div></div>
      ${opened ? `<p style="margin:12px 0 0"><span class="badge accent">Showing ${esc(opened)}</span> <button class="btn sm" id="capLive">Back to live</button></p>` : ""}
      <div style="display:flex;gap:8px;align-items:center;margin-top:14px;flex-wrap:wrap"><label for="watchPath" style="margin:0">Follow a file on this server as it grows</label>
        <input id="watchPath" placeholder="C:\\logs\\syslog.txt" style="flex:1;min-width:240px" value="${esc(status?.watching || "")}" spellcheck="false">
        ${status?.watching ? `<button class="btn sm" id="watchStop">Stop following</button>` : `<button class="btn sm" id="watchGo">Follow</button>`}</div>
      <div class="help">New lines join the live syslog view, as Syslog Viewer's monitor mode does. Useful when another syslog server already holds port 514.${status?.watching ? ` Following <span class="mono">${esc(status.watching)}</span>: ${fmtN(status.watchLines)} lines${status.watchError ? ` · <b style="color:var(--bad)">${esc(status.watchError)}</b>` : ""}.` : ""}</div>
    </div></div>`;
  const html = openCard + `<div class="card"><div class="card-h"><h2>Saved captures</h2><span class="grow"></span><span class="muted">${n} capture${n === 1 ? "" : "s"} · ${bytes(info.used || 0)}${free}</span></div>
    <div class="card-b" style="padding-bottom:0">
      <div style="display:flex;gap:8px;align-items:center"><div class="search" style="flex:1"><span data-i="search"></span><input type="search" id="capFind" placeholder="Search saved captures: number, Call-ID or SID" aria-label="Search saved captures" value="${esc(capQuery)}"></div><button class="btn sm" id="capGo">Search</button></div>
      <div id="capHits">${hitRows}</div>
    </div>
    <div class="card-b form-grid">
      <div><label for="keepDays">Delete captures older than (days, 0 = keep)</label><input type="number" id="keepDays" min="0" max="3650" value="${info.retentionDays ?? 14}"></div>
      <div><label for="keepMb">Keep captures under (MB, 0 = no cap)</label><input type="number" id="keepMb" min="0" max="1000000" value="${info.retentionMaxMb ?? 2048}"></div>
      <div class="full" style="display:flex;gap:12px;align-items:center;flex-wrap:wrap"><button class="btn primary" id="saveKeep">Save</button><span class="help" style="margin:0">Checked when MediantTrace starts and when a capture stops. A capture that is still recording is kept. The oldest are removed first when the cap is passed.</span></div>
    </div>
    ${table}</div>
    <p class="muted"><b>Open</b> loads a capture back into the call view. The <b>.pcap</b> is used when there is one; otherwise the <b>.txt</b>. The .pcap also holds every packet for AudioCodes support, and the .txt is readable in Notepad. Search looks through every saved capture. Files are in <span class="mono">${esc(status?.settings.traceFolder || "")}</span>.</p>`;
  if (setHTML(box, html)) {
    paintIcons(box);
    const runSearch = async () => {
      const q = $("capFind").value.trim();
      if (q.length < 2) { toast("Type at least two characters.", true); return; }
      capQuery = q;
      try { capHits = await api("/api/files/search?q=" + encodeURIComponent(q)); }
      catch (e) { toast(e.message, true); return; }
      const hits = $("capHits");
      if (hits && setHTML(hits, capHitRows())) bindCapHits(hits);
    };
    $("capFind").onkeydown = e => { if (e.key === "Enter") { e.preventDefault(); runSearch(); } };
    $("capPick").onclick = () => $("fileOpen").click();
    $("capPaste").onclick = () => pasteDialog();
    $("capFormats").onclick = () => formatsDialog();
    const cl = $("capLive"); if (cl) cl.onclick = async () => { try { const r = await api("/api/files/close", {}); toast(r.message); afterOpen(); } catch (e) { toast(e.message, true); } };
    const wg = $("watchGo"); if (wg) wg.onclick = async () => { try { const r = await api("/api/watch", {text: $("watchPath").value}); toast(r.message); box.__h = null; refresh(); } catch (e) { toast(e.message, true); } };
    const ws = $("watchStop"); if (ws) ws.onclick = async () => { try { const r = await api("/api/watch/stop", {}); toast(r.message); box.__h = null; refresh(); } catch (e) { toast(e.message, true); } };
    $("capGo").onclick = runSearch;
    $("saveKeep").onclick = async () => {
      const days = parseInt($("keepDays").value || "0", 10), mb = parseInt($("keepMb").value || "0", 10);
      try { const r = await api("/api/settings", {retentionDays: days, retentionMaxMb: mb}); toast(r.message); box.__h = null; refresh(); }
      catch (e) { toast(e.message, true); }
    };
    bindCapHits(box);
    box.querySelectorAll("[data-open]").forEach(b => b.onclick = async () => {
      if (b.disabled) return;
      b.disabled = true;
      try { const r = await api("/api/files/open", {id: b.dataset.open}); toast(r.message); selected = null; detail = null; go("calls"); }
      catch (e) { toast(e.message, true); b.disabled = false; }
    });
    box.querySelectorAll("[data-delcap]").forEach(b => b.onclick = async () => {
      if (b.disabled) return;
      if (!confirm("Delete this capture? Its .pcap and .txt are removed from disk.")) return;
      b.disabled = true;
      try { const r = await api("/api/files/delete", {id: b.dataset.delcap}); toast(r.message); box.__h = null; refresh(); }
      catch (e) { toast(e.message, true); b.disabled = false; }
    });
  }
}

/* ------------------------------------------------------------ SBCs */
let sbcEdit = null;
function renderSbcs(s) {
  const listed = s.sbcs.filter(x => x.listed), unlisted = s.sbcs.filter(x => !x.listed);
  if (sbcEdit === null) sbcEdit = listed.map(x => ({name: x.name, ip: x.ip, url: x.url}));
  const feed = (label, x) => { const a = ago(s, x); const cls = a < 60 ? "ok" : a < Infinity ? "warn" : "";
    return `<div class="feed"><span class="dot ${cls}"></span><span class="l">${label}</span><span>${a < 60 ? "Receiving" : a < Infinity ? "Stopped" : "Not set up"}</span><span class="s">${x ? fmtN(x.packets) + " packets · " + tm(x.lastSeen) : ""}</span></div>`; };
  const cards = listed.map(x => `<div class="card sbc-card"><div class="card-h">${ic("server", 17)}<h2>${esc(x.name)}</h2><span class="muted mono">${esc(x.ip)}</span><span class="grow"></span>${x.url ? `<a class="btn sm" href="${esc(x.url)}" target="_blank" rel="noopener">${ic("ext", 13)} Web page</a>` : ""}</div>
    <div class="card-b">${feed("Syslog", x.syslog)}${feed("Debug Recording", x.dr)}${loginCard(x)}</div></div>`).join("");
  const others = unlisted.map(x => `<div class="card sbc-card"><div class="card-h">${ic("server", 17)}<h2>${esc(x.ip)}</h2><span class="badge warn">not in list</span><span class="grow"></span><button class="btn sm" data-addip="${esc(x.ip)}">${ic("plus", 13)} Add</button></div><div class="card-b">${feed("Syslog", x.syslog)}${feed("Debug Recording", x.dr)}</div></div>`).join("");
  const top = cards || others ? `<div class="grid-cards">${cards}${others}</div>` : `<div class="card">${emptyState("server", "No SBCs yet", "Add your SBCs below, then point their logging at this server.")}</div>`;
  const addrs = s.serverAddresses || [];
  const how = `<div class="card" style="margin-top:16px"><div class="card-h"><h2>Connect an SBC to this server</h2></div><div class="card-b"><ol class="steps">
      <li><div>Open the SBC's web page and go to <b>Troubleshoot → Logging</b>.</div></li>
      <li><div><b>Syslog Settings</b>: enable syslog, server ${addrs.map(copyChip).join(" or ")}, port ${copyChip(String(s.settings.syslogPort))}, debug level <b>Detailed</b>. This gives the SIP messages and the call flow.</div></li>
      <li><div><b>Debug Recording</b>: destination ${addrs.map(copyChip).join(" or ")}, port ${copyChip(String(s.settings.drPort))}. Then add a <b>Logging Filters</b> row: type <b>Any</b> (or one IP group), destination <b>Debug Recording</b>, log type <b>Signaling &amp; Media</b>, mode <b>Enable</b>. This adds the audio.</div></li>
      <li><div>Allow UDP ${s.settings.syslogPort} and ${s.settings.drPort} from the SBC to this server on any firewall in between. ${s.firewall?.rule === true ? '<span class="badge ok">Windows Firewall rule present</span>' : s.firewall?.rule === false ? '<span class="badge warn">Windows Firewall rule missing</span>' : ""}</div></li>
      <li><div>Press <b>Start capture</b>, make the call, then <b>Stop</b>. Afterwards turn the logging filter off: recording costs the SBC CPU.</div></li>
    </ol><p class="muted" style="margin:14px 0 0">Menu names are from AudioCodes 7.20A and can differ slightly by firmware.</p></div></div>`;
  const editor = `<div class="card" style="margin-top:16px"><div class="card-h"><h2>SBC list</h2><span class="muted">Name, the address it sends logs from, and its web page</span></div><div class="card-b">
      <div id="sbcRows">${sbcEdit.map((e, i) => `<div class="sbc-edit" data-row="${i}">
        <input type="text" data-k="name" placeholder="Name, e.g. London SBC" value="${esc(e.name)}" aria-label="Name"><input type="text" data-k="ip" placeholder="IP, e.g. 192.0.2.10" value="${esc(e.ip)}" aria-label="IP address">
        <input type="text" data-k="url" placeholder="Web page, e.g. https://192.0.2.10" value="${esc(e.url)}" aria-label="Web page"><button class="btn icon" data-del="${i}" title="Remove" aria-label="Remove">${ic("trash", 15)}</button></div>`).join("")}</div>
      <div style="display:flex;gap:10px;flex-wrap:wrap;align-items:center;margin-top:6px"><button class="btn" id="sbcAdd">${ic("plus", 14)} Add SBC</button><span class="grow"></span>
      <label class="toggle"><input type="checkbox" id="onlyListed" ${s.settings.onlyListedSbcs ? "checked" : ""}> Ignore senders not in this list</label>
      <button class="btn primary" id="sbcSave">Save</button></div></div></div>`;
  const box = $("sbcBox");
  const editing = box.contains(document.activeElement) && document.activeElement.tagName === "INPUT";
  if (editing && $("sbcStatus")) {
    if (setHTML($("sbcStatus"), top)) box.querySelectorAll("[data-names]").forEach(b => b.onclick = () => readNames(b));
    return;
  }
  const book = listed.map(addressCard).join("");
  if (setHTML(box, `<div id="sbcStatus">${top}</div>${book}${how}${editor}`)) {
    box.querySelectorAll("#sbcRows input").forEach(inp => inp.oninput = () => { sbcEdit[+inp.closest("[data-row]").dataset.row][inp.dataset.k] = inp.value; });
    wireLogins(box);
    box.querySelectorAll("[data-names]").forEach(b => b.onclick = () => readNames(b));
    box.querySelectorAll("[data-backup]").forEach(b => b.onclick = () => loadBackupNames(b));
    box.querySelectorAll("[data-ab-add]").forEach(b => b.onclick = () => saveLabel(b));
    box.querySelectorAll("[data-ab-template]").forEach(b => b.onclick = () => downloadText("address-book-template.csv", "Name,Address,Note\r\nDealerboard,192.0.2.58,PBX\r\nCarrier,192.0.2.10,ITSP\r\n"));
    box.querySelectorAll("[data-ab-file]").forEach(inp => inp.onchange = () => importLabels(inp));
    box.querySelectorAll("[data-label-del]").forEach(b => b.onclick = () => deleteLabel(b));
    const redraw = () => { box.__h = null; renderSbcs(status); };
    box.querySelectorAll("[data-del]").forEach(b => b.onclick = () => { sbcEdit.splice(+b.dataset.del, 1); redraw(); });
    $("sbcAdd").onclick = () => { sbcEdit.push({name: "", ip: "", url: ""}); redraw(); box.querySelector("#sbcRows .sbc-edit:last-child input")?.focus(); };
    box.querySelectorAll("[data-addip]").forEach(b => b.onclick = () => { sbcEdit.push({name: "", ip: b.dataset.addip, url: "https://" + b.dataset.addip}); redraw(); toast("Added below: give it a name, then Save"); });
    $("sbcSave").onclick = async () => {
      try { await api("/api/settings", {sbcs: sbcEdit, onlyListedSbcs: $("onlyListed").checked}); toast("SBC list saved"); sbcEdit = null; box.__h = null; refresh(); }
      catch (e) { toast(e.message, true); }
    };
  }
}

/* ------------------------------------------------------------ settings */
// Cloud drives can report absurd sizes (8 EB): say so rather than print the number.
const fmtGb = g => g == null ? "–" : g > 1e6 ? "no limit reported" : g >= 1024 ? (g / 1024).toFixed(1) + " TB" : g + " GB";
const fmtBytes = b => b >= 1073741824 ? (b / 1073741824).toFixed(1) + " GB" : b >= 1048576 ? (b / 1048576).toFixed(1) + " MB" : b >= 1024 ? Math.round(b / 1024) + " KB" : b + " B";

/* Folder picker: the server's drives, then its folders, one level at a time. Resolves with a path, or null. */
function pickFolder(start, what) {
  return new Promise(resolve => {
    const dlg = document.createElement("dialog");
    dlg.className = "fp";
    dlg.setAttribute("aria-label", "Choose a folder");
    document.body.appendChild(dlg);
    let current = "";
    const close = v => { dlg.close(); dlg.remove(); resolve(v); };
    const crumbs = p => {
      if (!p) return `<span class="muted" style="font-family:var(--font)">This server's drives</span>`;
      const unc = p.startsWith("\\\\");
      const parts = p.replace(/^\\\\/, "").split(/[\\/]+/).filter(Boolean);
      const out = [`<button type="button" data-go-path="">Drives</button>`];
      let acc = "";
      parts.forEach((part, i) => {
        if (unc) { acc = i === 0 ? "\\\\" + part : acc + "\\" + part; if (i === 0) return; }   // \server alone cannot be listed
        else acc = i === 0 ? part + "\\" : acc.replace(/\\$/, "") + "\\" + part;
        out.push(`<span class="muted">›</span><button type="button" data-go-path="${esc(acc)}">${esc(unc && i === 1 ? "\\\\" + parts[0] + "\\" + part : part)}</button>`);
      });
      return out.join("");
    };
    const draw = async path => {
      let d;
      try { d = await api("/api/storage/browse" + (path ? "?path=" + encodeURIComponent(path) : "")); }
      catch (e) { toast(e.message, true); return; }
      current = d.path || "";
      const items = !current
        ? d.drives.map(v => `<button type="button" class="fp-item fp-drive" data-go-path="${esc(v.path)}">${ic("server", 16)}<span class="grow"><b>${esc(v.path)}</b> ${esc(v.label || "")} <span class="muted">${esc(v.type)}</span></span>
            <span class="meter" role="img" aria-label="${fmtGb(v.freeGb)} free of ${fmtGb(v.totalGb)}"><i style="width:${v.totalGb ? Math.round((1 - v.freeGb / v.totalGb) * 100) : 0}%"></i></span><span class="muted" style="width:170px;text-align:right">${fmtGb(v.freeGb)} free of ${fmtGb(v.totalGb)}</span></button>`).join("")
          || '<div class="card-b muted">No drives are ready.</div>'
        : (d.parent ? `<button type="button" class="fp-item" data-go-path="${esc(d.parent)}">${ic("out", 15)}<span class="grow muted">Up one level</span></button>` : `<button type="button" class="fp-item" data-go-path="">${ic("out", 15)}<span class="grow muted">All drives</span></button>`)
          + (d.error ? `<div class="card-b"><div class="alert warn"><span class="ic">${ic("alert", 18)}</span><div>${esc(d.error)}</div></div></div>` : "")
          + d.folders.map(f => `<button type="button" class="fp-item" data-go-path="${esc(f.path)}">${ic("file", 15)}<span class="grow">${esc(f.name)}</span>${ic("chev", 14)}</button>`).join("")
          + (!d.error && !d.folders.length ? '<div class="card-b muted">No folders here.</div>' : "");
      dlg.innerHTML = `<div class="fp-h">${ic("file", 18)}<h2>Choose a folder: ${esc(what)}</h2><span class="grow"></span><button type="button" class="btn ghost icon sm" data-fp="cancel" aria-label="Close">${ic("x", 15)}</button></div>
        <div class="fp-path">${crumbs(current)}</div>
        <div class="fp-list">${items}</div>
        <div class="fp-f">${current ? `<input type="text" id="fpNew" placeholder="New folder name" style="width:190px" aria-label="New folder name"><button type="button" class="btn sm" data-fp="mkdir">${ic("plus", 13)} New folder</button>` : ""}
          <span class="grow"></span><button type="button" class="btn" data-fp="cancel">Cancel</button><button type="button" class="btn primary" data-fp="use" ${current ? "" : "disabled"}>${ic("check", 13)} Use this folder</button></div>`;
      paintIcons(dlg);
      dlg.querySelector(".fp-item, [data-fp=use]:not([disabled]), [data-fp=cancel]")?.focus();
    };
    dlg.addEventListener("click", async e => {
      const go = e.target.closest("[data-go-path]");
      if (go) { draw(go.dataset.goPath); return; }
      const act = e.target.closest("[data-fp]")?.dataset.fp;
      if (act === "cancel") close(null);
      else if (act === "use") close(current);
      else if (act === "mkdir") {
        const name = dlg.querySelector("#fpNew").value;
        try { const r = await api("/api/storage/mkdir", {parent: current, name}); toast(r.message); draw(r.path); }
        catch (err) { toast(err.message, true); }
      }
    });
    dlg.addEventListener("cancel", e => { e.preventDefault(); close(null); });
    dlg.showModal();
    // Open at the folder in use when it exists; otherwise at the drive list.
    draw(start).catch(() => draw(""));
  });
}

/* Last check before a plan touches an SBC: names the SBC, the commands and what happens. Resolves true to go ahead.
   Cancel has the focus, so Enter or Escape never applies by accident. */
function confirmChange(p) {
  return new Promise(resolve => {
    const save = p.kind === "save";
    const dlg = document.createElement("dialog");
    dlg.className = "fp";
    dlg.setAttribute("aria-labelledby", "askTitle");
    dlg.innerHTML = `<div class="fp-h"><span style="color:var(--warn)">${ic("alert", 18)}</span><h2 id="askTitle">${save ? "Save to flash" : "Apply change"} on ${esc(p.sbcName)}?</h2></div>
      <div class="ask-b">
        <div class="kv"><span class="k">SBC</span><b>${esc(p.sbcName)}</b><span class="k">Reason</span><span>${esc(p.reason)}</span>${save ? "" : `<span class="k">Commands</span><span>${p.commands.length}</span>`}</div>
        ${save ? "" : `<pre class="sip">${esc(p.commands.join("\n"))}</pre>`}
        <p class="t2">${save ? "The running configuration is written to flash, so it survives a reboot." : "A backup of the running configuration is taken first. The change is live straight away but not saved to flash: a reboot undoes it unless you save."}</p>
      </div>
      <div class="fp-f"><span class="grow"></span><button type="button" class="btn" data-ask="no">Cancel</button><button type="button" class="btn primary" data-ask="yes">${ic("check", 13)} ${save ? "Save to flash" : "Apply to " + esc(p.sbcName)}</button></div>`;
    document.body.appendChild(dlg);
    const close = v => { dlg.close(); dlg.remove(); resolve(v); };
    dlg.addEventListener("click", e => { const a = e.target.closest("[data-ask]")?.dataset.ask; if (a) close(a === "yes"); });
    dlg.addEventListener("cancel", e => { e.preventDefault(); close(false); });
    dlg.showModal();
    dlg.querySelector("[data-ask=no]").focus();
  });
}

function storageCard(st) {
  const c = st.settings, row = k => st.rows.find(r => r.key === k) || {};
  const num = (id, v, min, max, w = 80) => `<input type="number" id="${id}" min="${min}" max="${max}" value="${v}" style="width:${w}px">`;
  const folder = (id, v, ph) => `<div style="display:flex;gap:6px"><input type="text" id="${id}" value="${esc(v)}" placeholder="${esc(ph)}" style="flex:1;min-width:0;font-family:var(--mono);font-size:12.5px" aria-label="Folder"><button class="btn sm" type="button" data-browse="${id}" title="Pick a folder on this server">${ic("file", 13)} Browse</button></div>`;
  const used = r => r.bytes != null ? `${fmtBytes(r.bytes)}<div class="muted" style="font-size:11.5px">${fmtN(r.files)} file${r.files === 1 ? "" : "s"}</div>` : "";
  const free = r => r.freeGb == null ? "–" : `<span style="${r.freeGb < 1 ? "color:var(--bad);font-weight:700" : r.freeGb < 5 ? "color:var(--warn);font-weight:700" : ""}">${fmtGb(r.freeGb)}</span>`;
  const tr = (k, name, folderCell, keepCell) => { const r = row(k); return `<tr><td style="white-space:normal;min-width:170px"><b>${name}</b><div class="muted" style="font-size:12px">${esc(r.note || "")}</div></td>
      <td style="min-width:260px">${folderCell}<div class="muted mono" style="font-size:11px;margin-top:3px" title="Full path">${esc(r.folder || "")}</div></td>
      <td style="white-space:normal">${keepCell}</td><td class="r">${used(r)}</td><td class="r">${free(r)}</td></tr>`; };
  return `<div class="card" style="margin-top:16px"><div class="card-h">${ic("file", 16)}<h2>Data and storage</h2><span class="muted">where each kind of data goes and how long it is kept</span><span class="grow"></span>
      <button class="btn sm" id="stClean" title="Run the clean-up now (it also runs every hour)">${ic("trash", 13)} Clean up now</button><button class="btn sm primary" id="stSave">${ic("check", 13)} Save</button></div>
    <div class="scroll"><table class="tbl"><thead><tr><th>Data</th><th>Folder</th><th>Keep</th><th class="r">Used</th><th class="r">Free on drive</th></tr></thead><tbody>
      ${tr("captures", "Packet captures", folder("stTrace", c.traceFolder, "traces"), `${num("stRetDays", c.retentionDays, 0, 3650)} days <span class="muted">(0 = keep)</span><br>at most ${num("stRetMb", c.retentionMaxMb, 0, 1000000, 100)} MB <span class="muted">(0 = no cap)</span>`)}
      ${tr("logs", "SBC logs (Splunk)", folder("stLog", c.logFolder, "splunk"), `${num("stLogDays", c.logRetentionDays, 1, 3650)} days<br>at most ${num("stLogTotal", c.logMaxTotalMb, 0, 10000000, 100)} MB <span class="muted">(0 = no cap)</span><br>new file every ${num("stLogMax", c.logMaxMb, 1, 100, 60)} MB`)}
      ${tr("backups", "SBC config backups", folder("stBackup", c.backupFolder, "sbc-backups"), `${num("stBackupDays", c.backupRetentionDays, 0, 3650)} days <span class="muted">(0 = keep)</span><div class="muted" style="font-size:12px">The newest 10 per SBC, and any an upgrade or change uses, are always kept.</div>`)}
      ${tr("programlog", "MediantTrace's own log", folder("stProg", c.programLogFolder, "(the program's folder)"), `${num("stProgMb", c.programLogMaxMb, 1, 1000, 70)} MB, then a new file <span class="muted">(3 older kept)</span>`)}
      ${tr("history", "History", `<span class="muted">next to the program, with the settings</span>`, `${num("stHist", c.historyDays, 0, 3650)} days <span class="muted">(0 = keep)</span><div class="muted" style="font-size:12px">Call test results, line test runs, ready checks, wire changes and uptime.</div>`)}
    </tbody></table></div>
    <div class="card-b help" style="margin:0;border-top:1px solid var(--border)"><b>Browse</b> picks a folder on any of this server's drives. A folder can also be typed: a full path (another drive such as <span class="mono">D:\\MediantTrace\\captures</span>, or a share such as <span class="mono">\\\\server\\logs\\mediant</span>) or a name relative to the program's folder <span class="mono">${esc(st.programFolder)}</span>. A new folder is used straight away; files already written stay where they are. When MediantTrace runs as a service, the service account needs write access to the folder. The clean-up runs every hour.</div></div>`;
}

async function renderSettings(s) {
  const box = $("setBox");
  if (box.contains(document.activeElement) && document.activeElement.tagName === "INPUT") return;
  let log = [], st = null;
  try { [log, st] = await Promise.all([api("/api/log"), api("/api/storage")]); } catch {}
  const fw = s.firewall || {}, a = s.alerts || {};
  const people = s.settings.accounts || [];
  const personRows = people.map(u => `<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;padding:8px 0;border-bottom:1px solid var(--border)"><b>${esc(u)}</b>${people.length > 1 ? `<button class="btn sm" data-remove-user="${esc(u)}">Remove</button>` : ""}</div>`).join("");
  const html = `<div class="card"><div class="card-h"><h2>People</h2><span class="muted">each person signs in with their own password</span></div><div class="card-b"><div class="form-grid">
      <div class="full">${personRows || `<span class="muted">No one can sign in yet.</span>`}</div>
      <div><label for="newUser">Username</label><input id="newUser" autocomplete="off"></div>
      <div><label for="newPass">Password</label><input id="newPass" type="password" autocomplete="new-password"></div>
      <div class="full" style="display:flex;gap:8px;align-items:center;flex-wrap:wrap"><button class="btn primary" id="addPerson">Add person</button><span class="help" style="margin:0">The first page asks for a username and password. Add everyone who should open MediantTrace. Passwords are stored encrypted. Removing someone signs them out.</span></div>
    </div></div></div>
    ${st ? storageCard(st) : ""}
    <div class="grid-cards" style="grid-template-columns:repeat(auto-fill,minmax(380px,1fr));margin-top:16px">
    <div class="card"><div class="card-h"><h2>Capture</h2></div><div class="card-b form">
      <div><label for="autoStop">Stop a forgotten packet capture after (minutes, 0 = never)</label><input type="number" id="autoStop" min="0" max="1440" value="${s.settings.autoStopMinutes}"></div>
      <div><button class="btn primary" id="saveCap">Save</button></div>
      <div class="help">SIP messages, call records and SBC log lines are written all the time. Each day gets a folder under <span class="mono">${esc(s.settings.logFolder || "")}</span>, and a file is closed at the size above so a Splunk forwarder can send it. Packet captures, for Wireshark, stay in <span class="mono">${esc(s.settings.traceFolder)}</span>. Folders and how long each kind of data is kept are under <b>Data and storage</b> below.</div></div></div>
    <div class="card"><div class="card-h"><h2>Network</h2></div><div class="card-b form">
      <div><label>This server</label>${(s.serverAddresses || []).map(copyChip).join(" ") || "—"}</div>
      <div><label>Listening now</label>${copyChip("UDP " + (s.settings.listening?.syslog ?? s.settings.syslogPort))} syslog &nbsp; ${copyChip("UDP " + (s.settings.listening?.dr ?? s.settings.drPort))} Debug Recording &nbsp; ${copyChip("TCP " + (s.settings.listening?.web ?? s.settings.webPort))} this page</div>
      <div><label for="syslogPort">Syslog port</label><input type="number" id="syslogPort" min="1" max="65535" value="${s.settings.syslogPort}"></div>
      <div><label for="drPort">Debug Recording port</label><input type="number" id="drPort" min="1" max="65535" value="${s.settings.drPort}"></div>
      <div><label for="webPort">Web page port</label><input type="number" id="webPort" min="1" max="65535" value="${s.settings.webPort}"></div>
      <div><button class="btn primary" id="savePorts">Save ports</button></div>
      <div class="help">Listening now on UDP ${s.settings.listening.syslog || "–"} (syslog) and ${s.settings.listening.dr || "–"} (Debug Recording). Syslog and Debug Recording ports change as soon as you save, no restart needed${s.settings.portsPending ? `; <b>restart MediantTrace to open the page on port ${s.settings.webPort}</b>` : "; the web page port needs a restart"}. A port another program holds (such as another copy of MediantTrace) cannot be used.</div>
      <div><label>Windows Firewall</label>${fw.rule === true ? '<span class="badge ok">Rule present</span>' : fw.rule === false ? `<span class="badge warn">No rule</span><div style="margin-top:8px">Run as administrator:</div><div style="margin-top:4px">${copyChip(fw.command || "")}</div>` : '<span class="badge">Unknown</span>'}</div>
      <div><label>This page</label>${(s.settings.accounts || []).length ? `Sign-in required (${s.settings.accounts.length} ${(s.settings.accounts.length === 1 ? "person" : "people")})` : s.settings.webOnNetwork ? `Open to the network <span class="badge warn">no password</span>` : "This server only (http://localhost). The first page asks for a username and password."}</div>
    </div></div></div>
    ${syslogCard(s)}
    <div class="card" style="margin-top:16px"><div class="card-h"><h2>Test alerts</h2><span class="muted">when a scheduled test starts failing</span></div><div class="card-b"><div class="form-grid">
      <div class="full"><label>Teams or webhook address</label><input id="alertHook" placeholder="${a.webhookSet ? "saved — paste a new https address to replace it" : "https://…"}" autocomplete="off" spellcheck="false"></div>
      <div class="full help">A scheduled test sends one message when it fails, and another when it passes again. A test run by hand does not send anything. Leave the address blank to keep the saved one.${a.webhookSet ? ` <label class="chk" style="display:inline-flex;margin-left:8px"><input type="checkbox" id="alertHookClear"> Remove the saved address</label>` : ""}</div>
      <div><label>Email to</label><input id="alertTo" value="${esc(a.emailTo || "")}" placeholder="you@example.com" autocomplete="off"></div>
      <div><label>From</label><input id="alertFrom" value="${esc(a.emailFrom || "")}" placeholder="mediantrace@example.com" autocomplete="off"></div>
      <div><label>Mail server</label><input id="alertHost" value="${esc(a.smtpHost || "")}" placeholder="smtp.example.com" autocomplete="off"></div>
      <div><label>Port</label><input id="alertPort" value="${a.smtpPort || 587}" inputmode="numeric"></div>
      <div><label>Username</label><input id="alertUser" value="${esc(a.smtpUser || "")}" autocomplete="off"></div>
      <div><label>Mail password</label><input id="alertPass" type="password" placeholder="${a.smtpPasswordSet ? "unchanged" : ""}" autocomplete="new-password"></div>
      <div class="full" style="display:flex;gap:8px;align-items:center;flex-wrap:wrap"><button class="btn primary" id="saveAlerts">Save alerts</button><span class="help" style="margin:0">The address and the mail password are stored encrypted on this PC.</span></div>
    </div></div></div>
    <div class="card" style="margin-top:16px"><div class="card-h"><h2>MCP server</h2><span class="muted">for Claude Code and other AI clients</span></div><div class="card-b form">
      <div><label>Endpoint</label>${copyChip(s.mcp?.url || "")}</div>
      <div><label>Token</label><span class="copy"><span id="mcpTok">••••••••••••••••</span><button data-copy="${esc(s.mcp?.token || "")}" title="Copy token" aria-label="Copy token">${ic("copy", 13)}</button></span></div>
      <div><label>Add to Claude Code</label>${copyChip(`claude mcp add --transport http mediantrace ${s.mcp?.url || ""} --header "Authorization: Bearer ${s.mcp?.token || ""}"`)}</div>
      <div class="help">Tools: list_sbcs, capture, list_calls, triage_call, get_sip_messages, search_sbc_log, call tests, sbc_show, sbc_backup, sbc_plan_change / sbc_plan_save (a person approves on the Changes page), sbc_apply_change, list_changes. Keep the token private: it gives access to everything above.</div>
    </div></div>
    <div class="card" style="margin-top:16px"><div class="card-h"><h2>Program log</h2></div><pre class="sip" style="border:0;border-radius:0 0 12px 12px;max-height:340px;margin:0">${esc(log.slice().reverse().join("\n")) || "—"}</pre></div>`;
  if (setHTML(box, html))
    $("saveCap").onclick = async () => { try { await api("/api/settings", {autoStopMinutes: parseInt($("autoStop").value || "0", 10)}); toast("Saved"); } catch (e) { toast(e.message, true); } };
    const sv = $("stSave"); if (sv) sv.onclick = async () => {
      const v = id => $(id).value.trim(), n = (id, d) => { const x = parseInt($(id).value, 10); return Number.isFinite(x) ? x : d; };
      try {
        const r = await api("/api/settings", {traceFolder: v("stTrace"), retentionDays: n("stRetDays", 14), retentionMaxMb: n("stRetMb", 2048),
          logFolder: v("stLog"), logRetentionDays: Math.max(1, n("stLogDays", 14)), logMaxMb: Math.max(1, n("stLogMax", 10)), logMaxTotalMb: n("stLogTotal", 0),
          backupFolder: v("stBackup"), backupRetentionDays: n("stBackupDays", 0), programLogFolder: v("stProg"), programLogMaxMb: Math.max(1, n("stProgMb", 10)), historyDays: n("stHist", 90)});
        toast(r.message || "Saved"); box.__h = null; refresh();
      } catch (e) { toast(e.message, true); }
    };
    box.querySelectorAll("[data-browse]").forEach(b => b.onclick = async () => {
      const input = $(b.dataset.browse);
      const row = st?.rows.find(r => ({stTrace: "captures", stLog: "logs", stBackup: "backups", stProg: "programlog"})[b.dataset.browse] === r.key);
      const picked = await pickFolder(row?.folder || "", b.closest("tr")?.querySelector("b")?.textContent || "Folder");
      if (picked) { input.value = picked; input.dispatchEvent(new Event("input")); toast("Folder chosen: press Save to use it"); }
    });
    const cu = $("stClean"); if (cu) cu.onclick = async () => { try { const r = await api("/api/storage/cleanup", {}); toast(r.message); box.__h = null; refresh(); } catch (e) { toast(e.message, true); } };
    $("addPerson").onclick = async () => {
      try { const r = await api("/api/settings", {webUser: $("newUser").value.trim(), webPassword: $("newPass").value}); toast(r.message || "Sign-in added"); refresh(); }
      catch (e) { toast(e.message, true); }
    };
    box.querySelectorAll("[data-remove-user]").forEach(b => b.onclick = async () => {
      try { const r = await api("/api/settings", {removeWebUser: b.dataset.removeUser}); toast(r.message || "Removed"); refresh(); }
      catch (e) { toast(e.message, true); }
    });
    $("savePorts").onclick = async () => {
      const n = id => parseInt($(id).value, 10);
      try { const r = await api("/api/settings", {syslogPort: n("syslogPort"), debugRecordingPort: n("drPort"), webPort: n("webPort")}); toast(r.message, /but /.test(r.message)); refresh(); }
      catch (e) { toast(e.message, true); }
    };
    bindSyslogCard();
    $("saveAlerts").onclick = async () => {
      const body = {
        alertEmailTo: $("alertTo").value.trim(),
        alertEmailFrom: $("alertFrom").value.trim(),
        alertSmtpHost: $("alertHost").value.trim(),
        alertSmtpPort: parseInt($("alertPort").value || "587", 10),
        alertSmtpUser: $("alertUser").value.trim(),
      };
      const hook = $("alertHook").value.trim();
      if ($("alertHookClear")?.checked) body.alertWebhook = "";
      else if (hook) body.alertWebhook = hook;
      const pass = $("alertPass").value;
      if (pass) body.alertSmtpPassword = pass;
      try { await api("/api/settings", body); toast("Alerts saved"); refresh(); }
      catch (e) { toast(e.message, true); }
    };
}

/* ------------------------------------------------------------ settings: syslog */
function syslogCard(s) {
  const st = s.settings, lis = st.listening || {};
  const chk = (id, on, label, help) => `<label class="chk" style="display:flex;gap:8px;align-items:flex-start"><input type="checkbox" id="${id}" ${on ? "checked" : ""}><span>${label}${help ? `<span class="muted" style="display:block;font-size:12px">${help}</span>` : ""}</span></label>`;
  const seq = (s.sbcs || []).filter(x => x.sequence).map(x => `<tr><td>${esc(x.listed ? x.name : x.ip)}</td><td class="r">${fmtN(x.sequence.received)}</td><td class="r" style="color:${x.sequence.missing ? "var(--bad)" : "inherit"}">${fmtN(x.sequence.missing)}</td><td class="r">${fmtN(x.sequence.outOfOrder)}</td><td class="r">${fmtN(x.sequence.restarts)}</td></tr>`).join("");
  return `<div class="card" style="margin-top:16px"><div class="card-h"><h2>Syslog</h2><span class="muted">receiving, ordering, filtering and writing the SBC's syslog</span></div><div class="card-b"><div class="form-grid">
      <div><label for="slTcp">Syslog over TCP, port (0 = off)</label><input type="number" id="slTcp" min="0" max="65535" value="${st.syslogTcpPort || 0}"></div>
      <div><label for="slTls">Syslog over TLS, port (0 = off; usually 6514)</label><input type="number" id="slTls" min="0" max="65535" value="${st.syslogTlsPort || 0}"></div>
      <div><label for="slPfx">TLS certificate (.pfx on this server; empty = one MediantTrace makes)</label><input id="slPfx" value="${esc(st.syslogTlsPfx || "")}" spellcheck="false" placeholder="self-signed"></div>
      <div><label for="slPfxPass">Certificate password</label><input id="slPfxPass" type="password" autocomplete="new-password" placeholder="unchanged"></div>
      <div class="full help">Listening now: UDP ${lis.syslog || "–"}${lis.syslogTcp ? `, TCP ${lis.syslogTcp}` : ""}${lis.syslogTls ? `, TLS ${lis.syslogTls}` : ""}${s.settings.syslogStreamConnections ? ` (${s.settings.syslogStreamConnections} connected)` : ""}. Messages over TCP and TLS are treated as if they had come by UDP, and saved in packet captures as UDP.
        ${lis.syslogTls ? `Certificate ${st.syslogTlsThumbprint ? `<span class="mono">${esc(st.syslogTlsThumbprint)}</span>` : ""} <a class="btn sm" href="/api/syslog-tls.cer">${ic("down", 13)} Certificate for the SBC</a> (load it on the SBC as a trusted root for the syslog TLS context).` : ""}</div>
      <div class="full" style="display:flex;gap:8px"><button class="btn primary" id="saveSlNet">Save TCP / TLS</button></div>

      <div class="full">${chk("slReorder", st.syslogReorder, "Put syslog back in order and count missing messages", "The SBC numbers each message ([S=n]). Over UDP some arrive out of order or not at all: a message that arrives early waits up to 0.4 s for the ones before it, and each gap shows as a warning line.")}</div>
      <div><label for="slBuf">Syslog lines kept for the page</label><input type="number" id="slBuf" min="1000" max="1000000" step="1000" value="${st.syslogBufferLines}"></div>
      <div><label>&nbsp;</label><button class="btn primary" id="saveSlView">Save</button></div>
      ${seq ? `<div class="full"><table class="tbl"><thead><tr><th>SBC</th><th class="r">Received</th><th class="r">Missing</th><th class="r">Out of order</th><th class="r">Restarts</th></tr></thead><tbody>${seq}</tbody></table></div>` : ""}

      <div><label for="slIgnIp">IP filters: drop syslog from these senders (one per line)</label><textarea id="slIgnIp" rows="3" spellcheck="false">${esc((st.ignoreSyslogFrom || []).join("\n"))}</textarea></div>
      <div><label for="slIgnRe">Content filters: drop lines that match (regular expressions, one per line)</label><textarea id="slIgnRe" rows="3" spellcheck="false">${esc((st.ignoreSyslogPatterns || []).join("\n"))}</textarea></div>
      <div class="full" style="display:flex;gap:8px;align-items:center;flex-wrap:wrap"><button class="btn primary" id="saveSlFilter">Save filters</button><span class="help" style="margin:0">Applied as syslog arrives: dropped lines are not shown, logged or used for calls (packet captures still keep them). The display filter on the Syslog page only hides lines.</span></div>

      <div class="full">${chk("slWl", st.writeLog, "Write log: every syslog line to text files", st.writeLog && st.writeLogCurrent ? `Writing to <span class="mono">${esc(st.writeLogCurrent)}</span>, ${fmtN(st.writeLogLines)} lines so far${st.writeLogError ? ` · <b style="color:var(--bad)">${esc(st.writeLogError)}</b>` : ""}` : "Plain text, one line per message, as Syslog Viewer writes it. Opens in Notepad, Syslog Viewer and MediantTrace. Stays on after a restart.")}</div>
      <div><label for="slWlDir">Folder</label><div style="display:flex;gap:6px"><input id="slWlDir" value="${esc(st.writeLogFolder)}" spellcheck="false" style="flex:1"><button class="btn sm" id="slWlBrowse" type="button">Browse</button></div></div>
      <div><label for="slWlFile">File name</label><input id="slWlFile" value="${esc(st.writeLogFile)}" spellcheck="false"></div>
      <div><label for="slWlMb">Log file size (MB, 0 = never rotate)</label><input type="number" id="slWlMb" min="0" max="10000" value="${st.writeLogFileMb}"></div>
      <div><label for="slWlCount">Log file count (0 = keep all)</label><input type="number" id="slWlCount" min="0" max="100000" value="${st.writeLogFileCount}"></div>
      <div class="full" style="display:grid;gap:6px">${chk("slWlPer", st.writeLogPerDevice, "A separate log file for each SBC")}${chk("slWlDirPer", st.writeLogFolderPerDevice, "A separate folder for each SBC")}${chk("slWlNames", st.writeLogUseNames, "Use SBC names (not addresses) in those file and folder names")}</div>
      <div class="full" style="display:flex;gap:8px;align-items:center;flex-wrap:wrap"><button class="btn primary" id="saveSlWrite">Save write log</button><span class="help" style="margin:0">The current file always has the name above. At the size limit it becomes ${esc((st.writeLogFile || "syslog.txt").replace(/(\.[^.]*)?$/, "_0001$1"))}, then _0002 and so on (highest = newest); the oldest go beyond the count.</span></div>
    </div></div></div>`;
}
function bindSyslogCard() {
  const lines = id => $(id).value.split(/\r?\n/).map(x => x.trim()).filter(Boolean);
  const save = async body => { try { const r = await api("/api/settings", body); toast(r.message, /but /.test(r.message)); $("setBox").__h = null; refresh(); } catch (e) { toast(e.message, true); } };
  const n = (id, d) => { const x = parseInt($(id).value, 10); return Number.isFinite(x) ? x : d; };
  $("saveSlNet").onclick = () => { const b = {syslogTcpPort: n("slTcp", 0), syslogTlsPort: n("slTls", 0), syslogTlsPfx: $("slPfx").value.trim()}; if ($("slPfxPass").value) b.syslogTlsPfxPassword = $("slPfxPass").value; save(b); };
  $("saveSlView").onclick = () => save({syslogReorder: $("slReorder").checked, syslogBufferLines: n("slBuf", 50000)});
  $("saveSlFilter").onclick = () => save({ignoreSyslogFrom: lines("slIgnIp"), ignoreSyslogPatterns: lines("slIgnRe")});
  $("saveSlWrite").onclick = () => save({writeLog: $("slWl").checked, writeLogFolder: $("slWlDir").value.trim(), writeLogFile: $("slWlFile").value.trim(),
    writeLogFileMb: n("slWlMb", 10), writeLogFileCount: n("slWlCount", 10), writeLogPerDevice: $("slWlPer").checked, writeLogFolderPerDevice: $("slWlDirPer").checked, writeLogUseNames: $("slWlNames").checked});
  $("slWlBrowse").onclick = async () => { const picked = await pickFolder(status?.settings?.writeLogFolderFull || "", "Write log folder"); if (picked) { $("slWlDir").value = picked; toast("Folder chosen: press Save write log to use it"); } };
}

/* ------------------------------------------------------------ quality helpers */
const mosGrade = m => m >= 4.0 ? "good" : m >= 3.6 ? "fair" : m >= 3.1 ? "poor" : "bad";
const mosLabel = {good:"Good", fair:"Fair", poor:"Poor", bad:"Bad"};
const mosBadge = m => m == null ? "" : `<span class="mos ${mosGrade(m)}" title="Estimated MOS (E-model): ${mosLabel[mosGrade(m)]}">${m.toFixed(1)}</span>`;
const outcomeBadge = o => ({passed:'<span class="badge ok">Passed</span>', warning:'<span class="badge warn">Warning</span>', failed:'<span class="badge bad">Failed</span>', running:'<span class="badge accent">Running</span>'}[o] || `<span class="badge">${esc(o)}</span>`);
const pct = (a, b) => b ? Math.round(a / b * 1000) / 10 : 0;

/* ------------------------------------------------------------ tooltip */
const tipEl = document.createElement("div"); tipEl.className = "tip"; tipEl.setAttribute("role", "status"); document.body.appendChild(tipEl);
function showTip(x, y, title, rows) {
  tipEl.replaceChildren();
  const t = document.createElement("div"); t.className = "t"; t.textContent = title; tipEl.appendChild(t);
  rows.forEach(r => {
    const row = document.createElement("div"); row.className = "r";
    const k = document.createElement("span"); k.className = "k";
    const key = document.createElement("i"); key.style.background = r.color; k.appendChild(key);
    k.appendChild(document.createTextNode(r.label));
    const v = document.createElement("b"); v.textContent = r.value;
    row.append(k, v); tipEl.appendChild(row);
  });
  tipEl.style.display = "block";
  const w = tipEl.offsetWidth, h = tipEl.offsetHeight;
  tipEl.style.left = Math.min(window.innerWidth - w - 8, x + 14) + "px";
  tipEl.style.top = Math.max(8, Math.min(window.innerHeight - h - 8, y - h - 10)) + "px";
}
function hideTip() { tipEl.style.display = "none"; }

/* ------------------------------------------------------------ charts (inline SVG, no libraries) */
const CH = {};
function niceMax(v) { if (v <= 0) return 1; const p = Math.pow(10, Math.floor(Math.log10(v))); for (const m of [1, 2, 2.5, 5, 10]) if (v <= m * p) return m * p; return 10 * p; }
const fmtTick = v => Math.abs(v) >= 100 ? Math.round(v).toString() : (Math.round(v * 10) / 10).toString();

/* Line chart: o = {id, xs, series:[{name,color,values}], yMin, yMax, h, fmtX, fmtY, refs:[{y,label}], unit} */
function lineChart(o) {
  o.type = "line"; CH[o.id] = o;
  const n = o.xs.length, yMin = o.yMin ?? 0;
  const yMax = o.yMax ?? niceMax(Math.max(0, ...o.series.flatMap(s => s.values.filter(v => v != null))) * 1.1);
  o._yMin = yMin; o._yMax = yMax;
  const X = i => n <= 1 ? 500 : i / (n - 1) * 1000, Y = v => (1 - (v - yMin) / (yMax - yMin)) * 100;
  const ticks = [0, .25, .5, .75, 1].map(f => yMin + f * (yMax - yMin));
  let svg = `<svg class="p" viewBox="0 0 1000 100" preserveAspectRatio="none" aria-hidden="true">`;
  ticks.forEach(t => svg += `<line x1="0" x2="1000" y1="${Y(t).toFixed(2)}" y2="${Y(t).toFixed(2)}" stroke="${t === yMin ? "var(--axis)" : "var(--grid)"}" vector-effect="non-scaling-stroke"/>`);
  o.refs = (o.refs || []).filter(r => r.y >= yMin && r.y <= yMax);
  (o.refs || []).forEach(r => svg += `<line x1="0" x2="1000" y1="${Y(r.y).toFixed(2)}" y2="${Y(r.y).toFixed(2)}" stroke="var(--axis)" stroke-dasharray="5 4" vector-effect="non-scaling-stroke"/>`);
  o.series.forEach(s => {
    let d = "", pen = false;
    s.values.forEach((v, i) => { if (v == null) { pen = false; return; } d += `${pen ? "L" : "M"}${X(i).toFixed(1)} ${Y(v).toFixed(2)} `; pen = true; });
    // a lone point still needs to show: draw it as a short flat segment
    s.values.forEach((v, i) => { if (v != null && (s.values[i - 1] == null) && (s.values[i + 1] == null)) d += `M${(X(i) - 3).toFixed(1)} ${Y(v).toFixed(2)} L${(X(i) + 3).toFixed(1)} ${Y(v).toFixed(2)} `; });
    svg += `<path d="${d}" fill="none" stroke="${s.color}" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" vector-effect="non-scaling-stroke"/>`;
  });
  svg += "</svg>";
  const yl = ticks.map(t => `<div class="yl" style="top:${Y(t)}%">${fmtTick(t)}</div>`).join("");
  const refs = (o.refs || []).map(r => `<div class="ref" style="top:${Y(r.y)}%">${esc(r.label)}</div>`).join("");
  const legend = o.series.length > 1 ? `<div class="legend">${o.series.map(s => `<span><i style="background:${s.color}"></i>${esc(s.name)}</span>`).join("")}</div>` : "";
  const xl = n ? `<div class="xl"><span>${esc(o.fmtX(o.xs[0]))}</span>${n > 2 ? `<span>${esc(o.fmtX(o.xs[Math.floor((n - 1) / 2)]))}</span>` : ""}<span>${esc(o.fmtX(o.xs[n - 1]))}</span></div>` : "";
  return `<div class="chart" data-chart="${o.id}" style="--h:${o.h || 170}px">${legend}<div class="plot" role="img" aria-label="${esc(o.label || "")}">${svg}${yl}${refs}<div class="xh"></div>${o.series.map((s, i) => `<div class="mk" data-k="${i}" style="background:${s.color}"></div>`).join("")}</div>${xl}</div>`;
}

/* Stacked bars: o = {id, xs, stacks:[{name,color,values}], h, fmtX} */
function barChart(o) {
  o.type = "bars"; CH[o.id] = o;
  const n = o.xs.length, h = o.h || 170;
  const totals = o.xs.map((_, i) => o.stacks.reduce((a, s) => a + (s.values[i] || 0), 0));
  const yMax = niceMax(Math.max(1, ...totals));
  const slot = 1000 / Math.max(1, n), bw = Math.max(2, slot * 0.7);
  let svg = `<svg class="p" viewBox="0 0 1000 ${h}" preserveAspectRatio="none" aria-hidden="true">`;
  [0, .25, .5, .75, 1].forEach(f => svg += `<line x1="0" x2="1000" y1="${(h - f * h).toFixed(1)}" y2="${(h - f * h).toFixed(1)}" stroke="${f === 0 ? "var(--axis)" : "var(--grid)"}" vector-effect="non-scaling-stroke"/>`);
  o.xs.forEach((_, i) => {
    let y = h;
    o.stacks.forEach(s => {
      const v = s.values[i] || 0; if (!v) return;
      const hh = v / yMax * h;
      const gap = y < h ? 2 : 0; // 2px surface gap between stacked segments
      svg += `<rect x="${(i * slot + (slot - bw) / 2).toFixed(1)}" y="${(y - hh).toFixed(1)}" width="${bw.toFixed(1)}" height="${Math.max(1, hh - gap).toFixed(1)}" fill="${s.color}"/>`;
      y -= hh;
    });
  });
  svg += "</svg>";
  const yl = [0, .25, .5, .75, 1].map(f => `<div class="yl" style="top:${(1 - f) * 100}%">${fmtTick(f * yMax)}</div>`).join("");
  const legend = `<div class="legend">${o.stacks.map(s => `<span><i class="box" style="background:${s.color}"></i>${esc(s.name)}</span>`).join("")}</div>`;
  const xl = n ? `<div class="xl"><span>${esc(o.fmtX(o.xs[0]))}</span>${n > 2 ? `<span>${esc(o.fmtX(o.xs[Math.floor((n - 1) / 2)]))}</span>` : ""}<span>${esc(o.fmtX(o.xs[n - 1]))}</span></div>` : "";
  return `<div class="chart" data-chart="${o.id}" style="--h:${h}px">${legend}<div class="plot" role="img" aria-label="${esc(o.label || "")}">${svg}${yl}<div class="xh"></div></div>${xl}</div>`;
}

let hoverPlot = null;
function clearHover() { if (hoverPlot) { hoverPlot.querySelectorAll(".xh,.mk").forEach(e => e.style.display = "none"); hoverPlot = null; } hideTip(); }
document.addEventListener("pointermove", e => {
  const plot = e.target.closest ? e.target.closest(".chart .plot") : null;
  if (!plot) { if (hoverPlot) clearHover(); return; }
  const o = CH[plot.closest(".chart").dataset.chart]; if (!o) return;
  if (hoverPlot && hoverPlot !== plot) clearHover();
  hoverPlot = plot;
  const r = plot.getBoundingClientRect(), fx = Math.min(1, Math.max(0, (e.clientX - r.left) / r.width)), n = o.xs.length;
  if (!n) return;
  if (o.type === "line") {
    const i = Math.round(fx * (n - 1)), left = n <= 1 ? 50 : i / (n - 1) * 100;
    const xh = plot.querySelector(".xh"); xh.style.display = "block"; xh.style.left = left + "%";
    const rows = [];
    o.series.forEach((s, k) => {
      const v = s.values[i], mk = plot.querySelector(`.mk[data-k="${k}"]`);
      if (v == null) { mk.style.display = "none"; rows.push({color: s.color, label: s.name, value: "—"}); return; }
      mk.style.display = "block"; mk.style.left = left + "%"; mk.style.top = ((1 - (v - o._yMin) / (o._yMax - o._yMin)) * 100) + "%";
      rows.push({color: s.color, label: s.name, value: o.fmtY(v)});
    });
    showTip(e.clientX, e.clientY, o.fmtX(o.xs[i]), rows);
  } else {
    const i = Math.min(n - 1, Math.floor(fx * n));
    const xh = plot.querySelector(".xh"); xh.style.display = "block"; xh.style.left = ((i + .5) / n * 100) + "%";
    const total = o.stacks.reduce((a, s) => a + (s.values[i] || 0), 0);
    showTip(e.clientX, e.clientY, `${o.fmtX(o.xs[i])} · ${total} call${total === 1 ? "" : "s"}`, o.stacks.slice().reverse().map(s => ({color: s.color, label: s.name, value: String(s.values[i] || 0)})));
  }
});
document.addEventListener("pointerleave", clearHover);

/* Loss and jitter per second in the call view: one measure per chart, the two directions as series. */
function qualityCharts(d, start, span) {
  if (span < 3 || !d.streams.some(s => s.perSecondLost)) return "";
  const xs = Array.from({length: span}, (_, i) => (start + i) * 1000);
  const series = [[false, "SBC → far end", "var(--s1)"], [true, "Far end → SBC", "var(--s2)"]].map(([inb, name, color]) => {
    const ss = d.streams.filter(s => s.inbound === inb);
    const loss = xs.map((_, i) => { let got = 0, lost = 0; ss.forEach(s => { const k = start + i - s.startSecond; if (k >= 0 && k < s.perSecond.length) { got += s.perSecond[k]; lost += s.perSecondLost[k]; } }); return got + lost ? lost / (got + lost) * 100 : null; });
    const jit = xs.map((_, i) => { let m = null; ss.forEach(s => { const k = start + i - s.startSecond; if (k >= 0 && k < s.perSecondJitter.length && s.perSecond[k]) m = Math.max(m ?? 0, s.perSecondJitter[k]); }); return m; });
    return {name, color, ss, loss, jit};
  }).filter(x => x.ss.length);
  if (!series.length) return "";
  const fx = v => tm(v);
  return `<div class="dash" style="margin-top:16px">
    <div class="card span-6"><div class="card-h"><h2>Packet loss</h2><span class="muted">% per second</span></div><div class="card-b">${lineChart({id: "q-loss", label: "Packet loss per second", xs, h: 150, yMax: Math.max(5, niceMax(Math.max(0, ...series.flatMap(s => s.loss.filter(v => v != null))) * 1.2)), fmtX: fx, fmtY: v => v.toFixed(1) + "%", series: series.map(s => ({name: s.name, color: s.color, values: s.loss}))})}</div></div>
    <div class="card span-6"><div class="card-h"><h2>Jitter</h2><span class="muted">ms</span></div><div class="card-b">${lineChart({id: "q-jit", label: "Jitter per second", xs, h: 150, yMax: Math.max(20, niceMax(Math.max(0, ...series.flatMap(s => s.jit.filter(v => v != null))) * 1.2)), refs: [{y: 30, label: "30 ms"}], fmtX: fx, fmtY: v => v.toFixed(1) + " ms", series: series.map(s => ({name: s.name, color: s.color, values: s.jit}))})}</div></div>
  </div>`;
}

/* ------------------------------------------------------------ dashboard */
function passRateChart(results) {
  const done = (results || []).filter(r => r.outcome === "passed" || r.outcome === "warning" || r.outcome === "failed");
  if (done.length < 2) return "";
  const times = done.map(r => +new Date(r.started));
  const span = Math.max(...times) - Math.min(...times);
  const step = span <= 36 * 3600 * 1000 ? 3600 * 1000 : 24 * 3600 * 1000;
  const t0 = Math.floor(Math.min(...times) / step) * step;
  const n = Math.max(1, Math.ceil((Math.max(...times) - t0 + 1) / step));
  const xs = Array.from({length: n}, (_, i) => t0 + i * step);
  const rate = xs.map(t => {
    const inb = done.filter(r => { const ms = +new Date(r.started); return ms >= t && ms < t + step; });
    if (!inb.length) return null;
    return Math.round(inb.filter(r => r.outcome === "passed").length / inb.length * 1000) / 10;
  });
  const hour = step < 24 * 3600 * 1000;
  return lineChart({id: "d-pass", label: "Share of finished tests that passed", xs, h: 160, yMin: 0, yMax: 100, fmtX: v => hour ? tm(v) : new Date(v).toLocaleDateString(), fmtY: v => Math.round(v) + "%", series: [{name: "Passed", color: "var(--good)", values: rate}]});
}

async function renderDashboard() {
  const [all, tests, results, drift, board] = await Promise.all([api("/api/calls"), api("/api/tests").catch(() => null), api("/api/tests/results?limit=400").catch(() => []), api("/api/standards/drift").catch(() => []), api("/api/lines").catch(() => null)]);
  const s = status, box = $("dashBox");
  const failed = all.filter(c => c.state === "failed").length, issues = all.filter(c => c.warnings > 0).length, oneway = all.filter(c => c.oneWay).length;
  const mosVals = all.map(c => c.mos).filter(v => v != null), avgMos = mosVals.length ? mosVals.reduce((a, b) => a + b, 0) / mosVals.length : null;
  const lastResults = (tests?.tests || []).map(t => t.last).filter(Boolean);
  const passed = lastResults.filter(r => r.outcome === "passed").length;

  const tile = (icon, label, value, sub, cls) => `<div class="card stat span-3"><div class="l">${ic(icon, 15)} ${label}</div><div class="v ${cls || ""}">${value}</div><div class="s">${sub}</div></div>`;
  let html = `<div class="dash">
    ${tile("phone", "Calls", fmtN(all.length), s.recording ? `Packet capture since ${tm(s.startedAt)}` : s.opened ? "Opened capture" : "Logging all the time")}
    ${tile("x", "Failed calls", `${fmtN(failed)}<small>${pct(failed, all.length)}%</small>`, `${fmtN(issues)} with issues · ${fmtN(oneway)} one-way audio`)}
    ${tile("wave", "Average audio quality", avgMos != null ? `${avgMos.toFixed(2)}<small>${mosLabel[mosGrade(avgMos)]}</small>` : "—", mosVals.length ? `MOS over ${mosVals.length} call${mosVals.length === 1 ? "" : "s"} with audio` : "Needs a media record or Debug Recording")}
    ${tile("beaker", "Call tests", lastResults.length ? `${passed}<small>of ${lastResults.length} passing</small>` : "—", lastResults.length ? "Latest result of each test" : `<a href="#" data-go="tests">Set up a test call</a>`)}`;
  html += lineMatrix(board?.lines || []);

  if (all.length) {
    // About 30 bars whatever the capture length, on a readable step (10 s ... 1 h).
    const first = Math.min(...all.map(c => +new Date(c.started))), spanS = Math.max(60, (Date.now() - first) / 1000);
    const stepS = [10, 15, 30, 60, 120, 300, 600, 900, 1800, 3600].find(v => spanS / v <= 36) || 3600;
    const t0 = Math.floor(first / (stepS * 1000)) * stepS * 1000, n = Math.max(1, Math.ceil((Date.now() - t0) / (stepS * 1000)));
    const size = stepS / 60;
    const xs = Array.from({length: n}, (_, i) => t0 + i * stepS * 1000);
    const bucket = c => Math.min(n - 1, Math.max(0, Math.floor((+new Date(c.started) - t0) / (stepS * 1000))));
    const count = pred => { const v = new Array(n).fill(0); all.filter(pred).forEach(c => v[bucket(c)]++); return v; };
    const mosPer = xs.map((_, i) => { const m = all.filter(c => bucket(c) === i && c.mos != null).map(c => c.mos); return m.length ? m.reduce((a, b) => a + b, 0) / m.length : null; });
    const per = stepS < 60 ? `${stepS} seconds` : stepS === 60 ? "minute" : stepS < 3600 ? `${stepS / 60} minutes` : "hour";
    html += `<div class="card span-7"><div class="card-h"><h2>Calls</h2><span class="muted">per ${per}</span></div><div class="card-b">${barChart({id: "d-calls", label: "Calls per " + per, xs, h: 190, fmtX: v => stepS < 60 ? tm(v) : tm(v).slice(0, 5),
        stacks: [{name: "No issues", color: "var(--good)", values: count(c => c.severity === "ok")}, {name: "Warnings", color: "var(--warning)", values: count(c => c.severity === "warn")}, {name: "Problems", color: "var(--critical)", values: count(c => c.severity === "bad")}]})}</div></div>
      <div class="card span-5"><div class="card-h"><h2>Audio quality</h2><span class="muted">average MOS per ${per}</span></div><div class="card-b">${mosVals.length ? lineChart({id: "d-mos", label: "Average MOS", xs, h: 190, yMin: 1, yMax: 4.5, refs: [{y: 4, label: "Good"}, {y: 3.6, label: "Fair"}], fmtX: v => stepS < 60 ? tm(v) : tm(v).slice(0, 5), fmtY: v => v.toFixed(2), series: [{name: "Average MOS", color: "var(--s1)", values: mosPer}]}) : emptyState("wave", "No audio measured", "Debug Recording scores audio live. Without it, the SBC's MEDIA_END record scores the call when it ends.")}</div></div>`;
  } else {
    html += `<div class="card span-12">${emptyState("grid", s.recording ? "Waiting for calls" : "No capture running", s.recording ? "Charts fill in as calls go through the SBCs." : "Press <b>Start capture</b> to record calls, or run a call test.")}</div>`;
  }

  const problems = all.filter(c => c.severity !== "ok").slice(0, 8);
  html += `<div class="card span-7"><div class="card-h"><h2>Latest problems</h2><span class="grow"></span><a href="#" data-go="calls">All calls</a></div>
    ${problems.length ? `<div class="feed-list">${problems.map(c => `<div class="feed-item" data-call="${c.id}"><span class="ic" style="color:var(--${c.severity === "bad" ? "bad" : "warn"})">${ic(c.severity === "bad" ? "x" : "alert", 17)}</span><div style="min-width:0"><div class="tt">${esc(c.from || "unknown")} → ${esc(c.to || "unknown")} <span class="muted" style="font-weight:500">· ${esc(sbcName(c.sbc))} · ${tm(c.started)}</span></div><div class="ss">${namedText(c.topIssue)}</div></div>${mosBadge(c.mos)}</div>`).join("")}</div>`
      : `<div class="empty" style="padding:28px">${ic("check", 22)}<p style="margin-top:8px">No problems in this capture.</p></div>`}</div>
    <div class="card span-5"><div class="card-h"><h2>Call tests</h2><span class="grow"></span><a href="#" data-go="tests">Manage</a></div>
    ${(tests?.tests || []).length ? `<div class="feed-list">${tests.tests.slice(0, 8).map(t => `<div class="feed-item" data-test="${esc(t.id)}"><span class="ic">${ic("beaker", 17)}</span><div style="min-width:0"><div class="tt">${esc(t.name)}</div><div class="ss">${t.last ? `${tm(t.last.started)} · ${esc(t.last.finding)}` : "Not run yet"}</div></div><div style="display:flex;gap:6px;align-items:center">${t.last ? mosBadge(t.last.mosRx) + outcomeBadge(t.last.outcome) : ""}</div></div>`).join("")}</div>`
      : `<div class="empty" style="padding:28px"><p>No call tests yet. <a href="#" data-go="tests">Create one</a> to check audio both ways through an SBC.</p></div>`}</div>
    ${(tests?.tests || []).length ? `<div class="card span-12"><div class="card-h"><h2>Test pass rate</h2><span class="muted">finished tests that passed · a warning counts as not passed</span></div><div class="card-b">${passRateChart(results) || `<div class="empty" style="padding:20px"><p>This chart fills in after two finished tests.</p></div>`}</div></div>` : ""}
    <div class="card span-12"><div class="card-h"><h2>Configuration</h2><span class="muted">live file against the saved standard</span><span class="grow"></span><a href="#" data-go="compare">Compare</a></div><div class="sbc-health">${(drift || []).map(d => {
      const text = d.state === "same" ? "Matches the saved standard" : d.state === "drift" ? `${fmtN(d.differences)} difference${d.differences === 1 ? "" : "s"} from the saved standard` : d.state === "none" ? "No standard saved yet" : d.state === "nologin" ? "No login saved, so the live file cannot be read" : d.state === "checking" ? "Checking the live configuration" : esc(d.message || "Could not read the live configuration");
      const dot = d.state === "same" ? "ok" : d.state === "drift" || d.state === "error" ? "warn" : "";
      return `<div><div class="n"><span class="dot ${dot}"></span>${esc(d.name)}<span class="muted mono" style="font-weight:500;font-size:11.5px;margin-left:auto">${esc(d.ip)}</span></div><div class="muted" style="font-size:12.5px;margin-top:8px">${text}</div></div>`;
    }).join("") || `<div class="muted">No SBCs yet.</div>`}</div></div>
    <div class="card span-12"><div class="card-h"><h2>SBC health</h2><span class="grow"></span><a href="#" data-go="sbcs">Manage SBCs</a></div><div class="sbc-health">${s.sbcs.map(x => {
      const mine = all.filter(c => c.sbc === x.ip), mm = mine.map(c => c.mos).filter(v => v != null), live = Math.min(ago(s, x.syslog), ago(s, x.dr)) < 60;
      return `<div><div class="n"><span class="dot ${live ? "ok" : (x.syslog || x.dr) ? "warn" : ""}"></span>${esc(x.listed ? x.name : x.ip)}<span class="muted mono" style="font-weight:500;font-size:11.5px;margin-left:auto">${esc(x.ip)}</span></div>
        <div class="m"><div><b>${fmtN(mine.length)}</b>calls</div><div><b>${fmtN(mine.filter(c => c.state === "failed").length)}</b>failed</div><div><b>${mm.length ? (mm.reduce((a, b) => a + b, 0) / mm.length).toFixed(1) : "—"}</b>avg MOS</div></div>
        <div class="muted" style="font-size:12px;margin-top:8px">${live ? "Logs arriving" : (x.syslog || x.dr) ? "Logs stopped" : "No logs yet"}</div></div>`;
    }).join("") || `<div class="muted">No SBCs yet.</div>`}</div></div>
  </div>`;
  if (setHTML(box, html)) {
    box.querySelectorAll("[data-call]").forEach(e => e.onclick = () => { go("calls"); select(+e.dataset.call); });
    box.querySelectorAll("[data-test]").forEach(e => e.onclick = () => { selectedTest = e.dataset.test; selectedResult = null; go("tests"); });
    box.querySelectorAll("[data-line]").forEach(e => e.onclick = () => { lineSel = e.dataset.line; const lb = $("linesBox"); if (lb) lb.__h = null; go("lines"); });
  }
}

/* The overview's line board: one square per line by group, plus the lines that need attention, worst first.
   Built for boards of 1,000+ lines; the Line board page has the full table. */
const LM_ORDER = {down: 0, warn: 1, unknown: 2, call: 3, idle: 4, up: 5};
function lineMatrix(lines) {
  if (!lines.length) return "";
  const n = st => lines.filter(l => l.state === st).length;
  const groups = [...new Set(lines.map(l => l.group || typeName(l.type)))].sort((a, b) => a.localeCompare(b));
  const rows = groups.map(g => {
    const gl = lines.filter(l => (l.group || typeName(l.type)) === g);
    const down = gl.filter(l => l.state === "down").length, check = gl.filter(l => l.state === "warn" || l.state === "unknown").length;
    const good = gl.filter(l => l.state === "up" || l.state === "call").length;
    const [cls, sum] = down ? ["bad", `${down} down`] : check ? ["warn", `${check} to check`] : ["", `${good}/${gl.length} up`];
    return `<div class="lm-row"><div class="lm-name"><b title="${esc(g)}">${esc(g)}</b><span>${gl.length} line${gl.length === 1 ? "" : "s"}</span></div><span class="lm-sum ${cls}">${sum}</span>
      <div class="lm-cells">${gl.map(l => `<button type="button" class="lm-c ${esc(l.state)}" data-line="${esc(l.id)}" tabindex="-1" title="${esc(l.name)} ${esc(l.number)}: ${esc(lineLabel(l))}"></button>`).join("")}</div></div>`;
  }).join("");
  const need = lines.filter(l => ["down", "warn", "unknown"].includes(l.state)).sort((a, b) => LM_ORDER[a.state] - LM_ORDER[b.state] || a.name.localeCompare(b.name));
  const dotOf = st => st === "down" ? "bad" : st === "warn" ? "warn" : "";
  const att = need.slice(0, 10).map(l => `<button type="button" class="lm-att" data-line="${esc(l.id)}"><span class="dot ${dotOf(l.state)}"></span>
      <span style="min-width:0"><span class="t" style="display:block">${esc(l.name)} <span class="mono muted" style="font-weight:400">${esc(l.number)}</span></span><span class="s" style="display:block">${esc(l.group || typeName(l.type))} · ${esc(l.detail || lineLabel(l))}</span></span>
      <span class="mono muted" style="font-size:12.5px">${alwaysOn(l.type) ? esc(sinceText(l.since)) : ""}</span></button>`).join("");
  const key = [["down", "Down"], ["warn", "Warning"], ["unknown", "Unknown"], ["call", "In a call"], ["idle", "Not tested"], ["up", "Up"]]
    .filter(([st]) => n(st)).map(([st, label]) => `<span><span class="lm-c ${st}" aria-hidden="true"></span>${label} <b class="mono">${n(st)}</b></span>`).join("");
  return `<div class="card span-12"><div class="card-h"><h2>Line board</h2><span class="muted"><span class="mono">${lines.length}</span> line${lines.length === 1 ? "" : "s"} in <span class="mono">${groups.length}</span> group${groups.length === 1 ? "" : "s"}</span><span class="grow"></span><a href="#" data-go="lines">Open line board</a></div>
    <div class="lm"><div class="lm-main"><div aria-hidden="true">${rows}</div><div class="lm-key">${key}<span>One square per line; click one to open it.</span></div></div>
      <aside class="lm-side" aria-label="Lines that need attention"><div class="card-h"><h3>Needs attention</h3>${need.length ? `<span class="badge ${need.some(l => l.state === "down") ? "bad" : "warn"}">${need.length}</span>` : ""}</div>
        ${att || `<div class="empty" style="padding:28px">${ic("check", 22)}<p style="margin-top:8px">Every line is up or idle.</p></div>`}
        ${need.length > 10 ? `<div style="padding:8px 16px 14px"><a href="#" data-go="lines">${need.length - 10} more on the line board</a></div>` : ""}</aside></div></div>`;
}

/* ------------------------------------------------------------ call tests */
let selectedTest = null, selectedResult = null, editingTest = null, resultCache = {};
async function renderTests(force) {
  const box = $("testsBox");
  // A refresh must not rebuild the form and throw away what is being typed.
  if (!force && editingTest && box.querySelector("#tf-save")) return;
  const data = await api("/api/tests");
  const tests = data.tests, live = data.live;
  // Calls into MediantTrace's test line are listed as their own entry.
  const inbound = await api("/api/tests/results?limit=30&testId=inbound");
  const testLine = {id: "inbound", name: "Test line (calls in)", pseudo: true, last: inbound[0] || null, history: inbound.slice().reverse()};
  if (!selectedTest) selectedTest = tests.length ? tests[0].id : "inbound";
  const t = selectedTest === "inbound" ? testLine : tests.find(x => x.id === selectedTest);
  const tc = status?.tester || {};

  const tlCard = `<div class="test-card ${selectedTest === "inbound" ? "sel" : ""}" data-tid="inbound">
      <div style="min-width:0"><div class="nm">${ic("phone", 14)} Test line <span class="muted" style="font-weight:500">· calls in</span></div><div class="tg">Dial the test number through an SBC: hear a message, speak, hear yourself back</div></div>
      <div class="act">${testLine.last ? outcomeBadge(testLine.last.outcome) : '<span class="badge">No calls yet</span>'}</div>
      <div></div><div class="act">${testLine.last ? mosBadge(testLine.last.mosRx) : ""}${status?.tester?.echoActive ? '<span class="badge accent">In a call</span>' : ""}</div></div>`;
  const list = tlCard + (tests.length ? tests.map(x => {
    const hist = x.history.filter(h => h.mosRx != null);
    const spark = hist.length > 1 ? `<svg class="spark" viewBox="0 0 120 26" preserveAspectRatio="none" aria-label="MOS history">${(() => { const pts = hist.map((h, i) => `${(i / (hist.length - 1) * 118 + 1).toFixed(1)},${(25 - (h.mosRx - 1) / 3.5 * 24).toFixed(1)}`).join(" "); return `<polyline points="${pts}" fill="none" stroke="var(--s1)" stroke-width="2" vector-effect="non-scaling-stroke"/>`; })()}</svg>` : "";
    const running = live && live.testId === x.id;
    return `<div class="test-card ${x.id === selectedTest ? "sel" : ""}" data-tid="${esc(x.id)}">
      <div style="min-width:0"><div class="nm">${esc(x.name)}</div><div class="tg">${esc(x.to)} via ${esc(x.sbcIp)}:${x.sbcPort} · ${esc(x.codec)} · ${x.durationSeconds}s${x.lineType ? ` · ${esc(x.lineType)}` : ""}${x.repeatMinutes ? ` · every ${x.repeatMinutes} min` : ""}</div></div>
      <div class="act">${running ? '<span class="badge accent">Running</span>' : x.last ? outcomeBadge(x.last.outcome) : '<span class="badge">Not run</span>'}</div>
      <div>${spark}</div>
      <div class="act">${x.last ? mosBadge(x.last.mosRx) : ""}<button class="btn sm" data-edit="${esc(x.id)}">${ic("edit", 12)} Edit</button><button class="btn sm primary" data-run="${esc(x.id)}" ${live ? "disabled" : ""}>${ic("play", 12)} Run</button></div>
    </div>`;
  }).join("") : emptyState("beaker", "No call tests yet", "A test calls a number through an SBC, plays a tone and checks the audio in both directions."));

  let right;
  if (editingTest) right = testForm(editingTest, tc);
  else {
    const liveCard = live ? liveTestCard(live) : "";
    let rid = selectedResult || (t?.last?.id ?? null);
    let detail = "";
    if (rid) {
      let r = resultCache[rid];
      if (!r || r.outcome === "running" || (live && live.id === rid)) { try { r = await api("/api/tests/results/" + rid); resultCache[rid] = r; } catch { r = null; } }
      if (r && !(live && live.id === rid)) detail = await resultDetail(r);
    }
    const history = t?.pseudo ? inbound : t ? await api("/api/tests/results?limit=30&testId=" + encodeURIComponent(t.id)) : [];
    const histCard = t ? `<div class="card" style="margin-top:16px"><div class="card-h"><h2>History · ${esc(t.name)}</h2><span class="grow"></span>
        ${t.pseudo ? "" : `<button class="btn sm" data-edit="${esc(t.id)}">${ic("edit", 13)} Edit</button><button class="btn sm" data-del="${esc(t.id)}">${ic("trash", 13)} Delete</button>`}</div>
      ${history.length ? `<div class="scroll"><table class="tbl"><thead><tr><th>When</th><th>Result</th><th>SIP</th><th class="r">Answer</th><th>MOS</th><th class="r">Loss</th><th class="r">Jitter</th><th>Finding</th></tr></thead><tbody>${history.map(h => `<tr class="click" data-rid="${esc(h.id)}" style="cursor:pointer${h.id === rid ? ";background:var(--accent-soft)" : ""}"><td>${dtm(h.started)}${h.trigger === "scheduled" ? ' <span class="muted">· scheduled</span>' : ""}</td><td>${outcomeBadge(h.outcome)}</td><td>${h.finalStatus || "—"}</td><td class="r">${h.answerMs != null ? (h.answerMs / 1000).toFixed(2) + " s" : "—"}</td><td>${mosBadge(h.mosRx) || "—"}</td><td class="r">${h.rxPackets ? h.rxLossPercent + "%" : "—"}</td><td class="r">${h.rxPackets ? h.rxJitterMs + " ms" : "—"}</td><td style="white-space:normal;min-width:260px">${esc(h.finding)}</td></tr>`).join("")}</tbody></table></div>` : `<p class="muted" style="padding:0 16px">Not run yet.</p>`}</div>` : "";
    const tlHelp = `<div class="card" style="margin-bottom:16px"><div class="card-h"><h2>How the test line works</h2></div><div class="card-b"><ol class="steps">
        <li><div>From a dealerboard line (or any phone behind the SBC), dial the test number. Route that number on the SBC to this server (for example 9998).</div></li>
        <li><div>MediantTrace answers and says: <i>"This is the Mediant Trace test line. After the tone, please say something."</i> If you hear it, audio reaches you.</div></li>
        <li><div>Speak for five seconds after the beep. It plays your voice back: if you hear yourself, audio works in both directions.</div></li>
        <li><div>The result appears here with the level and quality of your audio, and the recording to listen to.</div></li>
      </ol><p class="muted" style="margin:12px 0 0">Calls are answered only from listed SBCs. MediantTrace's own loop tests to the same number are echoed instead.</p></div></div>`;
    right = t?.pseudo ? (detail ? detail : tlHelp) + histCard
      : liveCard + (detail || (t && !live ? `<div class="card">${emptyState("play", "Run this test", "Press <b>Run</b> to place the call now.")}</div>` : "")) + histCard;
    if (!tests.length && !t?.pseudo) right = setupHelp(tc);
  }
  const html = (data.error ? `<div class="alert bad" style="margin-bottom:16px"><span class="ic">${ic("alert", 18)}</span><div>${esc(data.error)}</div></div>` : "") +
    `<div class="tests"><div class="card list"><div class="card-h"><h2>Tests</h2><span class="grow"></span><button class="btn sm primary" id="newTest">${ic("plus", 13)} New test</button></div><div class="rows" style="max-height:calc(100vh - 260px)">${list}</div></div><div>${right}</div></div>`;
  if (setHTML(box, html)) {
    $("newTest").onclick = () => { editingTest = {name: "", sbcIp: (status?.sbcs || []).find(x => x.listed)?.ip || "", sbcPort: 5060, to: "", toDomain: "", from: "mediantrace", codec: "PCMA", durationSeconds: 15, repeatMinutes: 0, ringSeconds: 60, lineType: "", ringSignal: "dtmf", username: "", hasPassword: false}; box.__h = null; renderTests(true); };
    box.querySelectorAll("[data-tid]").forEach(e => e.onclick = ev => { if (ev.target.closest("button")) return; selectedTest = e.dataset.tid; selectedResult = null; editingTest = null; box.__h = null; renderTests(); });
    box.querySelectorAll("[data-run]").forEach(b => b.onclick = async () => {
      try { const r = await api("/api/tests/run", {id: b.dataset.run}); selectedTest = b.dataset.run; selectedResult = r.resultId; toast("Test call started"); } catch (e) { toast(e.message, true); }
      box.__h = null; renderTests();
    });
    box.querySelectorAll("[data-rid]").forEach(r => r.onclick = () => { selectedResult = r.dataset.rid; history.replaceState(null, "", "#result=" + selectedResult); box.__h = null; renderTests(); });
    box.querySelectorAll("[data-edit]").forEach(b => b.onclick = () => { editingTest = {...tests.find(x => x.id === b.dataset.edit)}; box.__h = null; renderTests(true); });
    box.querySelectorAll("[data-del]").forEach(b => b.onclick = async () => {
      if (!confirm("Delete this test? Its past results stay in the history file.")) return;
      try { await api("/api/tests/delete", {id: b.dataset.del}); toast("Test deleted"); selectedTest = null; selectedResult = null; } catch (e) { toast(e.message, true); }
      box.__h = null; renderTests();
    });
    box.querySelectorAll("[data-sbc-call]").forEach(a => a.onclick = e => { e.preventDefault(); go("calls"); select(+a.dataset.sbcCall); });
    const rr = box.querySelector("#testReport"); if (rr) rr.onclick = () => testReport(resultCache[rr.dataset.id]);
    wireForm(box);
  }
}

function setupHelp(tc) {
  const addrs = status?.serverAddresses || [];
  return `<div class="card"><div class="card-h"><h2>Before the first test</h2></div><div class="card-b"><ol class="steps">
    <li><div>On the SBC, allow calls from this server: add it as a trusted peer (a <b>Proxy Set</b> and <b>IP Group</b> for ${addrs.map(copyChip).join(" or ")} port ${copyChip(String(tc.port || 5090))}), with a route to where test calls should go.</div></li>
    <li><div>Open the firewall on this server for UDP ${copyChip(String(tc.port || 5090))} and the audio ports ${copyChip(`${tc.rtpFirst}-${tc.rtpFirst + tc.rtpCount - 1}`)}. <span class="mono">MediantTrace.exe install</span> does this.</div></li>
    <li><div>Create a test: the SBC's SIP address and the number to call. An <b>echo</b> number tests audio in both directions; any other number tests the far end → us direction.</div></li>
  </ol></div></div>`;
}

function liveTestCard(l) {
  const stages = ["calling", "ringing", "connected", "hanging up"];
  const at = Math.max(0, stages.indexOf(l.stage));
  return `<div class="card" style="margin-bottom:16px"><div class="card-h"><span class="rec" style="padding:3px 10px"><span class="pulse"></span>LIVE</span><h2>${esc(l.name)}</h2><span class="grow"></span><span class="muted mono">${esc(l.callId)}</span></div><div class="card-b">
    <div class="stepper">${["Calling", "Ringing", "Connected", "Hanging up"].map((s, i) => `<div class="${i < at ? "done" : i === at ? "now" : ""}">${s}</div>`).join("")}</div>
    <div class="facts" style="margin-bottom:0"><div><div class="l">Talk time</div><div class="v">${dur(l.talkSeconds)}</div></div><div><div class="l">Audio sent</div><div class="v">${fmtN(l.txPackets)} packets</div></div><div><div class="l">Audio received</div><div class="v" style="color:${l.stage === "connected" && l.talkSeconds >= 2 && !l.rxPackets ? "var(--bad)" : "inherit"}">${fmtN(l.rxPackets)} packets</div></div></div>
  </div></div>`;
}

function mosGauge(m) {
  const v = m == null ? 0 : (m - 1) / 3.5, a = Math.PI * (1 - v), r = 46, cx = 56, cy = 54;
  const x = cx + r * Math.cos(a), y = cy - r * Math.sin(a), g = m == null ? null : mosGrade(m);
  const color = g ? (g === "good" ? "var(--good)" : g === "fair" ? "var(--warning)" : "var(--critical)") : "var(--border-strong)";
  return `<div class="gauge"><svg width="112" height="64" viewBox="0 0 112 64" role="img" aria-label="Estimated MOS ${m ?? "not measured"}">
    <path d="M10 54 A46 46 0 0 1 102 54" fill="none" stroke="var(--sunken)" stroke-width="10" stroke-linecap="round"/>
    ${m != null ? `<path d="M10 54 A46 46 0 0 1 ${x.toFixed(1)} ${y.toFixed(1)}" fill="none" stroke="${color}" stroke-width="10" stroke-linecap="round"/>` : ""}
    <text x="56" y="52" text-anchor="middle" font-size="20" font-weight="700" fill="var(--text)">${m != null ? m.toFixed(1) : "—"}</text></svg>
    <div><div style="font-weight:700">${g ? mosLabel[g] + " quality" : "Not measured"}</div><div class="muted" style="font-size:12.5px">Estimated MOS of the audio received (E-model, G.711). 4.0+ good · 3.6 fair · below 3.1 bad.</div></div></div>`;
}

async function resultDetail(r) {
  let sbcCall = null;
  try { const cl = calls.length ? calls : await api("/api/calls"); sbcCall = cl.find(c => c.callId === r.callId) || null; } catch {}
  const cls = r.outcome === "passed" ? "ok" : r.outcome === "warning" ? "warn" : "bad";
  const icn = r.outcome === "passed" ? "check" : r.outcome === "warning" ? "alert" : "x";
  const facts = [["SIP result", r.finalStatus ? `${r.finalStatus} ${r.finalReason}` : "—"], ["100 Trying", r.tryingMs != null ? r.tryingMs + " ms" : "—"], ["Ringing", r.ringingMs != null ? (r.ringingMs / 1000).toFixed(2) + " s" : "—"], ["Answered", r.answerMs != null ? (r.answerMs / 1000).toFixed(2) + " s" : "—"],
    ["Talk time", r.talkSeconds ? dur(r.talkSeconds) : "—"], ["Codec", r.codec], ["First audio", r.firstAudioMs != null ? r.firstAudioMs + " ms after answer" : "—"], ["Our media", r.localMedia || "—"], ["Far-end media (SDP)", r.remoteMedia || "—"], ["Audio came from", r.audioFrom || "—"]];
  const max = Math.max(r.txPackets, r.rxPackets, 1);
  const dir = (label, v, rx) => `<div class="dir"><span class="t2">${label}</span><div class="bar"><i class="${v ? (rx ? "rx" : "") : "zero"}" style="width:${Math.max(2, v / max * 100)}%"></i></div><span class="n ${v ? "" : "zero"}">${fmtN(v)}</span></div>`;
  const secs = Math.max(r.txPerSecond.length, r.rxPerSecond.length);
  const chart = secs > 1 ? lineChart({id: "t-pps", label: "Packets per second", xs: Array.from({length: secs}, (_, i) => i), h: 150, yMax: 60, fmtX: v => v + " s", fmtY: v => v + " /s",
    series: [{name: "Sent by MediantTrace", color: "var(--s1)", values: Array.from({length: secs}, (_, i) => r.txPerSecond[i] ?? 0)}, {name: "Received from the far end", color: "var(--s2)", values: Array.from({length: secs}, (_, i) => r.rxPerSecond[i] ?? 0)}]}) : "";
  return `<div class="card">
    <div class="d-head"><div class="d-title"><h2>${esc(r.name)}</h2>${outcomeBadge(r.outcome)}
      <div class="d-actions">${sbcCall ? `<a class="btn sm" href="#" data-sbc-call="${sbcCall.id}" title="This test call as the SBC logged it">${ic("server", 13)} As the SBC saw it</a>` : ""}
      <button class="btn sm" data-copy="${esc(location.origin + location.pathname + "#result=" + r.id)}">${ic("link", 13)} Link</button>
      <button class="btn sm primary" id="testReport" data-id="${esc(r.id)}">${ic("report", 13)} Report</button></div></div>
      <div class="d-sub"><span>${ic("clock", 13)} ${dtm(r.started)}</span><span>${esc(r.target)}</span><span>${r.trigger === "scheduled" ? "Scheduled" : "Run by hand"}</span><span class="mono">${esc(r.callId)}</span></div></div>
    <div class="verdict ${cls}">${ic(icn, 20)}<div><span>${esc(r.findings[0] || (r.outcome === "passed" ? "Audio passed in the measured directions." : ""))}</span></div></div>
    <div class="tab-b">
      <div class="facts">${facts.map(([l, v]) => `<div><div class="l">${l}</div><div class="v" title="${esc(v)}">${esc(v)}</div></div>`).join("")}</div>
      ${r.trigger === "inbound" ? `<h3>What the caller said</h3><div class="path" style="display:flex;gap:14px;align-items:center;flex-wrap:wrap"><audio controls preload="none" src="/api/tests/results/${encodeURIComponent(r.id)}/recording.wav" style="height:36px"></audio><span class="muted" style="font-size:12.5px">The 5 seconds recorded after the beep: what MediantTrace heard, and what was played back to the caller.</span></div>` : ""}
      <h3>Audio</h3>
      <div class="paths"><div class="path">${mosGauge(r.mosRx)}</div>
        <div class="path"><div class="path-h"><span>Packets</span>${r.trigger === "inbound" ? '<span class="badge">Announcement and play-back sent</span>' : `<span class="badge ${r.toneReturned ? "ok" : ""}">${r.toneReturned ? "Tone came back" : "Tone not returned"}</span>`}</div>
          ${dir("Sent (us → far end)", r.txPackets)}${dir("Received (far end → us)", r.rxPackets, true)}
          <div class="path-f"><span>Loss ${r.rxLossPercent}%</span><span>Jitter ${r.rxJitterMs} ms</span><span>Longest gap ${fmtN(r.rxMaxGapMs)} ms</span><span>Level ${r.rxLevelDb > -99 ? r.rxLevelDb + " dBov" : "—"}</span><span>Sound in ${r.rxAudioPercent}% of frames</span></div></div></div>
      ${chart ? `<h3>Audio over time</h3><div class="timeline">${chart}</div>` : ""}
      ${r.findings.length > 1 ? `<h3>Findings</h3><div class="checks">${r.findings.slice(1).map(f => `<div class="check ${/one-way|no audio|stopped|loss|jitter|MOS|silent|NAT/i.test(f) ? "warn" : "ok"}"><span class="ic">${ic(/one-way|no audio|stopped|loss|jitter|MOS|silent|NAT/i.test(f) ? "alert" : "check", 17)}</span><div>${esc(f)}</div></div>`).join("")}</div>` : ""}
      <h3>SIP</h3>${testLadder(r)}
    </div></div>`;
}

function testLadder(r) {
  if (!r.ladder.length) return `<p class="muted">No SIP messages.</p>`;
  const W = 300, pad = 112, H = 34, top = 64, cols = 2, width = pad + cols * W, height = top + r.ladder.length * H + 12;
  const cx = i => pad + i * W + W / 2;
  let svg = `<svg width="${width}" height="${height}" role="img" aria-label="SIP messages of the test call">`;
  [["MediantTrace", r.localMedia.split(":")[0]], ["SBC", r.target.split(" via ")[1] || ""]].forEach(([t1, t2], i) => {
    svg += `<line class="life" x1="${cx(i)}" y1="54" x2="${cx(i)}" y2="${height}"/><g class="colh"><rect x="${cx(i) - W / 2 + 20}" y="8" width="${W - 40}" height="44" rx="8" ${i === 1 ? 'style="stroke:var(--accent);stroke-width:1.5"' : ""}/><text class="t1" x="${cx(i)}" y="26" text-anchor="middle">${esc(t1)}</text><text class="t2" x="${cx(i)}" y="43" text-anchor="middle">${esc(t2)}</text></g>`;
  });
  let prev = null;
  r.ladder.forEach((m, i) => {
    const y = top + i * H + 18, x1 = cx(m.outbound ? 0 : 1), x2 = cx(m.outbound ? 1 : 0), dirn = x2 >= x1 ? 1 : -1;
    const color = /^SIP\/2\.0 [3-6]/.test(m.first) ? "var(--fail)" : /^SIP\/2\.0 2/.test(m.first) ? "var(--succ)" : /^SIP\/2\.0 1/.test(m.first) ? "var(--prov)" : "var(--req)";
    const label = m.first.replace(/^SIP\/2\.0 /, "").replace(/ SIP\/2\.0$/, "").replace(/sips?:([^@ ;>]*)@\S*/, "$1").slice(0, 34);
    const delta = prev ? new Date(m.time) - new Date(prev) : null; prev = m.time;
    svg += `<g class="msg"><text class="time" x="12" y="${y - 2}">${tms(m.time)}</text>${delta !== null ? `<text class="delta" x="12" y="${y + 12}">+${delta < 1000 ? delta + " ms" : (delta / 1000).toFixed(2) + " s"}</text>` : ""}
      <line x1="${x1}" y1="${y}" x2="${x2 - dirn * 8}" y2="${y}" stroke="${color}" stroke-width="1.8"/><path d="M${x2 - dirn * 11},${y - 5} L${x2},${y} L${x2 - dirn * 11},${y + 5} z" fill="${color}"/>
      <text x="${(x1 + x2) / 2}" y="${y - 6}" text-anchor="middle" style="fill:${color};font-weight:600">${esc(label)}</text></g>`;
  });
  return `<div class="ladder-wrap ladder">${svg}</svg></div>` +
    `<details class="m" style="margin-top:12px"><summary><b>All SIP messages</b><span class="muted">${r.ladder.length}</span></summary>${r.ladder.map(m => sipHtml(m.text)).join("")}</details>`;
}

function testForm(t, tc) {
  const sbcs = (status?.sbcs || []).filter(x => x.listed);
  return `<div class="card"><div class="card-h"><h2>${t.id ? "Edit test" : "New call test"}</h2></div><div class="card-b">
    <datalist id="sbcIps">${sbcs.map(x => `<option value="${esc(x.ip)}">${esc(x.name)}</option>`).join("")}</datalist>
    <div class="form-grid">
      <div class="full"><label for="tf-name">Name</label><input type="text" id="tf-name" value="${esc(t.name)}" placeholder="e.g. London SBC → echo"></div>
      <div><label for="tf-sbc">SBC SIP address</label><input type="text" id="tf-sbc" list="sbcIps" value="${esc(t.sbcIp)}" placeholder="192.0.2.10"><div class="help">Where the INVITE is sent: the SBC's SIP interface.</div></div>
      <div><label for="tf-port">SBC SIP port</label><input type="number" id="tf-port" min="1" max="65535" value="${t.sbcPort}"></div>
      <div><label for="tf-to">Number to call</label><input type="text" id="tf-to" value="${esc(t.to)}" placeholder="e.g. 9999 (echo)"><div class="help">An echo number tests audio both ways.</div></div>
      <div><label for="tf-from">Calling number</label><input type="text" id="tf-from" value="${esc(t.from)}"></div>
      <div><label for="tf-domain">Destination domain <span class="muted">(optional)</span></label><input type="text" id="tf-domain" value="${esc(t.toDomain)}" placeholder="the SBC address"></div>
      <div><label for="tf-codec">Codec</label><select id="tf-codec"><option value="PCMA" ${t.codec === "PCMA" ? "selected" : ""}>G.711 A-law (PCMA)</option><option value="PCMU" ${t.codec === "PCMU" ? "selected" : ""}>G.711 µ-law (PCMU)</option></select></div>
      <div><label for="tf-dur">Talk time (seconds)</label><input type="number" id="tf-dur" min="3" max="300" value="${t.durationSeconds}"></div>
      <div><label for="tf-ring">Ring for (seconds)</label><input type="number" id="tf-ring" min="5" max="300" value="${t.ringSeconds || 60}"><div class="help">How long to wait for an answer. An MRD test also keeps the voice message going for at least this long.</div></div>
      <div><label for="tf-type">Line type</label><select id="tf-type">
        ${[["", "Plain call"], ["MRD", "MRD — ring inside the answered call"], ["ARD", "ARD — someone picks up"], ["HOOT", "Hoot — answers by itself"], ["DDI", "DDI — a phone number"]].map(([v, l]) => `<option value="${v}" ${(t.lineType || "") === v ? "selected" : ""}>${l}</option>`).join("")}
      </select><div class="help">MRD sends the ring after the line answers, then plays the voice-team message.</div></div>
      <div id="tf-sig-wrap"><label for="tf-sig">MRD ring signal</label><select id="tf-sig">
        ${[["dtmf", "DTMF * to ring, # to stop"], ["info", "SIP INFO signal=ring"], ["none", "No ring signal"]].map(([v, l]) => `<option value="${v}" ${(t.ringSignal || "dtmf") === v ? "selected" : ""}>${l}</option>`).join("")}
      </select><div class="help">DTMF travels across the E1. SIP INFO only works when the path stays on SIP.</div></div>
      <div><label for="tf-rep">Run automatically</label><select id="tf-rep">${[[0, "Only when I press Run"], [5, "Every 5 minutes"], [15, "Every 15 minutes"], [30, "Every 30 minutes"], [60, "Every hour"], [240, "Every 4 hours"], [1440, "Once a day"]].map(([v, l]) => `<option value="${v}" ${t.repeatMinutes === v ? "selected" : ""}>${l}</option>`).join("")}</select></div>
      <div><label for="tf-user">Username <span class="muted">(only if the trunk asks for a login)</span></label><input type="text" id="tf-user" value="${esc(t.username)}" autocomplete="off"></div>
      <div><label for="tf-pass">Password</label><input type="password" id="tf-pass" placeholder="${t.hasPassword ? "unchanged" : ""}" autocomplete="new-password" style="height:34px;padding:0 11px;border-radius:8px;border:1px solid var(--border-strong);background:var(--surface);color:var(--text);width:100%"><div class="help">Stored encrypted on this server.</div></div>
    </div>
    <div style="display:flex;gap:8px;margin-top:16px;justify-content:flex-end"><button class="btn" id="tf-cancel">Cancel</button><button class="btn primary" id="tf-save">Save test</button></div>
    <p class="muted" style="margin:14px 0 0;font-size:12.5px">Test calls come from this server on UDP ${tc.port || 5090} (audio ${tc.rtpFirst}-${tc.rtpFirst + tc.rtpCount - 1}). The SBC must accept calls from it.</p>
  </div></div>`;
}

function wireForm(box) {
  const save = box.querySelector("#tf-save"); if (!save) return;
  box.querySelector("#tf-cancel").onclick = () => { editingTest = null; box.__h = null; renderTests(); };
  const type = box.querySelector("#tf-type"), sig = box.querySelector("#tf-sig-wrap");
  const showSignal = () => { if (sig) sig.style.display = type && type.value === "MRD" ? "" : "none"; };
  if (type) { type.onchange = showSignal; showSignal(); }
  save.onclick = async () => {
    const v = id => box.querySelector(id).value;
    const pass = v("#tf-pass");
    const body = {id: editingTest.id || null, name: v("#tf-name"), sbcIp: v("#tf-sbc"), sbcPort: parseInt(v("#tf-port") || "5060", 10), to: v("#tf-to"), from: v("#tf-from"), toDomain: v("#tf-domain"),
      codec: v("#tf-codec"), durationSeconds: parseInt(v("#tf-dur") || "15", 10), ringSeconds: parseInt(v("#tf-ring") || "60", 10),
      lineType: v("#tf-type"), ringSignal: v("#tf-sig"),
      repeatMinutes: parseInt(v("#tf-rep") || "0", 10), username: v("#tf-user"), password: pass.length ? pass : (editingTest.hasPassword ? null : "")};
    try { const r = await api("/api/tests/save", body); toast("Test saved"); selectedTest = r.id; selectedResult = null; editingTest = null; box.__h = null; renderTests(); }
    catch (e) { toast(e.message, true); }
  };
}

/* ------------------------------------------------------------ reports: one self-contained HTML file */
function openReport(title, subtitle, body) {
  const css = document.querySelector("style").textContent;
  const tool = `<div class="noprint" style="display:flex;gap:8px;justify-content:flex-end;margin-bottom:18px"><button class="btn" onclick="window.print()">Print / save as PDF</button><button class="btn primary" onclick="dl()">Download .html</button></div>`;
  const html = `<!doctype html><html lang="en" data-theme="light"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>${esc(title)}</title><style>${css}
body{background:#fff}.rep{max-width:1040px;margin:0 auto;padding:28px 28px 60px}.rep-h{display:flex;align-items:center;gap:12px;border-bottom:2px solid var(--border);padding-bottom:14px;margin-bottom:18px}
.rep-h h1{margin:0;font-size:22px}.rep-h .muted{font-size:12.5px}.ladder-wrap{max-height:none}.card{box-shadow:none}.verdict{margin:0 0 16px}.tab-b{padding:0}
@media print{.noprint{display:none!important}.rep{padding:0}h3{break-after:avoid}.ladder-wrap,.path,.check,.sdp-pair{break-inside:avoid}}</style>
<script>function dl(){var b=new Blob(["<!doctype html>\\n"+document.documentElement.outerHTML],{type:"text/html"});var a=document.createElement("a");a.href=URL.createObjectURL(b);a.download=document.title.replace(/[^A-Za-z0-9._-]+/g,"_")+".html";a.click();}<\/script></head>
<body><div class="rep">${tool}<div class="rep-h"><svg width="34" height="34" viewBox="0 0 32 32"><rect width="32" height="32" rx="8" fill="#2563eb"/><path d="M5 17h5l3-8 5 15 3-9h6" stroke="#fff" stroke-width="2.6" fill="none" stroke-linecap="round" stroke-linejoin="round"/></svg><div><h1>${esc(title)}</h1><div class="muted">${subtitle}</div></div></div>${body}
<p class="muted" style="margin-top:30px;font-size:11.5px">Generated by MediantTrace ${esc(status?.version || "")} on ${esc(new Date().toLocaleString())}. MOS is the SBC's own score when it sends one, otherwise an E-model estimate from packet loss, jitter and delay.</p></div></body></html>`;
  if (reportInPlace) { document.open(); document.write(html); document.close(); return; }
  const w = window.open(URL.createObjectURL(new Blob([html], {type: "text/html"})), "_blank");
  if (!w) toast("Allow pop-ups for this page to open the report", true);
}

function callReport(d) {
  if (!d) return;
  const [vc, vi, vt] = verdict(d);
  const keepTab = dtab, keepOpen = openMsg; openMsg = null;
  const body = `<div class="verdict ${vc}">${ic(vi, 20)}<div><span>${esc(vt)}</span></div></div>
    ${overview(d, d.answered ? Math.round(new Date(d.answered) - new Date(d.started)) : null)}
    <h3>Call flow</h3>${flowTab(d).replace(/<div class="ladder-tools">[\s\S]*?<\/div>\s*<div class="ladder-wrap/, '<div class="ladder-wrap').replace(/<p class="muted"[^>]*>Click a message[\s\S]*?<\/p>/, "")}
    ${d.streams.length ? mediaTab(d).replace(/<h3>Audio by far end<\/h3>[\s\S]*?(?=<h3>Audio timeline)/, "") : ""}
    <h3>SDP</h3>${sdpTab(d)}
    <h3>SIP messages</h3>${d.messages.map(m => `<div style="margin-bottom:10px"><div style="font-weight:650;margin-bottom:4px">${esc(m.first)} <span class="muted" style="font-weight:500">${tms(m.time)} · ${esc(m.src || "SBC")} → ${esc(m.dst || "SBC")}</span></div>${sipHtml(m.text)}</div>`).join("")}`;
  dtab = keepTab; openMsg = keepOpen;
  openReport(`Call report: ${d.from || "unknown"} to ${d.to || "unknown"}`, `${esc(sbcName(d.sbc))} · started ${esc(dtm(d.started))} · Call-ID <span class="mono">${esc(d.callId)}</span>${d.session ? ` · SID <span class="mono">${esc(d.session)}</span>` : ""}`, body);
}

async function testReport(r) {
  if (!r) return;
  const body = (await resultDetail(r)).replace(/<div class="d-actions">[\s\S]*?<\/div><\/div>/, "</div>");
  openReport(`Call test report: ${r.name}`, `${esc(r.target)} · ${esc(dtm(r.started))} · Call-ID <span class="mono">${esc(r.callId)}</span>`, body);
}

/* ------------------------------------------------------------ config compare (runs in the browser; files never leave it) */
/* AudioCodes .ini: "Param = value" lines under [Section] headers, and tables:
     [ \TableName ]
     FORMAT Index = Col1, Col2, ...;
     TableName 0 = v1, v2, ...;
     [ \TableName ] */
function parseIni(text) {
  const params = new Map(), tables = new Map();
  let section = "", table = null;
  for (let raw of text.split(/\r?\n/)) {
    const line = raw.replace(/^﻿/, "").trim();
    if (!line || line.startsWith(";")) continue;
    const tableMark = /^\[\s*\\(.+?)\s*\]$/.exec(line);
    if (tableMark) {
      const name = tableMark[1];
      if (table && table.name === name) { table = null; continue; } // closing marker
      table = tables.get(name) || {name, cols: [], rows: new Map()}; tables.set(name, table); continue;
    }
    const sec = /^\[\s*(.+?)\s*\]$/.exec(line);
    if (sec) { section = sec[1]; table = null; continue; }
    if (table) {
      const fmt = /^FORMAT\s+([^=]+?)\s*=\s*(.*?);?$/i.exec(line);
      if (fmt) { table.cols = fmt[2].split(",").map(c => c.trim()).filter(Boolean); table.keyCol = fmt[1].trim(); continue; }
      const row = /^(\S+)\s+(\d+)\s*=\s*(.*?);?$/.exec(line);
      if (row) { table.rows.set(row[2], splitIniRow(row[3])); continue; }
    }
    const kv = /^([^=]+?)\s*=\s*(.*)$/.exec(line);
    if (kv) params.set(kv[1].trim(), {value: kv[2].trim().replace(/^'(.*)'$|^"(.*)"$/, "$1$2"), section});
  }
  return {params, tables};
}
function splitIniRow(s) {
  const out = []; let cur = "", q = null;
  for (const ch of s) {
    if ((ch === "'" || ch === '"') && (q === null || q === ch)) { q = q ? null : ch; continue; } // quoted strings keep their commas
    if (ch === "," && !q) { out.push(cur.trim()); cur = ""; continue; }
    cur += ch;
  }
  out.push(cur.trim());
  return out;
}
/* Rows are matched by index, or by a name column when asked (tables re-ordered on one box). */
function rowKey(table, idx, vals, byName) {
  if (!byName) return idx;
  const nameCol = table.cols.findIndex(c => /(^|_)name$|Name$/i.test(c) && !/^Is/i.test(c));
  return nameCol >= 0 && vals[nameCol] ? "name:" + vals[nameCol] : idx;
}
function compareIni(a, b, byName) {
  const params = [];
  new Set([...a.params.keys(), ...b.params.keys()]).forEach(k => {
    const x = a.params.get(k), y = b.params.get(k);
    if (!x) params.push({k, sec: y.section, a: null, b: y.value, kind: "only-b"});
    else if (!y) params.push({k, sec: x.section, a: x.value, b: null, kind: "only-a"});
    else params.push({k, sec: x.section || y.section, a: x.value, b: y.value, kind: x.value === y.value ? "same" : "changed"});
  });
  const tables = [];
  new Set([...a.tables.keys(), ...b.tables.keys()]).forEach(name => {
    const ta = a.tables.get(name), tb = b.tables.get(name);
    const cols = (ta?.cols.length ? ta.cols : tb?.cols) || [];
    const rows = [];
    const index = (t) => { const m = new Map(); if (t) t.rows.forEach((v, i) => m.set(rowKey(t, i, v, byName), {i, v})); return m; };
    const ma = index(ta), mb = index(tb);
    new Set([...ma.keys(), ...mb.keys()]).forEach(key => {
      const x = ma.get(key), y = mb.get(key);
      if (!x) rows.push({key, ia: null, ib: y.i, kind: "only-b", diffs: cols.map((c, j) => ({c, a: null, b: y.v[j]})), name: rowLabel(tb, y.v)});
      else if (!y) rows.push({key, ia: x.i, ib: null, kind: "only-a", diffs: cols.map((c, j) => ({c, a: x.v[j], b: null})), name: rowLabel(ta, x.v)});
      else {
        const d = []; const n = Math.max(x.v.length, y.v.length, cols.length);
        for (let j = 0; j < n; j++) if ((x.v[j] ?? "") !== (y.v[j] ?? "")) d.push({c: cols[j] || `Column ${j + 1}`, a: x.v[j] ?? "", b: y.v[j] ?? ""});
        rows.push({key, ia: x.i, ib: y.i, kind: d.length ? "changed" : "same", diffs: d, name: rowLabel(ta, x.v)});
      }
    });
    tables.push({name, cols, rows, onlyIn: !ta ? "b" : !tb ? "a" : null});
  });
  return {params, tables};
}
function rowLabel(t, v) { const j = t.cols.findIndex(c => /Name$/i.test(c)); return j >= 0 ? v[j] : ""; }

let cmpFiles = [null, null], cmpNames = ["A", "B"], cmpResult = null, cmpOnlyDiff = true, cmpByName = false, cmpQuery = "", cmpClosed = new Set();
const cmpTag = (i, sm) => `<span class="cmp-tag ${i ? "b" : "a"}${sm ? " sm" : ""}">${i ? "B" : "A"}</span>`;
const cmpSize = n => n < 1024 ? n + " B" : (n / 1024).toFixed(n < 10240 ? 1 : 0) + " KB";
function renderCompare() {
  const box = $("cmpBox");
  if (box.__built) { cmpPaintStd(); drawCompare(); return; }
  box.__built = true;
  box.innerHTML = `<div class="card"><div class="card-h"><h2>Compare two configurations</h2><span class="muted">AudioCodes .ini files. A is the stored standard. B is a download or an upload.</span></div><div class="card-b">
    <div class="cmp-std">
      <label for="cmpSbc">Standard for</label><select id="cmpSbc" aria-label="SBC"></select>
      <span class="muted" id="cmpStdMeta"></span>
      <button class="btn" id="cmpGet">${ic("down", 14)} Download from SBC</button>
      <label class="btn cmp-up">Upload INI<input type="file" id="cmpUp" accept=".ini,.txt" aria-label="Upload an INI file"></label>
      <button class="btn" id="cmpSaveStd">Save B as standard</button>
    </div>
    <div class="cmp-files">${[0, 1].map(i => `${i ? `<button class="btn icon cmp-swap" id="cmpSwap" title="Swap A and B" aria-label="Swap A and B">${ic("swap", 16)}</button>` : ""}
      <div class="cmp-drop" id="cmpDrop${i}">
        <label class="cmp-pick">${cmpTag(i)}<span class="cmp-txt" id="cmpTxt${i}"></span><input type="file" id="cmpFile${i}" accept=".ini,.txt"></label>
        <input type="text" id="cmpName${i}" placeholder="Label, e.g. ${i ? "New York SBC, or After" : "London SBC, or Before"}" aria-label="Label for file ${i ? "B" : "A"}">
      </div>`).join("")}</div>
    <p class="help" style="margin-top:12px">Download reads the INI from the SBC with the saved login and compares it with the stored standard. The dashboard repeats that check and flags an SBC when the live file has drifted. Upload does the same with a file from this PC. <b>Save B as standard</b> keeps the file on the right as the one later downloads are checked against. You can still drop two files below.</p>
  </div></div><div id="cmpOut"></div>`;
  $("cmpSbc").onchange = () => cmpStdLabel();
  $("cmpGet").onclick = async () => {
    const ip = $("cmpSbc").value;
    if (!ip) { toast("Add an SBC first", true); return; }
    $("cmpGet").disabled = true;
    try { const got = await api("/api/standards/download", {id: ip}); await cmpAgainstStandard(ip, {name: "Downloaded", text: got.text}); }
    catch (e) { toast(e.message, true); }
    $("cmpGet").disabled = false;
  };
  $("cmpUp").onchange = e => {
    const f = e.target.files[0]; e.target.value = ""; if (!f) return;
    const rd = new FileReader();
    rd.onload = () => cmpAgainstStandard($("cmpSbc").value, {name: f.name, text: String(rd.result)}).catch(err => toast(err.message, true));
    rd.onerror = () => toast(`Couldn't read ${f.name}`, true);
    rd.readAsText(f);
  };
  $("cmpSaveStd").onclick = async () => {
    const ip = $("cmpSbc").value, file = cmpFiles[1] || cmpFiles[0];
    if (!ip || !file) { toast("Choose an SBC and load a file into B first", true); return; }
    try { await api("/api/standards/save", {ip, text: file.text}); cmpStdAt = 0; toast("Standard saved"); cmpPaintStd(); }
    catch (e) { toast(e.message, true); }
  };
  cmpPaintStd();
  [0, 1].forEach(i => {
    const drop = $("cmpDrop" + i);
    $("cmpFile" + i).onchange = e => { const f = e.target.files[0]; e.target.value = ""; if (f) cmpLoad(i, f); };
    drop.ondragover = e => { e.preventDefault(); drop.classList.add("over"); };
    drop.ondragleave = e => { if (!drop.contains(e.relatedTarget)) drop.classList.remove("over"); };
    drop.ondrop = e => { e.preventDefault(); drop.classList.remove("over"); const f = e.dataTransfer.files[0]; if (f) cmpLoad(i, f); };
    $("cmpName" + i).oninput = () => { cmpSetNames(); cmpBuild(); };
    cmpPaintFile(i);
  });
  // A file dropped just outside a box would otherwise replace the whole page.
  ["dragover", "drop"].forEach(t => document.addEventListener(t, e => { if (view === "compare" && !e.target.closest?.(".cmp-drop")) e.preventDefault(); }));
  $("cmpSwap").onclick = () => {
    cmpFiles.reverse();
    const n0 = $("cmpName0").value; $("cmpName0").value = $("cmpName1").value; $("cmpName1").value = n0;
    cmpPaintFile(0); cmpPaintFile(1); cmpRun();
  };
  cmpRun();
}
function cmpLoad(i, f) {
  const rd = new FileReader();
  rd.onload = () => {
    const text = String(rd.result), parsed = parseIni(text);
    if (!parsed.params.size && !parsed.tables.size) { toast(`${f.name} doesn't look like an AudioCodes .ini file`, true); return; }
    cmpFiles[i] = {name: f.name, size: f.size, text, parsed};
    cmpPaintFile(i); cmpRun();
  };
  rd.onerror = () => toast(`Couldn't read ${f.name}`, true);
  rd.readAsText(f);
}
function cmpPaintFile(i) {
  const f = cmpFiles[i];
  $("cmpDrop" + i).classList.toggle("loaded", !!f);
  $("cmpTxt" + i).innerHTML = f
    ? `<span class="cmp-fn" title="${esc(f.name)}">${esc(f.name)}</span><span class="cmp-meta">${cmpSize(f.size)} · ${f.parsed.params.size} settings · ${f.parsed.tables.size} tables · click to replace</span>`
    : `<span class="cmp-fn">${i ? "Second" : "First"} configuration</span><span class="cmp-meta">Drop an .ini file here, or click to choose</span>`;
}
let cmpStdInfo = [], cmpStdAt = 0;
function cmpAdopt(i, file) {
  const parsed = parseIni(file.text);
  if (!parsed.params.size && !parsed.tables.size) { toast(`${file.name} doesn't look like an AudioCodes .ini file`, true); return false; }
  cmpFiles[i] = {name: file.name, size: file.text.length, text: file.text, parsed};
  if ($("cmpName" + i)) $("cmpName" + i).value = i ? file.name : "Standard";
  cmpPaintFile(i);
  return true;
}
async function cmpAgainstStandard(ip, file) {
  let standard = null;
  if (ip) {
    try { standard = await api("/api/standards/text?ip=" + encodeURIComponent(ip)); }
    catch (e) { if (!/no standard/i.test(e.message)) throw e; }
  }
  if (standard?.text && !cmpAdopt(0, {name: "Standard", text: standard.text})) return;
  if (!cmpAdopt(1, file)) return;
  cmpRun();
  if (!standard?.text) toast("No standard saved yet. Use Save B as standard if this file is the one to keep.");
}
function cmpStdLabel() {
  const el = $("cmpStdMeta"); if (!el) return;
  const hit = cmpStdInfo.find(x => x.ip === $("cmpSbc").value);
  el.textContent = hit ? `Saved ${new Date(hit.savedAt).toLocaleString()} · ${cmpSize(hit.bytes)}` : "No standard saved";
}
async function cmpPaintStd() {
  const sel = $("cmpSbc"); if (!sel || document.activeElement === sel) return;
  const keep = sel.value;
  const listed = (status?.sbcs || []).filter(x => x.listed);
  sel.innerHTML = listed.length ? listed.map(s => `<option value="${esc(s.ip)}">${esc(s.name)} (${esc(s.ip)})</option>`).join("") : `<option value="">No SBC yet</option>`;
  if ([...sel.options].some(o => o.value === keep)) sel.value = keep;
  if (Date.now() - cmpStdAt > 4000) {
    try { cmpStdInfo = await api("/api/standards"); cmpStdAt = Date.now(); } catch { /* the label stays as it was */ }
  }
  cmpStdLabel();
}
function cmpSetNames() { cmpNames = [0, 1].map(i => $("cmpName" + i).value.trim() || cmpFiles[i]?.name || (i ? "B" : "A")); }
function cmpRun() {
  cmpSetNames();
  cmpResult = cmpFiles[0] && cmpFiles[1] ? compareIni(cmpFiles[0].parsed, cmpFiles[1].parsed, cmpByName) : null;
  cmpBuild();
}
function cmpBuild() {
  const out = $("cmpOut");
  if (!cmpResult) {
    out.innerHTML = cmpFiles[0] || cmpFiles[1] ? `<div class="card" style="margin-top:16px">${emptyState("diff", "One more file", `Add the ${cmpFiles[0] ? "second (B)" : "first (A)"} configuration to see the differences.`)}</div>` : "";
    return;
  }
  const s = cmpStats();
  out.innerHTML = cmpTiles(s, false) + `<div class="cmp-bar">
      <div class="search"><span data-i="search"></span><input type="search" id="cmpQ" placeholder="Filter by parameter, table or value" aria-label="Filter"></div>
      <div class="chips" role="group" aria-label="Show"><button class="chip${cmpOnlyDiff ? " on" : ""}" data-cmpmode="1">Differences <span class="n">${s.total}</span></button><button class="chip${cmpOnlyDiff ? "" : " on"}" data-cmpmode="0">Everything</button></div>
      <label class="toggle" title="Match table rows by their name column instead of their index (use when rows were added in a different order)"><input type="checkbox" id="cmpByName"${cmpByName ? " checked" : ""}> Match rows by name</label>
      <span class="grow"></span>
      <span class="cmp-legend"><i></i>Different</span>
      <button class="btn sm ghost" id="cmpFold">Collapse tables</button>
      <button class="btn sm" id="cmpReport">${ic("report", 14)} Report</button>
    </div><div id="cmpList"></div>`;
  paintIcons(out);
  const q = $("cmpQ"); q.value = cmpQuery;
  q.oninput = () => { cmpQuery = q.value; drawCompare(); };
  out.querySelectorAll("[data-cmpmode]").forEach(b => b.onclick = () => {
    cmpOnlyDiff = b.dataset.cmpmode === "1";
    out.querySelectorAll("[data-cmpmode]").forEach(x => x.classList.toggle("on", x === b));
    drawCompare();
  });
  $("cmpByName").onchange = e => { cmpByName = e.target.checked; cmpRun(); };
  $("cmpFold").onclick = () => {
    const all = cmpResult.tables.map(t => t.name);
    cmpClosed = new Set(all.some(n => !cmpClosed.has(n)) ? all : []);
    drawCompare();
  };
  $("cmpReport").onclick = () => openReport(`Configuration compare: ${cmpNames[0]} vs ${cmpNames[1]}`, `A: ${esc(cmpFiles[0].name)} · B: ${esc(cmpFiles[1].name)}`, compareHtml(true));
  out.onclick = e => {
    const j = e.target.closest("[data-jump]"); if (!j) return;
    e.preventDefault();
    const key = j.dataset.jump;
    const el = key.startsWith("#") ? $(key.slice(1)) : [...out.querySelectorAll("details[data-t]")].find(d => d.dataset.t === key);
    if (!el) return;
    if (el.tagName === "DETAILS") el.open = true;
    el.scrollIntoView({behavior: "smooth", block: "start"});
  };
  $("cmpList").addEventListener("toggle", e => {
    const d = e.target; if (!d.dataset?.t) return;
    if (d.open) cmpClosed.delete(d.dataset.t); else cmpClosed.add(d.dataset.t);
    cmpFoldLabel();
  }, true);
  drawCompare();
}
function cmpFoldLabel() { const b = $("cmpFold"); if (b && cmpResult) b.textContent = cmpResult.tables.some(t => !cmpClosed.has(t.name)) ? "Collapse tables" : "Expand tables"; }
function drawCompare() { const l = $("cmpList"); if (!cmpResult || !l) return; l.innerHTML = cmpListHtml(false); cmpFoldLabel(); }
function compareHtml(forReport) { return cmpTiles(cmpStats(), forReport) + cmpListHtml(forReport); }
function cmpStats() {
  const r = cmpResult, pd = r.params.filter(p => p.kind !== "same"), rd = r.tables.flatMap(t => t.rows.filter(x => x.kind !== "same"));
  const all = [...pd, ...rd], n = k => all.filter(x => x.kind === k).length;
  return {params: r.params.length, pd: pd.length, rd: rd.length, tablesHit: r.tables.filter(t => t.onlyIn || t.rows.some(x => x.kind !== "same")).length,
    onlyA: n("only-a"), onlyB: n("only-b"), total: all.length};
}
function cmpTiles(s, forReport) {
  const [A, B] = cmpNames, tag = forReport ? "div" : "button";
  return `<div class="dash" style="margin:16px 0">
    <div class="card stat span-3 cmp-verdict ${s.total ? "warn" : "ok"}"><div class="l">${ic(s.total ? "alert" : "check", 15)} Verdict</div><div class="v">${s.total ? `${s.total}<small>difference${s.total === 1 ? "" : "s"}</small>` : "Identical"}</div><div class="s" title="${esc(A)} vs ${esc(B)}">${cmpTag(0, 1)}${esc(A)} &nbsp;vs&nbsp; ${cmpTag(1, 1)}${esc(B)}</div></div>
    <${tag} class="card stat span-3" data-jump="#cmp-settings"><div class="l">${ic("gear", 15)} Settings that differ</div><div class="v">${s.pd}<small>of ${s.params}</small></div><div class="s">General parameters</div></${tag}>
    <${tag} class="card stat span-3" data-jump="#cmp-tables"><div class="l">${ic("list", 15)} Table rows that differ</div><div class="v">${s.rd}</div><div class="s">in ${s.tablesHit} table${s.tablesHit === 1 ? "" : "s"}</div></${tag}>
    <div class="card stat span-3"><div class="l">${ic("diff", 15)} Only in one file</div><div class="v">${s.onlyA + s.onlyB}</div><div class="s">${s.onlyA} only in A · ${s.onlyB} only in B</div></div>
  </div>`;
}
const cmpVal = v => v == null ? '<span class="cmp-none">not set</span>' : v === "" ? '<span class="cmp-none">empty</span>' : esc(v);
/* Marks the part of two values that differs, widened to whole words so 244 vs 245 marks the number, not one digit. */
function cmpInline(a, b) {
  if (!a || !b) return [cmpVal(a), cmpVal(b)];
  const w = c => /\w/.test(c || "");
  let p = 0; while (p < a.length && p < b.length && a[p] === b[p]) p++;
  while (p > 0 && w(a[p - 1]) && (w(a[p]) || w(b[p]))) p--;
  let s = 0; while (s < a.length - p && s < b.length - p && a[a.length - 1 - s] === b[b.length - 1 - s]) s++;
  while (s > 0 && w(a[a.length - s]) && (w(a[a.length - 1 - s]) || w(b[b.length - 1 - s]))) s--;
  const one = v => { const m = v.slice(p, v.length - s); return esc(v.slice(0, p)) + (m ? `<mark>${esc(m)}</mark>` : "") + esc(v.slice(v.length - s)); };
  return [one(a), one(b)];
}
const cmpBadge = k => k === "changed" ? '<span class="badge warn">different</span>' : k === "only-a" ? '<span class="badge warn">only in A</span>' : k === "only-b" ? '<span class="badge warn">only in B</span>' : '<span class="badge ok">same</span>';
function cmpCells(a, b, kind) {
  const d = kind !== "same", [ha, hb] = kind === "changed" ? cmpInline(a, b) : [cmpVal(a), cmpVal(b)];
  return `<td class="v a${d ? " d" : ""}">${ha}</td><td class="v b${d ? " d" : ""}">${hb}</td>`;
}
const cmpHead = cols => `<thead><tr>${cols.map(c => `<th>${c}</th>`).join("")}<th>${cmpTag(0, 1)}${esc(cmpNames[0])}</th><th>${cmpTag(1, 1)}${esc(cmpNames[1])}</th><th></th></tr></thead>`;
function cmpListHtml(forReport) {
  const r = cmpResult, onlyDiff = cmpOnlyDiff || forReport, q = forReport ? "" : cmpQuery.trim().toLowerCase();
  const hit = (...xs) => !q || xs.some(x => x != null && String(x).toLowerCase().includes(q));
  const rowHit = x => hit(x.name) || x.diffs.some(d => hit(d.c, d.a, d.b));
  const changedT = t => t.onlyIn || t.rows.some(x => x.kind !== "same");

  const plist = r.params.filter(p => (!onlyDiff || p.kind !== "same") && hit(p.k, p.sec, p.a, p.b))
    .sort((x, y) => (x.sec || "").localeCompare(y.sec || "") || x.k.localeCompare(y.k));
  const pd = r.params.filter(p => p.kind !== "same").length;
  let pRows = "", sec;
  for (const p of plist) {
    if (p.sec !== sec) { sec = p.sec; pRows += `<tr class="sec"><td colspan="4">${esc(sec || "General")}</td></tr>`; }
    pRows += `<tr><td class="lead mono${p.kind !== "same" ? " d" : ""}">${esc(p.k)}</td>${cmpCells(p.a, p.b, p.kind)}<td>${cmpBadge(p.kind)}</td></tr>`;
  }
  const settings = `<section class="card cmp-card" id="cmp-settings"><div class="card-h">${ic("gear", 16)}<h2>General settings</h2><span class="grow"></span>${pd ? `<span class="badge warn">${pd} differ</span>` : '<span class="badge ok">identical</span>'}<span class="muted">of ${r.params.length}</span></div>
    ${plist.length ? `<div class="scroll"><table class="tbl cmp-tbl"><colgroup><col style="width:26%"><col><col><col style="width:112px"></colgroup>${cmpHead(["Parameter"])}<tbody>${pRows}</tbody></table></div>`
      : `<div class="card-b muted">${q ? "No settings match the filter." : "All general settings are the same."}</div>`}</section>`;

  const tlist = r.tables.filter(t => (!onlyDiff || changedT(t)) && (hit(t.name) || t.rows.some(rowHit))).sort((x, y) => x.name.localeCompare(y.name));
  const tables = tlist.map(t => {
    const nameHit = hit(t.name), n = t.rows.filter(x => x.kind !== "same").length;
    const body = t.rows.filter(x => (!onlyDiff || x.kind !== "same") && (nameHit || rowHit(x))).map(x => {
      const moved = x.ia != null && x.ib != null && x.ia !== x.ib;
      const label = `<div class="cmp-rl"><span class="mono">#${esc(x.ia ?? x.ib)}${moved ? ` → #${esc(x.ib)}` : ""}</span>${x.name ? `<span>${esc(x.name)}</span>` : ""}</div>`;
      if (x.kind === "same") return `<tr><td class="lead">${label}</td><td colspan="3" class="muted">identical</td><td>${cmpBadge("same")}</td></tr>`;
      const diffs = x.kind === "changed" ? x.diffs : x.diffs.filter(d => (d.a ?? d.b ?? "") !== "");
      return diffs.map((d, j) => `<tr>${j ? "" : `<td class="lead d" rowspan="${diffs.length}">${label}</td>`}<td class="mono">${esc(d.c)}</td>${cmpCells(d.a, d.b, x.kind)}${j ? "" : `<td rowspan="${diffs.length}">${cmpBadge(x.kind)}</td>`}</tr>`).join("");
    }).join("");
    const head = `${ic("chev", 16)}<h2 class="mono" style="font-size:13.5px">${esc(t.name)}</h2>${t.onlyIn ? `<span class="badge warn">whole table only in ${t.onlyIn === "a" ? "A" : "B"}</span>` : ""}<span class="grow"></span>${n ? `<span class="badge warn">${n} row${n === 1 ? "" : "s"} differ</span>` : '<span class="badge ok">identical</span>'}<span class="muted">of ${t.rows.length}</span>`;
    const tbl = body ? `<div class="scroll"><table class="tbl cmp-tbl"><colgroup><col style="width:17%"><col style="width:19%"><col><col><col style="width:112px"></colgroup>${cmpHead(["Row", "Column"])}<tbody>${body}</tbody></table></div>` : '<div class="card-b muted">No rows.</div>';
    return `<details class="card cmp-card" data-t="${esc(t.name)}"${forReport || !cmpClosed.has(t.name) ? " open" : ""}><summary class="card-h">${head}</summary>${tbl}</details>`;
  }).join("");
  const hitT = tlist.filter(changedT);
  const toc = !forReport && hitT.length > 1 ? `<div class="cmp-toc">${hitT.map(t => { const n = t.rows.filter(x => x.kind !== "same").length; return `<a href="#" data-jump="${esc(t.name)}">${esc(t.name)}${n ? ` <b>${n}</b>` : ""}</a>`; }).join("")}</div>` : "";
  return settings + `<h3 id="cmp-tables" style="scroll-margin-top:130px">Tables</h3>` + toc
    + (tables || `<div class="card"><div class="card-b muted">${q ? "No tables match the filter." : "All tables are the same."}</div></div>`);
}

/* ------------------------------------------------------------ SBC configuration changes (approved by a person) */
let changeForm = false;
async function renderChanges(s) {
  const box = $("chgBox");
  if (box.contains(document.activeElement) && /INPUT|TEXTAREA|SELECT/.test(document.activeElement.tagName)) return;
  const plans = await api("/api/changes");
  const pending = plans.filter(p => p.status === "pending"), done = plans.filter(p => p.status !== "pending");
  const sbcs = (s.sbcs || []).filter(x => x.listed);
  const left = p => Math.max(0, Math.round((new Date(p.expires) - new Date(s.now)) / 60000));
  const stat = p => ({applied: '<span class="badge ok">Applied</span>', failed: '<span class="badge bad">Failed</span>', rejected: '<span class="badge">Rejected</span>', expired: '<span class="badge">Expired</span>', applying: '<span class="badge accent">Applying</span>'}[p.status] || `<span class="badge">${esc(p.status)}</span>`);
  const pend = pending.length ? pending.map(p => `<div class="card" style="margin-bottom:14px;border-color:var(--warn)">
      <div class="card-h"><span style="color:var(--warn)">${ic("alert", 18)}</span><h2>${p.kind === "save" ? "Save configuration to flash" : "Configuration change"} · ${esc(p.sbcName)}</h2><span class="grow"></span><span class="muted">plan ${esc(p.id)} · from ${esc(p.requestedBy)} · expires in ${left(p)} min</span></div>
      <div class="card-b">
        <div style="font-weight:600;margin-bottom:8px">${esc(p.reason)}</div>
        <pre class="sip" style="max-height:300px">${esc(p.commands.join("\n"))}</pre>
        <div style="display:flex;gap:16px;align-items:center;margin-top:14px;flex-wrap:wrap">
          <div><div class="muted" style="font-size:12px">Approval code (give it to the MCP client only if you approve)</div><div style="font:700 26px var(--mono);letter-spacing:.12em">${esc(p.code)}</div></div>
          <span class="grow"></span>
          <button class="btn" data-reject="${esc(p.id)}">Reject</button>
          <button class="btn primary" data-approve="${esc(p.id)}">${ic("check", 15)} Approve and apply</button>
        </div>
        <p class="muted" style="margin:12px 0 0;font-size:12.5px">${p.kind === "save" ? "Writes the running configuration to flash, so it survives a reboot." : "Applied live after a backup of the running configuration. Not saved to flash: a reboot undoes it unless you also approve a save."}</p>
      </div></div>`).join("")
    : `<div class="card" style="margin-bottom:16px">${emptyState("check", "Nothing waiting for approval", "Changes proposed by an MCP client (or below) appear here with their commands and an approval code.")}</div>`;
  const form = changeForm ? `<div class="card" style="margin-bottom:16px"><div class="card-h"><h2>New change</h2></div><div class="card-b form-grid">
      <div><label for="cf-sbc">SBC</label><select id="cf-sbc">${sbcs.map(x => `<option value="${esc(x.ip)}">${esc(x.name)} (${esc(x.ip)})</option>`).join("")}</select></div>
      <div><label for="cf-kind">Type</label><select id="cf-kind"><option value="change">Configuration commands</option><option value="save">Save running configuration to flash</option></select></div>
      <div class="full"><label for="cf-reason">Reason</label><input type="text" id="cf-reason" placeholder="What it fixes and what it affects"></div>
      <div class="full" id="cf-cmd-wrap"><label for="cf-cmds">CLI commands, one per line (as in the running-config: configure voip / table row / settings / activate / exit)</label><textarea id="cf-cmds" rows="10" style="width:100%;font:13px var(--mono);padding:10px;border-radius:8px;border:1px solid var(--border-strong);background:var(--surface);color:var(--text)"></textarea></div>
      <div class="full" style="display:flex;gap:8px;justify-content:flex-end"><button class="btn" id="cf-cancel">Cancel</button><button class="btn primary" id="cf-plan">Create plan</button></div></div></div>` : "";
  const hist = done.length ? `<div class="card"><div class="card-h"><h2>History</h2><span class="grow"></span><span class="muted">Backups in <span class="mono">${esc(status?.settings.backupFolder || "sbc-backups")}</span></span></div><div class="scroll"><table class="tbl"><thead><tr><th>When</th><th>SBC</th><th>Change</th><th>Status</th><th>Approved via</th><th>Backup</th></tr></thead><tbody>${done.map(p => `<tr><td>${dtm(p.applied || p.created)}</td><td>${esc(p.sbcName)}</td><td style="white-space:normal;min-width:280px">${esc(p.reason)}<details class="m" style="margin-top:6px"><summary><span class="muted">${p.commands.length} command${p.commands.length === 1 ? "" : "s"}${p.output ? " and output" : ""}</span></summary><pre class="sip">${esc(p.commands.join("\n") + (p.output ? "\n\n" + p.output : ""))}</pre></details></td><td>${stat(p)}${p.errors ? ` <span class="muted">${p.errors} error${p.errors === 1 ? "" : "s"}</span>` : ""}</td><td class="muted">${esc(p.approvedVia || "—")}</td><td class="mono" style="font-size:11.5px">${esc(p.backup || "—")}</td></tr>`).join("")}</tbody></table></div></div>` : "";
  const html = `<div class="toolbar"><span class="muted">Every change needs a person's approval. MCP clients can propose but never approve.</span><span class="grow"></span>${changeForm ? "" : `<button class="btn primary" id="newChange">${ic("plus", 14)} New change</button>`}</div>${form}<h3>Waiting for approval</h3>${pend}${hist ? "<h3>History</h3>" + hist : ""}`;
  if (setHTML(box, html)) {
    const nc = $("newChange"); if (nc) nc.onclick = () => { changeForm = true; box.__h = null; renderChanges(status); };
    const cc = $("cf-cancel"); if (cc) cc.onclick = () => { changeForm = false; box.__h = null; renderChanges(status); };
    const kind = $("cf-kind"); if (kind) kind.onchange = () => $("cf-cmd-wrap").classList.toggle("hidden", kind.value === "save");
    const cp = $("cf-plan"); if (cp) cp.onclick = async () => {
      try { await api("/api/changes/plan", {sbc: $("cf-sbc").value, kind: $("cf-kind").value, reason: $("cf-reason").value, commands: $("cf-cmds").value}); toast("Plan created: review and approve it below"); changeForm = false; box.__h = null; refresh(); }
      catch (e) { toast(e.message, true); }
    };
    box.querySelectorAll("[data-approve]").forEach(b => b.onclick = async () => {
      const p = pending.find(x => x.id === b.dataset.approve);
      if (!p || !await confirmChange(p)) return;
      b.disabled = true; b.textContent = "Applying…";
      try { const r = await api("/api/changes/approve", {id: b.dataset.approve}); toast(r.message, !r.ok); } catch (e) { toast(e.message, true); }
      box.__h = null; refresh();
    });
    box.querySelectorAll("[data-reject]").forEach(b => b.onclick = async () => { try { await api("/api/changes/reject", {id: b.dataset.reject}); toast("Rejected"); } catch (e) { toast(e.message, true); } box.__h = null; refresh(); });
  }
}

async function readNames(b) {
  b.disabled = true; const was = b.textContent; b.textContent = "Reading…";
  try { const r = await api("/api/labels/refresh", {id: b.dataset.names}); toast(r.message); const box = $("sbcBox"); if (box) box.__h = null; refresh(); }
  catch (e) { toast(e.message, true); b.disabled = false; b.textContent = was; }
}
async function loadBackupNames(b) {
  b.disabled = true; const was = b.textContent; b.textContent = "Reading…";
  try { const r = await api("/api/labels/backup", {id: b.dataset.backup}); toast(r.message); const box = $("sbcBox"); if (box) box.__h = null; refresh(); }
  catch (e) { toast(e.message, true); b.disabled = false; b.textContent = was; }
}
async function saveLabel(b) {
  const f = b.closest("[data-book]"), v = k => f.querySelector(`[data-ab="${k}"]`).value;
  try { const r = await api("/api/labels/save", {sbc: b.dataset.abAdd, ip: v("ip"), name: v("name"), detail: v("detail")}); toast(r.message); const box = $("sbcBox"); if (box) box.__h = null; refresh(); }
  catch (e) { toast(e.message, true); }
}
async function deleteLabel(b) {
  try { const r = await api("/api/labels/delete", {sbc: b.dataset.labelDel, ip: b.dataset.ip}); toast(r.message); const box = $("sbcBox"); if (box) box.__h = null; refresh(); }
  catch (e) { toast(e.message, true); }
}
async function importLabels(inp) {
  const f = inp.files && inp.files[0];
  inp.value = "";
  if (!f) return;
  try {
    const r = await api("/api/labels/import", {sbc: inp.dataset.abFile, text: await f.text()});
    toast(r.message, !r.ok);
    const box = $("sbcBox"); if (box) box.__h = null; refresh();
  } catch (e) { toast(e.message, true); }
}
const kindLabel = k => k === "manual" ? "typed" : k === "proxy" ? "proxy set" : k === "interface" ? "interface" : k === "route" ? "route" : k;
function addressCard(x) {
  const rows = (status?.labels || []).filter(n => n.sbc === x.ip);
  const when = rows.find(n => n.kind !== "manual")?.readAt;
  const list = rows.length
    ? `<table class="tbl" style="margin-top:8px"><thead><tr><th>Name</th><th>Address</th><th>From</th><th></th></tr></thead><tbody>${rows.map(n => `<tr><td><b>${esc(n.name)}</b></td><td class="mono">${esc(n.ip)}</td><td class="muted">${esc(kindLabel(n.kind))}${n.detail ? " · " + esc(n.detail) : ""}</td><td class="r"><button class="btn sm" data-label-del="${esc(x.ip)}" data-ip="${esc(n.ip)}">${ic("trash", 13)} Remove</button></td></tr>`).join("")}</tbody></table>`
    : `<p class="muted" style="margin:8px 0 0">No names yet. Add one, or take them from the last backup.</p>`;
  return `<div class="card" style="margin-top:16px"><div class="card-h">${ic("server", 16)}<h2>Address book · ${esc(x.name)}</h2><span class="grow"></span>${when ? `<span class="muted">read ${tm(when)}</span>` : ""}<button class="btn sm" data-backup="${esc(x.ip)}">From last backup</button><button class="btn sm" data-names="${esc(x.ip)}" ${x.hasPassword ? "" : "disabled"} title="${x.hasPassword ? "SSH in and read show running-config" : "Save an SSH login first"}">Read from SBC</button></div>
    <div class="card-b">${list}
      <div class="form-grid" style="margin-top:12px" data-book="${esc(x.ip)}">
        <div><label>Name</label><input type="text" data-ab="name" placeholder="e.g. Dealerboard" aria-label="Name"></div>
        <div><label>Address</label><input type="text" data-ab="ip" placeholder="e.g. 192.0.2.58" aria-label="IP address"></div>
        <div><label>Note <span class="muted">(optional)</span></label><input type="text" data-ab="detail" placeholder="e.g. PBX" aria-label="Note"></div>
        <div style="display:flex;align-items:flex-end"><button class="btn primary" data-ab-add="${esc(x.ip)}">Add</button></div>
      </div>
      <div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center;margin-top:10px">
        <button class="btn sm" type="button" data-ab-template>${ic("down", 13)} Download template</button>
        <label class="btn sm cmp-up">Upload CSV<input type="file" data-ab-file="${esc(x.ip)}" accept=".csv,.txt,.tsv" aria-label="Upload a CSV of addresses"></label>
      </div>
      <p class="help">These names appear on call flows, audio streams and checks. A name you type, or one loaded from a CSV, is kept when names are read from the SBC, and it wins for that address. A CSV updates matching addresses and leaves the others. In Excel, use Save As and choose CSV.</p>
    </div></div>`;
}
/* SSH login per SBC, shown on the SBCs page */
function loginCard(x) {
  return `<details class="m" style="margin-top:4px"><summary><b>SSH login</b> ${x.sshUser ? `<span class="badge ok">${esc(x.sshUser)}${x.hasPassword ? "" : " · no password"}</span>` : '<span class="badge">not set</span>'}<span class="muted" style="font-size:12px">for reading and (with approval) changing configuration</span></summary>
    <div class="form-grid" style="padding:12px" data-login="${esc(x.ip)}">
      <div><label>SSH address <span class="muted">(blank = ${esc(x.ip)})</span></label><input type="text" data-k="sshHost" value="${esc(x.sshHost || "")}"></div>
      <div><label>Port</label><input type="number" data-k="sshPort" value="${x.sshPort || 22}"></div>
      <div><label>Username</label><input type="text" data-k="sshUser" value="${esc(x.sshUser || "")}" autocomplete="off"></div>
      <div><label>Password</label><input type="password" data-k="password" placeholder="${x.hasPassword ? "unchanged" : ""}" autocomplete="new-password" style="height:34px;padding:0 11px;border-radius:8px;border:1px solid var(--border-strong);background:var(--surface);color:var(--text);width:100%"></div>
      <div><label>Enable password <span class="muted">(blank = same as login)</span></label><input type="password" data-k="enablePassword" placeholder="${x.hasEnable ? "unchanged" : ""}" autocomplete="new-password" style="height:34px;padding:0 11px;border-radius:8px;border:1px solid var(--border-strong);background:var(--surface);color:var(--text);width:100%"></div>
      <div style="display:flex;gap:8px;align-items:flex-end;justify-content:flex-end"><button class="btn" data-logintest="${esc(x.ip)}">Test login</button><button class="btn primary" data-loginsave="${esc(x.ip)}">Save login</button></div>
      <div class="full help">Stored encrypted on this server (Windows DPAPI). MCP clients and this page never see the password again.</div>
    </div></details>`;
}
function wireLogins(box) {
  box.querySelectorAll("[data-loginsave]").forEach(b => b.onclick = async () => {
    const f = box.querySelector(`[data-login="${b.dataset.loginsave}"]`), v = k => f.querySelector(`[data-k="${k}"]`).value;
    const body = {ip: b.dataset.loginsave, sshHost: v("sshHost"), sshPort: parseInt(v("sshPort") || "22", 10), sshUser: v("sshUser")};
    if (v("password")) body.password = v("password");
    if (v("enablePassword")) body.enablePassword = v("enablePassword");
    try { await api("/api/sbc-login", body); toast("Login saved"); box.__h = null; refresh(); } catch (e) { toast(e.message, true); }
  });
  box.querySelectorAll("[data-logintest]").forEach(b => b.onclick = async () => {
    b.disabled = true; b.textContent = "Testing…";
    try { const r = await api("/api/sbc-login/test", {id: b.dataset.logintest}); toast(r.message); } catch (e) { toast(e.message, true); }
    b.disabled = false; b.textContent = "Test login";
  });
}

/* ------------------------------------------------------------ bulk line tests */
const LINE_TYPES = {ARD: "ARD", "AUTO RINGDOWN": "ARD", "AUTOMATIC RINGDOWN": "ARD", MRD: "MRD", "MANUAL RINGDOWN": "MRD", HOOT: "HOOT", "HOOT N HOLLER": "HOOT", HOOTNHOLLER: "HOOT", "HOOT & HOLLER": "HOOT", DDI: "DDI", DID: "DDI", DIAL: "DDI", PHONE: "DDI"};
let bulkLines = [], bulkFileName = "", bulkRunSel = null;

/* CSV / TSV / semicolon, quoted fields, header row found by its column names. */
function parseSheet(text) {
  text = text.replace(/^﻿/, "");
  const first = text.split(/\r?\n/)[0] || "";
  const sep = (first.match(/\t/g) || []).length > 0 ? "\t" : (first.match(/;/g) || []).length > (first.match(/,/g) || []).length ? ";" : ",";
  const rows = []; let row = [], cur = "", q = false;
  for (let i = 0; i < text.length; i++) {
    const ch = text[i];
    if (q) { if (ch === '"' && text[i + 1] === '"') { cur += '"'; i++; } else if (ch === '"') q = false; else cur += ch; continue; }
    if (ch === '"') q = true; else if (ch === sep) { row.push(cur.trim()); cur = ""; }
    else if (ch === "\n") { row.push(cur.trim()); rows.push(row); row = []; cur = ""; } else if (ch !== "\r") cur += ch;
  }
  if (cur.length || row.length) { row.push(cur.trim()); rows.push(row); }
  const data = rows.filter(r => r.some(c => c !== ""));
  if (!data.length) return [];
  const h = data[0].map(c => c.toLowerCase().replace(/[^a-z]/g, ""));
  const col = (...names) => h.findIndex(c => names.includes(c));
  let iName = col("name", "line", "label", "description", "linename"), iUri = col("uri", "sipuri", "sip", "address", "number", "aor", "sipaddress");
  let iType = col("type", "linetype", "signalling", "signallingtype", "signaling", "signalingtype", "mode"), iHold = col("hold", "holdseconds", "holds"), iRing = col("ring", "ringseconds", "rings", "timeout"), iSig = col("signal", "ringsignal", "mrdsignal", "signalmethod"), iTest = col("test", "check", "testtype", "how"), iGroup = col("group", "desk", "counterparty", "site", "team", "book");
  let body = data.slice(1);
  if (iUri < 0 && iType < 0) { body = data; iName = 0; iUri = 1; iType = 2; } // no header: name, uri, type
  return body.map((r, n) => {
    const typeRaw = (r[iType] || "").trim(), type = LINE_TYPES[typeRaw.toUpperCase()] || "";
    const uri = (r[iUri] || "").trim();
    const problems = [];
    if (!uri) problems.push("no SIP URI");
    if (!type) problems.push(typeRaw ? `type '${typeRaw}' unknown` : "no line type");
    const sig = iSig >= 0 ? (r[iSig] || "").trim().toUpperCase() : "";
    if (sig && !["DTMF", "INFO", "SIP INFO", "NONE"].includes(sig)) problems.push(`signal '${sig}' unknown`);
    const test = iTest >= 0 ? (r[iTest] || "").trim().toUpperCase() : "";
    if (test && !["WIRE", "CHECK", "CALL", "DIAL"].includes(test)) problems.push(`test '${test}' must be Wire or Call`);
    // Always-on lines (MRD, Hoot) are checked on their live wire call unless the sheet says Call.
    const wire = (type === "MRD" || type === "HOOT") && test !== "CALL" && test !== "DIAL";
    return {row: n + 2, name: iName >= 0 ? r[iName] || "" : "", group: iGroup >= 0 ? (r[iGroup] || "").trim() : "", uri, type, typeRaw, signal: sig, test, wire, hold: iHold >= 0 && r[iHold] ? +r[iHold] : null, ring: iRing >= 0 && r[iRing] ? +r[iRing] : null, problems};
  });
}

function downloadText(name, text, type = "text/csv") {
  const a = document.createElement("a");
  a.href = URL.createObjectURL(new Blob([text], {type}));
  a.download = name; a.click();
  setTimeout(() => URL.revokeObjectURL(a.href), 2000);
}
const csvCell = v => /[",\n]/.test(String(v ?? "")) ? `"${String(v).replace(/"/g, '""')}"` : String(v ?? "");

async function renderBulk(s, force) {
  const box = $("bulkBox");
  // A live refresh must not wipe a field the user is typing in. Loading a sheet is explicit and should redraw.
  if (!force && box.contains(document.activeElement) && /INPUT|TEXTAREA|SELECT/.test(document.activeElement.tagName)) { await drawRuns(); return; }
  const sbcs = (s.sbcs || []).filter(x => x.listed);
  const validLines = bulkLines.filter(l => !l.problems.length);
  const valid = validLines.length;
  const dialN = validLines.filter(l => !l.wire).length;
  const wireN = validLines.filter(l => l.wire).length;
  const badN = bulkLines.length - valid;
  const counts = t => bulkLines.filter(l => l.type === t).length;
  const how = l => !l.type ? "" : l.wire ? `<span class="badge">Check wire</span>` : l.type === "MRD" ? `<span class="badge accent">Call · ${esc(l.signal || "DTMF")}</span>` : `<span class="badge accent">Call</span>`;
  const startLabel = !valid ? "Test lines" : dialN && !wireN ? `Dial ${valid} line${valid === 1 ? "" : "s"}` : wireN && !dialN ? `Measure ${valid} wire${valid === 1 ? "" : "s"}` : `Test ${valid} lines`;
  const plan = bulkLines.length ? `<div class="bulk-plan">${dialN ? `<span class="badge accent">${dialN} will be dialled</span>` : ""}${wireN ? `<span class="badge">${wireN} live wire${wireN === 1 ? "" : "s"} measured, not dialled</span>` : ""}${badN ? `<span class="badge bad">${badN} row${badN === 1 ? "" : "s"} to fix</span>` : ""}</div>` : "";
  const html = `<div class="dash">
    <div class="card span-5"><div class="card-h"><h2>New line test</h2><span class="grow"></span><button class="btn sm" id="bulkTemplate">${ic("down", 13)} Template</button></div><div class="card-b form">
      <div><label for="bulkFile">Sheet of lines (.csv; in Excel use Save As → CSV) or paste rows below</label><input type="file" id="bulkFile" accept=".csv,.txt,.tsv" style="width:100%"></div>
      <div><textarea id="bulkPaste" rows="4" placeholder="Paste from Excel: Name, SIP URI, Type (ARD / MRD / Hoot / DDI), optional Test (Wire / Call), Hold, Ring" style="width:100%;font:12.5px var(--mono);padding:8px 10px;border-radius:8px;border:1px solid var(--border-strong);background:var(--surface);color:var(--text)"></textarea></div>
      <div class="form-grid">
        <div class="full"><label for="bulkName">Run name</label><input type="text" id="bulkName" placeholder="e.g. London dealerboard lines"></div>
        <div class="full"><label for="bulkVia">Send every call through this SBC <span class="muted">(optional)</span></label><input type="text" id="bulkVia" list="bulkSbcs" placeholder="ip:port, e.g. 192.0.2.10:5070 (blank = each URI's host)"><datalist id="bulkSbcs">${sbcs.map(x => `<option value="${esc(x.ip)}">${esc(x.name)}</option>`).join("")}</datalist></div>
        <div><label for="bulkFrom">Calling number</label><input type="text" id="bulkFrom" value="mediantrace"></div>
        <div><label for="bulkHold">Hold after answer / measure wire for (s)</label><input type="number" id="bulkHold" min="1" max="120" value="5"></div>
        <div><label for="bulkAuto">ARD: ring for up to (s)</label><input type="number" id="bulkAuto" min="2" max="120" value="30"></div>
        <div><label for="bulkManual">DDI, and MRD / Hoot set to Call: ring for up to (s)</label><input type="number" id="bulkManual" min="5" max="300" value="30"></div>
        <div class="full"><label for="bulkSig">MRD ring signal, for MRD lines set to Call (when the sheet has no Signal column)</label><select id="bulkSig" style="width:100%"><option value="DTMF">DTMF * to ring, # to stop (SIP INFO dtmf-relay): works through E1/ISDN gateways</option><option value="INFO">SIP INFO signal=ring / stop: SIP end to end only</option></select></div>
      </div>
      <p class="help" style="margin:0">Lines are tested one after another. <b>ARD</b> and <b>DDI</b> lines are called and pass when a person picks up; the call holds with the test tone, then hangs up. <b>MRD</b> and <b>Hoot</b> lines are always-on wires and are not called (a call would replace the live wire call): MediantTrace reads the SBC's active calls to see the wire is up, and measures its audio from the capture. Put <b>Call</b> in a Test column to dial one anyway; an MRD then gets the ring signal after it answers.</p>
    </div></div>
    <div class="card span-7"><div class="card-h"><h2>Lines in this test</h2>${bulkLines.length ? `<span class="muted">${esc(bulkFileName)} · ${bulkLines.length} rows · ARD ${counts("ARD")} · MRD ${counts("MRD")} · Hoot ${counts("HOOT")} · DDI ${counts("DDI")}</span>` : ""}<span class="grow"></span>
      <button class="btn primary" id="bulkStart" ${valid && valid === bulkLines.length && !s.bulkRunning ? "" : "disabled"}>${ic("play", 12)} ${startLabel}</button></div>
      ${plan}
      ${bulkLines.length ? `<div class="scroll" style="max-height:440px"><table class="tbl"><thead><tr><th>Row</th><th>Name</th><th>SIP URI</th><th>Type</th><th>Test</th><th class="r">Hold</th><th class="r">Ring</th><th></th></tr></thead><tbody>${bulkLines.map(l => `<tr><td class="muted">${l.row}</td><td>${esc(l.name)}</td><td class="mono">${esc(l.uri)}</td><td>${l.type ? `<span class="badge">${esc(l.type)}</span>` : `<span class="muted">${esc(l.typeRaw)}</span>`}</td><td>${how(l)}</td><td class="r">${l.hold ?? ""}</td><td class="r">${l.ring ?? ""}</td><td>${l.problems.length ? `<span class="badge bad">${esc(l.problems.join(", "))}</span>` : '<span class="badge ok">ok</span>'}</td></tr>`).join("")}</tbody></table></div>`
        : emptyState("list", "No sheet loaded", "Choose a CSV file or paste rows. Columns: <b>Name</b>, <b>SIP URI</b> (sip:9912@192.0.2.10), <b>Type</b> (ARD, MRD, Hoot or DDI), and optionally <b>Test</b> (Wire or Call), <b>Hold</b> and <b>Ring</b> seconds.")}</div>
  </div><div id="bulkRuns" style="margin-top:16px"></div>`;
  if (setHTML(box, html)) {
    $("bulkTemplate").onclick = () => downloadText("line-test-template.csv", "Name,SIP URI,Type,Test,Signal,Hold,Ring\r\nB2B-1,sip:9901@192.0.2.10,ARD,,,5,30\r\nMRD_B,sip:9912@192.0.2.10,MRD,Wire,,5,\r\nHoot_A,sip:9921@192.0.2.10,Hoot,Wire,,5,\r\nDesk DDI,sip:02071234567@192.0.2.10,DDI,,,10,\r\n");
    $("bulkFile").onchange = () => { const f = $("bulkFile").files[0]; if (!f) return; const r = new FileReader(); r.onload = () => { bulkLines = parseSheet(r.result); bulkFileName = f.name; box.__h = null; renderBulk(status, true); }; r.readAsText(f); };
    $("bulkPaste").oninput = () => { clearTimeout(bulkTimer); bulkTimer = setTimeout(() => { const t = $("bulkPaste").value; bulkLines = parseSheet(t); bulkFileName = "pasted"; const keep = t; box.__h = null; renderBulk(status, true).then(() => { const p = $("bulkPaste"); if (p) p.value = keep; }); }, 500); };
    $("bulkStart").onclick = async () => {
      const dial = bulkLines.filter(l => !l.problems.length && !l.wire).length;
      const wires = bulkLines.filter(l => !l.problems.length && l.wire).length;
      const tone = "Each answered line hears the test tone, then the call hangs up.";
      const measure = "These are not dialled. The live wire call stays up; MediantTrace checks the SBC's active calls and measures the audio already on the wire.";
      const ask = dial && !wires ? `Dial ${dial} line${dial === 1 ? "" : "s"}, one after another?\n\n${tone}`
        : wires && !dial ? `Measure ${wires} live wire${wires === 1 ? "" : "s"}?\n\n${measure}`
        : `Test ${dial + wires} lines, one after another?\n\n${dial} will be dialled. ${tone}\n\n${wires} always-on wire${wires === 1 ? "" : "s"} will only be measured. ${measure}`;
      if (!confirm(ask)) return;
      const n = id => parseInt($(id).value || "0", 10) || undefined;
      try {
        const r = await api("/api/bulk/start", {name: $("bulkName").value, via: $("bulkVia").value, from: $("bulkFrom").value, holdSeconds: n("bulkHold"), autoRingSeconds: n("bulkAuto"), manualRingSeconds: n("bulkManual"), mrdSignal: $("bulkSig").value,
          lines: bulkLines.map(l => ({name: l.name, uri: l.uri, type: l.type, test: l.test || undefined, signal: l.signal || undefined, holdSeconds: l.hold || undefined, ringSeconds: l.ring || undefined}))});
        toast(r.message); bulkRunSel = r.id; bulkLines = []; box.__h = null;
      } catch (e) { toast(e.message, true); }
      refresh();
    };
  }
  await drawRuns();
}
let bulkTimer;

async function drawRuns() {
  const runs = await api("/api/bulk");
  const el = $("bulkRuns"); if (!el) return;
  if (!runs.length) { setHTML(el, ""); return; }
  if (!bulkRunSel || !runs.some(r => r.id === bulkRunSel)) bulkRunSel = runs[0].id;
  const run = runs.find(r => r.id === bulkRunSel);
  const done = run.lines.filter(l => ["passed", "warning", "failed", "skipped"].includes(l.status)).length;
  const c = st => run.lines.filter(l => l.status === st).length;
  const statusBadge = st => ({queued: '<span class="badge">Queued</span>', calling: '<span class="badge accent">Calling…</span>', checking: '<span class="badge accent">Checking wire…</span>', ringing: '<span class="badge accent">Ringing…</span>', connected: '<span class="badge accent">Connected</span>', passed: '<span class="badge ok">Passed</span>', warning: '<span class="badge warn">Warning</span>', failed: '<span class="badge bad">Failed</span>', skipped: '<span class="badge">Skipped</span>'}[st] || esc(st));
  const html = `<div class="card"><div class="card-h"><h2>${esc(run.name)}</h2>${run.state === "running" ? '<span class="rec" style="padding:3px 10px"><span class="pulse"></span>RUNNING</span>' : `<span class="badge">${esc(run.state)}</span>`}
      <span class="muted">${dtm(run.started)}${run.via ? " · via " + esc(run.via) : ""}</span><span class="grow"></span>
      ${runs.length > 1 ? `<select id="bulkPick" aria-label="Run">${runs.map(r => `<option value="${esc(r.id)}" ${r.id === run.id ? "selected" : ""}>${esc(r.name)} · ${dtm(r.started)}</option>`).join("")}</select>` : ""}
      ${run.state === "running" ? `<button class="btn danger sm" id="bulkStop">${ic("stop", 12)} Stop</button>` : `<button class="btn sm" id="bulkCsv">${ic("down", 13)} CSV</button><button class="btn sm primary" id="bulkReport">${ic("report", 13)} Report</button>`}</div>
    <div class="card-b">
      <div style="display:flex;gap:16px;align-items:center;flex-wrap:wrap;margin-bottom:12px">
        <div style="flex:1;min-width:200px"><div class="dir" style="grid-template-columns:1fr 70px;margin:0"><div class="bar" style="height:10px"><i style="width:${Math.max(2, done / run.lines.length * 100)}%"></i></div><span class="n">${done}/${run.lines.length}</span></div></div>
        <span class="badge ok">${c("passed")} passed</span><span class="badge warn">${c("warning")} warning</span><span class="badge bad">${c("failed")} failed</span>${c("skipped") ? `<span class="badge">${c("skipped")} skipped</span>` : ""}
      </div>
      <div class="scroll" style="max-height:60vh"><table class="tbl"><thead><tr><th>#</th><th>Line</th><th>Type</th><th>Status</th><th>SIP</th><th class="r">Answered after</th><th class="r">Audio back</th><th>MOS</th><th>Finding</th></tr></thead><tbody>
      ${run.lines.map(l => `<tr ${l.resultId ? `class="click" data-res="${esc(l.resultId)}" style="cursor:pointer"` : ""}><td class="muted">${l.index}</td><td><b>${esc(l.name)}</b><div class="muted mono" style="font-size:11.5px">${esc(l.uri)}</div></td><td><span class="badge">${esc(l.type)}</span>${l.signal ? `<div class="muted" style="font-size:11px">${esc(l.signal)}</div>` : ""}</td><td>${statusBadge(l.status)}</td>
        <td>${l.sipStatus || ""}</td><td class="r">${l.answerSeconds != null ? l.answerSeconds.toFixed(1) + " s" : ""}</td><td class="r">${l.rxPackets ? fmtN(l.rxPackets) : ""}</td><td>${mosBadge(l.mos)}</td><td style="white-space:normal;min-width:240px">${esc(l.summary)}</td></tr>`).join("")}
      </tbody></table></div></div></div>`;
  if (setHTML(el, html)) {
    const pick = $("bulkPick"); if (pick) pick.onchange = () => { bulkRunSel = pick.value; drawRuns(); };
    const stop = $("bulkStop"); if (stop) stop.onclick = async () => { try { const r = await api("/api/bulk/stop", {}); toast(r.message); } catch (e) { toast(e.message, true); } };
    const csv = $("bulkCsv"); if (csv) csv.onclick = () => downloadText(`${run.name.replace(/[^A-Za-z0-9._-]+/g, "_")}.csv`,
      ["#,Name,SIP URI,Type,Status,SIP,Answered after (s),Audio packets back,MOS,Finding"].concat(run.lines.map(l => [l.index, l.name, l.uri, l.type, l.status, l.sipStatus || "", l.answerSeconds ?? "", l.rxPackets || "", l.mos ?? "", l.summary].map(csvCell).join(","))).join("\r\n"));
    const rep = $("bulkReport"); if (rep) rep.onclick = () => openReport(`Line test: ${run.name}`, `${esc(dtm(run.started))}${run.via ? " · via " + esc(run.via) : ""} · ${run.lines.length} lines`,
      `<div class="dash" style="margin-bottom:16px"><div class="card stat span-4"><div class="l">Passed</div><div class="v" style="color:var(--ok)">${c("passed")}</div></div><div class="card stat span-4"><div class="l">Warnings</div><div class="v" style="color:var(--warn)">${c("warning")}</div></div><div class="card stat span-4"><div class="l">Failed</div><div class="v" style="color:var(--bad)">${c("failed")}</div></div></div>` +
      `<table class="tbl"><thead><tr><th>#</th><th>Line</th><th>Type</th><th>Status</th><th>SIP</th><th class="r">Answered after</th><th class="r">Audio back</th><th>MOS</th><th>Finding</th></tr></thead><tbody>${run.lines.map(l => `<tr><td>${l.index}</td><td><b>${esc(l.name)}</b><div class="muted mono">${esc(l.uri)}</div></td><td>${esc(l.type)}</td><td>${statusBadge(l.status)}</td><td>${l.sipStatus || ""}</td><td class="r">${l.answerSeconds != null ? l.answerSeconds.toFixed(1) + " s" : ""}</td><td class="r">${l.rxPackets ? fmtN(l.rxPackets) : ""}</td><td>${mosBadge(l.mos)}</td><td style="white-space:normal">${esc(l.summary)}</td></tr>`).join("")}</tbody></table>`);
    el.querySelectorAll("[data-res]").forEach(r => r.onclick = () => { selectedResult = r.dataset.res; selectedTest = null; go("tests"); });
  }
}

/* ------------------------------------------------------------ timeline */
const TL_KINDS = {alarm: "SBC alarm", restart: "SBC restart", change: "Config change", upgrade: "Upgrade", wire: "Wire", test: "Call test", linetest: "Line test", ready: "Ready check"};
let tlFlash = null;

function followLink(link) {
  const q = new URLSearchParams(link.replace(/^#/, ""));
  if (q.get("result")) { selectedResult = q.get("result"); selectedTest = null; go("tests"); }
  else if (q.get("view")) go(q.get("view"));
}

async function renderTimeline() {
  const box = $("tlBox");
  if (box.contains(document.activeElement) && /INPUT|SELECT/.test(document.activeElement.tagName)) return;
  const hours = store.get("tlHours", 24), only = store.get("tlOnly", false), kinds = store.get("tlKinds", Object.keys(TL_KINDS));
  const [events, calls] = await Promise.all([api("/api/timeline?hours=" + hours), api("/api/calls").catch(() => [])]);
  const now = Date.now(), from = now - hours * 3600e3;
  const shown = events.filter(e => kinds.includes(e.kind) && (!only || e.level === "bad" || e.level === "warn"));

  // Calls per bucket over the range: failed against completed, so a spike lines up with the events under it.
  const stepS = [300, 600, 900, 1800, 3600, 7200, 10800, 21600].find(v => hours * 3600 / v <= 48) || 21600;
  const n = Math.ceil(hours * 3600 / stepS), t0 = now - n * stepS * 1000;
  const xs = Array.from({length: n}, (_, i) => t0 + i * stepS * 1000);
  const inRange = calls.filter(c => +new Date(c.started) >= t0);
  const count = pred => { const v = new Array(n).fill(0); inRange.filter(pred).forEach(c => v[Math.min(n - 1, Math.floor((+new Date(c.started) - t0) / (stepS * 1000)))]++); return v; };
  const per = stepS < 3600 ? `${stepS / 60} minutes` : stepS === 3600 ? "hour" : `${stepS / 3600} hours`;
  const fmtX = v => hours > 24 ? new Date(v).toLocaleString([], {weekday: "short", hour: "2-digit", minute: "2-digit"}) : tm(v).slice(0, 5);
  const marks = shown.filter(e => e.level !== "ok" || e.kind === "wire").filter(e => +new Date(e.at) >= t0)
    .map((e, i) => `<i class="${e.level}" style="left:${((+new Date(e.at) - t0) / (now - t0) * 100).toFixed(2)}%" title="${esc(tm(e.at))} ${esc(e.title)}" data-mark="${events.indexOf(e)}"></i>`).join("");
  const failed = count(c => c.state === "failed"), ok = count(c => c.state !== "failed");

  let lastDay = "";
  const rows = shown.map(e => {
    const d = new Date(e.at), day = d.toDateString();
    const head = day !== lastDay ? `<div class="tl-day">${esc(d.toLocaleDateString([], {weekday: "long", day: "numeric", month: "long"}))}</div>` : "";
    lastDay = day;
    return head + `<div class="tl-ev ${e.level}" id="tl-${events.indexOf(e)}" ${e.link ? `data-link="${esc(e.link)}"` : ""}><span class="t">${esc(tm(e.at).slice(0, 5))}</span><span class="i" title="${esc(e.level)}">${ic(e.level === "bad" ? "x" : e.level === "warn" ? "alert" : e.level === "ok" ? "check" : "info", 16)}</span>
      <span class="k"><span class="badge">${esc(TL_KINDS[e.kind] || e.kind)}</span></span><span><b>${esc(e.title)}</b>${e.sbc ? ` <span class="muted">${esc(e.sbc)}</span>` : ""}${e.detail ? `<div class="muted" style="margin-top:2px">${esc(e.detail)}</div>` : ""}</span></div>`;
  }).join("");

  const html = `
    <div class="card" style="margin-bottom:16px"><div class="card-b" style="display:flex;gap:14px;align-items:center;flex-wrap:wrap;padding:12px 16px">
      <select id="tlHours" aria-label="How far back">${[[6, "Last 6 hours"], [24, "Last 24 hours"], [72, "Last 3 days"], [168, "Last 7 days"], [720, "Last 30 days"]].map(([h, l]) => `<option value="${h}" ${hours === h ? "selected" : ""}>${l}</option>`).join("")}</select>
      <label style="display:flex;gap:6px;align-items:center"><input type="checkbox" id="tlOnly" ${only ? "checked" : ""}> Problems and warnings only</label>
      <span class="grow"></span>
      ${Object.entries(TL_KINDS).map(([k, l]) => `<label style="display:flex;gap:5px;align-items:center;font-size:12.5px"><input type="checkbox" class="tlKind" value="${k}" ${kinds.includes(k) ? "checked" : ""}>${l}</label>`).join("")}
    </div></div>
    <div class="card" style="margin-bottom:16px"><div class="card-h"><h2>Calls and events</h2><span class="muted">calls per ${per} from the SBC logs since MediantTrace started; dots are events, click one to find it below</span></div>
      <div class="card-b">${inRange.length ? barChart({id: "tl-calls", label: "Calls per " + per, xs, h: 150, fmtX, stacks: [{name: "Completed", color: "var(--good)", values: ok}, {name: "Failed", color: "var(--critical)", values: failed}]})
        : `<div class="muted" style="padding:16px 0">No calls in the SBC logs in this range${calls.length ? "" : " (the call view starts empty when MediantTrace starts)"}.</div>`}
        <div class="tl-strip" aria-label="Events on the time axis">${marks}</div></div></div>
    <div class="card">${rows || emptyState("clock", "Nothing in this range", "Events appear here as they happen: SBC alarms and restarts (read on the SBC health page), config changes, upgrades, wires going down (turn on the line monitor), call tests, line tests and ready checks.")}</div>`;
  if (!setHTML(box, html)) { if (tlFlash) { $("tl-" + tlFlash)?.classList.add("flash"); tlFlash = null; } return; }
  $("tlHours").onchange = () => { store.set("tlHours", +$("tlHours").value); box.__h = null; renderTimeline(); };
  $("tlOnly").onchange = () => { store.set("tlOnly", $("tlOnly").checked); box.__h = null; renderTimeline(); };
  box.querySelectorAll(".tlKind").forEach(c => c.onchange = () => { store.set("tlKinds", [...box.querySelectorAll(".tlKind:checked")].map(x => x.value)); box.__h = null; renderTimeline(); });
  box.querySelectorAll("[data-link]").forEach(r => r.onclick = () => followLink(r.dataset.link));
  box.querySelectorAll("[data-mark]").forEach(m => m.onclick = () => { const row = $("tl-" + m.dataset.mark); if (row) { row.scrollIntoView({behavior: "smooth", block: "center"}); row.classList.remove("flash"); void row.offsetWidth; row.classList.add("flash"); } });
}

/* ------------------------------------------------------------ upgrades */
let upSel = null, upCompare = {};

function upSnapshotBox(label, sn) {
  if (!sn) return "";
  const wiresUp = sn.wires.filter(w => w.state === "up" || w.state === "warn").length;
  return `<div class="hl-sub"><h3>${label} <span class="muted" style="font-weight:500">${esc(dtm(sn.at))}</span></h3><dl class="lb-kv card-b" style="margin:0">
    <dt>Version</dt><dd class="mono">${esc(sn.version)}</dd><dt>Board / serial</dt><dd>${esc(sn.board)} · ${esc(sn.serial)}</dd>
    <dt>SBC licence</dt><dd>${esc((sn.licence || "–").replace(/\s*\(.*$/, ""))}</dd><dt>Up for</dt><dd>${upText(sn.uptimeHours)}</dd>
    <dt>Calls up</dt><dd>${sn.activeCalls}</dd><dt>Alarms</dt><dd>${sn.alarms.length ? sn.alarms.map(a => sevBadge(a.severity)).join(" ") : "none"}</dd>
    <dt>Proxy sets</dt><dd>${sn.proxySets.map(p => `<span class="badge ${p.status === "ONLINE" ? "ok" : p.keepAlive ? "bad" : ""}">${p.id}: ${esc(p.status.toLowerCase())}</span>`).join(" ") || "–"}</dd>
    <dt>E1 trunks</dt><dd>${sn.trunks.map(t => `<span class="badge ${t.state === "active" && t.dChannel === "established" ? "ok" : "bad"}">${esc(t.port)}</span>`).join(" ") || "none"}</dd>
    <dt>Wires up</dt><dd>${sn.wires.length ? `${wiresUp} of ${sn.wires.length}` : "no wires on the line board"}</dd>
    <dt>Config backup</dt><dd class="mono" style="font-size:12px">${esc(sn.configFile)} (${sn.configLines} lines)</dd></dl></div>`;
}

function upCompareHtml(c) {
  if (!c) return `<div class="card-b muted">Comparing…</div>`;
  if (c.message) return `<div class="card-b"><div class="alert bad"><span class="ic">${ic("alert", 18)}</span><div>${esc(c.message)}</div></div></div>`;
  const vt = {good: ["ok", "check", "The upgrade looks clean"], check: ["warn", "alert", "The upgrade worked, with things to check"], problems: ["bad", "x", "Problems after the upgrade"]}[c.verdict];
  const diff = c.config.hunks.length ? `<details class="hl-sub" style="margin:0 16px 16px" ${c.config.hunks.length <= 6 ? "open" : ""}><summary style="padding:9px 12px;background:var(--surface-2);cursor:pointer;font-weight:650;font-size:13px">Configuration: ${c.config.added} line${c.config.added === 1 ? "" : "s"} added, ${c.config.removed} removed</summary>
      <div class="up-diff">${c.config.hunks.map(h => `<div class="sec">${esc(h.section || "(top)")}</div>${h.lines.map(l => `<div class="${l.op === "+" ? "add" : l.op === "-" ? "del" : ""}">${l.op === " " ? "  " : esc(l.op) + " "}${esc(l.text)}</div>`).join("")}`).join("")}</div></details>` : "";
  return `<div class="alert ${vt[0] === "ok" ? "info" : vt[0]}" style="margin:16px"><span class="ic">${ic(vt[1], 18)}</span><div><b>${vt[2]}.</b></div></div>
    ${c.changes.map(x => `<div class="up-chg ${x.level}"><span class="i">${ic(x.level === "bad" ? "x" : x.level === "warn" ? "alert" : "info", 16)}</span><b>${esc(x.area)}</b><span class="t">${esc(x.text)}</span></div>`).join("")}
    <div style="height:12px"></div>${diff}`;
}

async function renderUpgrades() {
  const box = $("upBox");
  if (box.contains(document.activeElement) && /INPUT|TEXTAREA|SELECT/.test(document.activeElement.tagName)) return;
  const data = await api("/api/upgrades");
  const recs = data.records;
  const rec = recs.find(r => r.id === upSel) || recs[0] || null;
  if (rec && rec.before && rec.after && !rec.busy && !upCompare[rec.id + rec.after.at]) {
    try { upCompare[rec.id + rec.after.at] = await api("/api/upgrades/compare?id=" + encodeURIComponent(rec.id)); }
    catch (e) { upCompare[rec.id + rec.after.at] = {message: e.message}; }
  }
  const step = !rec ? 1 : !rec.before ? 1 : !rec.after ? 2 : 3;
  const st = (n, title, text) => `<div class="up-step ${step > n || (n === 3 && rec?.after) ? "done" : step === n ? "now" : ""}"><h3><span class="n">${step > n || (n === 3 && rec?.after) ? "✓" : n}</span>${title}</h3><p>${text}</p></div>`;
  const sbcs = data.sbcs.filter(x => x.login);
  const html = `
    <div class="card" style="margin-bottom:16px"><div class="card-h">${ic("out", 16)}<h2>Firmware upgrade assistant</h2><span class="grow"></span></div>
      <div class="card-b">
        <div class="up-steps">
          ${st(1, "Baseline before", "Version, licence, full config backup, alarms, proxy sets, E1 trunks and which wires are up. Read-only.")}
          ${st(2, "Upgrade the SBC yourself", "In the SBC's web page (Maintenance → Software Update) or with AudioCodes. All calls on the SBC drop while it restarts: do it in a maintenance window.")}
          ${st(3, "Snapshot after, compare", "The same readings again; only what changed is shown: version, licence, new alarms, proxy sets, trunks, wires that did not come back, config lines.")}
        </div>
        <div style="display:flex;gap:10px;flex-wrap:wrap;align-items:end">
          <div><label for="upSbc">SBC</label><select id="upSbc">${sbcs.map(x => `<option value="${esc(x.ip)}">${esc(x.name)} (${esc(x.ip)})</option>`).join("")}</select></div>
          <div style="flex:1;min-width:220px"><label for="upNote">Note <span class="muted">(e.g. 7.20A.104 → 7.40A.606, change ticket)</span></label><input type="text" id="upNote" style="width:100%"></div>
          <button class="btn primary" id="upBase" ${sbcs.length ? "" : "disabled"}>${ic("rec", 12)} Take baseline</button>
        </div>
        ${sbcs.length ? "" : '<p class="help">Add an SSH login for the SBC on the SBCs page first.</p>'}
      </div></div>
    ${rec ? `<div class="dash">
      <div class="span-8"><div class="card" style="margin-bottom:16px"><div class="card-h"><h2>${esc(rec.sbcName)}</h2><span class="muted mono">${esc(rec.sbcIp)}</span>${rec.note ? `<span class="muted">${esc(rec.note)}</span>` : ""}<span class="grow"></span>
          ${rec.busy ? `<span class="badge accent">${ic("clock", 12)} taking the ${rec.busy === "before" ? "baseline" : "after snapshot"}…</span>` : rec.before && !rec.after ? `<button class="btn primary sm" id="upAfter">${ic("rec", 12)} Snapshot after the upgrade</button>` : rec.after ? `<button class="btn sm" id="upAfter">${ic("swap", 13)} Snapshot again</button>` : ""}
          ${rec.after ? `<button class="btn sm" id="upReport">${ic("report", 13)} Report</button>` : ""}
          ${!rec.busy ? `<button class="btn ghost icon sm" id="upDel" title="Remove this upgrade" aria-label="Remove this upgrade">${ic("trash", 14)}</button>` : ""}</div>
        ${rec.error ? `<div class="card-b"><div class="alert bad"><span class="ic">${ic("alert", 18)}</span><div>${esc(rec.error)}</div></div></div>` : ""}
        ${rec.after && !rec.busy ? upCompareHtml(upCompare[rec.id + rec.after.at]) : rec.before ? `<div class="card-b"><div class="alert info"><span class="ic">${ic("info", 18)}</span><div><b>Baseline taken.</b> Upgrade the SBC now${rec.before.activeCalls ? ` (it had <b>${rec.before.activeCalls} call${rec.before.activeCalls === 1 ? "" : "s"} up</b> at the baseline: they will drop)` : ""}. When it is back, press <b>Snapshot after the upgrade</b>.${rec.before.uptimeHours != null && rec.before.uptimeHours < 24 ? " The SBC restarted less than a day ago: make sure the configuration you want is saved before upgrading." : ""}</div></div></div>` : ""}
        <div class="card-b"><div class="hl-two" style="margin:0">${upSnapshotBox("Before", rec.before)}${upSnapshotBox("After", rec.after)}</div></div>
      </div></div>
      <div class="card span-4" style="align-self:start"><div class="card-h">${ic("clock", 16)}<h2>Upgrades</h2></div>
        ${recs.map(r => `<div class="rd-hist ${r.id === rec.id ? "sel" : ""}" data-up="${esc(r.id)}"><span><b>${esc(r.sbcName)}</b> ${esc(dtm(r.created))}<div class="muted" style="font-size:12px">${esc(r.before?.version || "")}${r.after ? " → " + esc(r.after.version) : ""} ${esc(r.note)}</div></span><span class="badge ${r.after ? "ok" : r.before ? "accent" : ""}">${r.busy ? "taking…" : r.after ? "compared" : r.before ? "baseline" : "–"}</span></div>`).join("")}
      </div></div>` : ""}`;
  if (!setHTML(box, html)) return;
  const act = async (body, msg) => { try { const r = await api("/api/upgrades/snapshot", body); toast(r.message); if (r.id) upSel = r.id; } catch (e) { toast(e.message, true); } box.__h = null; renderUpgrades(); };
  $("upBase").onclick = () => act({sbc: $("upSbc").value, note: $("upNote").value});
  const af = $("upAfter"); if (af) af.onclick = () => act({id: rec.id});
  const del = $("upDel"); if (del) del.onclick = async () => { if (!confirm("Remove this upgrade record? The config backups stay in sbc-backups.")) return; try { const r = await api("/api/upgrades/delete", {id: rec.id}); toast(r.message); upSel = null; } catch (e) { toast(e.message, true); } box.__h = null; renderUpgrades(); };
  const rp = $("upReport"); if (rp) rp.onclick = () => openReport(`SBC upgrade: ${rec.sbcName}`, `${esc(rec.before.version)} → ${esc(rec.after.version)} · ${esc(dtm(rec.before.at))} to ${esc(dtm(rec.after.at))}${rec.note ? " · " + esc(rec.note) : ""}`,
    `<div class="card">${upCompareHtml(upCompare[rec.id + rec.after.at]).replace(/<details /g, "<details open ")}</div><div class="hl-two" style="margin-top:16px">${upSnapshotBox("Before", rec.before)}${upSnapshotBox("After", rec.after)}</div>`);
  box.querySelectorAll("[data-up]").forEach(e => e.onclick = () => { upSel = e.dataset.up; box.__h = null; renderUpgrades(); });
}

/* ------------------------------------------------------------ sbc health */
let healthRefresh = null;
const upText = h => h == null ? "–" : h < 1 ? `${Math.round(h * 60)} min` : h < 48 ? `${Math.floor(h)} h ${Math.round((h % 1) * 60)} min` : `${Math.floor(h / 24)} days`;
const sevBadge = sv => `<span class="badge ${sv === "critical" || sv === "major" ? "bad" : sv === "cleared" ? "ok" : "warn"}">${esc(sv)}</span>`;
const hlKpi = (label, value, state, pct) => `<div class="hl-kpi ${state || ""}"><div class="l">${label}</div><div class="v">${value}</div>${pct != null ? `<div class="hl-meter" role="img" aria-label="${label} ${pct}%"><i style="width:${Math.min(100, pct)}%"></i></div>` : ""}</div>`;

function healthCard(x) {
  const r = x.report;
  const st = !r ? "" : r.state;
  const head = `<div class="card-h">${ic(st === "bad" ? "x" : st === "warn" ? "alert" : st === "ok" ? "check" : "server", 18)}<h2>${esc(x.name)}</h2><span class="muted mono">${esc(x.ip)}</span>
      ${r ? `<span class="muted">${esc(r.board)} · ${esc(r.version)}</span>` : ""}<span class="grow"></span>
      ${st ? `<span class="badge ${st === "bad" ? "bad" : st === "warn" ? "warn" : "ok"}">${st === "bad" ? "Problem" : st === "warn" ? "Warning" : "Healthy"}</span>` : ""}
      <span class="muted">${x.reading ? "reading…" : x.readAt ? "read " + esc(tm(x.readAt)) : ""}</span>
      ${x.login ? `<button class="btn sm" data-refresh="${esc(x.ip)}" ${x.reading ? "disabled" : ""}>${ic("swap", 13)} Read now</button>` : ""}
      ${x.url ? `<a class="btn sm" href="${esc(x.url)}" target="_blank" rel="noopener">${ic("ext", 13)} Web</a>` : ""}</div>`;
  if (!x.login) return `<div class="card" style="margin-bottom:16px">${head}<div class="card-b">${emptyState("server", "No SSH login for this SBC", "Add one on the SBCs page. The health page only runs read-only show commands.")}</div></div>`;
  if (!r) return `<div class="card" style="margin-bottom:16px">${head}<div class="card-b">${x.error ? `<div class="alert bad"><span class="ic">${ic("alert", 18)}</span><div><b>Could not read the SBC.</b> ${esc(x.error)}</div></div>` : emptyState("clock", "Reading the SBC…", "Ten show commands over one SSH session: a few seconds.")}</div></div>`;

  const gw = r.gateway || {}, sbc = r.sbc || {};
  const cpuSt = r.cpu >= 85 ? "bad" : r.cpu >= 70 ? "warn" : "";
  const memSt = r.memory >= 90 ? "bad" : r.memory >= 80 ? "warn" : "";
  const kpis = `<div class="hl-kpis">
      ${hlKpi("Up for", upText(r.uptimeHours), r.uptimeHours != null && r.uptimeHours < 24 ? "warn" : "")}
      ${hlKpi("CPU (voice)", r.cpu != null ? r.cpu + "%" : "–", cpuSt, r.cpu)}
      ${hlKpi("Memory (voice)", r.memory != null ? r.memory + "%" : "–", memSt, r.memory)}
      ${hlKpi("Calls now", `${esc(gw["Active TDM channels"] ?? "–")} <span class="muted" style="font-size:12px;font-weight:500">TDM</span> · ${esc(sbc["Active INVITE dialogs"] ?? "–")} <span class="muted" style="font-size:12px;font-weight:500">SBC</span>`)}
      ${hlKpi("Clock", r.ntpSynced == null ? "–" : r.ntpSynced ? "In step" : "Not synced", r.ntpSynced === false ? "warn" : "")}
      ${hlKpi("SBC licence", esc((r.sbcSessions || "–").replace(/\s*\(.*$/, "")))}
    </div>`;
  const findings = r.findings.length ? `<div class="alert ${st === "bad" ? "bad" : "warn"}" style="margin-bottom:14px"><span class="ic">${ic("alert", 18)}</span><div>${r.findings.map(f => `<div>${esc(f)}</div>`).join("")}</div></div>` : "";
  const proxies = `<div class="hl-sub"><h3>${ic("link", 14)} Proxy sets</h3>${r.proxySets.length ? `<table class="tbl"><thead><tr><th>Set</th><th>Address</th><th>Keepalive</th><th class="r">OK</th><th class="r">Failed</th><th>Status</th></tr></thead><tbody>
      ${r.proxySets.map(p => (p.addresses.length ? p.addresses : [{address: "", success: null, failed: null}]).map((a, i) => `<tr><td>${i ? "" : p.id}</td><td class="mono">${esc(a.address)}</td><td>${i ? "" : p.keepAlive ? "on" : "off"}</td><td class="r">${a.success ?? ""}</td><td class="r">${a.failed ? `<b style="color:var(--bad)">${fmtN(a.failed)}</b>` : a.failed ?? ""}</td>
        <td>${i ? "" : `<span class="badge ${p.status === "ONLINE" ? "ok" : p.keepAlive ? "bad" : ""}">${esc(p.status.toLowerCase())}</span>`}</td></tr>`).join("")).join("")}</tbody></table>` : '<div class="card-b muted">None reported.</div>'}</div>`;
  const trunks = `<div class="hl-sub"><h3>${ic("wave", 14)} E1 trunks</h3>${r.trunks.length ? `<table class="tbl"><thead><tr><th>Port</th><th>State</th><th>D-channel</th><th>Line alarms</th></tr></thead><tbody>
      ${r.trunks.map(t => `<tr><td class="mono">${esc(t.port)}</td><td><span class="badge ${t.state === "active" ? "ok" : "bad"}">${esc(t.state)}</span></td><td><span class="badge ${t.dChannel === "established" ? "ok" : "bad"}">${esc(t.dChannel)}</span></td><td class="${t.alarmsClear ? "muted" : ""}">${t.alarmsClear ? "none" : `<b style="color:var(--bad)">${esc(t.alarms)}</b>`}</td></tr>`).join("")}</tbody></table>` : '<div class="card-b muted">No E1/T1 trunks.</div>'}</div>`;
  const alarmRows = list => list.map(a => `<tr><td>${sevBadge(a.severity)}</td><td style="white-space:normal">${esc(a.description)}</td><td class="mono muted">${esc(a.source)}</td><td class="muted">${esc(a.date)}</td></tr>`).join("");
  const alarms = `<div class="hl-sub" style="margin-bottom:14px"><h3>${ic("alert", 14)} Active alarms <span class="muted" style="font-weight:500">${r.alarms.length}</span></h3>${r.alarms.length ? `<table class="tbl"><thead><tr><th>Severity</th><th>Alarm</th><th>Source</th><th>Since</th></tr></thead><tbody>${alarmRows(r.alarms)}</tbody></table>` : '<div class="card-b muted">No active alarms.</div>'}</div>`;
  const history = r.alarmHistory.length ? `<details class="hl-sub" style="margin-bottom:14px"><summary style="padding:9px 12px;background:var(--surface-2);cursor:pointer;font-weight:650;font-size:13px">Alarm history (latest ${r.alarmHistory.length})</summary><table class="tbl"><tbody>${alarmRows(r.alarmHistory)}</tbody></table></details>` : "";
  const stat = (o, k, label) => o[k] != null ? `<dt>${label}</dt><dd>${esc(o[k])}</dd>` : "";
  const stats = `<div class="hl-two" style="margin-bottom:0">
      <div class="hl-sub"><h3>${ic("phone", 14)} Gateway (TDM) calls since restart</h3><dl class="lb-kv card-b" style="margin:0">${stat(gw, "Total call attempts", "Call attempts")}${stat(gw, "Active TDM channels", "TDM channels in use")}${stat(gw, "Active DSP resources", "DSP resources in use")}${stat(gw, "Average voice delay (ms)", "Average delay (ms)")}${stat(gw, "Average voice jitter (ms)", "Average jitter (ms)")}${stat(gw, "Total Tx RTP packets", "Audio packets sent")}${stat(gw, "Total Rx RTP packets", "Audio packets received")}</dl></div>
      <div class="hl-sub"><h3>${ic("phone", 14)} SBC calls since restart</h3><dl class="lb-kv card-b" style="margin:0">${stat(sbc, "Call attempts", "Call attempts")}${stat(sbc, "Established calls", "Established")}${stat(sbc, "Calls terminated due to busy line", "Busy")}${stat(sbc, "Calls terminated due to no answer", "No answer")}${stat(sbc, "Calls terminated due to media negotiation failure", "Media negotiation failed")}${stat(sbc, "Calls terminated due to resource allocation failure", "No resources")}${stat(sbc, "Calls terminated due to general failure", "General failure")}${stat(sbc, "Calls abnormally terminated", "Ended abnormally")}</dl></div>
    </div>`;
  return `<div class="card" style="margin-bottom:16px">${head}<div class="card-b">${x.error ? `<div class="alert warn" style="margin-bottom:14px"><span class="ic">${ic("alert", 18)}</span><div>Last read failed: ${esc(x.error)}. Showing the read from ${esc(tm(x.readAt))}.</div></div>` : ""}${findings}${kpis}<div class="hl-two">${proxies}${trunks}</div>${alarms}${history}${stats}</div></div>`;
}

async function renderHealth() {
  const box = $("healthBox");
  const list = await api("/api/health" + (healthRefresh ? "?refresh=" + encodeURIComponent(healthRefresh) : ""));
  healthRefresh = null;
  const bad = list.filter(x => x.report?.state === "bad").length;
  $("nc-health").textContent = bad ? String(bad) : "";
  const html = list.length ? list.map(healthCard).join("") : `<div class="card">${emptyState("server", "No SBCs listed", "Add your SBCs on the SBCs page.")}</div>`;
  const open = [...box.querySelectorAll("details[open]")].length > 0;
  if (!setHTML(box, html)) return;
  if (open) box.querySelectorAll("details").forEach(d => d.open = true);
  box.querySelectorAll("[data-refresh]").forEach(b => b.onclick = () => { healthRefresh = b.dataset.refresh; box.__h = null; renderHealth(); });
}

/* ------------------------------------------------------------ ready check */
let readySel = null, readySettings = false;
const READY = {
  ready: {t: "Ready for business", i: "check"}, warnings: {t: "Ready, with warnings", i: "alert"},
  notready: {t: "Not ready", i: "x"}, running: {t: "Checking…", i: "clock"}, stopped: {t: "Stopped", i: "stop"},
};
const readyIcon = st => ic(st === "ok" ? "check" : st === "warn" ? "alert" : st === "bad" ? "x" : st === "running" ? "clock" : "info", 16);
const readyStateText = st => ({ok: "OK", warn: "Warning", bad: "Problem", skip: "Not checked", running: "Checking"}[st] || st);

function readyItems(run) {
  const areas = [...new Set(run.items.map(i => i.area))];
  return areas.map(a => {
    const items = run.items.filter(i => i.area === a);
    const bad = items.filter(i => i.state === "bad").length, warn = items.filter(i => i.state === "warn").length;
    return `<div class="card" style="margin-bottom:12px"><div class="card-h"><h2>${esc(a)}</h2><span class="grow"></span>
      ${bad ? `<span class="badge bad">${bad} problem${bad === 1 ? "" : "s"}</span>` : ""}${warn ? `<span class="badge warn">${warn} warning${warn === 1 ? "" : "s"}</span>` : ""}${!bad && !warn && items.every(i => i.state !== "running") ? '<span class="badge ok">OK</span>' : ""}</div>
      ${items.map(i => `<div class="rd-item ${i.state}"><span class="ic" title="${readyStateText(i.state)}">${readyIcon(i.state)}</span><span class="nm">${esc(i.name)}<span class="sr-only"> (${readyStateText(i.state)})</span></span><span class="dt">${esc(i.detail || (i.state === "running" ? "Checking…" : ""))}</span></div>`).join("")}</div>`;
  }).join("");
}

function readySettingsCard(cfg, tests, alerts) {
  return `<div class="card" style="margin-bottom:16px"><div class="card-h">${ic("gear", 16)}<h2>Ready check settings</h2><span class="grow"></span><button class="btn ghost icon sm" id="rdSetClose" aria-label="Close">${ic("x", 15)}</button></div>
    <div class="card-b form"><div class="form-grid">
      <div><label for="rdTargets">Trading servers' SIP addresses, one per line <span class="muted">(where the SBC sends calls)</span></label><textarea id="rdTargets" rows="3" placeholder="192.0.2.58:5060" style="width:100%;font:12.5px var(--mono);padding:8px 10px;border-radius:8px;border:1px solid var(--border-strong);background:var(--surface);color:var(--text)">${esc(cfg.sipTargets.join("\n"))}</textarea></div>
      <div><label for="rdAccepted">Accepted alarms, one per line <span class="muted">(text in the alarm, e.g. LAN port number 2)</span></label><textarea id="rdAccepted" rows="3" style="width:100%;font:12.5px var(--mono);padding:8px 10px;border-radius:8px;border:1px solid var(--border-strong);background:var(--surface);color:var(--text)">${esc(cfg.acceptedAlarms.join("\n"))}</textarea></div>
      <div class="full"><label>Call tests to run as part of the check</label>${tests.length ? `<div style="display:flex;flex-wrap:wrap;gap:6px 18px;margin-top:4px">${tests.map(t => `<label style="display:flex;gap:6px;align-items:center;font-weight:500"><input type="checkbox" class="rdTest" value="${esc(t.id)}" ${cfg.callTestIds.includes(t.id) ? "checked" : ""}>${esc(t.name)} <span class="muted mono">${esc(t.to)}</span></label>`).join("")}</div>` : '<div class="muted">No saved call tests yet: make one on Call tests (an echo test is a good start).</div>'}</div>
      <div><label for="rdAt">Run every morning at <span class="muted">(blank = by hand only)</span></label><input type="time" id="rdAt" value="${esc(cfg.dailyAt)}"></div>
      <div><label for="rdErr">SBC error lines per hour before warning</label><input type="number" id="rdErr" min="0" value="${cfg.syslogErrorsPerHour}"></div>
      <div><label style="display:flex;gap:8px;align-items:center"><input type="checkbox" id="rdWeek" ${cfg.weekdaysOnly ? "checked" : ""}> Weekdays only</label></div>
      <div><label style="display:flex;gap:8px;align-items:center"><input type="checkbox" id="rdAlert" ${cfg.alertWhenNotReady ? "checked" : ""}> Send the result when not fully ready</label>${alerts ? "" : '<div class="help">No webhook or email set yet: add one under Settings → Alerts.</div>'}</div>
    </div>
    <div><button class="btn primary" id="rdSave">${ic("check", 13)} Save</button></div></div></div>`;
}

async function renderReady() {
  const box = $("readyBox");
  if (box.contains(document.activeElement) && /INPUT|TEXTAREA|SELECT/.test(document.activeElement.tagName)) return;
  const data = await api("/api/ready");
  const runs = data.runs;
  const run = runs.find(r => r.id === readySel) || runs[0] || null;
  const latest = runs[0];
  $("nc-ready").textContent = latest && latest.verdict === "notready" ? "!" : "";
  const v = run ? (READY[run.verdict] || READY.running) : null;
  const took = run && run.ended ? Math.max(1, Math.round((new Date(run.ended) - new Date(run.started)) / 1000)) : null;
  const n = st => run ? run.items.filter(i => i.state === st).length : 0;
  const notToday = latest && new Date(latest.started).toDateString() !== new Date().toDateString();

  const html = `
    ${readySettings ? readySettingsCard(data.config, data.tests, data.alerts) : ""}
    <div class="rd-verdict ${run ? run.verdict : ""}">
      <div style="flex:1;min-width:260px">
        <div class="big">${run ? `${ic(v.i, 28)}${esc(v.t)}` : `${ic("shield", 28)}Not checked yet`}</div>
        <div class="muted" style="margin-top:6px">${run ? `${run === latest ? "Last check" : "Check"} ${esc(dtm(run.started))}${run.trigger === "daily" ? " (daily)" : run.startedBy ? " by " + esc(run.startedBy) : ""}${took ? ` · took ${took} s` : ""} · ${n("bad")} problem${n("bad") === 1 ? "" : "s"}, ${n("warn")} warning${n("warn") === 1 ? "" : "s"}, ${n("ok")} OK` : "Run the check before the market opens: SBC alarms and trunks, the trading servers' SIP, the wires, recent line tests and your chosen call tests, in one go/no-go."}
        ${notToday && run === latest ? ' · <b>not run today</b>' : ""}${data.config.dailyAt ? ` · runs daily at ${esc(data.config.dailyAt)}${data.config.weekdaysOnly ? " (weekdays)" : ""}` : ""}</div>
      </div>
      ${data.running ? `<button class="btn danger" id="rdStop">${ic("stop", 12)} Stop</button>` : `<button class="btn primary" id="rdRun">${ic("play", 12)} Run check now</button>`}
      ${run && run.verdict !== "running" ? `<button class="btn" id="rdReport">${ic("report", 13)} Report</button>` : ""}
      <button class="btn ghost icon" id="rdSet" title="Ready check settings" aria-label="Ready check settings">${ic("gear", 16)}</button>
    </div>
    <div class="dash">
      <div class="span-8">${run ? readyItems(run) : `<div class="card">${emptyState("shield", "No checks yet", "Press Run check now. Set the trading servers' SIP addresses and any call tests to include with the settings button.")}</div>`}</div>
      <div class="card span-4" style="align-self:start"><div class="card-h">${ic("clock", 16)}<h2>History</h2></div>
        ${runs.length ? runs.map(r => `<div class="rd-hist ${run && r.id === run.id ? "sel" : ""}" data-run="${esc(r.id)}"><span>${esc(dtm(r.started))}${r.trigger === "daily" ? ' <span class="muted">daily</span>' : ""}</span><span class="badge ${r.verdict === "ready" ? "ok" : r.verdict === "warnings" ? "warn" : r.verdict === "notready" ? "bad" : ""}">${esc((READY[r.verdict] || READY.running).t)}</span></div>`).join("") : '<div class="card-b muted">None yet.</div>'}
      </div>
    </div>`;
  if (!setHTML(box, html)) return;
  box.querySelectorAll("[data-run]").forEach(e => e.onclick = () => { readySel = e.dataset.run === latest?.id ? null : e.dataset.run; box.__h = null; renderReady(); });
  const runBtn = $("rdRun"); if (runBtn) runBtn.onclick = async () => { try { const r = await api("/api/ready/run", {}); toast(r.message); readySel = null; } catch (e) { toast(e.message, true); } box.__h = null; renderReady(); };
  const stopBtn = $("rdStop"); if (stopBtn) stopBtn.onclick = async () => { try { const r = await api("/api/ready/stop", {}); toast(r.message); } catch (e) { toast(e.message, true); } };
  $("rdSet").onclick = () => { readySettings = !readySettings; box.__h = null; renderReady(); };
  const rep = $("rdReport"); if (rep) rep.onclick = () => openReport(`Ready check: ${v.t}`, `${esc(dtm(run.started))}${run.startedBy ? " · " + esc(run.startedBy) : ""}${run.trigger === "daily" ? " · daily" : ""} · ${n("bad")} problems, ${n("warn")} warnings, ${n("ok")} OK`,
    `<div class="rd-verdict ${run.verdict}"><div class="big">${ic(v.i, 26)}${esc(v.t)}</div></div>` + readyItems(run));
  if (readySettings) {
    $("rdSetClose").onclick = () => { readySettings = false; box.__h = null; renderReady(); };
    $("rdSave").onclick = async () => {
      const lines = id => $(id).value.split(/\r?\n/).map(x => x.trim()).filter(Boolean);
      try {
        const r = await api("/api/ready/config", {sipTargets: lines("rdTargets"), acceptedAlarms: lines("rdAccepted"),
          callTestIds: [...box.querySelectorAll(".rdTest:checked")].map(c => c.value), dailyAt: $("rdAt").value, weekdaysOnly: $("rdWeek").checked,
          alertWhenNotReady: $("rdAlert").checked, syslogErrorsPerHour: +$("rdErr").value || 0});
        toast(r.message); readySettings = false;
      } catch (e) { toast(e.message, true); }
      box.__h = null; renderReady();
    };
  }
}

/* ------------------------------------------------------------ line board */
let lineSel = null, lineImport = false, lineSheet = [], lineMon = false, lineMonData = null;
const pctText = v => v == null ? "" : (v >= 99.95 ? "100" : v.toFixed(1)) + "%";
const pctClass = v => v == null ? "" : v >= 99.5 ? "" : v >= 95 ? "zero" : "bad-t";
const stateWord = st => ({up: "up", warn: "one-way audio", down: "down", call: "in call", idle: "not tested", unknown: "unknown"}[st] || st || "–");
const LINE_STATE = {
  up:      {c: "up",      i: "check", wire: "Wire up",  other: "Passed"},
  warn:    {c: "warn",    i: "alert", wire: "Check",    other: "Warning"},
  down:    {c: "down",    i: "x",     wire: "Wire down", other: "Failed"},
  call:    {c: "call",    i: "phone", wire: "In call",  other: "In call"},
  idle:    {c: "idle",    i: "clock", wire: "Not tested", other: "Not tested"},
  unknown: {c: "unknown", i: "info",  wire: "Unknown",  other: "Unknown"},
};
const alwaysOn = t => t === "MRD" || t === "HOOT";
const typeName = t => t === "HOOT" ? "Hoot" : t;
const lineLabel = l => (LINE_STATE[l.state] || LINE_STATE.unknown)[alwaysOn(l.type) ? "wire" : "other"];
const sinceText = d => { if (!d) return ""; const m = /^(\d+):(\d+):(\d+)$/.exec(d); if (!m) return d; const h = +m[1], mi = +m[2]; return h ? `${h} h ${mi} min` : mi ? `${mi} min` : `${+m[3]} s`; };

function lineTile(l) {
  const st = LINE_STATE[l.state] || LINE_STATE.unknown;
  const audio = l.toSbcPerSec != null
    ? `<div class="au"><span title="Audio from the SBC to the dealerboard, packets per second" class="${l.fromSbcPerSec === 0 ? "zero" : ""}">${ic("in", 12)}${fmtN(l.fromSbcPerSec)}/s</span><span title="Audio from the dealerboard to the SBC, packets per second" class="${l.toSbcPerSec === 0 ? "zero" : ""}">${ic("out", 12)}${fmtN(l.toSbcPerSec)}/s</span>${mosBadge(l.mos)}</div>`
    : l.lastTest ? `<div class="au">${ic("clock", 12)}${esc(dtm(l.lastTest.at))}${mosBadge(l.mos)}</div>` : "";
  return `<button class="lt ${st.c}${lineSel === l.id ? " sel" : ""}" data-line="${esc(l.id)}" aria-label="${esc(l.name)}: ${esc(lineLabel(l))}">
    <div class="nm"><b title="${esc(l.name)}">${esc(l.name)}</b><span class="badge">${esc(typeName(l.type))}</span></div>
    <div class="num">${esc(l.number)}</div>
    <div class="st ${st.c}">${ic(st.i, 13)}${esc(lineLabel(l))}${l.since && alwaysOn(l.type) ? `<span class="muted" style="font-weight:500">· ${esc(sinceText(l.since))}</span>` : ""}</div>
    ${audio}${lineMonData?.uptime?.[l.id]?.day != null ? `<div class="muted ${pctClass(lineMonData.uptime[l.id].day)}" style="font-size:11.5px">${pctText(lineMonData.uptime[l.id].day)} up in 24 h</div>` : ""}</button>`;
}

const LINE_SORT = {
  state: l => ["down", "warn", "unknown", "call", "idle", "up"].indexOf(l.state),
  name: l => l.name.toLowerCase(), number: l => l.number, type: l => l.type, group: l => (l.group || "").toLowerCase(),
  since: l => { const m = /^(\d+):(\d+):(\d+)$/.exec(l.since || ""); return m ? +m[1] * 3600 + +m[2] * 60 + +m[3] : -1; },
  from: l => l.fromSbcPerSec ?? -1, to: l => l.toSbcPerSec ?? -1, mos: l => l.mos ?? 99,
  test: l => l.lastTest ? new Date(l.lastTest.at).getTime() : 0,
};
function sortLines(list, key, dir, up) {
  const f = key === "up24" ? (l => up?.(l.id)?.day ?? 101) : (LINE_SORT[key] || LINE_SORT.state);
  return list.slice().sort((a, b) => { const x = f(a), y = f(b); return (x < y ? -1 : x > y ? 1 : 0) * dir || a.name.localeCompare(b.name); });
}
function lineRow(l, upt) {
  const st = LINE_STATE[l.state] || LINE_STATE.unknown;
  const badge = st.c === "up" ? "ok" : st.c === "down" ? "bad" : st.c === "warn" ? "warn" : st.c === "call" ? "accent" : "";
  const rate = v => v == null ? "" : `<span class="${v === 0 ? "zero" : ""}">${fmtN(v)}</span>`;
  return `<tr data-line="${esc(l.id)}" class="${lineSel === l.id ? "sel" : ""}${l.state === "down" ? " down" : ""}">
    <td><span class="badge ${badge}">${ic(st.i, 12)}${esc(lineLabel(l))}</span></td>
    <td><b>${esc(l.name)}</b></td><td class="mono">${esc(l.number)}</td><td>${esc(typeName(l.type))}</td><td>${esc(l.group)}</td>
    <td>${alwaysOn(l.type) ? esc(sinceText(l.since)) : ""}</td>
    <td class="r">${rate(l.fromSbcPerSec)}</td><td class="r">${rate(l.toSbcPerSec)}</td><td>${mosBadge(l.mos)}</td>
    <td class="r ${pctClass(upt?.day)}" title="${upt?.week != null ? "7 days: " + pctText(upt.week) : ""}">${pctText(upt?.day)}</td>
    <td>${l.lastTest ? `${esc(dtm(l.lastTest.at))} · ${esc(l.lastTest.status)}` : ""}</td>
    <td class="dt" title="${esc(l.detail)}">${esc(l.detail)}</td></tr>`;
}

function lineDetail(l) {
  const st = LINE_STATE[l.state] || LINE_STATE.unknown;
  const wire = alwaysOn(l.type);
  const rows = [
    ["SIP URI", `<span class="mono">${esc(l.uri)}</span>`],
    ["Type", `${esc(typeName(l.type))}${wire ? " (always-on wire)" : ""}`],
    l.group ? ["Group", esc(l.group)] : null,
    ["SBC", `<span class="mono">${esc(l.via || l.sbc)}</span>`],
    ["State", `<span class="badge ${st.c === "up" ? "ok" : st.c === "down" ? "bad" : st.c === "warn" ? "warn" : st.c === "call" ? "accent" : ""}">${esc(lineLabel(l))}</span> ${esc(l.detail)}`],
    l.since ? ["Connected for", esc(sinceText(l.since))] : null,
    l.toSbcPerSec != null ? ["Audio now", `SBC → dealerboard ${fmtN(l.fromSbcPerSec)} packets/s · dealerboard → SBC ${fmtN(l.toSbcPerSec)} packets/s ${mosBadge(l.mos)}`] : null,
    l.sessions?.length ? ["SBC sessions", `<span class="mono">${l.sessions.map(esc).join(", ")}</span>`] : null,
    alwaysOn(l.type) ? ["Uptime", (() => { const u = lineMonData?.uptime?.[l.id]; return u ? `${pctText(u.day)} in the last 24 h · ${pctText(u.week)} in 7 days` : lineMonData?.config?.enabled ? "measuring…" : '<span class="muted">turn on the line monitor to measure</span>'; })()] : null,
    alwaysOn(l.type) && lineMonData ? ["Changes", (() => { const ev = lineMonData.events.filter(e => e.lineId === l.id).slice(0, 8); return ev.length ? ev.map(e => `<div>${esc(dtm(e.at))}: ${e.from ? esc(stateWord(e.from)) + " → " : ""}<b>${esc(stateWord(e.to))}</b></div>`).join("") : '<span class="muted">none recorded</span>'; })()] : null,
    l.lastTest ? ["Last line test", `${esc(dtm(l.lastTest.at))} · ${esc(l.lastTest.status)}<div class="muted" style="margin-top:3px">${esc(l.lastTest.summary)}</div>`] : ["Last line test", '<span class="muted">none yet</span>'],
  ].filter(Boolean);
  return `<div class="card" style="margin-bottom:16px"><div class="card-h">${ic(st.i, 16)}<h2>${esc(l.name)}</h2><span class="muted mono">${esc(l.number)}</span><span class="grow"></span>
      <button class="btn sm primary" id="lineTest">${ic("play", 12)} ${wire ? "Check wire" : "Test call"}</button>
      <button class="btn sm" id="lineRemove">${ic("trash", 13)} Remove</button>
      <button class="btn ghost icon sm" id="lineClose" aria-label="Close">${ic("x", 15)}</button></div>
    <div class="card-b"><dl class="lb-kv" style="margin:0">${rows.map(([k, v]) => `<dt>${k}</dt><dd>${v}</dd>`).join("")}</dl>
    ${wire ? '<p class="help" style="margin:12px 0 0">A wire check never calls the line: a call would replace the live wire call. It reads the SBC\u2019s call list and measures the audio for a few seconds.</p>' : '<p class="help" style="margin:12px 0 0">A test call rings the line through the SBC for up to 30 s; someone must pick up. The call then holds with a test tone and hangs up.</p>'}</div></div>`;
}

function lineMonitorCard(mon) {
  const c = mon.config;
  return `<div class="card" style="margin-bottom:16px"><div class="card-h">${ic("clock", 16)}<h2>Line monitor</h2><span class="muted">${mon.lastCheck ? "last check " + esc(dtm(mon.lastCheck)) : "not run yet"}${mon.lastError ? ` · <span style="color:var(--warn)">${esc(mon.lastError)}</span>` : ""}</span><span class="grow"></span><button class="btn ghost icon sm" id="lbMonClose" aria-label="Close">${ic("x", 15)}</button></div>
    <div class="card-b" style="display:flex;gap:18px;flex-wrap:wrap;align-items:center;border-bottom:1px solid var(--border)">
      <label style="display:flex;gap:8px;align-items:center;font-weight:650"><input type="checkbox" id="lmOn" ${c.enabled ? "checked" : ""}> Check the wires in the background</label>
      <label style="display:flex;gap:8px;align-items:center">every <select id="lmEvery">${[1, 2, 5, 10, 15, 30].map(m => `<option value="${m}" ${c.intervalMinutes === m ? "selected" : ""}>${m} min</option>`).join("")}</select></label>
      <label style="display:flex;gap:8px;align-items:center"><input type="checkbox" id="lmDown" ${c.alertDown ? "checked" : ""}> Alert when a wire goes down or comes back</label>
      <label style="display:flex;gap:8px;align-items:center"><input type="checkbox" id="lmOneWay" ${c.alertOneWay ? "checked" : ""}> Alert on one-way audio</label>
      <button class="btn sm primary" id="lmSave">${ic("check", 13)} Save</button>
      <div class="help" style="flex-basis:100%;margin:0">Each check reads the SBC's call list (one SSH session per SBC). A change counts after it is seen on ${c.confirm} checks in a row, so one missed read is not an outage. Alerts go to the webhook / email under Settings.</div>
    </div>
    <div class="scroll" style="max-height:300px">${mon.events.length ? mon.events.map(e => `<div class="lb-ev"><span class="muted">${esc(dtm(e.at))}</span><span><b>${esc(e.name)}</b> <span class="muted mono">${esc(e.number)}</span></span><span>${e.from ? esc(stateWord(e.from)) + " → " : ""}<b style="color:var(--${e.to === "down" ? "bad" : e.to === "warn" ? "warn" : e.to === "up" ? "ok" : "text-2"})">${esc(stateWord(e.to))}</b> <span class="muted">${esc(e.detail)}</span></span></div>`).join("") : '<div class="card-b muted">No changes recorded yet.</div>'}</div></div>`;
}

function lineImportCard(lastRun) {
  const ok = lineSheet.filter(l => !l.problems.length).length;
  return `<div class="card" style="margin-bottom:16px"><div class="card-h">${ic("down", 16)}<h2>Add lines to the board</h2><span class="grow"></span>
      <button class="btn sm" id="lbTemplate">${ic("down", 13)} Template</button><button class="btn ghost icon sm" id="lbImportClose" aria-label="Close">${ic("x", 15)}</button></div>
    <div class="card-b form">
      <div><label for="lbFile">Sheet of lines (.csv) or paste rows below. Same columns as Line tests, plus an optional <b>Group</b> (desk, counterparty or site).</label><input type="file" id="lbFile" accept=".csv,.txt,.tsv" style="width:100%"></div>
      <div><textarea id="lbPaste" rows="4" placeholder="Name, SIP URI, Type (ARD / MRD / Hoot / DDI), Group" style="width:100%;font:12.5px var(--mono);padding:8px 10px;border-radius:8px;border:1px solid var(--border-strong);background:var(--surface);color:var(--text)"></textarea></div>
      <div class="form-grid">
        <div class="full"><label for="lbVia">Lines are reached through this SBC <span class="muted">(blank = each URI's host)</span></label><input type="text" id="lbVia" list="bulkSbcs" placeholder="ip:port, e.g. 192.0.2.10:5070"></div>
        <div class="full"><label style="display:flex;gap:8px;align-items:center"><input type="checkbox" id="lbReplace"> Replace the lines already on the board</label></div>
      </div>
      ${lineSheet.length ? `<p class="help" style="margin:0">${ok} of ${lineSheet.length} rows ready${lineSheet.length > ok ? `; problems: ${esc(lineSheet.filter(l => l.problems.length).slice(0, 3).map(l => `row ${l.row}: ${l.problems.join(", ")}`).join("; "))}` : ""}.</p>` : ""}
      <div style="display:flex;gap:8px;flex-wrap:wrap">
        <button class="btn primary" id="lbDoImport" ${ok && ok === lineSheet.length ? "" : "disabled"}>${ic("plus", 13)} Add ${ok || ""} line${ok === 1 ? "" : "s"}</button>
        ${lastRun ? `<button class="btn" id="lbFromRun" title="${esc(lastRun.name)}">${ic("list", 13)} Use the last line test's ${lastRun.lines.length} lines</button>` : ""}
      </div>
    </div></div>`;
}

async function renderLines() {
  const box = $("linesBox");
  if (box.contains(document.activeElement) && /INPUT|TEXTAREA|SELECT/.test(document.activeElement.tagName)) return;
  const [data, mon] = await Promise.all([api("/api/lines"), api("/api/lines/monitor?limit=60").catch(() => null)]);
  lineMonData = mon;
  const up = id => mon?.uptime?.[id] || null;
  const lines = data.lines;
  const q = (store.get("lineQ", "") || "").toLowerCase(), ft = store.get("lineType", ""), fs = store.get("lineState", ""), by = store.get("lineBy", "group");
  // A sortable table by default (boards run to 1,000+ lines); tiles on request.
  const mode = store.get("lineView", null) === "tiles" ? "tiles" : "table";
  const sortKey = store.get("lineSortKey", "state"), sortDir = store.get("lineSortDir", 1);
  const count = st => lines.filter(l => l.state === st).length;
  const down = count("down");
  $("nc-lines").textContent = down ? String(down) : "";
  let lastRun = null;
  if (lineImport) { const runs = await api("/api/bulk"); lastRun = runs.find(r => r.lines?.length) || null; }

  const shown = lines.filter(l => (!ft || l.type === ft) && (!fs || l.state === fs || (fs === "idle" && l.state === "unknown"))
    && (!q || (l.name + " " + l.number + " " + l.group).toLowerCase().includes(q)));
  const keyOf = l => by === "type" ? typeName(l.type) : by === "state" ? lineLabel(l) : (l.group || typeName(l.type));
  const order = ["down", "warn", "unknown", "call", "idle", "up"];
  const groups = [...new Set(shown.map(keyOf))].sort((a, b) => a.localeCompare(b));
  const sel = lines.find(l => l.id === lineSel) || null;
  const sbcNote = data.sbcs.map(x => x.error ? `<span class="badge warn" title="${esc(x.error)}">${ic("alert", 12)} ${esc(x.sbc)}: call list failed</span>`
    : x.checkedAt ? `<span class="muted">${esc(x.sbc)} checked ${esc(tm(x.checkedAt))} · ${x.calls} call${x.calls === 1 ? "" : "s"} up</span>` : `<span class="muted">${esc(x.sbc)}: reading…</span>`).join(" · ");

  const kpi = (label, v, color, filter) => `<button class="card stat span-2" data-fs="${filter}" style="text-align:left;cursor:pointer;font:inherit;color:inherit${fs === filter ? ";outline:2px solid var(--accent)" : ""}"><div class="l">${label}</div><div class="v" ${color ? `style="color:var(--${color})"` : ""}>${v}</div></button>`;
  const html = `
    <div class="dash" style="margin-bottom:16px">
      ${kpi("Lines", lines.length, "", "")}${kpi("Up", count("up"), "ok", "up")}${kpi("Need a look", count("warn"), "warn", "warn")}${kpi("Down", down, "bad", "down")}${kpi("In a call", count("call"), "accent", "call")}${kpi("Unknown / not tested", count("unknown") + count("idle"), "", "idle")}
    </div>
    <div class="card" style="margin-bottom:16px"><div class="card-b" style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;padding:12px 16px">
      <input type="search" id="lbQ" placeholder="Find a line, number or group" value="${esc(store.get("lineQ", ""))}" style="min-width:220px" aria-label="Find a line">
      <select id="lbType" aria-label="Line type"><option value="">All types</option>${["ARD", "MRD", "HOOT", "DDI"].map(t => `<option value="${t}" ${ft === t ? "selected" : ""}>${typeName(t)}</option>`).join("")}</select>
      <select id="lbView" aria-label="Show as"><option value="table" ${mode === "table" ? "selected" : ""}>Table</option><option value="tiles" ${mode === "tiles" ? "selected" : ""}>Tiles</option></select>
      <select id="lbBy" aria-label="Group by" ${mode === "table" ? 'class="hidden"' : ""}><option value="group" ${by === "group" ? "selected" : ""}>Group by desk / group</option><option value="type" ${by === "type" ? "selected" : ""}>Group by type</option><option value="state" ${by === "state" ? "selected" : ""}>Group by state</option></select>
      <span class="grow"></span>${sbcNote}
      ${lines.length ? `<button class="btn sm" id="lbCsv" title="The lines shown, as a CSV file">${ic("down", 13)} CSV</button>` : ""}
      <button class="btn sm" id="lbMon" title="Background checks of the wires, history and alerts">${ic("clock", 13)} Monitor: ${mon?.config?.enabled ? `every ${mon.config.intervalMinutes} min${mon.lastCheck ? " · " + esc(tm(mon.lastCheck)) : ""}` : "off"}</button>
      <button class="btn sm" id="lbAdd">${ic("plus", 13)} Add lines</button>
      ${lines.length ? `<button class="btn sm primary" id="lbTestAll" ${status?.bulkRunning ? "disabled" : ""} title="Wire check for MRD / Hoot, test call for ARD / DDI, one after another">${ic("play", 12)} Test ${shown.length === lines.length ? "all" : shown.length} lines</button>` : ""}
    </div></div>
    ${lineMon && mon ? lineMonitorCard(mon) : ""}
    ${lineImport ? lineImportCard(lastRun) : ""}
    ${sel ? lineDetail(sel) : ""}
    ${!lines.length ? `<div class="card">${emptyState("board", "No lines on the board yet", "Add the lines you want to watch: the same sheet you use for Line tests (Name, SIP URI, Type, optional Group), or the lines of the last line test. MRD and Hoot wires show live from the SBC; ARD and DDI show their last test.")}</div>`
      : !shown.length ? `<div class="card">${emptyState("search", "No lines match", "Clear the search or filters.")}</div>`
      : mode === "table" ? (() => {
          const th = (k, label, cls = "") => `<th class="sortable ${cls}" data-sort="${k}" aria-sort="${sortKey === k ? (sortDir > 0 ? "ascending" : "descending") : "none"}">${label}${sortKey === k ? (sortDir > 0 ? " ▲" : " ▼") : ""}</th>`;
          return `<div class="card"><div class="scroll" id="lbScroll" style="max-height:calc(100vh - 330px);min-height:240px"><table class="tbl lb-table"><thead><tr>
            ${th("state", "State")}${th("name", "Line")}${th("number", "Number")}${th("type", "Type")}${th("group", "Group")}${th("since", "Connected")}
            ${th("from", "SBC → board /s", "r")}${th("to", "Board → SBC /s", "r")}${th("mos", "MOS")}${th("up24", "Up 24 h", "r")}${th("test", "Last test")}<th>Detail</th></tr></thead>
            <tbody>${sortLines(shown, sortKey, sortDir, up).map(l => lineRow(l, up(l.id))).join("")}</tbody></table></div>
            <div class="muted" style="padding:8px 14px;font-size:12px;border-top:1px solid var(--border)">${shown.length === lines.length ? `${fmtN(lines.length)} lines` : `${fmtN(shown.length)} of ${fmtN(lines.length)} lines`} · click a column to sort, a row for details</div></div>`;
        })()
      : groups.map(g => {
          const gl = shown.filter(l => keyOf(l) === g).sort((a, b) => order.indexOf(a.state) - order.indexOf(b.state) || a.name.localeCompare(b.name));
          const c = st => gl.filter(l => l.state === st).length;
          return `<div class="card lb-group"><div class="card-h"><h2>${esc(g)}</h2><span class="muted">${gl.length} line${gl.length === 1 ? "" : "s"}</span><span class="grow"></span>
            ${c("down") ? `<span class="badge bad">${c("down")} down</span>` : ""}${c("warn") ? `<span class="badge warn">${c("warn")} to check</span>` : ""}${c("up") ? `<span class="badge ok">${c("up")} up</span>` : ""}</div>
            <div class="card-b"><div class="lb-grid">${gl.map(lineTile).join("")}</div></div></div>`;
        }).join("")}`;

  const keepScroll = $("lbScroll")?.scrollTop || 0;
  if (!setHTML(box, html)) return;
  if ($("lbScroll")) $("lbScroll").scrollTop = keepScroll;
  box.onclick = e => {
    const h = e.target.closest("th[data-sort]");
    if (h) { const k = h.dataset.sort; store.set("lineSortDir", k === sortKey ? -sortDir : 1); store.set("lineSortKey", k); box.__h = null; renderLines(); return; }
    const row = e.target.closest("[data-line]");
    if (!row) return;
    lineSel = lineSel === row.dataset.line ? null : row.dataset.line; box.__h = null; renderLines();
    if (lineSel) window.scrollTo({top: 0, behavior: "smooth"});
  };
  box.querySelectorAll("[data-fs]").forEach(b => b.onclick = () => { store.set("lineState", fs === b.dataset.fs ? "" : b.dataset.fs); box.__h = null; renderLines(); });
  let qTimer;
  $("lbQ").oninput = () => { clearTimeout(qTimer); qTimer = setTimeout(() => { store.set("lineQ", $("lbQ").value); box.__h = null; $("lbQ").blur(); renderLines().then(() => { const e = $("lbQ"); e.focus(); e.setSelectionRange(e.value.length, e.value.length); }); }, 400); };
  $("lbType").onchange = () => { store.set("lineType", $("lbType").value); box.__h = null; renderLines(); };
  $("lbBy").onchange = () => { store.set("lineBy", $("lbBy").value); box.__h = null; renderLines(); };
  $("lbView").onchange = () => { store.set("lineView", $("lbView").value); box.__h = null; renderLines(); };
  const csv = $("lbCsv"); if (csv) csv.onclick = () => downloadText(`line-board-${new Date().toISOString().slice(0, 16).replace(/[:T]/g, "-")}.csv`,
    ["Line,Number,Type,Group,State,Connected,SBC to board pkt/s,Board to SBC pkt/s,MOS,Up 24 h %,Up 7 days %,Last test,Last test result,Detail"].concat(sortLines(shown, sortKey, sortDir, up).map(l =>
      [l.name, l.number, typeName(l.type), l.group, lineLabel(l), alwaysOn(l.type) ? sinceText(l.since) : "", l.fromSbcPerSec ?? "", l.toSbcPerSec ?? "", l.mos ?? "", up(l.id)?.day ?? "", up(l.id)?.week ?? "",
       l.lastTest ? dtm(l.lastTest.at) : "", l.lastTest?.status || "", l.detail].map(csvCell).join(","))).join("\r\n"));
  $("lbAdd").onclick = () => { lineImport = !lineImport; box.__h = null; renderLines(); };
  $("lbMon").onclick = () => { lineMon = !lineMon; box.__h = null; renderLines(); };
  if (lineMon && mon) {
    $("lbMonClose").onclick = () => { lineMon = false; box.__h = null; renderLines(); };
    $("lmSave").onclick = async () => {
      try { const r = await api("/api/lines/monitor", {enabled: $("lmOn").checked, intervalMinutes: +$("lmEvery").value, alertDown: $("lmDown").checked, alertOneWay: $("lmOneWay").checked, confirm: mon.config.confirm}); toast(r.message); }
      catch (e) { toast(e.message, true); }
      box.__h = null; renderLines();
    };
  }
  const testIds = async (ids, label) => {
    try { const r = await api("/api/lines/test", {ids}); toast(r.message + " Results on Line tests."); } catch (e) { toast(e.message, true); }
    refresh();
  };
  const all = $("lbTestAll"); if (all) all.onclick = () => testIds(shown.length === lines.length ? [] : shown.map(l => l.id));
  if (sel) {
    $("lineTest").onclick = () => testIds([sel.id]);
    $("lineClose").onclick = () => { lineSel = null; box.__h = null; renderLines(); };
    $("lineRemove").onclick = async () => {
      if (!confirm(`Remove ${sel.name} from the board? Its line test history stays.`)) return;
      try { const r = await api("/api/lines/delete", {ids: [sel.id]}); toast(r.message); lineSel = null; } catch (e) { toast(e.message, true); }
      box.__h = null; renderLines();
    };
  }
  if (lineImport) {
    const doImport = async (rows, via) => {
      try {
        const r = await api("/api/lines/import", {via, replace: $("lbReplace").checked, lines: rows});
        toast(r.message); lineImport = false; lineSheet = [];
      } catch (e) { toast(e.message, true); }
      box.__h = null; renderLines();
    };
    $("lbImportClose").onclick = () => { lineImport = false; lineSheet = []; box.__h = null; renderLines(); };
    $("lbTemplate").onclick = () => downloadText("line-board-template.csv", "Name,SIP URI,Type,Group\r\nB2B-1,sip:9901@192.0.2.10,ARD,Rates desk\r\nMRD_B,sip:9912@192.0.2.10,MRD,Rates desk\r\nHoot_A,sip:9921@192.0.2.10,Hoot,FX hoot\r\nDesk DDI,sip:02071234567@192.0.2.10,DDI,Sales\r\n");
    const keep = () => { const v = $("lbVia").value, rp = $("lbReplace").checked; return () => { $("lbVia").value = v; $("lbReplace").checked = rp; }; };
    $("lbFile").onchange = () => { const f = $("lbFile").files[0]; if (!f) return; const rd = new FileReader(); const back = keep(); rd.onload = () => { lineSheet = parseSheet(rd.result); box.__h = null; renderLines().then(back); }; rd.readAsText(f); };
    let pt; $("lbPaste").oninput = () => { clearTimeout(pt); pt = setTimeout(() => { const t = $("lbPaste").value, back = keep(); lineSheet = parseSheet(t); box.__h = null; $("lbPaste").blur(); renderLines().then(() => { $("lbPaste").value = t; back(); }); }, 500); };
    $("lbDoImport").onclick = () => doImport(lineSheet.map(l => ({name: l.name, uri: l.uri, type: l.type, group: l.group})), $("lbVia").value);
    const fr = $("lbFromRun"); if (fr) fr.onclick = () => doImport(lastRun.lines.map(l => ({name: l.name, uri: l.uri, type: l.type})), lastRun.via);
  }
}

/* ------------------------------------------------------------ refresh loop */
let busy = false, failures = 0;
// A tab in the background does not poll; it catches up the moment it is shown again.
document.addEventListener("visibilitychange", () => { if (!document.hidden && appStarted) refresh(); });
async function refresh() {
  if (busy || document.hidden) return; busy = true;
  try {
    status = await api("/api/status");
    failures = 0;
    renderTop(status); renderSide(status); renderAlerts(status);
    [$("sbcFilter"), $("sbcFilter2")].forEach(el => fillSbcSelect(el, status));
    if (view === "dashboard") await renderDashboard();
    else if (view === "tests") await renderTests();
    else if (view === "bulk") await renderBulk(status);
    else if (view === "lines") await renderLines();
    else if (view === "ready") await renderReady();
    else if (view === "health") await renderHealth();
    else if (view === "upgrades") await renderUpgrades();
    else if (view === "timeline") await renderTimeline();
    else if (view === "calls") {
      calls = await api("/api/calls?" + sbcQ($("sbcFilter")) + ($("showOptions").checked ? "&options=1" : ""));
      renderCalls();
      if (selected !== null) await loadDetail(); else setHTML($("detail"), emptyState("phone", "Select a call", "Pick a call on the left to see its SIP flow, checks and audio."));
    } else if (view === "streams") await loadStreams();
    else if (view === "syslog") await loadSyslog();
    else if (view === "captures") await loadCaptures();
    else if (view === "sbcs") renderSbcs(status);
    else if (view === "compare") renderCompare();
    else if (view === "changes") await renderChanges(status);
    else if (view === "settings") await renderSettings(status);
  } catch (e) {
    if (String(e.message).includes("Sign in required")) {
      if (!document.body.classList.contains("locked")) { try { showGate(await api("/api/session")); } catch { showGate(null); } }
      return;
    }
    if (++failures >= 2) setHTML($("alerts"), `<div class="alert bad"><span class="ic">${ic("alert", 18)}</span><div><b>Lost contact with MediantTrace.</b> ${esc(e.message)}. Is the program or service running?</div></div>`);
  } finally { busy = false; }
}
// Links: #call=12, #call=12&tab=flow, #view=sbcs (theme=dark|light also accepted).
const hp = new URLSearchParams(location.hash.slice(1));
if (hp.get("call")) { view = "calls"; selected = +hp.get("call"); }
if (["overview", "flow", "media", "sdp", "cdr", "sbclog", "messages"].includes(hp.get("tab"))) dtab = hp.get("tab");
if (titles[hp.get("view")]) view = hp.get("view");
if (hp.get("result")) { view = "tests"; selectedResult = hp.get("result"); }
// #report=12 opens that call's report in this tab (a link you can send).
let reportInPlace = false;
if (hp.get("report")) { reportInPlace = true; status = null; }
if (["dark", "light"].includes(hp.get("theme"))) applyTheme(hp.get("theme"));

let appStarted = false;
function showGate(session) {
  document.body.classList.add("locked");
  $("gate").classList.remove("hidden");
  const setup = !!session?.needsSetup;
  $("gateTitle").textContent = setup ? "Create the sign-in" : "Sign in";
  $("gateHelp").textContent = setup
    ? "Choose your username and password. MediantTrace stays closed until you do. Other people can be added from Settings afterwards."
    : "Enter the username and password to open MediantTrace.";
  $("gatePass2wrap").classList.toggle("hidden", !setup);
  $("gatePass").autocomplete = setup ? "new-password" : "current-password";
  $("gateBtn").textContent = setup ? "Create sign-in" : "Sign in";
  $("gateUser").focus();
}
function openApp(name) {
  if (name) { const w = $("who"); if (w) w.textContent = name; }
  $("gate").classList.add("hidden");
  document.body.classList.remove("locked");
  if (appStarted) { refresh(); return; }
  appStarted = true;
  if (reportInPlace) {
    (async () => { try { status = await api("/api/status"); const d = await api("/api/calls/" + (+hp.get("report"))); callReport(d); } catch (e) { document.body.textContent = "Report not available: " + e.message; } })();
    return;
  }
  paintIcons();
  go(view, "replace");
  setInterval(refresh, 2500);
}
$("gateForm").onsubmit = async (e) => {
  e.preventDefault();
  const setup = !$("gatePass2wrap").classList.contains("hidden");
  const user = $("gateUser").value.trim();
  const pass = $("gatePass").value;
  $("gateErr").textContent = "";
  if (!user || !pass) { $("gateErr").textContent = "Type a username and a password."; return; }
  if (setup && pass !== $("gatePass2").value) { $("gateErr").textContent = "The passwords do not match."; return; }
  try {
    await api(setup ? "/api/session/setup" : "/api/session/login", {user, password: pass});
    $("gatePass").value = ""; $("gatePass2").value = "";
    openApp(user);
  } catch (err) { $("gateErr").textContent = err.message; }
};
$("signOut").onclick = async () => { try { await api("/api/session/logout", {}); } catch {} location.reload(); };
(async () => {
  try {
    const s = await api("/api/session");
    if (s.signedIn) openApp(s.user); else showGate(s);
  } catch (e) {
    showGate(null);
    $("gateErr").textContent = e.message || "MediantTrace is not answering.";
  }
})();
</script>
</body>
</html>
"""";
}
