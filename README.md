# MediantTrace

A small always-on server for troubleshooting SIP calls through AudioCodes Mediant SBCs, without Wireshark. Any number of SBCs can send to it.

Each SBC sends its syslog (UDP 514) and Debug Recording (UDP 925) to this server. MediantTrace receives them and, while a capture is running:

- saves every packet to a **`.pcap`** (for AudioCodes support) and every SIP message and syslog line to a **`.txt`** (Notepad);
- shows each call on a web page:
  - SIP ladder diagram (click a message to read it in full);
  - **checks for one-way audio**: private media address offered to a public peer (NAT), `sendonly`/`recvonly`/`inactive`, hold, rejected audio, no common codec, audio sent but none received (or the reverse), audio arriving from a different address than the SDP says, packet loss, jitter, long gaps;
  - audio streams in each direction with packets, loss, jitter and the longest gap (needs Debug Recording with media);
  - both legs of an SBC call linked by the Mediant's session id (SID);
  - which SBC each call came through, with a filter per SBC, and per-SBC status (syslog / Debug Recording arriving or not).

Version 2.0 adds what AudioCodes Syslog Viewer does:

- **Syslog page as a log viewer**: a tab per device; display filter (word or regular expression, match case, whole words, filter out); severity, CDR, call / non-call and session filters; freeze and auto-scroll; find (Ctrl+F, F3); go to line (Ctrl+G); marked lines (Ctrl+F2, F2); highlight keywords; right-click to filter or highlight a session or selected text, copy SID / From / To, or open the session's SIP ladder; date and UTC display; save as text; clean.
- **Syslog over TCP and TLS** (RFC 6587 / 5425) as well as UDP, with a self-signed certificate made for you (or your own .pfx).
- **Missing and out-of-order syslog**: the SBC's `[S=n]` numbers put UDP back in order, and each gap shows as a warning line, with counts per SBC.
- **Receive filters**: drop syslog from chosen senders, or lines matching a regular expression, on arrival.
- **Write log**: every syslog line to plain text files, rotated by size and count, optionally per SBC (by name).
- **Open other logs** (drop a file on the page, choose one, or paste): Syslog Viewer logs, acSyslog / raw syslog, CSV, TSV, Excel, JSON (Azure Log Analytics, Elastic, NDJSON), SL4NT, the device's legacy web format, Wireshark `.pcap` / `.pcapng` (syslog, Debug Recording or plain SIP). **Back to live** returns to the SBCs. **Follow a file** as another program writes it.
- **Ladders**: HTTP, DNS and VoiceAI messages for a session appear in its ladder (dashed); IP phone SIP (`recv <--` / `send -->`) is read; add other calls to one flow; export the ladder as .txt or .html.
- Command line: `MediantTrace.exe <file>` or `open <file>` opens a log, `monitor <file>` follows one, `--no-browser` skips opening the page.

Not included: pulling logs from the device's web interface and capturing through Wireshark. MediantTrace only listens on its own ports; open a Wireshark capture, or follow a log file instead.

It only listens on its own ports. It does not sniff the network and needs no packet driver (Npcap) and no install of .NET.

## Run

| | |
|---|---|
| `MediantTrace.exe` | run in a window; opens the page at http://localhost:8925 |
| `MediantTrace.exe install` | install as a Windows service, always on, and open the firewall for UDP 514/925 (run as administrator) |
| `MediantTrace.exe uninstall` | remove the service and firewall rules (run as administrator) |

Then on the page: **Open SBC** (a link to each SBC's web interface), turn logging on there, **Start capture**, make the call, **Stop capture**. One capture records every SBC; filter by SBC on the page. A capture stops itself after 60 minutes if forgotten.

## Point each SBC at this server

Web interface, **Troubleshoot → Logging** (names from the 7.20A manual; check on the device):

- **Syslog Settings**: Enable Syslog, Syslog Server IP = this server, port 514, Debug Level = Detailed. This alone gives the SIP messages and ladder.
- **Debug Recording**: destination IP = this server, port 925. Then a row in **Logging Filters**: filter type Any (or one IP group), log destination Debug Recording, log type **Signaling & Media**, mode Enable. This adds the audio streams.

Turn them off afterwards: Debug Recording and detailed syslog cost the Mediant CPU.

## Settings

`settings.json` next to the program (created on first run; the page edits the first three):

| Setting | Default | |
|---|---|---|
| `Sbcs` | one: `Mediant`, 192.0.2.10 | list of `{ "Name", "Ip", "Url" }`: the address each SBC sends from, and its web page (the **Open SBC** links) |
| `OnlyListedSbcs` | false | true: ignore packets from addresses not in `Sbcs` |
| `AutoStopMinutes` | 60 | stop a forgotten capture (0 = never) |
| `SyslogPort` / `DebugRecordingPort` | 514 / 925 | |
| `WebPort` | 8925 | |
| `WebOnNetwork` | false | false: page on this server only. true: other PCs can open it (needs the service, or administrator) |
| `WebPassword` | empty | asked for by browsers on other PCs when `WebOnNetwork` is true. You may set it in this file; the next start encrypts it and removes the clear value |
| `TraceFolder` | `traces` | next to the program |

Restart the program or service after editing the file by hand.

## Build

```
dotnet publish -c Release -o publish
```

Produces one `publish\MediantTrace.exe` (about 35 MB) that runs on Windows x64 with nothing installed.

The Debug Recording header layout follows Wireshark's open-source `packet-acdr` dissector (format only; no code copied).
