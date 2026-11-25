// 接続・ペアリングのメッセージ(0x01〜0x06、0x30、0x31、0x3F、0x40)の中身の型と変換。TabSpan の仕様書 §9.3 と同じ形。
// TabSpan の実装(Messages.cs)から写した(仕様書 D-2・§8.9)。違いは Hello だけ: decoder を除き、app を足した(DeskSlate の仕様書 §9.3)。
// JSON はキーが camelCase、null の項目は書かない。null 許容でない項目が無い・null・型違いは FormatException(Link はこの例外で接続を閉じる)。
// 決定(2026-11-25): HelloAck の touchEnabled は TabSpan と同じ形のまま残し、DeskSlate はいつも false を送る。
using System.Buffers.Binary;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace DeskSlate.Core;

/// <summary>App は null 許容にしてある。無い・"deskslate" でない Hello には、Link が HelloAck(reason=version) を返す。</summary>
public sealed record HelloMsg(int ProtocolVersion, string? App, string DeviceId, string? Token, string DeviceName, string AppVersion, ScreenInfo Screen);
public sealed record ScreenInfo(int WidthPx, int HeightPx, int DensityDpi);
public sealed record HelloAckMsg(bool Ok, string? Reason, string PcInstanceId, string PcName, string AppVersion, bool TouchEnabled);
public sealed record PairRequestMsg(int ProtocolVersion, string DeviceId, string DeviceName, string ClientNonce); // Base64
public sealed record PairChallengeMsg(string PcInstanceId, string PcName, string ServerNonce);
public sealed record PairAcceptMsg(string Token);
public sealed record ReasonMsg(string Reason); // PairReject と Bye
public sealed record NoticeMsg(string Kind, JsonElement Value);

public static class Messages
{
    public const int ProtocolVersion = 1;
    public const string App = "deskslate";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        RespectNullableAnnotations = true, // null 許容でない項目の null は例外
        // 日本語の端末名や Base64 の + / を \uXXXX にしない(Android 側とバイトを揃える)
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { RequireNonNullable } },
    };

    // null 許容でない項目は必須にする(無ければ例外)。null 許容の項目(app、token、reason)は省いてよい。
    private static void RequireNonNullable(JsonTypeInfo info)
    {
        foreach (var p in info.Properties)
        {
            p.IsRequired = !p.IsSetNullable;
        }
    }

    public static byte[] ToJson<T>(T msg) => JsonSerializer.SerializeToUtf8Bytes(msg, Options);

    public static T FromJson<T>(ReadOnlySpan<byte> utf8)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(utf8, Options) ?? throw new FormatException($"{typeof(T).Name} の JSON が null");
        }
        catch (JsonException ex)
        {
            throw new FormatException($"{typeof(T).Name} の JSON が読めない: {ex.Message}", ex);
        }
    }

    public static byte[] Ping(long t1Us)
    {
        var p = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(p, t1Us);
        return p;
    }

    public static long ParsePing(ReadOnlySpan<byte> p) =>
        p.Length == 8 ? BinaryPrimitives.ReadInt64BigEndian(p) : throw new FormatException($"Ping は 8 バイト(受けたのは {p.Length})");

    public static byte[] Pong(long t1Us, long t2Us)
    {
        var p = new byte[16];
        BinaryPrimitives.WriteInt64BigEndian(p, t1Us);
        BinaryPrimitives.WriteInt64BigEndian(p.AsSpan(8), t2Us);
        return p;
    }

    public static (long T1, long T2) ParsePong(ReadOnlySpan<byte> p) =>
        p.Length == 16
            ? (BinaryPrimitives.ReadInt64BigEndian(p), BinaryPrimitives.ReadInt64BigEndian(p[8..]))
            : throw new FormatException($"Pong は 16 バイト(受けたのは {p.Length})");
}
