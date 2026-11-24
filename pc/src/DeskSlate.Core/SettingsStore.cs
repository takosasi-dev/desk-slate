// 設定ファイルの読み書き(仕様書 FR-30)。読めないときは既定値を返し、元のファイルを settings.broken-<日時>.json に残す。
// 書くときは、同じフォルダの一時ファイルに書いてから置き換える。
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace DeskSlate.Core;

public sealed class SettingsStore(string path, IClock clock)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), // ラベルの日本語を \u にしない
    };

    /// <summary>設定を読む。壊れていたら、既定値と、残した元のファイルの場所を返す(呼ぶ側がログに書く)。</summary>
    public (Settings Settings, string? BrokenCopy) Load()
    {
        if (!File.Exists(path))
        {
            return (new Settings(), null);
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path), Options) ?? throw new JsonException("設定が null です");
            return (loaded.Normalize(), null);
        }
        catch (JsonException)
        {
            var broken = Path.Combine(Path.GetDirectoryName(path)!, $"settings.broken-{clock.UtcNow.ToLocalTime():yyyyMMdd-HHmmss}.json");
            File.Move(path, broken, overwrite: true);
            return (new Settings(), broken);
        }
    }

    public void Save(Settings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, Serialize(settings));
        File.Move(temp, path, overwrite: true);
    }

    public static string Serialize(Settings settings) => JsonSerializer.Serialize(settings, Options);
}
