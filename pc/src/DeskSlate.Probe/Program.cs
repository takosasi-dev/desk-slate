// DeskSlate.Probe の入口。Phase 0 の実測用で、引数で 5 つの確認(audio / metrics / foreground / keys / pipe)を選ぶ。
// 出力は仕様書 §8.1 に貼る。製品の本体(DeskSlate.App)からは参照しない。
using DeskSlate.Probe;

return args.FirstOrDefault() switch
{
    "audio" => AudioProbe.Run(args[1..]),
    "metrics" => MetricsProbe.Run(),
    "foreground" => ForegroundProbe.Run(),
    "keys" => KeysProbe.Run(),
    "pipe" => PipeProbe.Run(),
    _ => Usage(),
};

static int Usage()
{
    Console.WriteLine("""
        DeskSlate.Probe  (管理者でない PowerShell から動かす)
          audio                       既定の再生デバイスと、音のセッションの一覧を出す
          audio --set <exe名> <0-100> その exe の音量を 2 秒だけ変えて戻す(確認あり)
          metrics                     CPU・メモリ・ディスク・ネットワーク・バッテリー・GPU を 5 秒分出す
          foreground                  30 秒の間、前面の切り替わりを exe 名と一緒に出す
          keys                        5 秒後に、前面のウィンドウへ 0-9 と ABC-123 を打つ(確認あり)
          pipe                        自分の SID だけに許した名前付きパイプを作れるかを試す
        """);
    return 2;
}
