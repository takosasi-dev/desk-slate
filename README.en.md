# DeskSlate

Turns an Android tablet on your desk into a side panel for a Windows PC. [日本語](README.md)

> **Work in progress. Nothing here is usable yet.**
> The repository currently contains only the measurement tools (probes, PC side and Android side) used to settle the design. The PC app and the Android app are not written yet.

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
| Phase 0 | Probes | PC side builds. Android side written; device info, vibration and camera checked on a test tablet, real barcode reads not done yet. No PC measurements yet |
| Phase 1 | Foundation | Part of `DeskSlate.Core` only (input gate, settings load/save, message types). Connection and UI not started |
| Phase 2–8 | The five features and release work | Not started |

## Download

The v0.0.1 pre-release on [Releases](https://github.com/takosasi-dev/desk-slate/releases) has two files:

| File | Content |
|---|---|
| `DeskSlate-Probe-v0.0.1-win-x64.zip` | PC-side Probe and SensorHost. The .NET 10 runtime is included, so you do not need to install .NET. Extract it and run the tools from PowerShell |
| `DeskSlate-Probe-v0.0.1-android.apk` | Android-side probe (Android 7.0 or later) |

- The APK is a **temporary build signed with a debug key**. Builds signed with the real key later will not install over it; uninstall this one first.
- `DeskSlate.SensorHost.exe` runs as administrator (a UAC prompt appears when it starts).

## What is here now: PC-side probes

Requirements: Windows 11 (x64), .NET 10 SDK.

```powershell
dotnet build pc\DeskSlate.sln -c Release
dotnet test pc\DeskSlate.sln -c Release
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

## What is here now: Android-side probe

An app that only inspects the device and prints the results on screen and to the log.

| Button (adb `action`) | What it does |
|---|---|
| 情報 (`info`) | Prints device info and the camera list. Also runs at startup |
| 振動 (`vibrate`) | Vibrates once |
| EAN-13 を 10 回 (`ean`) | Reads an EAN-13 barcode 10 times with the camera and prints each time, the median, and whether R-3 (8 or more reads within 3 seconds) is met |
| QR を 3 回 (`qr`) | Reads a QR code 3 times, same output |

Build (needs JDK 17 and the Android SDK; put `sdk.dir=<SDK path>` in `android\local.properties`):

```powershell
cd android
.\gradlew.bat assembleDebug
```

Install and use:

- Install the APK, open "DeskSlate Probe" and tap a button. For reads, each tap on 「読む」 (Read) is one attempt.
- It can also be driven from adb. Every line is also written to logcat with the tag `DeskSlateProbe`.

```powershell
adb install -r DeskSlate-Probe-v0.0.1-android.apk
adb shell am start -n io.github.takosasidev.deskslate.probe/.MainActivity --es action info
adb logcat -s DeskSlateProbe
```

- The only permissions are camera (`CAMERA`) and vibration (`VIBRATE`). It has no internet permission and neither saves nor sends anything.

## License

MIT ([LICENSE](LICENSE)). Third-party licenses are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
