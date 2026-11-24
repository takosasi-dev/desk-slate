// 端末からの操作を通すか断るかを決める門番(仕様書 FR-2・FR-3・FR-4、INV-5・INV-10)。
// OS は呼ばず、渡された状態だけで決める。判定の表は docs/test-vectors/gate.json。
namespace DeskSlate.Core;

public enum Operation
{
    ButtonOpen,
    ButtonKeys,
    ButtonMedia,
    ButtonScript,
    MixerSet,
    ScanType,
    ScanClipboard,
    Photo,
    Pad,
}

public enum GateVerdict
{
    Allow,

    /// <summary>認証の前。何も実行せず、接続を閉じる(INV-5)。</summary>
    Unauthenticated,
    Disabled,
    Panic,
    BlockedApp,
}

/// <param name="ForegroundExe">前面のアプリの exe 名。取れないときは null。</param>
public sealed record GateState(bool Authenticated, Settings Settings, string? ForegroundExe);

public static class InputGate
{
    public static GateVerdict Decide(Operation op, GateState state)
    {
        var s = state.Settings;
        if (!state.Authenticated)
        {
            return GateVerdict.Unauthenticated;
        }

        // 仮決め: 理由が重なったときの順は仕様書に無い。機能オフ → 非常停止 → 動作の種類オフ → 入力を送らないアプリ、の順にした
        if (!FeatureOn(op, s.Features))
        {
            return GateVerdict.Disabled;
        }

        if (s.Panic)
        {
            return GateVerdict.Panic;
        }

        if ((op == Operation.ButtonKeys && !s.AllowKeyActions) || (op == Operation.ButtonScript && !s.AllowScriptActions))
        {
            return GateVerdict.Disabled;
        }

        return InjectsInput(op) && IsBlocked(state.ForegroundExe, s.BlockedApps) ? GateVerdict.BlockedApp : GateVerdict.Allow;
    }

    /// <summary>端末に返す理由の文字(仕様書 §9.3)。Allow と Unauthenticated には理由が無い。</summary>
    public static string? ToReason(GateVerdict verdict) => verdict switch
    {
        GateVerdict.Disabled => Reasons.Disabled,
        GateVerdict.Panic => Reasons.Panic,
        GateVerdict.BlockedApp => Reasons.BlockedApp,
        _ => null,
    };

    private static bool FeatureOn(Operation op, Features f) => op switch
    {
        Operation.ButtonOpen or Operation.ButtonKeys or Operation.ButtonMedia or Operation.ButtonScript => f.Panel,
        Operation.MixerSet => f.Mixer,
        Operation.ScanType or Operation.ScanClipboard or Operation.Photo => f.Scan,
        Operation.Pad => f.Pad,
        _ => false,
    };

    // キー・文字・マウスを注入する操作だけが、入力を送らないアプリの対象(FR-3)
    private static bool InjectsInput(Operation op) =>
        op is Operation.ButtonKeys or Operation.ButtonMedia or Operation.ScanType or Operation.Pad;

    // 前面の exe 名が取れないときは、登録が空でも止める側に倒す(D-17)
    private static bool IsBlocked(string? foregroundExe, IReadOnlyList<string> blockedApps) =>
        foregroundExe is null || blockedApps.Contains(foregroundExe, StringComparer.OrdinalIgnoreCase);
}
