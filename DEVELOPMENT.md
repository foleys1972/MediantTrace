# MediantTrace: development list

Status as of 13 October 2026. "Verified" means checked against real Mediant 800B (firmware 7.20A.104.001) traffic; "tested" means checked with synthetic packets or a simulated far end only.

## Done

### Capture
- [x] Receive syslog (UDP, default 514; 5514 at home) and Debug Recording (UDP 925); per-SBC status of each feed. **Verified**
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
- [x] Test calls to a live dealerboard wire through the SBC (9912 via the Mediant E1 loop). Ran once: 408, because Docker had lost UDP 5060 for subscriber-02 (fixed by restarting the container)

### Bulk line tests
- [x] Upload or paste a sheet of lines (Name, SIP URI, Type ARD/MRD/Hoot/DDI, optional Hold/Ring), optionally sent through an SBC; called one after another; ARD/Hoot must answer by themselves within N s, MRD/DDI need a person within N s; hold with the test tone, hang up; per-line result, CSV and report; MCP tools `bulk_line_test`, `bulk_status`. Tested with a simulated far end (answer, silent, busy, no answer)
- [x] ARD means a person picks up: ARD rings for up to 30 s by default and passes on answer (14 October)
- [x] Always-on lines (MRD, Hoot) are not called by default, because a call replaces the live wire call (the far end redials). Instead the SBC's `show voip calls active` (read-only, over the stored SSH login) shows whether the wire is up, and the capture measures audio on the wire's Mediant sessions for the hold time, each way. RTP streams now carry the Debug Recording session ID, so a wire set up before the capture started is still measured. A Test column (Wire / Call) forces a call; 486 on an always-on line counts as "wire connected". Verified on the four wires on 14 October: all up, audio both ways, MOS 4.36-4.38

### MCP server and SBC configuration
- [x] MCP endpoint at `/mcp` (Streamable HTTP, JSON responses, bearer token, Origin check), 18 tools: SBCs, capture, list/triage calls, SIP messages, SBC log search, call tests, `sbc_show`, `sbc_backup`, `sbc_plan_change`, `sbc_plan_save`, `sbc_apply_change`, `list_changes`. **Verified** read-only against the real Mediant
- [x] SBC logins per SBC, DPAPI-encrypted; SSH CLI client (prompt, paging, enable) ported from the mediant-sbc skill; trust-on-first-use host keys. **Verified**
- [x] Changes need a person: plans with a one-time code shown only on the Changes page; apply = backup, run live, log; save is its own plan; resets refused. **Verified** (guards); a real apply not yet run

### Interface
- [x] Dashboard, calls (ladder of both legs, overview, media, SDP, SBC report, SBC log, messages), call tests, audio streams, syslog (warnings & errors filter), captures, SBCs, config compare, settings. Tested
- [x] One-click reports (call, test, config compare): self-contained HTML, print to PDF. Tested
- [x] Light/dark, keyboard shortcuts, shareable links (`#call=`, `#result=`, `#report=`), offline (no internet fonts or scripts).

## Next (in priority order)

0. **First approved change through the MCP** on the home Mediant (for example renaming the echo route), to prove apply, backup and audit end to end.
1. **Run it at work.** First real multi-SBC capture; confirm the SBC (not gateway) CDR layout and SIP-to-SIP Debug Recording media trace points. Adjust from what is seen.
2. **Install as a service** on the work server and open the page to the team (`WebOnNetwork` + `WebPassword`); test the firewall rules `install` creates.
3. **Address book**: names for IP addresses (PBXs, carriers, SIP interfaces) shown in ladders, streams and checks. Idea from niekvlessert/audiocodes_syslog_tool.
4. **Media from SBC MEDIA_END records**: SBC-measured loss, jitter, MOS and latched address per leg, so audio quality shows without Debug Recording.
5. **Test calls at scale**: several SBCs on a schedule, alerts when a test fails (e-mail or Teams webhook), pass-rate trend on the dashboard.
6. **Retention**: delete captures older than N days and cap disk use; show disk space on Captures.
7. **Open a saved capture**: load an old `.pcap` (or an AudioCodes Syslog Viewer log) back into the call view.
8. **Search across captures** by number, Call-ID or SID.

## Later / ideas

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

## Found while building (outside the tool)

- **Docker loses UDP 5060** (9 and 13 October): **reproduced 14 October.** Docker Desktop's Windows-side port forwarder (com.docker.backend.exe) loses a published UDP port that is receiving traffic while its container restarts, in about half of restarts (5 of 10 in a 400-port probe), while `docker port` still lists it. Each restart churns 404 published ports per server (4-14 minutes); the 200 TCP media ports are unused (mediasoup enableTcp false). Not production-safe: see the recommendations in the investigation notes.
- **Wire audio:** on the home system's four always-on wire legs the Mediant sent about 366,000 audio packets each over two hours but received only 59-191 back from the dealerboard servers. The server seems to send RTP only while someone talks; the Mediant ended those calls with "RTP broken connection" when the subscriber restarted. Worth checking whether a quiet wire should keep sending (silence or comfort noise).
- **Home Mediant:** live (unsaved) route "MediantTrace echo test" at tel2ip index 0 (9998 to 192.168.1.58:5090). A Mediant reboot removes it; save only if wanted.
