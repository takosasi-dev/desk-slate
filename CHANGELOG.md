# Changelog

この形式は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に従います。

## [Unreleased]

### Added

- PC 側の接続の部品 `DeskSlate.Link` を追加(TLS の待ち受け、ペアリング、認証、LAN の探索、adb の見回り、ログ)。[TabSpan](https://github.com/takosasi-dev/tab-span) の v0.3.0(`b656208`)の実装から写し、ポート 47816・探索の行 `DESKSLATE-DISCOVER`・`Hello` の `app` に合わせた。端末側と画面はまだ無く、これだけでは使えない。
- `DeskSlate.Core` に、枠の読み書き・ペアリングの 6 桁と端末の台帳・セッションの状態・認証の失敗の遮断を追加。
- 端末側と共有するテストベクタ `docs/test-vectors/frames.json`・`pairing.json` を追加。

## [0.0.1] - 2026-11-24

### Added

- Phase 0: PC 側の測定用ツールを追加(`DeskSlate.Probe` の audio / metrics / foreground / keys / pipe、`DeskSlate.SensorHost --probe`)。
- Phase 0: Android 側の probe を追加(CameraX + ZXing。端末の情報とカメラの一覧、EAN-13 を 10 回・QR を 3 回読む計測と R-3 の判定、振動の確認。adb の `--es action <info|vibrate|ean|qr>` でも動かせ、logcat のタグ `DeskSlateProbe` に出す)。単体テストつき。
- `DeskSlate.Core` の土台を追加: 操作の門番(InputGate)、設定の読み書き、メッセージの型、PadInput の読み。単体テストつき。

### Changed

- 名前を TabDeck から DeskSlate に変更(同じ名前のリポジトリが多く、同じ用途の物もあったため)。

[Unreleased]: https://github.com/takosasi-dev/desk-slate/compare/v0.0.1...HEAD
[0.0.1]: https://github.com/takosasi-dev/desk-slate/releases/tag/v0.0.1
