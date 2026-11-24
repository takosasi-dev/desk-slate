# DeskSlate

Turns an Android tablet on your desk into a side panel for a Windows PC. [日本語](README.md)

> **Work in progress. Nothing here is usable yet.**
> The repository currently contains only the measurement tools (probes) used to settle the design. The PC app and the Android app are not written yet.

## What it will be

One Android app (Kotlin) and one Windows tray app (C# / .NET 10 / WPF) with five tabs:

| Tab | What it does |
|---|---|
| Panel | Buttons that open apps, send key combinations, control media, or run scripts you registered and approved on the PC |
| Mixer | Per-app volume and mute on faders |
| Dashboard | PC status, service up/down checks, recent earthquake information |
| Scan | Read a barcode and type it into the PC, or take a photo and send it to the PC |
| Pad | Touchpad and numeric keypad |

Design rules:

- The tablet only sends things like "button N was pressed". What to open and which keys to send are looked up from the PC-side settings only.
- The main app never runs as administrator. Temperatures and fan speeds are read by a separate read-only process (SensorHost).
- The PC can stop all input from the tablet at any time (panic switch).

## Status

| Phase | Content | State |
|---|---|---|
| Phase 0 | Probes | PC side builds. Android side not written. No measurements yet |
| Phase 1–8 | The app | Not started |

## What is here now: PC-side probes

Requirements: Windows 11 (x64), .NET 10 SDK.

```powershell
dotnet build pc\DeskSlate.sln -c Release
```

| Executable | What it does | Privilege |
|---|---|---|
| `DeskSlate.Probe.exe audio` | Lists the default playback device and per-app volumes | Normal |
| `DeskSlate.Probe.exe audio --set <exe> <0-100>` | Changes that app's volume for 2 seconds, then restores it (asks first) | Normal |
| `DeskSlate.Probe.exe metrics` | Prints CPU, memory, disk, network, battery and GPU values for 5 seconds | Normal |
| `DeskSlate.Probe.exe foreground` | Prints foreground window changes for 30 seconds | Normal |
| `DeskSlate.Probe.exe keys` | After 5 seconds, types `0123456789ABC-123` into the foreground window (asks first) | Normal |
| `DeskSlate.Probe.exe pipe` | Checks that a named pipe readable only by you can be created | Normal |
| `DeskSlate.SensorHost.exe --probe` | Lists the hardware and sensors LibreHardwareMonitorLib reports | Administrator |

Notes:

- `keys` really types into the foreground window. Bring Notepad to the front first.
- Run `SensorHost` from an elevated PowerShell. It only reads. It does not bundle or install any driver (PawnIO).
- Probe output contains names of apps running on your PC. Do not paste it in public places as is.

## License

MIT ([LICENSE](LICENSE)). Third-party licenses are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
