// 端末と PC の間のメッセージの番号・理由の文字・中身の型(仕様書 §9.3 のうち DeskSlate が足す分)。
// 枠と、接続・ペアリングのメッセージ(0x01〜0x40)は Link が持つ。PhotoChunk と PadInput はバイナリなので、ここに型が無い。
// 仮決め: pressId と scanId は数(JSON の number)、detail は文字、at と since は ISO 8601 の文字。仕様書に型の指定が無い。
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskSlate.Core;

public enum MessageType : byte
{
    Reject = 0x41,
    Features = 0x50,
    PanelLayout = 0x51,
    ButtonPress = 0x52,
    ButtonResult = 0x53,
    ShowPage = 0x54,
    MixerState = 0x60,
    MixerSet = 0x61,
    DashLayout = 0x70,
    DashData = 0x71,
    DashAlert = 0x72,
    ScanText = 0x80,
    ScanResult = 0x81,
    PhotoBegin = 0x82,
    PhotoChunk = 0x83,
    PhotoEnd = 0x84,
    PhotoResult = 0x85,
    PadInput = 0x90,
    PadResult = 0x91,
}

public static class Reasons
{
    public const string Disabled = "disabled";
    public const string Panic = "panic";
    public const string BlockedApp = "blocked_app";
    public const string UnknownButton = "unknown_button";
    public const string NotFound = "not_found";
    public const string NotApproved = "not_approved";
    public const string Busy = "busy";
    public const string Timeout = "timeout";
    public const string ExitCode = "exit_code";
    public const string LaunchFailed = "launch_failed";
    public const string NotTypable = "not_typable";
    public const string TooLong = "too_long";
    public const string NotJpeg = "not_jpeg";
    public const string TooLarge = "too_large";
    public const string HashMismatch = "hash_mismatch";
    public const string WriteFailed = "write_failed";
    public const string UnknownKey = "unknown_key";
}

public sealed record RejectMsg(int ForType, string Reason);

public sealed record FeaturesMsg(bool Panel, bool Mixer, bool Dashboard, bool Scan, bool Pad, bool Panic);

// 端末へ送るのは見た目だけ。動作の種類と中身は載せない(D-12・INV-4)
public sealed record PanelButton(string Id, int Row, int Col, string Label, string Color, string? Icon = null);

public sealed record PanelPage(string Id, string Name, int Rows, int Cols, IReadOnlyList<PanelButton> Buttons);

public sealed record PanelLayoutMsg(IReadOnlyList<PanelPage> Pages);

public sealed record ButtonPressMsg(string ButtonId, long PressId);

public sealed record ButtonResultMsg(long PressId, bool Ok, string? Reason = null, string? Detail = null);

public sealed record ShowPageMsg(string PageId);

public sealed record MixerMaster(int Volume, bool Muted);

public sealed record MixerRow(string Key, string Name, int Volume, bool Muted, bool Active);

public sealed record MixerStateMsg(MixerMaster Master, IReadOnlyList<MixerRow> Rows);

public sealed record MixerSetMsg(string Key, int? Volume = null, bool? Muted = null);

public sealed record DashTile(string Id, string Kind, string Title, string? Unit = null);

public sealed record DashPage(string Id, string Name, IReadOnlyList<DashTile> Tiles);

public sealed record DashLayoutMsg(IReadOnlyList<DashPage> Pages);

public sealed record DashTileData(string State, double? Value = null, string? Text = null, string? Since = null);

public sealed record DashDataMsg(string At, IReadOnlyDictionary<string, DashTileData> Tiles);

public sealed record DashAlertMsg(string TileId, string Kind, string Text);

public sealed record ScanTextMsg(long ScanId, string Format, string Text);

public sealed record ScanResultMsg(long ScanId, bool Ok, string? Reason = null);

public sealed record PhotoBeginMsg(uint PhotoId, long Bytes, string Sha256);

public sealed record PhotoEndMsg(uint PhotoId);

public sealed record PhotoResultMsg(uint PhotoId, bool Ok, string? Reason = null, string? FileName = null);

public sealed record PadResultMsg(bool Ok, string Reason);

public static class MessageJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static byte[] Serialize<T>(T message) => JsonSerializer.SerializeToUtf8Bytes(message, Options);

    /// <summary>読めない payload は JsonException を投げる。呼ぶ側が捨ててログに残す。</summary>
    public static T Deserialize<T>(ReadOnlySpan<byte> payload) =>
        JsonSerializer.Deserialize<T>(payload, Options) ?? throw new JsonException("payload が null です");
}
