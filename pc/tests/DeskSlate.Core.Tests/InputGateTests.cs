// InputGate の判定を、共有のテストベクタ(docs/test-vectors/gate.json)と、表に無い状態(認証の前、機能オフ、既定の設定)で確かめる。
using System.Text.Json;

namespace DeskSlate.Core.Tests;

public class InputGateTests
{
    private static readonly Settings AllOn = new() { AllowKeyActions = true, AllowScriptActions = true, BlockedApps = ["game.exe"] };

    public static TheoryData<string, string, string> Vectors()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "test-vectors", "gate.json")));
        var data = new TheoryData<string, string, string>();
        foreach (var c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            data.Add(c.GetProperty("op").GetString()!, c.GetProperty("state").GetString()!, c.GetProperty("expect").GetString()!);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Vectors))]
    public void 表のとおりに判定する(string op, string state, string expect)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "test-vectors", "gate.json")));
        var s = doc.RootElement.GetProperty("states").GetProperty(state);
        var gate = new GateState(true, AllOn with { Panic = s.GetProperty("panic").GetBoolean() }, s.GetProperty("foregroundExe").GetString());

        var verdict = InputGate.Decide(Enum.Parse<Operation>(op, ignoreCase: true), gate);

        Assert.Equal(expect, InputGate.ToReason(verdict) ?? "ok");
    }

    [Fact]
    public void 表は操作9種と状態4つの全部を持つ()
    {
        var rows = Vectors().Select(r => (Op: (string)r[0], State: (string)r[1])).ToList();
        Assert.Equal(Enum.GetValues<Operation>().Length * 4, rows.Distinct().Count());
        Assert.All(Enum.GetNames<Operation>(), name => Assert.Equal(4, rows.Count(r => string.Equals(r.Op, name, StringComparison.OrdinalIgnoreCase))));
    }

    [Fact]
    public void 認証の前は全部の操作を通さない()
    {
        var gate = new GateState(false, AllOn, "notepad.exe");
        Assert.All(Enum.GetValues<Operation>(), op => Assert.Equal(GateVerdict.Unauthenticated, InputGate.Decide(op, gate)));
    }

    [Fact]
    public void オフの機能は_disabled_で断る()
    {
        var gate = new GateState(true, AllOn with { Features = new Features { Mixer = false } }, "notepad.exe");
        Assert.Equal(GateVerdict.Disabled, InputGate.Decide(Operation.MixerSet, gate));
        Assert.Equal(GateVerdict.Allow, InputGate.Decide(Operation.Pad, gate));
    }

    [Fact]
    public void 既定の設定ではキーとスクリプトの動作だけ断る()
    {
        var gate = new GateState(true, new Settings(), "notepad.exe");
        Assert.Equal(GateVerdict.Disabled, InputGate.Decide(Operation.ButtonKeys, gate));
        Assert.Equal(GateVerdict.Disabled, InputGate.Decide(Operation.ButtonScript, gate));
        Assert.Equal(GateVerdict.Allow, InputGate.Decide(Operation.ButtonOpen, gate));
        Assert.Equal(GateVerdict.Allow, InputGate.Decide(Operation.ButtonMedia, gate));
    }

    [Fact]
    public void 入力を送らないアプリは大文字と小文字を区別しない()
    {
        var gate = new GateState(true, AllOn, "GAME.EXE");
        Assert.Equal(GateVerdict.BlockedApp, InputGate.Decide(Operation.Pad, gate));
    }
}
