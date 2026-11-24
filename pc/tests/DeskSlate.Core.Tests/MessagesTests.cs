// メッセージの JSON の形(項目の名前、無い項目の省き方)と、PadInput のバイナリの読みを確かめる(仕様書 §9.3)。
using System.Text;
using System.Text.Json;

namespace DeskSlate.Core.Tests;

public class MessagesTests
{
    private static string Json<T>(T message) => Encoding.UTF8.GetString(MessageJson.Serialize(message));

    [Fact]
    public void 項目の名前は仕様書のとおり()
    {
        Assert.Equal("""{"panel":true,"mixer":false,"dashboard":true,"scan":true,"pad":false,"panic":true}""",
            Json(new FeaturesMsg(true, false, true, true, false, true)));
        Assert.Equal("""{"forType":97,"reason":"disabled"}""", Json(new RejectMsg((int)MessageType.MixerSet, Reasons.Disabled)));
        Assert.Equal("""{"photoId":7,"ok":true,"fileName":"DeskSlate_20261124_100000.jpg"}""",
            Json(new PhotoResultMsg(7, true, FileName: "DeskSlate_20261124_100000.jpg")));
    }

    [Fact]
    public void 値の無い項目は書かない()
    {
        Assert.Equal("""{"pressId":12,"ok":true}""", Json(new ButtonResultMsg(12, true)));
        Assert.Equal("""{"pressId":12,"ok":false,"reason":"exit_code","detail":"3"}""", Json(new ButtonResultMsg(12, false, Reasons.ExitCode, "3")));
    }

    [Fact]
    public void 端末からの_JSON_を読む()
    {
        var set = MessageJson.Deserialize<MixerSetMsg>("""{"key":"master","volume":40}"""u8);
        Assert.Equal(("master", 40, null), (set.Key, set.Volume, set.Muted));

        var press = MessageJson.Deserialize<ButtonPressMsg>("""{"buttonId":"b-1","pressId":5}"""u8);
        Assert.Equal(("b-1", 5L), (press.ButtonId, press.PressId));

        Assert.Throws<JsonException>(() => MessageJson.Deserialize<ButtonPressMsg>("not json"u8));
    }

    [Theory]
    [InlineData("{}")]                                  // 必須の項目が無い
    [InlineData("""{"pressId":1}""")]
    [InlineData("""{"buttonId":null,"pressId":1}""")] // null を入れてきた
    [InlineData("""{"buttonId":"b-1","pressId":"x"}""")]
    [InlineData("null")]
    public void 項目の欠けた_JSON_は読まずに断る(string json)
    {
        Assert.Throws<JsonException>(() => MessageJson.Deserialize<ButtonPressMsg>(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void 省いてよい項目は無くても読める()
    {
        var result = MessageJson.Deserialize<ScanResultMsg>("""{"scanId":3,"ok":true}"""u8);
        Assert.Equal((3L, true, null), (result.ScanId, result.Ok, result.Reason));
    }

    [Fact]
    public void パネルの並びに動作の中身が出ない()
    {
        var layout = new PanelLayoutMsg([new PanelPage("p1", "作業", 3, 5, [new PanelButton("b1", 0, 0, "ビルド", "blue", "play")])]);
        var json = Json(layout);
        Assert.All(new[] { "action", "target", "path", "steps", "sha256" }, word => Assert.DoesNotContain(word, json));
    }

    [Fact]
    public void PadInput_を読む()
    {
        Assert.Equal(new PadEvent.Move(10, -4), PadEvent.Parse([1, 0x00, 0x0A, 0xFF, 0xFC]));
        Assert.Equal(new PadEvent.Scroll(-1, 2), PadEvent.Parse([2, 0xFF, 0xFF, 0x00, 0x02]));
        Assert.Equal(new PadEvent.Button(1, true), PadEvent.Parse([3, 1, 1]));
        Assert.Equal(new PadEvent.Button(2, false), PadEvent.Parse([3, 2, 0]));
        Assert.Equal(new PadEvent.Click(2), PadEvent.Parse([4, 2]));
        Assert.Equal(new PadEvent.NumpadKey("Enter"), PadEvent.Parse([5, 5, .. "Enter"u8]));
        Assert.Equal(new PadEvent.NumpadKey(""), PadEvent.Parse([5, 0]));
    }

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 9, 0, 0 })]          // 知らない kind
    [InlineData(new byte[] { 1, 0, 10, 0 })]      // 移動が 1 バイト足りない
    [InlineData(new byte[] { 1, 0, 10, 0, 1, 0 })] // 移動が 1 バイト多い
    [InlineData(new byte[] { 3, 3, 1 })]          // 無いボタン
    [InlineData(new byte[] { 3, 1, 2 })]          // 押す・離すのどちらでもない
    [InlineData(new byte[] { 4, 0 })]
    [InlineData(new byte[] { 5, 3, 65, 66 })]     // 名前の長さが合わない
    [InlineData(new byte[] { 5 })]
    public void 形の合わない_PadInput_は捨てる(byte[] payload)
    {
        Assert.Null(PadEvent.Parse(payload));
    }
}
