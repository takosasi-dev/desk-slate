# DeskSlate

机の上の Android 端末を、Windows PC の「手元パネル」にするソフトです。[English](README.en.md)

> **開発中です。まだ使える物はありません。**
> いまリポジトリにあるのは、設計を決めるための測定用ツール(Probe。PC 側と Android 側)だけです。アプリ本体は、PC 側も Android 側もこれから作ります。

## 作ろうとしている物

端末側 1 本(Kotlin)と PC 側 1 本(C# / .NET 10 / WPF の常駐アプリ)で、次の 5 つをタブで切り替えます。

| タブ | できること |
|---|---|
| 操作パネル | ボタンでアプリを開く、キーを送る、メディアを操作する、登録したスクリプトを動かす |
| 音量ミキサー卓 | アプリごとの音量とミュートをフェーダーで動かす |
| ダッシュボード | PC の状態、サーバーの生死、直近の地震情報を出し続ける |
| 読み取り | バーコードを読んで PC に打ち込む、写真を撮って PC に送る |
| パッド | タッチパッドとテンキー |

設計で守ること:

- 端末が送るのは「押したボタンの番号」などだけです。何を開くか、どのキーを送るかは PC 側の設定からしか引きません。
- 本体は管理者で動かしません。温度とファンだけを、読むことしかできない別プロセス(SensorHost)に任せます。
- 端末からの操作は、PC 側の非常停止でいつでも止められます。

## 開発の状況

| 段階 | 中身 | 状態 |
|---|---|---|
| Phase 0 | 測定用ツール(Probe) | PC 側はビルドできる。Android 側は作った。dtab で一覧・振動・カメラまで確認、バーコードの実読はこれから。PC 側の実測もこれから |
| Phase 1 | 土台 | `DeskSlate.Core` の一部だけ先に作った(操作の門番、設定の読み書き、メッセージの型)。接続と画面は未着手 |
| Phase 2〜8 | 5 つの機能と仕上げ | 未着手 |

## ダウンロード

[Releases](https://github.com/takosasi-dev/desk-slate/releases) の v0.0.1(プレリリース)に、次の 2 つを置いています。

| ファイル | 中身 |
|---|---|
| `DeskSlate-Probe-v0.0.1-win-x64.zip` | PC 側の Probe と SensorHost。.NET 10 のランタイムごと入っているので、.NET を入れなくても動きます。展開して PowerShell から動かします |
| `DeskSlate-Probe-v0.0.1-android.apk` | Android 側の probe(Android 7.0 以降) |

- APK は**デバッグ用の鍵で署名した仮の物**です。正式な鍵に変えた後の版は上書きで入らないので、この版を消してから入れ直してください。
- `DeskSlate.SensorHost.exe` は管理者で動きます(起動すると UAC の確認が出ます)。

## いまある物: PC 側の Probe

動作環境: Windows 11(x64)、.NET 10 SDK。

```powershell
dotnet build pc\DeskSlate.sln -c Release
dotnet test pc\DeskSlate.sln -c Release
```

| 実行ファイル | やること | 権限 |
|---|---|---|
| `DeskSlate.Probe.exe audio` | 既定の再生デバイスと、アプリごとの音量の一覧を出す | 通常 |
| `DeskSlate.Probe.exe audio --set <exe名> <0-100>` | その exe の音量を 2 秒だけ変えて戻す(確認あり) | 通常 |
| `DeskSlate.Probe.exe metrics` | CPU・メモリ・ディスク・ネットワーク・バッテリー・GPU を 5 秒分出す | 通常 |
| `DeskSlate.Probe.exe foreground` | 30 秒の間、前面のウィンドウの切り替わりを出す | 通常 |
| `DeskSlate.Probe.exe keys` | 5 秒後に、前面のウィンドウへ `0123456789ABC-123` を打つ(確認あり) | 通常 |
| `DeskSlate.Probe.exe pipe` | 自分だけが読める名前付きパイプを作れるかを試す | 通常 |
| `DeskSlate.SensorHost.exe --probe` | LibreHardwareMonitorLib が返すハードウェアとセンサーの一覧を出す | 管理者 |

注意:

- `keys` は、前面のウィンドウに実際にキーを打ち込みます。メモ帳を前面にしてから使ってください。
- `SensorHost` は管理者の PowerShell から動かしてください。読むだけで、ハードウェアの設定は変えません。ドライバ(PawnIO)は同梱も導入もしません。
- Probe の出力には、PC で動いているアプリの名前などが入ります。そのまま公開の場所に貼らないでください。

## いまある物: Android 側の probe

端末の実物を調べて、画面とログに出すだけのアプリです。

| ボタン(adb の `action`) | やること |
|---|---|
| 情報(`info`) | 端末の情報とカメラの一覧を出す。起動したときにも出す |
| 振動(`vibrate`) | 振動を 1 回鳴らす |
| EAN-13 を 10 回(`ean`) | カメラで EAN-13 を 10 回読み、1 回ごとの時間と中央値、R-3 の基準(3 秒以内に 8 回以上)を満たすかを出す |
| QR を 3 回(`qr`) | カメラで QR を 3 回読み、同じように出す |

ビルド(JDK 17 と Android SDK が要ります。`android\local.properties` に `sdk.dir=<SDK の場所>` を書いておきます):

```powershell
cd android
.\gradlew.bat assembleDebug
```

入れ方と使い方:

- APK を端末に入れて「DeskSlate Probe」を開き、ボタンを押します。読み取りは「読む」を押すたびに 1 回です。
- adb からも動かせます。出した行は logcat のタグ `DeskSlateProbe` にも出ます。

```powershell
adb install -r DeskSlate-Probe-v0.0.1-android.apk
adb shell am start -n io.github.takosasidev.deskslate.probe/.MainActivity --es action info
adb logcat -s DeskSlateProbe
```

- 権限はカメラ(`CAMERA`)と振動(`VIBRATE`)だけです。インターネットの権限は持たず、何も保存も送信もしません。

## ライセンス

MIT([LICENSE](LICENSE))。使っているライブラリのライセンスは [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) にあります。
