// 設定の既定値・範囲の外の値の直し・読み書き・壊れたファイルの扱いを確かめる(仕様書 §9.2、FR-30、AC-36)。
using System.Text.Json.Nodes;

namespace DeskSlate.Core.Tests;

public sealed class SettingsTests : IDisposable
{
    // 仕様書 §9.2 の JSON をそのまま写した物。既定値と項目の名前の正
    private const string SpecDefaults = """
        {
          "schemaVersion": 1, "port": 47816, "usbEnabled": true, "lanEnabled": false, "autoLaunchAndroidApp": true, "adbPath": "",
          "features": { "panel": true, "mixer": true, "dashboard": true, "scan": true, "pad": true },
          "allowKeyActions": false, "allowScriptActions": false, "panic": false, "panicHotkey": "Ctrl+Alt+P", "blockedApps": [],
          "panel": { "pages": [], "pageRules": [] },
          "scriptTimeoutSec": 60, "mixerPollMs": 500, "mixerPinned": [], "mixerHidden": [],
          "dashboard": { "pages": [] }, "monitors": [],
          "sensorHostEnabled": false, "quakeEnabled": false, "quakeBannerSec": 180,
          "scanMode": "type", "scanSuffix": "enter", "scanKeyIntervalMs": 2, "photoDir": "", "photoToClipboard": false,
          "padSpeed": 1.0, "padScrollInvert": false, "startWithWindows": false, "language": "auto"
        }
        """;

    private readonly string _dir = Directory.CreateTempSubdirectory("deskslate-test-").FullName;
    private readonly FixedClock _clock = new(new DateTime(2026, 11, 24, 1, 2, 3, DateTimeKind.Utc));

    private string SettingsPath => Path.Combine(_dir, "settings.json");

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void 既定値は仕様書の_JSON_と同じ()
    {
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(SpecDefaults), JsonNode.Parse(SettingsStore.Serialize(new Settings()))));
    }

    [Fact]
    public void ファイルが無ければ既定値()
    {
        var (settings, broken) = new SettingsStore(SettingsPath, _clock).Load();
        Assert.Equal(47816, settings.Port);
        Assert.Null(broken);
    }

    [Fact]
    public void 保存して読み直すと同じ値になり_パネルの中身も保つ()
    {
        var store = new SettingsStore(SettingsPath, _clock);
        var json = JsonNode.Parse(SpecDefaults)!;
        json["port"] = 50000;
        json["blockedApps"] = new JsonArray("game.exe");
        json["panel"]!["pages"] = new JsonArray(new JsonObject { ["id"] = "p1", ["name"] = "作業" });
        File.WriteAllText(SettingsPath, json.ToJsonString());

        var (loaded, _) = store.Load();
        store.Save(loaded);

        Assert.True(JsonNode.DeepEquals(json, JsonNode.Parse(File.ReadAllText(SettingsPath))));
        Assert.Contains("作業", File.ReadAllText(SettingsPath)); // 日本語を \u にしない
        Assert.False(File.Exists(SettingsPath + ".tmp"));
    }

    [Fact]
    public void 壊れた_JSON_は既定値で起動して元のファイルを残す()
    {
        File.WriteAllText(SettingsPath, "{ \"port\": 50000, ");

        var (settings, broken) = new SettingsStore(SettingsPath, _clock).Load();

        Assert.Equal(47816, settings.Port);
        Assert.Equal(Path.Combine(_dir, $"settings.broken-{_clock.UtcNow.ToLocalTime():yyyyMMdd-HHmmss}.json"), broken);
        Assert.Equal("{ \"port\": 50000, ", File.ReadAllText(broken!));
        Assert.False(File.Exists(SettingsPath));
    }

    [Fact]
    public void 範囲の外の値は表の値に直る()
    {
        File.WriteAllText(SettingsPath, """
            { "port": 80, "scriptTimeoutSec": 4, "mixerPollMs": 2001, "quakeBannerSec": 29, "scanMode": "paste",
              "scanSuffix": "space", "scanKeyIntervalMs": 51, "padSpeed": 3.1, "blockedApps": null }
            """);

        var (s, broken) = new SettingsStore(SettingsPath, _clock).Load();

        Assert.Null(broken);
        Assert.Equal((47816, 60, 500, 180, "type", "enter", 2, 1.0),
            (s.Port, s.ScriptTimeoutSec, s.MixerPollMs, s.QuakeBannerSec, s.ScanMode, s.ScanSuffix, s.ScanKeyIntervalMs, s.PadSpeed));
        Assert.Empty(s.BlockedApps);
    }

    [Fact]
    public void 範囲の端の値はそのまま使う()
    {
        File.WriteAllText(SettingsPath, """{ "port": 1024, "scriptTimeoutSec": 600, "mixerPollMs": 200, "padSpeed": 0.5, "scanKeyIntervalMs": 0 }""");

        var (s, _) = new SettingsStore(SettingsPath, _clock).Load();

        Assert.Equal((1024, 600, 200, 0.5, 0), (s.Port, s.ScriptTimeoutSec, s.MixerPollMs, s.PadSpeed, s.ScanKeyIntervalMs));
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public long NowUs => 0;

        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
