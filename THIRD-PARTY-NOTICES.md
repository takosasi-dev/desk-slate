# Third-party notices

DeskSlate 自身のコードは MIT です。ビルドのときに NuGet から取るライブラリと、そのライセンスは次のとおりです(2026-11-24 に各パッケージの nuspec とライセンスの本文で確認)。

## PC 側

| パッケージ | 版 | ライセンス | 使っている所 |
|---|---|---|---|
| NAudio.Wasapi | 3.3.0 | MIT | Probe |
| NAudio.Core | 3.3.0 | MIT | NAudio.Wasapi の依存 |
| System.Diagnostics.PerformanceCounter | 10.0.15 | MIT | Probe |
| System.Security.Cryptography.ProtectedData | 10.0.0 | MIT | `DeskSlate.Link`。PC の識別の鍵を DPAPI で守る |
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

## Windows の zip に同梱する .NET のランタイム

Releases の `DeskSlate-Probe-v0.0.1-win-x64.zip` は self-contained で出しているので、.NET 10 のランタイム(Microsoft.NETCore.App 10.0.11)のファイルをそのまま同梱しています。ライセンスは MIT です。ランタイムが取り込んでいる他の物の告知は、同じリポジトリの THIRD-PARTY-NOTICES.TXT にあります。

- .NET Runtime: https://github.com/dotnet/runtime(LICENSE.TXT / THIRD-PARTY-NOTICES.TXT)

Probe も SensorHost も WPF や Windows Forms を使っていないので、Windows Desktop のランタイム(dotnet/wpf・dotnet/winforms)は入っていません。zip に入る NuGet のライブラリは、上の「PC 側」の表のとおりです。

## PC 側(テストのときだけ使う物)

| パッケージ | 版 | ライセンス |
|---|---|---|
| xunit(xunit.core / xunit.assert / xunit.analyzers / xunit.extensibility.* を含む) | 2.9.3(analyzers は 1.18.0) | Apache-2.0 |
| xunit.abstractions | 2.0.3 | Apache-2.0 |
| xunit.runner.visualstudio | 3.1.4 | Apache-2.0 |
| Microsoft.NET.Test.Sdk(Microsoft.CodeCoverage / Microsoft.TestPlatform.* を含む) | 17.14.1 | MIT |
| Newtonsoft.Json | 13.0.3 | MIT |

## Android 側(probe)

Gradle で Google Maven と Maven Central から取ります。2026-11-24 に `:probe:dependencies --configuration releaseRuntimeClasspath` で解決後の版を出し、各版の POM の `<licenses>`(無い物は親 POM)で確認しました。

| パッケージ | 版 | ライセンス | 使っている所 |
|---|---|---|---|
| androidx.camera:camera-core | 1.5.3 | Apache-2.0(POM には BSD License も併記) | probe |
| androidx.camera:camera-camera2 / camera-lifecycle / camera-view | 1.5.3 | Apache-2.0 | probe |
| androidx.camera:camera-video | 1.5.3 | Apache-2.0 | camera-view の依存 |
| androidx.camera.featurecombinationquery:featurecombinationquery | 1.5.3 | Apache-2.0 | CameraX の依存 |
| androidx.activity:activity | 1.10.1 | Apache-2.0 | probe |
| com.google.zxing:core | 3.5.4 | Apache-2.0(親 POM zxing-parent) | probe |
| androidx.annotation:annotation / annotation-jvm | 1.8.1 | Apache-2.0 | 上の依存 |
| androidx.annotation:annotation-experimental | 1.4.1 | Apache-2.0 | 同上 |
| androidx.appcompat:appcompat / appcompat-resources | 1.1.0 | Apache-2.0 | 同上 |
| androidx.arch.core:core-common / core-runtime | 2.2.0 | Apache-2.0 | 同上 |
| androidx.collection:collection | 1.1.0 | Apache-2.0 | 同上 |
| androidx.concurrent:concurrent-futures / concurrent-futures-ktx | 1.1.0 | Apache-2.0 | 同上 |
| androidx.core:core / core-ktx | 1.13.0 | Apache-2.0 | 同上 |
| androidx.core:core-viewtree | 1.0.0 | Apache-2.0 | 同上 |
| androidx.cursoradapter:cursoradapter / customview / drawerlayout / interpolator / loader / viewpager | 1.0.0 | Apache-2.0 | 同上 |
| androidx.exifinterface:exifinterface | 1.4.2 | Apache-2.0 | 同上 |
| androidx.fragment:fragment | 1.1.0 | Apache-2.0 | 同上 |
| androidx.lifecycle:lifecycle-common / -livedata / -livedata-core / -runtime / -viewmodel / -viewmodel-savedstate | 2.6.2 | Apache-2.0 | 同上 |
| androidx.profileinstaller:profileinstaller | 1.4.0 | Apache-2.0 | 同上 |
| androidx.savedstate:savedstate | 1.2.1 | Apache-2.0 | 同上 |
| androidx.startup:startup-runtime | 1.1.1 | Apache-2.0 | 同上 |
| androidx.tracing:tracing / tracing-ktx | 1.2.0 | Apache-2.0 | 同上 |
| androidx.vectordrawable:vectordrawable / vectordrawable-animated | 1.1.0 | Apache-2.0 | 同上 |
| androidx.versionedparcelable:versionedparcelable | 1.1.1 | Apache-2.0 | 同上 |
| com.google.auto.value:auto-value-annotations | 1.6.3 | Apache-2.0(親 POM auto-parent) | 同上 |
| com.google.guava:listenablefuture | 1.0 | Apache-2.0(親 POM guava-parent) | 同上 |
| org.jetbrains.kotlin:kotlin-stdlib | 2.0.21 | Apache-2.0 | Kotlin |
| org.jetbrains.kotlinx:kotlinx-coroutines-core / -core-jvm / -android / -bom | 1.8.1 | Apache-2.0 | 上の依存 |
| org.jetbrains:annotations | 23.0.0 | Apache-2.0 | 同上 |
| org.jspecify:jspecify | 1.0.0 | Apache-2.0 | 同上 |

## Android 側(テストのときだけ使う物)

アプリ(APK)には入りません。junit 4.13.2(EPL-1.0)と hamcrest-core 1.3 は、作者の判断でテスト専用の例外として使っています。

| パッケージ | 版 | ライセンス |
|---|---|---|
| junit:junit | 4.13.2 | EPL-1.0 |
| org.hamcrest:hamcrest-core | 1.3 | BSD-3-Clause(New BSD License、親 POM hamcrest-parent) |
