# MediantTrace: development list

Status as of 13 October 2026. "Verified" means checked against real Mediant 800B (firmware 7.20A.104.001) traffic; "tested" means checked with synthetic packets or a simulated far end only.

## Done

### Capture
- [x] Receive syslog (UDP, default 514) and Debug Recording (UDP 925); per-SBC status of each feed. **Verified**
- [x] Save every capture as `.pcap` (for AudioCodes support) and `.txt` (Notepad). **Verified**
- [x] Start/stop from the page, auto-stop after 60 minutes. **Verified**
- [x] Multiple SBCs: list with name, IP and web link; calls, streams and syslog kept apart per SBC; option to ignore unlisted senders. Tested
- [x] Windows service (`install` / `uninstall`), firewall rules limited to the listed SBCs. `install` not yet run

### Decoding
- [x] Debug Recording (ACDR) header, versions 0.1-0.9, header-added packets, session ID (SID). **Verified**
- [x] SIP direction from the Debug Recording header (0 = into the SBC, 1 = out), checked against real IP headers. **Verified**
- [x] SIP from syslog: banners, messages split over several packets, interleaved sessions, messages without a banner. **Verified** (627 SIP messages from a real 15-minute capture)
- [x] SBC call records (CDRs) from syslog, matched by the device's own title line; gateway (GW) layout built in. **Verified** (44 calls)
- [x] RTP per stream: packets, loss, jitter (RFC 3550), longest gap, per-second timeline, estimated MOS (E-model). Tested

### Analysis
- [x] One-way-audio checks: SDP (private address to public peer, sendonly/recvonly/inactive, hold, port 0, no common codec), RTP (sent but none back, NAT latching, loss, jitter, gaps, MOS), SBC records (InPackets vs OutPackets, RTP broken connection, abnormal end). **Verified** on the SBC records
- [x] Both legs of a call linked by SID; cancelled calls not counted as failures; OPTIONS keep-alives hidden. **Verified**

### Call tests
- [x] SIP test caller: INVITE/ACK/BYE/CANCEL, digest login, re-INVITE handling, 1004 Hz tone, scores the audio coming back, scheduling, history. **Verified** (round trip through the Mediant and its E1 loop: MOS 4.37, tone returned)
- [x] Echo answerer for calls that loop back through the SBC (known SBCs only). **Verified**
- [x] Credentials stored with Windows DPAPI. Tested

### Test line and calls into the dealerboard
- [x] Test line: calls that reach MediantTrace (other than its own loop tests) hear a spoken announcement and a beep, the caller speaks, hears it played back; incoming audio measured and recorded (listen on the page). Tested with a simulated caller
- [x] Test calls to a live dealerboard wire through the SBC (9912 via the Mediant E1 loop). Tested once against a live wire

### Bulk line tests
- [x] Upload or paste a sheet of lines (Name, SIP URI, Type ARD/MRD/Hoot/DDI, optional Hold/Ring), optionally sent through an SBC; called one after another; ARD/Hoot must answer by themselves within N s, MRD/DDI need a person within N s; hold with the test tone, hang up; per-line result, CSV and report; MCP tools `bulk_line_test`, `bulk_status`. Tested with a simulated far end (answer, silent, busy, no answer)
- [x] ARD means a person picks up: ARD rings for up to 30 s by default and passes on answer (14 October)
- [x] Always-on lines (MRD, Hoot) are not called by default, because a call replaces the live wire call (the far end redials). Instead the SBC's `show voip calls active` (read-only, over the stored SSH login) shows whether the wire is up, and the capture measures audio on the wire's Mediant sessions for the hold time, each way. RTP streams now carry the Debug Recording session ID, so a wire set up before the capture started is still measured. A Test column (Wire / Call) forces a call; 486 on an always-on line counts as "wire connected". Verified on four lab wires on 14 October: all up, audio both ways, MOS 4.36-4.38

### Ports
- [x] Changing the syslog or Debug Recording port takes effect when saved, no restart: the new port is opened first and the old one closed only if that worked, so a port in use (another MediantTrace copy, a syslog server) or refused leaves the feed where it was, with the reason shown. Only the web page port still needs a restart. The firewall check now also spots a rule that allows the old ports only, and gives the command to fix it (19 October)

### Data and storage
- [x] Settings → Data and storage: one table of everything written to disk, each with its folder (full path, another drive, a share, or relative to the program), how long it is kept, space used and free on its drive. Packet captures (days + MB), SBC logs (days + total MB cap + file size), SBC config backups (folder now configurable; days, always the newest 10 per SBC and any an upgrade or change uses), MediantTrace's own log (folder; rotated at N MB, 3 older kept; it used to grow for ever), history (test results, line test runs, ready checks, wire changes; one "keep N days", default 90). Folders are checked for write access when saved and used at once (the SBC log switches without a restart); the clean-up runs hourly and on demand (20 October)
- [x] Folder picker: a Browse button beside each folder lists the server's drives (local, network, USB; with free and total space, empty card readers left out, absurd cloud-drive sizes shown as "no limit reported"), then its folders one level at a time (names only, hidden and system folders left out), with New folder and Use this folder. Tested on this server: drives C, E, F, H, I, J, Y (network) and Z (network) listed; a folder made in the picker and chosen moved the SBC logs at once

### Timeline ("which change broke it")
- [x] Timeline view (Look up → Timeline): calls per bucket (completed / failed) over 6 h - 30 days with every event as a dot on the same axis, and the events listed by day: SBC alarms raised / cleared from the SBC's alarm history (SBC clock offset found from its "System Time" and removed, so times line up with this server), SBC restarts (from uptime), config changes and saves made through MediantTrace, upgrade snapshots, wires down / back (line monitor), call tests failing and passing again, line test runs, ready checks. Filters by kind and problems only; click an event to open its page; MCP tool `timeline` (19 October)

### Upgrade assistant
- [x] Upgrades page (Set up → Upgrades): baseline before a firmware upgrade (version, serial, SBC licence and feature list, uptime, calls up, active alarms, proxy sets, E1 trunks, wires up from the line board, full running-config backup in sbc-backups), snapshot after, and a comparison showing only what changed: version (warns if unchanged), licence and features, new / cleared alarms, proxy sets and trunks that changed, wires that did not come back, and config lines added / removed (Myers line diff with the configure section and table row, secrets masked). Verdict, printable report, MCP tools `upgrade_snapshot` and `upgrade_compare`, and `MediantTrace.exe configdiff a b` for two saved configs. The upgrade itself stays manual. The diff was checked on two real lab backups (one change: the Debug Recording filter, 50 ms) (19 October)

### Line monitor (scheduled checks, history, alerts)
- [x] Background line monitor (Line board → Monitor): every 1-30 min reads the SBCs' call lists and records when a wire changes state (up / one-way / down); a change counts after it is seen on two checks in a row; per-line uptime by day (24 h and 7 days in the table, tiles, detail and CSV); change history kept 30 days; alert to the webhook / email when a wire goes down and when it is back (one-way audio alerts optional); MCP tool `line_history`. Off until turned on (19 October)
- Call tests already repeat on a schedule and alert on failure; the ready check runs daily

### SBC health
- [x] SBC health page (Watch → SBC health): per SBC, read-only in one SSH session at most once a minute while the page is open (Read now button): version, serial, SBC licence, uptime, CPU and memory with meters, NTP, active alarms and alarm history, proxy sets with keepalive OK / failed counts and status, E1 trunks (state, D-channel, line alarms), gateway and SBC call statistics, findings worst first; MCP tool `sbc_health`. The ready check now also fails on a kept-alive proxy set that is not online (19 October)

### Ready check ("ready for business")
- [x] One go / no-go check (Watch → Ready check): verdict Ready / Ready with warnings / Not ready with every reason, by hand or daily at a set time (weekdays only optional), history of every run, printable report, result sent to the webhook / email when not fully ready, MCP tools `ready_check_run` and `ready_check_status` (19 October)
- [x] Per SBC: syslog and Debug Recording arriving; over SSH (read-only, one session): `show active-alarms` (critical/major = problem, minor = warning, accepted alarms by text), `show system uptime` (restart in the last 24 h = warning: unsaved changes lost), `show system utilization` (CPU), `show ntp`, `show voip interface e1-t1` (state, D-channel, line alarms); config drift against the saved standard. Commands checked against a lab Mediant (7.20A); parsers have self-tests on its real output
- [x] Trading servers: SIP OPTIONS to each address set (addresses set on the page)
- [x] Lines: always-on wires up (fresh SBC call list), ARD / DDI lines passed a line test in the last 24 h; chosen saved call tests run (e.g. echo); SBC error lines in the last hour; free disk space

### Line board
- [x] **Line board** view (Watch → Line board): every line as a tile, grouped by desk/group, type or state; filter by type, state or search. Lines are kept in `lines.json`, added from the Line tests sheet format (plus an optional Group column) or from the last line test run (19 October)
- [x] MRD / Hoot tiles: wire up or down from the SBC's `show voip calls active`, read in the background at most every 30 s and only while someone looks at the board; connected time; audio packets per second each way from Debug Recording on the wire's sessions; MOS. Amber when audio is missing one way
- [x] ARD / DDI tiles: "in call" when the SBC logs show one, otherwise the last line test result and time
- [x] Table view, the default (tiles on request; boards up to 1,200+ lines): sortable columns (problems first by default), sticky header, scroll position kept across refreshes, CSV export of the lines shown. The server builds the board in one pass per SBC over streams, calls and the SBC call list instead of one pass per line
- [x] Click a tile for details; Check wire (MRD / Hoot) or Test call (ARD / DDI) starts a line test of that line; Test all lines; MCP tool `line_board`
- [x] The call view is fed all the time now, so the stream table drops the longest-quiet streams when full instead of ignoring new ones; the wire check measures audio from the live feed (no capture needed)

### MCP server and SBC configuration
- [x] MCP endpoint at `/mcp` (Streamable HTTP, JSON responses, bearer token, Origin check), 18 tools: SBCs, capture, list/triage calls, SIP messages, SBC log search, call tests, `sbc_show`, `sbc_backup`, `sbc_plan_change`, `sbc_plan_save`, `sbc_apply_change`, `list_changes`. **Verified** read-only against the real Mediant
- [x] SBC logins per SBC, DPAPI-encrypted; SSH CLI client (prompt, paging, enable) ported from the mediant-sbc skill; trust-on-first-use host keys. **Verified**
- [x] Changes need a person: plans with a one-time code shown only on the Changes page; apply = backup, run live, log; save is its own plan; resets refused. **Verified** (guards); a real apply not yet run

### Interface
- [x] Dashboard, calls (ladder of both legs, overview, media, SDP, SBC report, SBC log, messages), call tests, audio streams, syslog (warnings & errors filter), captures, SBCs, config compare, settings. Tested
- [x] One-click reports (call, test, config compare): self-contained HTML, print to PDF. Tested
- [x] Light/dark, keyboard shortcuts, shareable links (`#call=`, `#result=`, `#report=`), offline (no internet fonts or scripts).

## Next (in priority order)

0. **First approved change through the MCP** on a lab Mediant (for example renaming the echo route), to prove apply, backup and audit end to end.
1. **Run it on a production estate.** First real multi-SBC capture; confirm the SBC (not gateway) CDR layout and SIP-to-SIP Debug Recording media trace points. Adjust from what is seen.
2. **Install as a service** on the work server and open the page to the team (`WebOnNetwork` + `WebPassword`); test the firewall rules `install` creates.
3. **Address book**: names for IP addresses (PBXs, carriers, SIP interfaces) shown in ladders, streams and checks. Idea from niekvlessert/audiocodes_syslog_tool.
4. **Media from SBC MEDIA_END records**: SBC-measured loss, jitter, MOS and latched address per leg, so audio quality shows without Debug Recording.
5. **Test calls at scale**: several SBCs on a schedule, alerts when a test fails (e-mail or Teams webhook), pass-rate trend on the dashboard.
6. **Retention**: delete captures older than N days and cap disk use; show disk space on Captures.
7. **Open a saved capture**: load an old `.pcap` (or an AudioCodes Syslog Viewer log) back into the call view.
8. **Search across captures** by number, Call-ID or SID.

## Later / ideas

- **Firmware upgrade from the app** (parked 19 October): upload the .cmp (model and version checked, fingerprint kept), pre-flight (recent baseline, config saved, no calls up or explicit override, ready check), one-time approval code on the page only (MCP may plan, never start), MediantTrace serves the file and runs the firmware-load command, watches the SBC restart, then snapshots and compares automatically. Needs: the exact `copy firmware` syntax confirmed on the device, the right M800B image, and the AudioCodes upgrade path / licence for 7.20A to 7.40A.

- Per-call `.pcap` download (only that call's packets).
- Audio playback of a captured stream (G.711 decode in the browser).
- DTMF (RFC 2833) events in the ladder.
- SRTP / TLS: show that media and signalling are encrypted, and what can't be decoded.
- Config compare: fetch the `.ini` straight from each SBC (needs a stored login; today the tool never logs in to an SBC).
- Compare a config against a saved "golden" baseline and flag drift on the dashboard.
- Export calls to CSV.

## Known limitations

- Jitter is computed only for static payload types (G.711, G.722, G.729); Opus and other dynamic types are counted but not scored.
- MOS is an E-model estimate from loss, jitter and delay, not a listening test.
- One test call at a time; scheduled tests queue behind manual ones.
- The page is HTTP; when opened to the network, use a password and a trusted network.
