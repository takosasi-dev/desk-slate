// FrameCodec(上限ちょうど・超え・途中で切れた流れ・細切れに返す流れ)と Messages(JSON の必須・型・null・camelCase、バイナリ)。
// TabSpan のテスト(takosasi-dev/tab-span b656208)から写した(仕様書 D-2・§8.9)。映像とタッチの分は除いた。
using System.Text;

namespace DeskSlate.Core.Tests;

public class FrameCodecTests
{
    // SslStream のように 1 回の Read で 1 バイトずつしか返さない流れ
    private sealed class TrickleStream(byte[] data) : MemoryStream(data)
    {
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 1));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, 1)], cancellationToken);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            base.ReadAsync(buffer, offset, Math.Min(count, 1), cancellationToken);
    }

    [Fact]
    public async Task 上限ちょうどの枠は書けて読める()
    {
        var payload = new byte[FrameCodec.MaxPayload];
        payload[^1] = 0xAB;
        using var s = new MemoryStream();
        await FrameCodec.WriteAsync(s, 0x83, payload, default);
        s.Position = 0;

        var f = await FrameCodec.ReadAsync(s, default);

        Assert.Equal(0x83, f!.Value.Type);
        Assert.Equal(FrameCodec.MaxPayload, f.Value.Payload.Length);
        Assert.Equal(0xAB, f.Value.Payload.Span[^1]);
    }

    [Fact]
    public async Task 上限を超える長さは中身を読まずに例外()
    {
        using var s = new MemoryStream([0x11, 0x00, 0x40, 0x00, 0x01, 9, 9, 9]); // 4 MiB + 1

        var ex = await Assert.ThrowsAsync<FrameTooLargeException>(async () => await FrameCodec.ReadAsync(s, default));

        Assert.Equal((uint)FrameCodec.MaxPayload + 1, ex.Length);
        Assert.Equal(5, s.Position);
    }

    [Fact]
    public void 上限を超える中身は書かない()
    {
        Assert.Throws<FrameTooLargeException>(() => FrameCodec.Encode(1, new byte[FrameCodec.MaxPayload + 1]));
    }

    [Fact]
    public async Task 空の流れは_null_で_ヘッダーや中身の途中で切れたら例外()
    {
        Assert.Null(await FrameCodec.ReadAsync(new MemoryStream([]), default));
        await Assert.ThrowsAsync<EndOfStreamException>(async () => await FrameCodec.ReadAsync(new MemoryStream([0x30, 0, 0]), default));
        await Assert.ThrowsAsync<EndOfStreamException>(async () => await FrameCodec.ReadAsync(new MemoryStream([0x30, 0, 0, 0, 8, 1, 2, 3]), default));
    }

    [Fact]
    public async Task 一バイトずつしか返さない流れでも続けて読める()
    {
        byte[] data = [.. FrameCodec.Encode(0x30, Messages.Ping(42)), .. FrameCodec.Encode(0x52, []), .. FrameCodec.Encode(0x31, Messages.Pong(1, 2))];
        using var s = new TrickleStream(data);

        Assert.Equal(42, Messages.ParsePing((await FrameCodec.ReadAsync(s, default))!.Value.Payload.Span));
        Assert.Equal(0, (await FrameCodec.ReadAsync(s, default))!.Value.Payload.Length);
        Assert.Equal((1L, 2L), Messages.ParsePong((await FrameCodec.ReadAsync(s, default))!.Value.Payload.Span));
        Assert.Null(await FrameCodec.ReadAsync(s, default));
    }
}

public class LinkMessagesTests
{
    private static byte[] U(string json) => Encoding.UTF8.GetBytes(json);

    private const string Hello =
        """{"protocolVersion":1,"app":"deskslate","deviceId":"d","token":"dA==","deviceName":"n","appVersion":"0.0.1","screen":{"widthPx":2560,"heightPx":1600,"densityDpi":320}}""";

    [Fact]
    public void Hello_を読める_token_や_app_の無い_Hello_も読める()
    {
        var h = Messages.FromJson<HelloMsg>(U(Hello));
        Assert.Equal((1, "d", "dA==", 2560, 320), (h.ProtocolVersion, h.DeviceId, h.Token, h.Screen.WidthPx, h.Screen.DensityDpi));
        Assert.Equal("deskslate", h.App);

        Assert.Null(Messages.FromJson<HelloMsg>(U(Hello.Replace("\"token\":\"dA==\",", "", StringComparison.Ordinal))).Token);
        // app は読む時には必須にしない。無い Hello には Link が HelloAck(reason=version) を返す(仕様書 §9.3)
        Assert.Null(Messages.FromJson<HelloMsg>(U(Hello.Replace("\"app\":\"deskslate\",", "", StringComparison.Ordinal))).App);
    }

    [Theory]
    [InlineData("\"deviceId\":\"d\",", "")]                         // 必須の項目が無い
    [InlineData("\"deviceId\":\"d\"", "\"deviceId\":null")]         // null 許容でない項目が null
    [InlineData("\"protocolVersion\":1", "\"protocolVersion\":\"1\"")] // 型が違う
    [InlineData("\"widthPx\":2560,", "")]                           // 入れ子の必須
    [InlineData("{\"protocolVersion\"", "[{\"protocolVersion\"")]   // 壊れた JSON
    public void 必須が無い_null_型違い_壊れた_JSON_は_FormatException(string from, string to)
    {
        var json = Hello.Replace(from, to, StringComparison.Ordinal);
        Assert.NotEqual(Hello, json);
        Assert.Throws<FormatException>(() => Messages.FromJson<HelloMsg>(U(json)));
    }

    [Fact]
    public void ルートが_null_や空なら_FormatException()
    {
        Assert.Throws<FormatException>(() => Messages.FromJson<ReasonMsg>(U("null")));
        Assert.Throws<FormatException>(() => Messages.FromJson<ReasonMsg>(U("")));
        Assert.Throws<FormatException>(() => Messages.FromJson<ReasonMsg>(U("{}")));
    }

    [Fact]
    public void null_の項目は書かず_キーは_camelCase()
    {
        var json = Encoding.UTF8.GetString(Messages.ToJson(new HelloAckMsg(true, null, "pc", "PC 名", "0.0.1", false)));
        Assert.Equal("""{"ok":true,"pcInstanceId":"pc","pcName":"PC 名","appVersion":"0.0.1","touchEnabled":false}""", json);

        var ack = Messages.FromJson<HelloAckMsg>(U(json));
        Assert.Null(ack.Reason);
        Assert.Equal("busy", Messages.FromJson<HelloAckMsg>(U(json.Replace("\"ok\":true", "\"ok\":false,\"reason\":\"busy\"", StringComparison.Ordinal))).Reason);
    }

    [Fact]
    public void Base64_の記号と日本語をエスケープしない()
    {
        Assert.Equal("""{"token":"a+/="}""", Encoding.UTF8.GetString(Messages.ToJson(new PairAcceptMsg("a+/="))));
        Assert.Equal("""{"reason":"端末"}""", Encoding.UTF8.GetString(Messages.ToJson(new ReasonMsg("端末"))));
    }

    [Fact]
    public void Ping_と_Pong_は_8_バイトと_16_バイト()
    {
        Assert.Equal(Convert.FromHexString("00000000000000FF"), Messages.Ping(255));
        Assert.Equal((-1L, 2L), Messages.ParsePong(Messages.Pong(-1, 2)));
        Assert.Throws<FormatException>(() => Messages.ParsePing(new byte[7]));
        Assert.Throws<FormatException>(() => Messages.ParsePong(new byte[8]));
    }

}
