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
| `Sbcs` | one: `Mediant`, 192.168.1.244 | list of `{ "Name", "Ip", "Url" }`: the address each SBC sends from, and its web page (the **Open SBC** links) |
| `OnlyListedSbcs` | false | true: ignore packets from addresses not in `Sbcs` |
| `AutoStopMinutes` | 60 | stop a forgotten capture (0 = never) |
| `SyslogPort` / `DebugRecordingPort` | 514 / 925 | |
| `WebPort` | 8925 | |
| `WebOnNetwork` | false | false: page on this server only. true: other PCs can open it (needs the service, or administrator) |
| `WebPassword` | empty | asked for by browsers on other PCs when `WebOnNetwork` is true |
| `TraceFolder` | `traces` | next to the program |

Restart the program or service after editing the file by hand.

## Build

```
dotnet publish -c Release -o publish
```

Produces one `publish\MediantTrace.exe` (about 35 MB) that runs on Windows x64 with nothing installed.

The Debug Recording header layout follows Wireshark's open-source `packet-acdr` dissector (format only; no code copied).
