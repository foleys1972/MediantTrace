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
:root{
  --bg:#f4f6f9;--surface:#ffffff;--surface-2:#f8fafc;--sunken:#eef1f5;--border:#e2e7ee;--border-strong:#cfd6e0;
  --text:#0f172a;--text-2:#475569;--text-3:#8391a5;
  --accent:#2563eb;--accent-soft:#e8efff;--accent-text:#1d4ed8;
  --ok:#059669;--ok-soft:#e3f6ef;--warn:#d97706;--warn-soft:#fdf2e1;--bad:#dc2626;--bad-soft:#fde8e8;--info:#7c3aed;
  --req:#2563eb;--prov:#8391a5;--succ:#059669;--fail:#dc2626;
  --shadow:0 1px 2px rgba(15,23,42,.04),0 2px 8px rgba(15,23,42,.06);
  --shadow-lg:0 12px 40px rgba(15,23,42,.18);
  --mono:ui-monospace,"Cascadia Mono","Cascadia Code",Consolas,"SF Mono",Menlo,monospace;
  --sans:"Segoe UI Variable Text","Segoe UI",system-ui,-apple-system,Roboto,"Helvetica Neue",Arial,sans-serif;
}
:root[data-theme="dark"]{
  --bg:#0b0f15;--surface:#121821;--surface-2:#161d28;--sunken:#0e141c;--border:#232d3a;--border-strong:#2f3b4b;
  --text:#e7ecf3;--text-2:#a3b0c2;--text-3:#6b7a8f;
  --accent:#4f8cff;--accent-soft:#16233d;--accent-text:#8fb4ff;
  --ok:#34d399;--ok-soft:#0f2a22;--warn:#fbbf24;--warn-soft:#2d2410;--bad:#f87171;--bad-soft:#331617;--info:#a78bfa;
  --req:#6aa0ff;--prov:#6b7a8f;--succ:#34d399;--fail:#f87171;
  --shadow:0 1px 2px rgba(0,0,0,.3),0 2px 8px rgba(0,0,0,.25);--shadow-lg:0 12px 40px rgba(0,0,0,.5);
}
@media (prefers-color-scheme:dark){:root:not([data-theme="light"]){
  --bg:#0b0f15;--surface:#121821;--surface-2:#161d28;--sunken:#0e141c;--border:#232d3a;--border-strong:#2f3b4b;
  --text:#e7ecf3;--text-2:#a3b0c2;--text-3:#6b7a8f;
  --accent:#4f8cff;--accent-soft:#16233d;--accent-text:#8fb4ff;
  --ok:#34d399;--ok-soft:#0f2a22;--warn:#fbbf24;--warn-soft:#2d2410;--bad:#f87171;--bad-soft:#331617;--info:#a78bfa;
  --req:#6aa0ff;--prov:#6b7a8f;--succ:#34d399;--fail:#f87171;
  --shadow:0 1px 2px rgba(0,0,0,.3),0 2px 8px rgba(0,0,0,.25);--shadow-lg:0 12px 40px rgba(0,0,0,.5);
}}
*{box-sizing:border-box}
html,body{height:100%}
body{margin:0;background:var(--bg);color:var(--text);font:14px/1.5 var(--sans);-webkit-font-smoothing:antialiased}
a{color:var(--accent-text);text-decoration:none}
a:hover{text-decoration:underline}
code,.mono{font-family:var(--mono);font-size:12.5px}
button{font:inherit;color:inherit}
.app{display:grid;grid-template-columns:232px 1fr;min-height:100vh}

/* sidebar */
.side{background:var(--surface);border-right:1px solid var(--border);display:flex;flex-direction:column;position:sticky;top:0;height:100vh}
.brand{display:flex;align-items:center;gap:10px;padding:18px 18px 14px;font-weight:700;font-size:16px;letter-spacing:-.01em}
.brand svg{flex:none}
.brand small{display:block;font-weight:500;font-size:11px;color:var(--text-3);letter-spacing:.02em}
.nav{padding:6px 10px;display:flex;flex-direction:column;gap:2px;flex:1;min-height:0;overflow:auto}
.nav-label{padding:12px 10px 3px;font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:var(--text-3)}
.nav-label:first-child{padding-top:2px}
.nav button{display:flex;align-items:center;gap:10px;width:100%;padding:8px 10px;border:0;background:none;border-radius:8px;cursor:pointer;color:var(--text-2);font-weight:550;text-align:left}
.nav button:hover{background:var(--sunken);color:var(--text)}
.nav button.on{background:var(--accent-soft);color:var(--accent-text)}
.nav .count{margin-left:auto;font-size:11.5px;font-weight:600;color:var(--text-3);font-variant-numeric:tabular-nums}
.nav button.on .count{color:var(--accent-text)}
.side-section{padding:14px 18px 6px;font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:var(--text-3)}
.sbc-mini{padding:0 10px;display:flex;flex-direction:column;gap:1px;overflow:auto}
.sbc-mini div{display:flex;align-items:center;gap:8px;padding:6px 8px;border-radius:7px;font-size:13px;color:var(--text-2)}
.sbc-mini div span:nth-child(2){overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.side-foot{margin-top:auto;padding:12px 18px;border-top:1px solid var(--border);font-size:11.5px;color:var(--text-3);display:flex;justify-content:space-between;align-items:center}

/* main */
.main{min-width:0;display:flex;flex-direction:column}
.top{display:flex;align-items:center;gap:12px;padding:14px 24px;background:var(--surface);border-bottom:1px solid var(--border);position:sticky;top:0;z-index:20}
.top h1{font-size:18px;margin:0;font-weight:650;letter-spacing:-.01em}
.top .sub{color:var(--text-3);font-size:13px}
.grow{flex:1}
.rec{display:flex;align-items:center;gap:10px;padding:5px 6px 5px 12px;border-radius:999px;background:var(--bad-soft);color:var(--bad);font-weight:650;font-variant-numeric:tabular-nums}
.rec .pulse{width:9px;height:9px;border-radius:50%;background:var(--bad);animation:pulse 1.4s ease-in-out infinite}
.rec small{color:var(--text-2);font-weight:500}
@keyframes pulse{0%,100%{opacity:1;transform:scale(1)}50%{opacity:.35;transform:scale(.8)}}
.content{padding:20px 24px 40px;max-width:1680px;width:100%}

/* controls */
.btn{display:inline-flex;align-items:center;gap:7px;height:34px;padding:0 14px;border-radius:8px;border:1px solid var(--border-strong);background:var(--surface);color:var(--text);font-weight:600;font-size:13.5px;cursor:pointer;white-space:nowrap;text-decoration:none}
.btn:hover{background:var(--sunken);text-decoration:none}
.btn.primary{background:var(--accent);border-color:var(--accent);color:#fff}
.btn.primary:hover{filter:brightness(1.08)}
.btn.danger{background:var(--bad);border-color:var(--bad);color:#fff}
.btn.ghost{border-color:transparent;background:none;color:var(--text-2)}
.btn.ghost:hover{background:var(--sunken);color:var(--text)}
.btn.sm{height:28px;padding:0 10px;font-size:12.5px}
.btn.icon{width:34px;padding:0;justify-content:center}
.btn:disabled{opacity:.5;cursor:default}
.btn:focus-visible,.nav button:focus-visible,input:focus-visible,select:focus-visible,.chip:focus-visible,.row:focus-visible{outline:2px solid var(--accent);outline-offset:2px}
input[type=text],input[type=number],input[type=search],select{height:34px;padding:0 11px;border-radius:8px;border:1px solid var(--border-strong);background:var(--surface);color:var(--text);font:inherit;font-size:13.5px}
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
.alert.bad{background:var(--bad-soft);border-color:color-mix(in srgb,var(--bad) 35%,transparent)}
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
.badge.ok{background:var(--ok-soft);color:var(--ok)}
.badge.warn{background:var(--warn-soft);color:var(--warn)}
.badge.bad{background:var(--bad-soft);color:var(--bad)}
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
.verdict.ok{background:var(--ok-soft);color:var(--ok)}
.verdict.warn{background:var(--warn-soft);color:var(--warn)}
.verdict.bad{background:var(--bad-soft);color:var(--bad)}
.verdict span{color:var(--text)}
.tabs{display:flex;gap:2px;padding:0 20px;margin-top:16px;border-bottom:1px solid var(--border);overflow:auto}
.tabs button{border:0;background:none;padding:10px 12px;font-weight:600;color:var(--text-3);cursor:pointer;border-bottom:2px solid transparent;margin-bottom:-1px;white-space:nowrap}
.tabs button:hover{color:var(--text)}
.tabs button.on{color:var(--accent-text);border-bottom-color:var(--accent)}
.tab-b{padding:18px 20px 20px}
.facts{display:grid;grid-template-columns:repeat(auto-fill,minmax(150px,1fr));border:1px solid var(--border);border-radius:10px;overflow:hidden;margin-bottom:18px;background:var(--surface)}
.facts > div{padding:10px 12px;border-right:1px solid var(--border);border-bottom:1px solid var(--border);margin:0 -1px -1px 0}
.facts .l{font-size:11.5px;color:var(--text-3);font-weight:600;text-transform:uppercase;letter-spacing:.04em}
.facts .v{font-weight:650;margin-top:2px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
h3{font-size:12px;margin:22px 0 10px;font-weight:700;color:var(--text-2);text-transform:uppercase;letter-spacing:.06em}
h3:first-child{margin-top:0}
.checks{display:flex;flex-direction:column;gap:8px}
.check{display:flex;gap:10px;align-items:flex-start;padding:10px 12px;border-radius:9px;background:var(--surface-2);border:1px solid var(--border)}
.check .ic{flex:none;margin-top:1px}
.check.bad .ic{color:var(--bad)}.check.warn .ic{color:var(--warn)}.check.ok .ic{color:var(--ok)}

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
.tl-axis{display:grid;grid-template-columns:260px 1fr 70px;gap:12px;font-size:11px;color:var(--text-3)}
.tl-axis div:nth-child(2){display:flex;justify-content:space-between}

/* tables */
.tbl{width:100%;border-collapse:collapse;font-size:13px}
.tbl th{text-align:left;font-size:11.5px;text-transform:uppercase;letter-spacing:.04em;color:var(--text-3);font-weight:650;padding:9px 12px;border-bottom:1px solid var(--border);white-space:nowrap;background:var(--surface-2);position:sticky;top:0}
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
.empty .ic{width:52px;height:52px;border-radius:14px;background:var(--accent-soft);color:var(--accent);display:flex;align-items:center;justify-content:center;margin:0 auto 14px}
.empty h4{color:var(--text);font-size:15px;margin:0 0 6px}
.empty p{margin:0 auto;max-width:420px;font-size:13.5px}

/* toast */
.toasts{position:fixed;right:20px;bottom:20px;display:flex;flex-direction:column;gap:8px;z-index:100}
.toast{background:var(--text);color:var(--bg);padding:10px 14px;border-radius:9px;box-shadow:var(--shadow-lg);font-weight:550;font-size:13.5px;animation:tin .18s ease-out;max-width:380px}
.toast.bad{background:var(--bad);color:#fff}
@keyframes tin{from{opacity:0;transform:translateY(8px)}to{opacity:1;transform:none}}

/* chart tokens (validated: dataviz validator, both themes) */
:root{--s1:#2a78d6;--s2:#eb6834;--good:#0ca30c;--warning:#fab219;--critical:#d03b3b;--grid:#e8ebf0;--axis:#c3c9d3}
:root[data-theme="dark"]{--s1:#3987e5;--s2:#d95926;--grid:#222b37;--axis:#323d4c}
@media (prefers-color-scheme:dark){:root:not([data-theme="light"]){--s1:#3987e5;--s2:#d95926;--grid:#222b37;--axis:#323d4c}}
.chart{position:relative;padding:4px 0 0 40px}
.chart .plot{position:relative;height:var(--h,180px)}
.chart svg.p{position:absolute;inset:0;width:100%;height:100%;overflow:visible}
.chart .yl{position:absolute;left:-40px;width:34px;text-align:right;font-size:11px;color:var(--text-3);transform:translateY(-50%);font-variant-numeric:tabular-nums}
.chart .xl{display:flex;justify-content:space-between;font-size:11px;color:var(--text-3);margin-top:6px;font-variant-numeric:tabular-nums}
.chart .xh{position:absolute;top:0;bottom:0;width:1px;background:var(--text-3);pointer-events:none;display:none}
.chart .mk{position:absolute;width:9px;height:9px;border-radius:50%;border:2px solid var(--surface);transform:translate(-50%,-50%);pointer-events:none;display:none}
.chart .ref{position:absolute;right:0;font-size:10.5px;color:var(--text-3);transform:translateY(-110%)}
.legend{display:flex;gap:14px;flex-wrap:wrap;font-size:12.5px;color:var(--text-2);margin-bottom:8px}
.legend span{display:inline-flex;align-items:center;gap:6px}
.legend i{display:inline-block;width:14px;height:3px;border-radius:2px}
.legend i.box{width:10px;height:10px;border-radius:3px}
.tip{position:fixed;z-index:200;pointer-events:none;background:var(--surface);color:var(--text);border:1px solid var(--border-strong);border-radius:9px;box-shadow:var(--shadow-lg);padding:8px 10px;font-size:12.5px;display:none;min-width:150px}
.tip .t{color:var(--text-3);font-size:11.5px;margin-bottom:4px}
.tip .r{display:flex;align-items:center;gap:8px;justify-content:space-between}
.tip .r b{font-variant-numeric:tabular-nums}
.tip .k{display:inline-flex;align-items:center;gap:6px;color:var(--text-2)}
.tip .k i{display:inline-block;width:12px;height:3px;border-radius:2px}
.dash{display:grid;grid-template-columns:repeat(12,minmax(0,1fr));gap:16px}
.span-3{grid-column:span 3}.span-4{grid-column:span 4}.span-5{grid-column:span 5}.span-6{grid-column:span 6}.span-7{grid-column:span 7}.span-8{grid-column:span 8}.span-12{grid-column:span 12}
.stat{padding:16px 18px}
.stat .l{font-size:12.5px;color:var(--text-2);font-weight:600;display:flex;align-items:center;gap:7px}
.stat .v{font-size:30px;font-weight:700;letter-spacing:-.02em;margin-top:4px;display:flex;align-items:baseline;gap:8px}
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
.gauge{display:flex;align-items:center;gap:16px}
.gauge svg{flex:none}
@media (max-width:1180px){.span-3,.span-4,.span-5{grid-column:span 6}.span-6,.span-7,.span-8{grid-column:span 12}.tests{grid-template-columns:1fr}}
@media (max-width:820px){.dash > *{grid-column:span 12!important}.form-grid{grid-template-columns:1fr}}
@media print{.side,.top,.alerts,.tabs,.d-actions,.toolbar{display:none!important}.app{display:block}}
.hidden{display:none!important}
@media (max-width:1180px){.split{grid-template-columns:1fr}.rows{max-height:42vh}.kpis{grid-template-columns:repeat(3,minmax(0,1fr))}}
@media (max-width:820px){
  .app{grid-template-columns:1fr}
  .side{position:static;height:auto;border-right:0;border-bottom:1px solid var(--border)}
  .nav{flex-direction:row;flex:none;overflow:auto;align-items:center}.nav button{width:auto;white-space:nowrap}.nav .count{display:none}
  .nav-label{flex:none;padding:0 4px 0 16px;align-self:center}
  .side-section,.sbc-mini,.side-foot{display:none}
  .top{padding:12px 16px;flex-wrap:wrap}.content{padding:16px}
  .kpis{grid-template-columns:repeat(2,minmax(0,1fr))}
  .sdp-pair{grid-template-columns:1fr}
  .tl-row,.tl-axis{grid-template-columns:1fr}
  .log > div{grid-template-columns:1fr}
  .sbc-edit{grid-template-columns:1fr}
}
@media (prefers-reduced-motion:reduce){*{animation:none!important}}
</style>
</head>
<body>
<div class="app">
  <aside class="side">
    <div class="brand">
      <svg width="30" height="30" viewBox="0 0 32 32" aria-hidden="true"><rect width="32" height="32" rx="8" fill="var(--accent)"/><path d="M5 17h5l3-8 5 15 3-9h6" stroke="#fff" stroke-width="2.6" fill="none" stroke-linecap="round" stroke-linejoin="round"/></svg>
      <div>MediantTrace<small>SBC call troubleshooting</small></div>
    </div>
    <nav class="nav" id="nav">
      <div class="nav-label">Watch</div>
      <button data-view="dashboard" class="on"><span data-i="grid"></span>Dashboard</button>
      <button data-view="calls"><span data-i="phone"></span>Calls<span class="count" id="nc-calls"></span></button>
      <div class="nav-label">Test</div>
      <button data-view="tests"><span data-i="beaker"></span>Call tests<span class="count" id="nc-tests"></span></button>
      <button data-view="bulk"><span data-i="list"></span>Line tests<span class="count" id="nc-bulk"></span></button>
      <div class="nav-label">Look up</div>
      <button data-view="streams"><span data-i="wave"></span>Audio streams<span class="count" id="nc-streams"></span></button>
      <button data-view="syslog"><span data-i="list"></span>Syslog<span class="count" id="nc-syslog"></span></button>
      <button data-view="captures"><span data-i="file"></span>Captures</button>
      <div class="nav-label">Set up</div>
      <button data-view="sbcs"><span data-i="server"></span>SBCs<span class="count" id="nc-sbcs"></span></button>
      <button data-view="compare"><span data-i="diff"></span>Config compare</button>
      <button data-view="changes"><span data-i="shield"></span>Changes<span class="count" id="nc-changes"></span></button>
      <button data-view="settings"><span data-i="gear"></span>Settings</button>
    </nav>
    <div class="side-section">SBC feeds</div>
    <div class="sbc-mini" id="sbcMini"></div>
    <div class="side-foot"><span id="ver"></span><button class="btn ghost icon sm" id="theme" title="Switch light / dark" aria-label="Switch light or dark theme"><span data-i="moon"></span></button></div>
  </aside>

  <div class="main">
    <header class="top">
      <div><h1 id="title">Calls</h1><div class="sub" id="subtitle"></div></div>
      <div class="grow"></div>
      <div id="sbcLinks"></div>
      <div id="recBox"></div>
    </header>
    <div class="content">
      <div class="alerts" id="alerts"></div>

      <section id="v-dashboard"><div id="dashBox"></div></section>

      <section id="v-tests" class="hidden"><div id="testsBox"></div></section>

      <section id="v-bulk" class="hidden"><div id="bulkBox"></div></section>

      <section id="v-calls" class="hidden">
        <div class="kpis" id="kpis"></div>
        <div class="toolbar">
          <div class="search"><span data-i="search"></span><input type="search" id="q" placeholder="Search number, Call-ID or SID" aria-label="Search calls"><kbd>/</kbd></div>
          <div class="chips" id="fchips" role="tablist"></div>
          <select id="sbcFilter" aria-label="SBC"></select>
          <label class="toggle" title="SIP OPTIONS keep-alives are hidden by default"><input type="checkbox" id="showOptions"> Show OPTIONS</label>
        </div>
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
        <div class="toolbar">
          <div class="search"><span data-i="search"></span><input type="search" id="sq" placeholder="Search syslog: number, Call-ID, SID, word" aria-label="Search syslog"></div>
          <select id="sbcFilter3" aria-label="SBC"></select>
          <label class="toggle"><input type="checkbox" id="follow" checked> Follow live</label>
          <label class="toggle"><input type="checkbox" id="errorsOnly"> Warnings &amp; errors</label>
          <span class="muted" id="syslogCount"></span>
        </div>
        <div class="log" id="syslogBox"></div>
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
const bytes = b => b < 1024 ? b + " B" : b < 1048576 ? (b / 1024).toFixed(1) + " KB" : (b / 1048576).toFixed(1) + " MB";
const host = ep => { if (!ep) return ""; const i = ep.lastIndexOf(":"); return i > 0 ? ep.slice(0, i) : ep; };
const peerName = addr => { const h = host(addr || ""); return h && (status?.labels || []).find(n => n.ip === h)?.name || ""; };
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

};
const ic = (n, size = 16) => `<svg width="${size}" height="${size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${ICONS[n] || ""}</svg>`;
function paintIcons(root = document) { root.querySelectorAll("[data-i]").forEach(e => { if (!e.firstChild) e.innerHTML = ic(e.dataset.i, e.closest(".search") ? 15 : 17); }); }

function toast(text, bad) {
  const t = document.createElement("div");
  t.className = "toast" + (bad ? " bad" : ""); t.textContent = text;
  $("toasts").appendChild(t); setTimeout(() => t.remove(), 3800);
}
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
  const r = await fetch(path, opt);
  const ct = r.headers.get("content-type") || "";
  const data = ct.includes("json") ? await r.json() : await r.text();
  if (!r.ok) throw new Error(data.message || data || r.statusText);
  return data;
}

/* ------------------------------------------------------------ theme */
function applyTheme(t) { if (t) document.documentElement.dataset.theme = t; else delete document.documentElement.dataset.theme; }
applyTheme(store.get("theme", null));
$("theme").onclick = () => {
  const cur = document.documentElement.dataset.theme;
  const dark = cur ? cur === "dark" : matchMedia("(prefers-color-scheme:dark)").matches;
  const next = dark ? "light" : "dark"; applyTheme(next); store.set("theme", next);
};

/* ------------------------------------------------------------ state */
let view = store.get("view", "dashboard"), status = null, calls = [], selected = null, detail = null;
let filter = store.get("filter", "all"), dtab = "overview", bothLegs = true, openMsg = null, legDetails = {};
const sbcName = ip => { const s = (status?.sbcs || []).find(x => x.ip === ip && x.listed); return s ? s.name : ip; };
const titles = {dashboard:"Dashboard", calls:"Calls", tests:"Call tests", bulk:"Line tests", streams:"Audio streams", syslog:"Syslog", captures:"Captures", sbcs:"SBCs", compare:"Config compare", changes:"Changes", settings:"Settings"};
const sbcQ = el => el.value ? "sbc=" + encodeURIComponent(el.value) : "";

function go(v) {
  if (!titles[v]) v = "calls";
  view = v; store.set("view", v);
  document.querySelectorAll("#nav button").forEach(b => b.classList.toggle("on", b.dataset.view === v));
  document.querySelectorAll(".content > section").forEach(s => s.classList.toggle("hidden", s.id !== "v-" + v));
  $("title").textContent = titles[v];
  refresh();
}
document.querySelectorAll("#nav button").forEach(b => b.onclick = () => go(b.dataset.view));

/* ------------------------------------------------------------ header: capture + sbc links */
function renderTop(s) {
  const rec = s.recording;
  let html;
  if (rec) {
    const secs = (new Date(s.now) - new Date(s.startedAt)) / 1000;
    const left = s.stopsAt ? Math.ceil((new Date(s.stopsAt) - new Date(s.now)) / 60000) : null;
    html = `<div class="rec"><span class="pulse"></span>REC ${dur(secs)}${left !== null ? `<small>auto-stop in ${left} min</small>` : ""}<button class="btn danger sm" id="stopBtn">${ic("stop", 12)} Stop</button></div>`;
  } else html = `<button class="btn primary" id="startBtn">${ic("rec", 13)} Start capture</button>`;
  if (setHTML($("recBox"), html)) {
    const sb = $("startBtn"), st = $("stopBtn");
    if (sb) sb.onclick = async () => { try { await api("/api/start", {}); toast("Capture started"); selected = null; detail = null; } catch (e) { toast(e.message, true); } refresh(); };
    if (st) st.onclick = async () => { try { await api("/api/stop", {}); toast("Capture stopped: the files are under Captures"); } catch (e) { toast(e.message, true); } refresh(); };
  }
  $("subtitle").textContent = rec ? `Recording since ${tm(s.startedAt)} · ${fmtN(s.sipMessages)} SIP messages · ${fmtN(s.rtpPackets)} audio packets`
    : s.pcap ? "Showing the last capture · " + s.pcap : "Not recording";

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
  if (s.firewall && s.firewall.rule === false)
    a.push(["warn", "shield", `<b>Windows Firewall has no rule for MediantTrace,</b> so logs from the SBCs may be blocked. Run <span class="mono">MediantTrace.exe install</span> as administrator, or this command:<div style="margin-top:6px">${copyChip(s.firewall.command)}</div>`]);
  const live = s.sbcs.some(x => ago(s, x.syslog) < 60 || ago(s, x.dr) < 60);
  if (!live && !s.listenErrors.length)
    a.push(["info", "info", `<b>Nothing is arriving from any SBC.</b> Point each SBC's syslog and Debug Recording at <b>${esc((s.serverAddresses || []).join(" or "))}</b> (UDP ${s.settings.syslogPort} and ${s.settings.drPort}). <a href="#" data-go="sbcs">How to connect an SBC</a>`]);
  const unlisted = s.sbcs.filter(x => !x.listed);
  if (unlisted.length) a.push(["info", "server", `Logs are arriving from ${unlisted.map(x => `<b>${esc(x.ip)}</b>`).join(", ")}, not in the SBC list. <a href="#" data-go="sbcs">Add it on the SBCs page</a>`]);
  if (s.ignored) a.push(["info", "info", `${fmtN(s.ignored)} packets ignored from addresses not in the SBC list.`]);
  if (s.changesPending) a.push(["warn", "shield", `<b>${s.changesPending} SBC change${s.changesPending > 1 ? "s" : ""} waiting for approval.</b> <a href="#" data-go="changes">Review on the Changes page</a>`]);
  setHTML($("alerts"), a.map(([c, i, h]) => `<div class="alert ${c}"><span class="ic">${ic(i, 18)}</span><div>${h}</div></div>`).join(""));
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
document.addEventListener("keydown", e => {
  const typing = /INPUT|TEXTAREA|SELECT/.test(document.activeElement?.tagName || "");
  if (e.key === "/" && !typing) { e.preventDefault(); go("calls"); $("q").focus(); return; }
  if (e.key === "Escape" && typing) { document.activeElement.blur(); return; }
  if (view !== "calls" || typing || !["ArrowDown", "ArrowUp", "j", "k"].includes(e.key)) return;
  const list = visibleCalls(); if (!list.length) return;
  e.preventDefault();
  let i = list.findIndex(c => c.id === selected);
  i = (e.key === "ArrowDown" || e.key === "j") ? Math.min(list.length - 1, i + 1) : Math.max(0, i - 1);
  select(list[i].id);
  document.querySelector(`.row[data-id="${list[i].id}"]`)?.scrollIntoView({block:"nearest"});
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
  if (!calls.length) html = emptyState("phone", status?.recording ? "Waiting for calls" : "No capture running",
      status?.recording ? "Calls appear here as soon as an SBC logs SIP for them. Make a test call." : "Press <b>Start capture</b>, then make the call you want to look at.");
  else if (!list.length) html = emptyState("search", "No calls match", "Try another filter or search.");
  else html = list.map(c => {
    const issue = c.topIssue ? `<div class="issue ${c.severity === "bad" ? "" : "warn"}">${ic(c.severity === "bad" ? "x" : "alert", 14)}<span>${esc(c.topIssue)}</span></div>` : "";
    const aIn = c.audioIn || c.audioOut ? c.audioIn : c.sbcIn, aOut = c.audioIn || c.audioOut ? c.audioOut : c.sbcOut;
    const audio = (aIn > 0 || aOut > 0) ? `<span class="flow" title="Audio packets received / sent by the SBC${c.audioIn || c.audioOut ? "" : " (from its call record)"}"><span class="arrow-in ${aIn ? "" : "z"}">${ic("in", 12)}${fmtN(aIn)}</span><span class="arrow-out ${aOut ? "" : "z"}">${ic("out", 12)}${fmtN(aOut)}</span></span>` : "";
    return `<div class="row ${c.id === selected ? "sel" : ""}" data-id="${c.id}" role="button" tabindex="-1">
      <div class="sev ${c.severity}"></div>
      <div style="min-width:0">
        <div class="who"><span>${esc(c.from || "unknown")}</span><span class="muted">→</span><span>${esc(c.to || "unknown")}</span></div>
        <div class="meta">${stateBadge(c)}${mosBadge(c.mos)}<span>${esc(sbcName(c.sbc))}</span>${c.seconds ? `<span>${dur(c.seconds)}</span>` : ""}${c.codec ? `<span>${esc(c.codec)}</span>` : ""}</div>
        ${issue}
      </div>
      <div class="side-r"><span>${tm(c.started)}</span>${audio}</div>
    </div>`;
  }).join("");
  if (setHTML($("rows"), html)) $("rows").querySelectorAll(".row").forEach(r => r.onclick = () => select(+r.dataset.id));
  if (selected === null && list.length) select(list[0].id, true);
}
function select(id, quiet) {
  if (selected === id) return;
  selected = id; openMsg = null; legDetails = {};
  if (!quiet) history.replaceState(null, "", "#call=" + id);
  document.querySelectorAll(".row").forEach(r => r.classList.toggle("sel", +r.dataset.id === id));
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
  const more = n => n > 0 ? `${n} more finding${n > 1 ? "s" : ""} below` : "";
  if (bad.length) return ["bad", "x", bad[0].text, more(d.checks.length - 1)];
  if (warn.length) return ["warn", "alert", warn[0].text, more(d.checks.length - 1)];
  if (d.state === "trying" || d.state === "ringing") return ["ok", "info", "Call in progress: nothing wrong so far.", ""];
  return ["ok", "check", "No problems found in the SIP, SDP or audio.", ""];
}
function renderDetail() {
  const d = detail; if (!d) return;
  const [vc, vi, vt, vs] = verdict(d);
  const setup = d.answered ? Math.round(new Date(d.answered) - new Date(d.started)) : null;
  const flowCount = d.messages.length + Object.values(legDetails).reduce((n, l) => n + l.messages.length, 0);
  const tabs = [["overview", "Overview"], ["flow", `Call flow · ${flowCount}`], ["media", `Media · ${d.streams.length}`], ["sdp", `SDP · ${d.sdps.length}`], ["cdr", `SBC report · ${d.cdrs.length}`], ["sbclog", "SBC log"], ["messages", "Messages"]];
  const head = `<div class="d-head">
      <div class="d-title"><h2>${esc(d.from || "unknown")} <span class="muted">→</span> ${esc(d.to || "unknown")}</h2>${stateBadge(d)}
        <div class="d-actions">
          <button class="btn sm" data-copy="${esc(location.origin + location.pathname + "#call=" + d.id)}" title="Copy a link to this call">${ic("link", 14)} Link</button>
          <a class="btn sm" href="/api/calls/${d.id}/text" title="Download this call's SIP messages">${ic("down", 14)} .txt</a>
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
    const rb = el.querySelector("#reportBtn"); if (rb) rb.onclick = () => callReport(detail);
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
    ["Legs", String(1 + d.otherLegs.length)], ["Estimated MOS", d.mos != null ? `${d.mos.toFixed(2)} · ${mosGrade(d.mos)}` : "—"],
  ];
  const checks = d.checks.length ? d.checks.map(c => `<div class="check ${c.level}"><span class="ic">${ic(c.level === "bad" ? "x" : "alert", 17)}</span><div>${esc(c.text)}</div></div>`).join("")
    : `<div class="check ok"><span class="ic">${ic("check", 17)}</span><div>No problems found.</div></div>`;
  const legs = d.otherLegs.length ? `<h3>Other legs of this call</h3><div class="checks">${d.otherLegs.map(o => `<div class="check"><span class="ic muted">${ic("phone", 16)}</span><div><a href="#" data-leg="${o.id}">${esc(o.from)} → ${esc(o.to)}</a> <span class="muted">· ${esc(o.state)}</span></div></div>`).join("")}</div>` : "";
  return `<div class="facts">${facts.map(([l, v]) => `<div><div class="l">${l}</div><div class="v" title="${esc(v)}">${esc(v)}</div></div>`).join("")}</div>
    <h3>Checks</h3><div class="checks">${checks}</div>${mediaPaths(d)}${legs}`;
}
function mediaPaths(d) {
  if (!d.media.length) return `<h3>Audio</h3><p class="muted" style="margin:0">${d.streams.length ? "Audio packets are present, but no far-end SDP has been seen yet." : "No audio packets. Turn on Debug Recording with <b>Signaling &amp; Media</b> on the SBC to see the audio."}</p>`;
  return `<h3>Audio by far end</h3><div class="paths">${d.media.map(m => {
    const max = Math.max(m.toPeer, m.fromPeer, 1);
    const dir = (label, v, rx) => `<div class="dir"><span class="t2">${label}</span><div class="bar"><i class="${v ? (rx ? "rx" : "") : "zero"}" style="width:${Math.max(2, v / max * 100)}%"></i></div><span class="n ${v ? "" : "zero"}">${fmtN(v)}</span></div>`;
    return `<div class="path"><div class="path-h"><span class="mono">${esc(m.sdpAddress)}</span><span class="badge">a=${esc(m.direction)}</span></div>
      ${dir("SBC → far end", m.toPeer)}${dir("Far end → SBC", m.fromPeer, true)}
      <div class="path-f"><span>MOS SBC → far end ${mosBadge(m.mosTo)}</span><span>MOS far end → SBC ${mosBadge(m.mosFrom)}</span></div>
      <div class="path-f"><span>Lost ${fmtN(m.lost)}</span><span>Jitter ${m.jitterMs} ms</span><span>Longest gap ${fmtN(m.maxGapMs)} ms</span>${m.actualFrom ? `<span>Actually from ${esc(m.actualFrom)}</span>` : ""}</div></div>`;
  }).join("")}</div>`;
}

/* Call flow: the first leg's far end | the SBC | the other legs' far ends */
function flowTab(d) {
  const legs = [{leg: 0, d}].concat(bothLegs ? Object.values(legDetails).map((l, i) => ({leg: i + 1, d: l})) : []);
  const msgs = [];
  legs.forEach(({leg, d: ld}) => ld.messages.forEach(m => msgs.push({...m, leg, key: leg + ":" + m.index})));
  msgs.sort((a, b) => new Date(a.time) - new Date(b.time));
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
    const color = /^SIP\/2\.0 [3-6]/.test(m.first) ? "var(--fail)" : /^SIP\/2\.0 2/.test(m.first) ? "var(--succ)" : /^SIP\/2\.0 1/.test(m.first) ? "var(--prov)" : "var(--req)";
    const label = m.first.replace(/^SIP\/2\.0 /, "").replace(/ SIP\/2\.0$/, "").replace(/sips?:([^@ ;>]*)@\S*/, "$1").slice(0, 30);
    const delta = prev ? new Date(m.time) - new Date(prev) : null; prev = m.time;
    svg += `<g class="msg ${openMsg === m.key ? "sel" : ""}" data-k="${m.key}">
      <rect class="hit" x="0" y="${y - 21}" width="${width}" height="${H}"/>
      <text class="time" x="12" y="${y - 2}">${tms(m.time)}</text>
      ${delta !== null ? `<text class="delta" x="12" y="${y + 12}">+${delta < 1000 ? delta + " ms" : (delta / 1000).toFixed(2) + " s"}</text>` : ""}
      <line x1="${x1}" y1="${y}" x2="${x2 - dir * 8}" y2="${y}" stroke="${color}" stroke-width="1.8"/>
      <path d="M${x2 - dir * 11},${y - 5} L${x2},${y} L${x2 - dir * 11},${y + 5} z" fill="${color}"/>
      <text x="${(x1 + x2) / 2}" y="${y - 6}" text-anchor="middle" style="fill:${color};font-weight:600">${esc(label)}</text>
      ${m.hasSdp ? `<text class="sdp" x="${(x1 + x2) / 2}" y="${y + 13}" text-anchor="middle">SDP</text>` : ""}
    </g>`;
  });
  svg += "</svg>";
  const legToggle = d.otherLegs.length ? `<label class="toggle"><input type="checkbox" id="bothLegs" ${bothLegs ? "checked" : ""}> Show both legs through the SBC</label>`
    : `<span class="muted">One leg: no other leg with the same SID.</span>`;
  const sel = openMsg ? msgs.find(m => m.key === openMsg) : null;
  const panel = sel ? `<div class="msg-panel"><div class="bar"><b>${esc(sel.first)}</b><span class="muted">${tms(sel.time)} · ${esc(sel.src || "SBC")} → ${esc(sel.dst || "SBC")} · via ${sel.source === "DR" ? "Debug Recording" : "syslog"}</span><span class="grow"></span><button class="btn sm" data-copy="${esc(sel.text)}">${ic("copy", 13)} Copy</button></div>${sipHtml(sel.text)}</div>`
    : `<p class="muted" style="margin:10px 0 0">Click a message to read it in full.</p>`;
  return `<div class="ladder-tools">${legToggle}<span class="grow"></span><span class="badge accent">Request</span><span class="badge">1xx</span><span class="badge ok">2xx</span><span class="badge bad">3xx–6xx</span></div>
    <div class="ladder-wrap ladder">${svg}</div>${panel}`;
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
    <h3>Streams</h3><div class="card scroll">${streamTable(d.streams.map(s => ({stream: s})), false)}</div>`;
}
function streamTable(items, withSbc) {
  return `<table class="tbl"><thead><tr>${withSbc ? "<th>SBC</th>" : ""}<th>From</th><th>To</th><th>SSRC</th><th class="r">PT</th><th class="r">Packets</th><th class="r">Lost</th><th class="r">Loss</th><th class="r">Jitter</th><th class="r">Longest gap</th><th>MOS</th><th>First</th><th>Last</th>${withSbc ? "<th>Call</th>" : ""}</tr></thead><tbody>` +
    items.map(x => { const s = x.stream; const lc = s.lossPercent > 2 ? "bad" : s.lossPercent > 0.5 ? "warn" : "";
      return `<tr>${withSbc ? `<td>${esc(sbcName(x.sbc))}</td>` : ""}<td>${addrCell(s.src)}</td><td>${addrCell(s.dst)}</td><td class="mono">${esc(s.ssrc)}</td><td class="r">${s.payloadType}</td><td class="r">${fmtN(s.packets)}</td><td class="r">${fmtN(s.lost)}</td><td class="r">${lc ? `<span class="badge ${lc}">${s.lossPercent}%</span>` : s.lossPercent + "%"}</td><td class="r">${s.jitterMs != null ? s.jitterMs + " ms" : "—"}</td><td class="r">${fmtN(s.maxGapMs)} ms</td><td>${mosBadge(s.mos)}</td><td>${tm(s.first)}</td><td>${tm(s.last)}</td>${withSbc ? `<td>${x.matched ? '<span class="badge ok">matched</span>' : '<span class="badge">no call</span>'}</td>` : ""}</tr>`; }).join("") +
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
      <span class="k">Codecs</span><span>${s.codecs.map(c => `<span class="codec ${common.includes(c) ? "common" : ""}">${esc(c)}</span>`).join("") || "—"}</span></div></div>`;
  };
  return `<p class="muted" style="margin-top:0">Each offer next to its answer. Codecs both sides support are highlighted.</p>` + pairs.map(([o, a]) => `<div class="sdp-pair">${card(o, a)}${card(a, o)}</div>`).join("");
}
/* The SBC's own call records (CDRs): the key facts first, every column below. */
const CDR_KEYS = [["TrmReason", "End reason"], ["TrmReasonCategory", "End category"], ["SIPTrmReason", "SIP end reason"], ["Durat", "Duration (s)"], ["Coder", "Codec"],
  ["InPackets", "Audio packets in"], ["OutPackets", "Audio packets out"], ["PackLoss", "Packets lost (in)"], ["RemotePackLoss", "Packets lost (far end)"], ["RTPjitter", "Jitter (ms)"], ["RTPdelay", "Delay (ms)"],
  ["RtpIp", "Far-end media IP"], ["Port", "Far-end media port"], ["LocalRtpIp", "SBC media IP"], ["LocalRtpPort", "SBC media port"],
  ["IPG", "IP group"], ["IPGroup", "IP group"], ["SIPInterfaceId", "SIP interface"], ["ProxySetId", "Proxy set"], ["IpProfileId", "IP profile"], ["MediaRealmId", "Media realm"], ["SrdId", "SRD"],
  ["SrcPhoneNum", "Calling"], ["DstPhoneNum", "Called"], ["Caller", "Caller"], ["Callee", "Callee"], ["SetupTime", "Set up"], ["ConnectTime", "Connected"], ["ReleaseTime", "Released"], ["TrmSd", "Ended by"]];
function cdrTab(d) {
  if (!d.cdrs.length) return emptyState("report", "No SBC call record yet", "The SBC sends its record (CDR) in syslog when the call ends. Syslog must be on, at debug level <b>Detailed</b>.");
  return d.cdrs.map(r => {
    const f = Object.fromEntries(r.fields.map(x => [x.k, x.v]));
    const inP = +f.InPackets, outP = +f.OutPackets;
    const key = CDR_KEYS.filter(([k]) => f[k] !== undefined && f[k] !== "" && f[k] !== "-1").map(([k, l]) => {
      const warn = (k === "InPackets" && inP >= 0 && outP > 500 && inP < outP / 20) || (k === "TrmReason" && /BROKEN|ABNORMAL|FAIL/i.test(f[k]));
      return `<div><div class="l">${l}</div><div class="v" title="${esc(f[k])}" ${warn ? 'style="color:var(--bad)"' : ""}>${esc(/^\d+$/.test(f[k]) && +f[k] > 999 ? fmtN(+f[k]) : f[k])}</div></div>`;
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

/* ------------------------------------------------------------ syslog view */
let syslogTimer;
$("sq").oninput = () => { clearTimeout(syslogTimer); syslogTimer = setTimeout(loadSyslog, 250); };
$("sbcFilter3").onchange = () => loadSyslog();
$("errorsOnly").onchange = () => loadSyslog();
async function loadSyslog() {
  const q = $("sq").value.trim();
  const lines = await api(`/api/syslog?limit=1500&${sbcQ($("sbcFilter3"))}&q=${encodeURIComponent(q)}${$("errorsOnly").checked ? "&errors=1" : ""}`);
  $("syslogCount").textContent = `${fmtN(lines.length)} line${lines.length === 1 ? "" : "s"}${q ? " matching" : ""}`;
  const hl = t => {
    let h = esc(t);
    if (q) h = h.split(esc(q)).join(`<mark>${esc(q)}</mark>`);
    return h.replace(/\[SID[=:]([^\]]*)\]/g, (m, sid) => `<span class="sid" data-sid="${sid}" title="Show only this session">${m}</span>`)
            .replace(/(-{3,} (?:Incoming|Outgoing) SIP Message[^-]*-{3,})/g, '<span class="banner">$1</span>');
  };
  const box = $("syslogBox");
  const sev = t => { const m = /^<(\d{1,3})>/.exec(t); return m ? (+m[1]) & 7 : 9; };
  const html = lines.length ? lines.slice().reverse().map(l => `<div class="${sev(l.text) <= 3 ? "lv-err" : sev(l.text) === 4 ? "lv-warn" : ""}"><span class="tm">${tms(l.time)}</span><span class="sb">${esc(sbcName(l.source))}</span><span>${hl(l.text)}</span></div>`).join("")
    : emptyState("list", "No syslog lines", status?.recording ? "Nothing logged yet, or nothing matches." : "Start a capture to collect syslog.");
  const atBottom = box.scrollHeight - box.scrollTop - box.clientHeight < 40;
  if (setHTML(box, html)) {
    box.querySelectorAll("[data-sid]").forEach(s => s.onclick = () => { $("sq").value = s.dataset.sid; loadSyslog(); });
    if (atBottom || !box.__init) { box.scrollTop = box.scrollHeight; box.__init = true; }
  }
}

/* ------------------------------------------------------------ captures */
async function loadCaptures() {
  const files = await api("/api/files");
  const groups = {};
  files.forEach(f => { const k = f.name.replace(/\.(pcap|txt)$/, ""); (groups[k] = groups[k] || {name: k, files: []}).files.push(f); });
  const list = Object.values(groups);
  const stamp = k => { const m = /mediant-(\d{4})(\d{2})(\d{2})-(\d{2})(\d{2})(\d{2})/.exec(k); return m ? new Date(+m[1], m[2] - 1, +m[3], +m[4], +m[5], +m[6]) : null; };
  setHTML($("capBox"), list.length ? `<div class="card"><div class="card-h"><h2>Saved captures</h2><span class="grow"></span><span class="muted mono">${esc(status?.settings.traceFolder || "")}</span></div>
    <div class="scroll"><table class="tbl"><thead><tr><th>Captured</th><th>Files</th><th class="r">Size</th><th class="r">Download</th></tr></thead><tbody>${list.map(g => {
      const active = g.files.some(f => f.active), started = stamp(g.name);
      return `<tr><td><b>${started ? started.toLocaleString([], {weekday:"short", day:"2-digit", month:"short", hour:"2-digit", minute:"2-digit", second:"2-digit"}) : esc(g.name)}</b> ${active ? '<span class="badge bad">recording</span>' : ""}<div class="muted mono">${esc(g.name)}</div></td>
        <td>${g.files.map(f => f.name.endsWith(".pcap") ? '<span class="badge accent">pcap</span>' : '<span class="badge">txt</span>').join(" ")}</td>
        <td class="r">${bytes(g.files.reduce((n, f) => n + f.size, 0))}</td>
        <td class="r">${g.files.map(f => `<a class="btn sm" href="/files/${encodeURIComponent(f.name)}">${ic("down", 13)} ${f.name.endsWith(".pcap") ? ".pcap" : ".txt"}</a>`).join(" ")}</td></tr>`;
    }).join("")}</tbody></table></div></div>
    <p class="muted">The <b>.pcap</b> holds every packet received from the SBCs, for AudioCodes support. The <b>.txt</b> holds every SIP message and syslog line, readable in Notepad.</p>`
    : `<div class="card">${emptyState("file", "No captures yet", "Press <b>Start capture</b>. Each capture saves a .pcap and a .txt here.")}</div>`);
}

/* ------------------------------------------------------------ SBCs */
let sbcEdit = null;
function renderSbcs(s) {
  const listed = s.sbcs.filter(x => x.listed), unlisted = s.sbcs.filter(x => !x.listed);
  if (sbcEdit === null) sbcEdit = listed.map(x => ({name: x.name, ip: x.ip, url: x.url}));
  const feed = (label, x) => { const a = ago(s, x); const cls = a < 60 ? "ok" : a < Infinity ? "warn" : "";
    return `<div class="feed"><span class="dot ${cls}"></span><span class="l">${label}</span><span>${a < 60 ? "Receiving" : a < Infinity ? "Stopped" : "Not set up"}</span><span class="s">${x ? fmtN(x.packets) + " packets · " + tm(x.lastSeen) : ""}</span></div>`; };
  const cards = listed.map(x => `<div class="card sbc-card"><div class="card-h">${ic("server", 17)}<h2>${esc(x.name)}</h2><span class="muted mono">${esc(x.ip)}</span><span class="grow"></span>${x.url ? `<a class="btn sm" href="${esc(x.url)}" target="_blank" rel="noopener">${ic("ext", 13)} Web page</a>` : ""}</div>
    <div class="card-b">${feed("Syslog", x.syslog)}${feed("Debug Recording", x.dr)}${namesBlock(x)}${loginCard(x)}</div></div>`).join("");
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
        <input type="text" data-k="name" placeholder="Name, e.g. London SBC" value="${esc(e.name)}" aria-label="Name"><input type="text" data-k="ip" placeholder="IP, e.g. 192.168.1.244" value="${esc(e.ip)}" aria-label="IP address">
        <input type="text" data-k="url" placeholder="Web page, e.g. https://192.168.1.244" value="${esc(e.url)}" aria-label="Web page"><button class="btn icon" data-del="${i}" title="Remove" aria-label="Remove">${ic("trash", 15)}</button></div>`).join("")}</div>
      <div style="display:flex;gap:10px;flex-wrap:wrap;align-items:center;margin-top:6px"><button class="btn" id="sbcAdd">${ic("plus", 14)} Add SBC</button><span class="grow"></span>
      <label class="toggle"><input type="checkbox" id="onlyListed" ${s.settings.onlyListedSbcs ? "checked" : ""}> Ignore senders not in this list</label>
      <button class="btn primary" id="sbcSave">Save</button></div></div></div>`;
  const box = $("sbcBox");
  const editing = box.contains(document.activeElement) && document.activeElement.tagName === "INPUT";
  if (editing && $("sbcStatus")) {
    if (setHTML($("sbcStatus"), top)) box.querySelectorAll("[data-names]").forEach(b => b.onclick = () => readNames(b));
    return;
  }
  if (setHTML(box, `<div id="sbcStatus">${top}</div>${how}${editor}`)) {
    box.querySelectorAll("#sbcRows input").forEach(inp => inp.oninput = () => { sbcEdit[+inp.closest("[data-row]").dataset.row][inp.dataset.k] = inp.value; });
    wireLogins(box);
    box.querySelectorAll("[data-names]").forEach(b => b.onclick = () => readNames(b));
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
async function renderSettings(s) {
  const box = $("setBox");
  if (box.contains(document.activeElement) && document.activeElement.tagName === "INPUT") return;
  let log = [];
  try { log = await api("/api/log"); } catch {}
  const fw = s.firewall || {};
  const html = `<div class="grid-cards" style="grid-template-columns:repeat(auto-fill,minmax(380px,1fr))">
    <div class="card"><div class="card-h"><h2>Capture</h2></div><div class="card-b form">
      <div><label for="autoStop">Stop a forgotten capture after (minutes, 0 = never)</label><input type="number" id="autoStop" min="0" max="1440" value="${s.settings.autoStopMinutes}"></div>
      <div><button class="btn primary" id="saveCap">Save</button></div>
      <div class="muted">Captures are saved in <span class="mono">${esc(s.settings.traceFolder)}</span></div></div></div>
    <div class="card"><div class="card-h"><h2>Network</h2></div><div class="card-b form">
      <div><label>This server</label>${(s.serverAddresses || []).map(copyChip).join(" ") || "—"}</div>
      <div><label>Listening on</label>${copyChip("UDP " + s.settings.syslogPort)} syslog &nbsp; ${copyChip("UDP " + s.settings.drPort)} Debug Recording</div>
      <div><label>Windows Firewall</label>${fw.rule === true ? '<span class="badge ok">Rule present</span>' : fw.rule === false ? `<span class="badge warn">No rule</span><div style="margin-top:8px">Run as administrator:</div><div style="margin-top:4px">${copyChip(fw.command || "")}</div>` : '<span class="badge">Unknown</span>'}</div>
      <div><label>This page</label>${s.settings.webOnNetwork ? `Open to the network${s.settings.passwordSet ? " (password protected)" : ' <span class="badge warn">no password</span>'}` : "This server only (http://localhost). Set <span class='mono'>WebOnNetwork</span> in settings.json to open it to the network."}</div>
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
async function renderDashboard() {
  const [all, tests] = await Promise.all([api("/api/calls"), api("/api/tests").catch(() => null)]);
  const s = status, box = $("dashBox");
  const failed = all.filter(c => c.state === "failed").length, issues = all.filter(c => c.warnings > 0).length, oneway = all.filter(c => c.oneWay).length;
  const mosVals = all.map(c => c.mos).filter(v => v != null), avgMos = mosVals.length ? mosVals.reduce((a, b) => a + b, 0) / mosVals.length : null;
  const lastResults = (tests?.tests || []).map(t => t.last).filter(Boolean);
  const passed = lastResults.filter(r => r.outcome === "passed").length;

  const tile = (icon, label, value, sub, cls) => `<div class="card stat span-3"><div class="l">${ic(icon, 15)} ${label}</div><div class="v ${cls || ""}">${value}</div><div class="s">${sub}</div></div>`;
  let html = `<div class="dash">
    ${tile("phone", "Calls this capture", fmtN(all.length), s.recording ? `Recording since ${tm(s.startedAt)}` : "Not recording")}
    ${tile("x", "Failed calls", `${fmtN(failed)}<small>${pct(failed, all.length)}%</small>`, `${fmtN(issues)} with issues · ${fmtN(oneway)} one-way audio`)}
    ${tile("wave", "Average audio quality", avgMos != null ? `${avgMos.toFixed(2)}<small>${mosLabel[mosGrade(avgMos)]}</small>` : "—", mosVals.length ? `Estimated MOS over ${mosVals.length} call${mosVals.length === 1 ? "" : "s"} with audio` : "Needs Debug Recording with media")}
    ${tile("beaker", "Call tests", lastResults.length ? `${passed}<small>of ${lastResults.length} passing</small>` : "—", lastResults.length ? "Latest result of each test" : `<a href="#" data-go="tests">Set up a test call</a>`)}`;

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
      <div class="card span-5"><div class="card-h"><h2>Audio quality</h2><span class="muted">average MOS per ${per}</span></div><div class="card-b">${mosVals.length ? lineChart({id: "d-mos", label: "Average MOS", xs, h: 190, yMin: 1, yMax: 4.5, refs: [{y: 4, label: "Good"}, {y: 3.6, label: "Fair"}], fmtX: v => stepS < 60 ? tm(v) : tm(v).slice(0, 5), fmtY: v => v.toFixed(2), series: [{name: "Average MOS", color: "var(--s1)", values: mosPer}]}) : emptyState("wave", "No audio measured", "Turn on Debug Recording with <b>Signaling &amp; Media</b> to score audio.")}</div></div>`;
  } else {
    html += `<div class="card span-12">${emptyState("grid", s.recording ? "Waiting for calls" : "No capture running", s.recording ? "Charts fill in as calls go through the SBCs." : "Press <b>Start capture</b> to record calls, or run a call test.")}</div>`;
  }

  const problems = all.filter(c => c.severity !== "ok").slice(0, 8);
  html += `<div class="card span-7"><div class="card-h"><h2>Latest problems</h2><span class="grow"></span><a href="#" data-go="calls">All calls</a></div>
    ${problems.length ? `<div class="feed-list">${problems.map(c => `<div class="feed-item" data-call="${c.id}"><span class="ic" style="color:var(--${c.severity === "bad" ? "bad" : "warn"})">${ic(c.severity === "bad" ? "x" : "alert", 17)}</span><div style="min-width:0"><div class="tt">${esc(c.from || "unknown")} → ${esc(c.to || "unknown")} <span class="muted" style="font-weight:500">· ${esc(sbcName(c.sbc))} · ${tm(c.started)}</span></div><div class="ss">${esc(c.topIssue)}</div></div>${mosBadge(c.mos)}</div>`).join("")}</div>`
      : `<div class="empty" style="padding:28px">${ic("check", 22)}<p style="margin-top:8px">No problems in this capture.</p></div>`}</div>
    <div class="card span-5"><div class="card-h"><h2>Call tests</h2><span class="grow"></span><a href="#" data-go="tests">Manage</a></div>
    ${(tests?.tests || []).length ? `<div class="feed-list">${tests.tests.slice(0, 8).map(t => `<div class="feed-item" data-test="${esc(t.id)}"><span class="ic">${ic("beaker", 17)}</span><div style="min-width:0"><div class="tt">${esc(t.name)}</div><div class="ss">${t.last ? `${tm(t.last.started)} · ${esc(t.last.finding)}` : "Not run yet"}</div></div><div style="display:flex;gap:6px;align-items:center">${t.last ? mosBadge(t.last.mosRx) + outcomeBadge(t.last.outcome) : ""}</div></div>`).join("")}</div>`
      : `<div class="empty" style="padding:28px"><p>No call tests yet. <a href="#" data-go="tests">Create one</a> to check audio both ways through an SBC.</p></div>`}</div>
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
  }
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
        <li><div>From a dealerboard line (or any phone behind the SBC), dial the test number. At home the Mediant sends <b>9998</b> to this server.</div></li>
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
      <div><label for="tf-sbc">SBC SIP address</label><input type="text" id="tf-sbc" list="sbcIps" value="${esc(t.sbcIp)}" placeholder="192.168.1.244"><div class="help">Where the INVITE is sent: the SBC's SIP interface.</div></div>
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
<p class="muted" style="margin-top:30px;font-size:11.5px">Generated by MediantTrace ${esc(status?.version || "")} on ${esc(new Date().toLocaleString())}. MOS values are E-model estimates from packet loss, jitter and delay.</p></div></body></html>`;
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
      if (!x) rows.push({key, ia: null, ib: y.i, kind: "only-b", diffs: cols.map((c, j) => ({c, a: null, b: y.v[j]}))});
      else if (!y) rows.push({key, ia: x.i, ib: null, kind: "only-a", diffs: cols.map((c, j) => ({c, a: x.v[j], b: null}))});
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

let cmpFiles = [null, null], cmpNames = ["", ""], cmpResult = null, cmpOnlyDiff = true, cmpByName = false;
function renderCompare() {
  const box = $("cmpBox");
  if (box.__built) { drawCompare(); return; }
  box.__built = true;
  box.innerHTML = `<div class="card"><div class="card-h"><h2>Compare two configurations</h2><span class="muted">AudioCodes .ini files: two SBCs, or before and after a change. The files stay in your browser.</span></div><div class="card-b">
    <div class="form-grid">
      ${[0, 1].map(i => `<div><label>${i ? "Second" : "First"} configuration</label><input type="file" id="cmpFile${i}" accept=".ini,.txt" style="width:100%"><input type="text" id="cmpName${i}" placeholder="${i ? "e.g. New York SBC, or After" : "e.g. London SBC, or Before"}" style="width:100%;margin-top:6px"></div>`).join("")}
    </div>
    <div style="display:flex;gap:14px;align-items:center;margin-top:14px;flex-wrap:wrap">
      <button class="btn primary" id="cmpRun">Compare</button>
      <label class="toggle"><input type="checkbox" id="cmpOnlyDiff" checked> Differences only</label>
      <label class="toggle" title="Match table rows by their name column instead of their index (use when rows were added in a different order)"><input type="checkbox" id="cmpByName"> Match table rows by name</label>
      <span class="grow"></span><button class="btn" id="cmpReport" disabled>${ic("report", 14)} Report</button>
    </div>
    <p class="help">Export from each SBC with <b>Actions → Configuration File → Save INI File</b> (or <b>Maintenance → Configuration File</b>).</p>
  </div></div><div id="cmpOut"></div>`;
  const load = i => new Promise(res => { const f = $("cmpFile" + i).files[0]; if (!f) return res(null); const r = new FileReader(); r.onload = () => res({name: f.name, text: r.result}); r.readAsText(f); });
  $("cmpRun").onclick = async () => {
    const [a, b] = await Promise.all([load(0), load(1)]);
    if (!a || !b) { toast("Choose both files first", true); return; }
    cmpFiles = [a, b]; cmpNames = [$("cmpName0").value || a.name, $("cmpName1").value || b.name];
    cmpByName = $("cmpByName").checked;
    cmpResult = compareIni(parseIni(a.text), parseIni(b.text), cmpByName);
    $("cmpReport").disabled = false;
    drawCompare();
  };
  $("cmpOnlyDiff").onchange = () => { cmpOnlyDiff = $("cmpOnlyDiff").checked; drawCompare(); };
  $("cmpByName").onchange = () => { if (cmpFiles[0]) $("cmpRun").click(); };
  $("cmpReport").onclick = () => openReport(`Configuration compare: ${cmpNames[0]} vs ${cmpNames[1]}`, `${esc(cmpFiles[0].name)} · ${esc(cmpFiles[1].name)}`, compareHtml(true));
}
function drawCompare() { if (cmpResult) $("cmpOut").innerHTML = compareHtml(false); }
function compareHtml(forReport) {
  const r = cmpResult, [A, B] = cmpNames;
  const pChanged = r.params.filter(p => p.kind !== "same"), tChanged = r.tables.filter(t => t.rows.some(x => x.kind !== "same") || t.onlyIn);
  const rowsChanged = r.tables.reduce((n, t) => n + t.rows.filter(x => x.kind !== "same").length, 0);
  const val = v => v === null ? '<span class="badge">missing</span>' : `<span class="mono">${esc(v === "" ? "(empty)" : v)}</span>`;
  const kindBadge = k => k === "changed" ? '<span class="badge warn">changed</span>' : k === "only-a" ? `<span class="badge bad">only in ${esc(A)}</span>` : k === "only-b" ? `<span class="badge accent">only in ${esc(B)}</span>` : '<span class="badge ok">same</span>';
  const tiles = `<div class="dash" style="margin:16px 0">
    <div class="card stat span-4"><div class="l">${ic("gear", 15)} Settings that differ</div><div class="v">${pChanged.length}<small>of ${r.params.length}</small></div><div class="s">General parameters</div></div>
    <div class="card stat span-4"><div class="l">${ic("list", 15)} Table rows that differ</div><div class="v">${rowsChanged}</div><div class="s">in ${tChanged.length} table${tChanged.length === 1 ? "" : "s"}</div></div>
    <div class="card stat span-4"><div class="l">${ic(pChanged.length + rowsChanged ? "alert" : "check", 15)} Verdict</div><div class="v" style="font-size:22px">${pChanged.length + rowsChanged ? "Different" : "Identical"}</div><div class="s">${esc(A)} vs ${esc(B)}</div></div></div>`;
  const plist = (cmpOnlyDiff || forReport ? pChanged : r.params).sort((x, y) => (x.sec + x.k).localeCompare(y.sec + y.k));
  const params = `<div class="card" style="margin-bottom:16px"><div class="card-h"><h2>General settings</h2><span class="muted">${pChanged.length} different</span></div>${plist.length ? `<div class="scroll"><table class="tbl"><thead><tr><th>Section</th><th>Parameter</th><th>${esc(A)}</th><th>${esc(B)}</th><th></th></tr></thead><tbody>${plist.map(p => `<tr><td class="muted">${esc(p.sec)}</td><td class="mono">${esc(p.k)}</td><td style="white-space:normal">${val(p.a)}</td><td style="white-space:normal">${val(p.b)}</td><td>${kindBadge(p.kind)}</td></tr>`).join("")}</tbody></table></div>` : `<p class="muted" style="padding:0 16px">No differences.</p>`}</div>`;
  const tlist = (cmpOnlyDiff || forReport ? tChanged : r.tables).sort((x, y) => x.name.localeCompare(y.name));
  const tables = tlist.map(t => {
    const rows = t.rows.filter(x => !(cmpOnlyDiff || forReport) || x.kind !== "same");
    const n = t.rows.filter(x => x.kind !== "same").length;
    return `<div class="card" style="margin-bottom:12px"><div class="card-h"><h2 class="mono" style="font-size:13.5px">${esc(t.name)}</h2>${t.onlyIn ? `<span class="badge ${t.onlyIn === "a" ? "bad" : "accent"}">table only in ${esc(t.onlyIn === "a" ? A : B)}</span>` : ""}<span class="grow"></span><span class="muted">${n} row${n === 1 ? "" : "s"} differ of ${t.rows.length}</span></div>
      ${rows.length ? `<div class="scroll"><table class="tbl"><thead><tr><th>Row</th><th>Column</th><th>${esc(A)}</th><th>${esc(B)}</th><th></th></tr></thead><tbody>${rows.map(x => {
        const label = `${x.ia ?? "—"}${x.ib !== null && x.ib !== x.ia ? " / " + x.ib : ""}${x.name ? ` <span class="muted">${esc(x.name)}</span>` : ""}`;
        if (x.kind === "same") return `<tr><td>${label}</td><td colspan="3" class="muted">identical</td><td>${kindBadge(x.kind)}</td></tr>`;
        const diffs = x.kind === "changed" ? x.diffs : x.diffs.filter(d => (d.a ?? d.b) !== "" && (d.a ?? d.b) !== undefined).slice(0, 12);
        return diffs.map((d, j) => `<tr>${j === 0 ? `<td rowspan="${diffs.length}" style="vertical-align:top">${label}</td>` : ""}<td class="mono">${esc(d.c)}</td><td style="white-space:normal">${val(d.a)}</td><td style="white-space:normal">${val(d.b)}</td>${j === 0 ? `<td rowspan="${diffs.length}" style="vertical-align:top">${kindBadge(x.kind)}</td>` : ""}</tr>`).join("");
      }).join("")}</tbody></table></div>` : ""}</div>`;
  }).join("");
  return tiles + params + `<h3>Tables</h3>` + (tables || `<p class="muted">No table differences.</p>`);
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
  const hist = done.length ? `<div class="card"><div class="card-h"><h2>History</h2><span class="grow"></span><span class="muted">Backups in ${esc("sbc-backups")} next to the program</span></div><div class="scroll"><table class="tbl"><thead><tr><th>When</th><th>SBC</th><th>Change</th><th>Status</th><th>Approved via</th><th>Backup</th></tr></thead><tbody>${done.map(p => `<tr><td>${dtm(p.applied || p.created)}</td><td>${esc(p.sbcName)}</td><td style="white-space:normal;min-width:280px">${esc(p.reason)}<details class="m" style="margin-top:6px"><summary><span class="muted">${p.commands.length} command${p.commands.length === 1 ? "" : "s"}${p.output ? " and output" : ""}</span></summary><pre class="sip">${esc(p.commands.join("\n") + (p.output ? "\n\n" + p.output : ""))}</pre></details></td><td>${stat(p)}${p.errors ? ` <span class="muted">${p.errors} error${p.errors === 1 ? "" : "s"}</span>` : ""}</td><td class="muted">${esc(p.approvedVia || "—")}</td><td class="mono" style="font-size:11.5px">${esc(p.backup || "—")}</td></tr>`).join("")}</tbody></table></div></div>` : "";
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
      if (!confirm("Apply this change to the SBC now? A backup is taken first; the change is live but not saved to flash.")) return;
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
function namesBlock(x) {
  const rows = (status?.labels || []).filter(n => n.sbc === x.ip);
  const when = rows[0]?.readAt ? tm(rows[0].readAt) : "";
  const list = rows.length
    ? `<table class="tbl" style="margin-top:8px"><thead><tr><th>Name</th><th>Address</th><th>From</th></tr></thead><tbody>${rows.map(n => `<tr><td><b>${esc(n.name)}</b></td><td class="mono">${esc(n.ip)}</td><td class="muted">${esc(n.kind)}${n.detail ? " · " + esc(n.detail) : ""}</td></tr>`).join("")}</tbody></table>`
    : `<p class="muted" style="margin:8px 0 0">No names yet. Read the configuration over SSH and call flows use these names instead of bare addresses.</p>`;
  return `<div style="margin-top:12px"><div style="display:flex;align-items:center;gap:8px"><b>Names</b>${when ? `<span class="muted" style="font-size:12px">${when}</span>` : ""}<span class="grow"></span><button class="btn sm" data-names="${esc(x.ip)}" ${x.hasPassword ? "" : "disabled"} title="${x.hasPassword ? "SSH in and read show running-config" : "Save an SSH login first"}">${ic("server", 13)} Read from SBC</button></div>${list}</div>`;
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
  let iType = col("type", "linetype", "signalling", "signallingtype", "signaling", "signalingtype", "mode"), iHold = col("hold", "holdseconds", "holds"), iRing = col("ring", "ringseconds", "rings", "timeout"), iSig = col("signal", "ringsignal", "mrdsignal", "signalmethod"), iTest = col("test", "check", "testtype", "how");
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
    return {row: n + 2, name: iName >= 0 ? r[iName] || "" : "", uri, type, typeRaw, signal: sig, test, wire, hold: iHold >= 0 && r[iHold] ? +r[iHold] : null, ring: iRing >= 0 && r[iRing] ? +r[iRing] : null, problems};
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
        <div class="full"><label for="bulkVia">Send every call through this SBC <span class="muted">(optional)</span></label><input type="text" id="bulkVia" list="bulkSbcs" placeholder="ip:port, e.g. 192.168.1.244:5070 (blank = each URI's host)"><datalist id="bulkSbcs">${sbcs.map(x => `<option value="${esc(x.ip)}">${esc(x.name)}</option>`).join("")}</datalist></div>
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
        : emptyState("list", "No sheet loaded", "Choose a CSV file or paste rows. Columns: <b>Name</b>, <b>SIP URI</b> (sip:9912@192.168.1.244), <b>Type</b> (ARD, MRD, Hoot or DDI), and optionally <b>Test</b> (Wire or Call), <b>Hold</b> and <b>Ring</b> seconds.")}</div>
  </div><div id="bulkRuns" style="margin-top:16px"></div>`;
  if (setHTML(box, html)) {
    $("bulkTemplate").onclick = () => downloadText("line-test-template.csv", "Name,SIP URI,Type,Test,Signal,Hold,Ring\r\nB2B-1,sip:9901@192.168.1.244,ARD,,,5,30\r\nMRD_B,sip:9912@192.168.1.244,MRD,Wire,,5,\r\nHoot_A,sip:9921@192.168.1.244,Hoot,Wire,,5,\r\nDesk DDI,sip:02071234567@192.168.1.244,DDI,,,10,\r\n");
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

/* ------------------------------------------------------------ refresh loop */
let busy = false, failures = 0;
async function refresh() {
  if (busy) return; busy = true;
  try {
    status = await api("/api/status");
    failures = 0;
    renderTop(status); renderSide(status); renderAlerts(status);
    [$("sbcFilter"), $("sbcFilter2"), $("sbcFilter3")].forEach(el => fillSbcSelect(el, status));
    if (view === "dashboard") await renderDashboard();
    else if (view === "tests") await renderTests();
    else if (view === "bulk") await renderBulk(status);
    else if (view === "calls") {
      calls = await api("/api/calls?" + sbcQ($("sbcFilter")) + ($("showOptions").checked ? "&options=1" : ""));
      renderCalls();
      if (selected !== null) await loadDetail(); else setHTML($("detail"), emptyState("phone", "Select a call", "Pick a call on the left to see its SIP flow, checks and audio."));
    } else if (view === "streams") await loadStreams();
    else if (view === "syslog") { if ($("follow").checked) await loadSyslog(); }
    else if (view === "captures") await loadCaptures();
    else if (view === "sbcs") renderSbcs(status);
    else if (view === "compare") renderCompare();
    else if (view === "changes") await renderChanges(status);
    else if (view === "settings") await renderSettings(status);
  } catch (e) {
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
if (hp.get("report")) { reportInPlace = true; status = null; (async () => { try { status = await api("/api/status"); const d = await api("/api/calls/" + (+hp.get("report"))); callReport(d); } catch (e) { document.body.textContent = "Report not available: " + e.message; } })(); }
if (["dark", "light"].includes(hp.get("theme"))) applyTheme(hp.get("theme"));
if (!reportInPlace) {
  paintIcons();
  go(view);
  setInterval(refresh, 2500);
}
</script>
</body>
</html>
"""";
}
