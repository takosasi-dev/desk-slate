// PC 側の設定(仕様書 §9.2)。既定値と、範囲の外の値を既定に直す決まりを持つ。
// shortcut: panel・dashboard・monitors の中身は JSON のまま持ち回る。型にするのは Phase 2・6 で、それまでは読み書きで中身を保つだけ。
using System.Text.Json;

namespace DeskSlate.Core;

public sealed record Features
{
    public bool Panel { get; init; } = true;
    public bool Mixer { get; init; } = true;
    public bool Dashboard { get; init; } = true;
    public bool Scan { get; init; } = true;
    public bool Pad { get; init; } = true;
}

public sealed record Settings
{
    private static readonly JsonElement EmptyPanel = JsonSerializer.SerializeToElement(new { pages = Array.Empty<object>(), pageRules = Array.Empty<object>() });
    private static readonly JsonElement EmptyDashboard = JsonSerializer.SerializeToElement(new { pages = Array.Empty<object>() });
    private static readonly JsonElement EmptyArray = JsonSerializer.SerializeToElement(Array.Empty<object>());

    public int SchemaVersion { get; init; } = 1;
    public int Port { get; init; } = 47816;
    public bool UsbEnabled { get; init; } = true;
    public bool LanEnabled { get; init; }
    public bool AutoLaunchAndroidApp { get; init; } = true;
    public string AdbPath { get; init; } = "";
    public Features Features { get; init; } = new();
    public bool AllowKeyActions { get; init; }
    public bool AllowScriptActions { get; init; }
    public bool Panic { get; init; }
    public string PanicHotkey { get; init; } = "Ctrl+Alt+P";
    public IReadOnlyList<string> BlockedApps { get; init; } = [];
    public JsonElement Panel { get; init; } = EmptyPanel;
    public int ScriptTimeoutSec { get; init; } = 60;
    public int MixerPollMs { get; init; } = 500;
    public IReadOnlyList<string> MixerPinned { get; init; } = [];
    public IReadOnlyList<string> MixerHidden { get; init; } = [];
    public JsonElement Dashboard { get; init; } = EmptyDashboard;
    public JsonElement Monitors { get; init; } = EmptyArray;
    public bool SensorHostEnabled { get; init; }
    public bool QuakeEnabled { get; init; }
    public int QuakeBannerSec { get; init; } = 180;
    public string ScanMode { get; init; } = "type";
    public string ScanSuffix { get; init; } = "enter";
    public int ScanKeyIntervalMs { get; init; } = 2;
    public string PhotoDir { get; init; } = "";
    public bool PhotoToClipboard { get; init; }
    public double PadSpeed { get; init; } = 1.0;
    public bool PadScrollInvert { get; init; }
    public bool StartWithWindows { get; init; }
    public string Language { get; init; } = "auto";

    /// <summary>範囲の外の値を §9.2 の表の値に直し、JSON に null と書かれていた項目を既定に戻す。</summary>
    public Settings Normalize()
    {
        var d = new Settings();
        return this with
        {
            Port = Port is >= 1024 and <= 65535 ? Port : d.Port,
            ScriptTimeoutSec = ScriptTimeoutSec is >= 5 and <= 600 ? ScriptTimeoutSec : d.ScriptTimeoutSec,
            MixerPollMs = MixerPollMs is >= 200 and <= 2000 ? MixerPollMs : d.MixerPollMs,
            QuakeBannerSec = QuakeBannerSec is >= 30 and <= 600 ? QuakeBannerSec : d.QuakeBannerSec,
            ScanMode = ScanMode is "type" or "clipboard" ? ScanMode : d.ScanMode,
            ScanSuffix = ScanSuffix is "enter" or "tab" or "none" ? ScanSuffix : d.ScanSuffix,
            ScanKeyIntervalMs = ScanKeyIntervalMs is >= 0 and <= 50 ? ScanKeyIntervalMs : d.ScanKeyIntervalMs,
            PadSpeed = PadSpeed is >= 0.5 and <= 3.0 ? PadSpeed : d.PadSpeed,
            AdbPath = AdbPath ?? d.AdbPath,
            Features = Features ?? d.Features,
            PanicHotkey = PanicHotkey ?? d.PanicHotkey,
            BlockedApps = BlockedApps ?? d.BlockedApps,
            MixerPinned = MixerPinned ?? d.MixerPinned,
            MixerHidden = MixerHidden ?? d.MixerHidden,
            Panel = Panel.ValueKind == JsonValueKind.Object ? Panel : d.Panel,
            Dashboard = Dashboard.ValueKind == JsonValueKind.Object ? Dashboard : d.Dashboard,
            Monitors = Monitors.ValueKind == JsonValueKind.Array ? Monitors : d.Monitors,
            PhotoDir = PhotoDir ?? d.PhotoDir,
            Language = Language ?? d.Language,
        };
    }
}
