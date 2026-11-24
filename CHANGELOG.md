# Changelog

## Unreleased

- Phase 0: PC 側の測定用ツールを追加(`DeskSlate.Probe` の audio / metrics / foreground / keys / pipe、`DeskSlate.SensorHost --probe`)。
- 名前を TabDeck から DeskSlate に変更(同じ名前のリポジトリが多く、同じ用途の物もあったため)。
- `DeskSlate.Core` の土台を追加: 操作の門番(InputGate)、設定の読み書き、メッセージの型、PadInput の読み。単体テストつき。
