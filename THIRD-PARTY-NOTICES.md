# Third-party notices

DeskSlate 自身のコードは MIT です。ビルドのときに NuGet から取るライブラリと、そのライセンスは次のとおりです(2026-11-24 に各パッケージの nuspec とライセンスの本文で確認)。

## PC 側

| パッケージ | 版 | ライセンス | 使っている所 |
|---|---|---|---|
| NAudio.Wasapi | 3.3.0 | MIT | Probe |
| NAudio.Core | 3.3.0 | MIT | NAudio.Wasapi の依存 |
| System.Diagnostics.PerformanceCounter | 10.0.15 | MIT | Probe |
| System.Configuration.ConfigurationManager | 10.0.15 | MIT | 上の依存 |
| System.Diagnostics.EventLog | 10.0.15 | MIT | 上の依存 |
| System.Security.Cryptography.ProtectedData | 10.0.15 | MIT | 上の依存 |
| LibreHardwareMonitorLib | 0.9.6 | MPL-2.0 | SensorHost |
| BlackSharp.Core | 1.0.7 | MPL-2.0 | LibreHardwareMonitorLib の依存 |
| DiskInfoToolkit | 1.1.2 | MPL-2.0 | 同上 |
| RAMSPDToolkit-NDD | 1.4.2 | MPL-2.0 | 同上 |
| HidSharp | 2.6.4 | Apache-2.0 | 同上 |
| Mono.Posix.NETStandard | 1.0.0 | MIT | 同上 |
| System.CodeDom | 10.0.2 | MIT | 同上 |
| System.IO.FileSystem.AccessControl | 5.0.0 | MIT | 同上 |
| System.IO.Ports(runtime.native.System.IO.Ports を含む) | 10.0.3 | MIT | 同上 |
| System.Management | 10.0.2 | MIT | 同上 |
| System.Numerics.Tensors | 9.0.0 | MIT | 同上 |

MPL-2.0 のライブラリは、改変せずにそのまま参照しています。ソースは各プロジェクトのリポジトリにあります。

- LibreHardwareMonitor: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor
- BlackSharp: https://github.com/Blacktempel/BlackSharp
- DiskInfoToolkit: https://github.com/Blacktempel/DiskInfoToolkit
- RAMSPDToolkit: https://github.com/Blacktempel/RAMSPDToolkit

## Android 側

まだありません。
