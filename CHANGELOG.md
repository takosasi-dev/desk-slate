# Changelog

この形式は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に従います。

## [Unreleased]

## [0.0.1] - 2026-11-24

### Added

- Phase 0: PC 側の測定用ツールを追加(`DeskSlate.Probe` の audio / metrics / foreground / keys / pipe、`DeskSlate.SensorHost --probe`)。
- Phase 0: Android 側の probe を追加(CameraX + ZXing。端末の情報とカメラの一覧、EAN-13 を 10 回・QR を 3 回読む計測と R-3 の判定、振動の確認。adb の `--es action <info|vibrate|ean|qr>` でも動かせ、logcat のタグ `DeskSlateProbe` に出す)。単体テストつき。
- `DeskSlate.Core` の土台を追加: 操作の門番(InputGate)、設定の読み書き、メッセージの型、PadInput の読み。単体テストつき。

### Changed

- 名前を TabDeck から DeskSlate に変更(同じ名前のリポジトリが多く、同じ用途の物もあったため)。

[Unreleased]: https://github.com/takosasi-dev/desk-slate/compare/v0.0.1...HEAD
[0.0.1]: https://github.com/takosasi-dev/desk-slate/releases/tag/v0.0.1
