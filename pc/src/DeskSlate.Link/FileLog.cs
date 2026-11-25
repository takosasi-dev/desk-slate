// ログのファイル deskslate-<yyyyMMdd>.log への書き込み(FR-28)。1 ファイル 5 MB で切り替え、14 日より古い物を起動時に消す。
// 打ち込んだ文字・バーコードの内容・クリップボードと写真の中身・トークン・秘密鍵は、呼ぶ側が渡さない(DeskSlate の INV-11)。時刻は壁時計(ログの時刻だけは壁時計でよい)。
// 決定(2026-11-25): ファイルの名前と 5 MB・14 日は、DeskSlate の仕様書に指定が無いので TabSpan と同じにした。
// TabSpan の実装(takosasi-dev/tab-span b656208)から写した(仕様書 D-2・§8.9)。コメントの FR・INV・AC・D・§ の番号は、断りが無ければ TabSpan の仕様書の物。
using System.Globalization;

namespace DeskSlate.Link;

public sealed class FileLog(string dir, DeskSlate.Core.IClock clock)
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const int KeepDays = 14;
    private const string Prefix = "deskslate-";

    private readonly Lock _lock = new();

    /// <summary>書いた行を横でも受けたいとき(コンソール・画面)。</summary>
    public event Action<string>? Written;

    public void Write(string message)
    {
        var now = clock.UtcNow.ToLocalTime();
        var line = $"{now:yyyy-MM-dd HH:mm:ss.fff} {message}";
        lock (_lock)
        {
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"deskslate-{now:yyyyMMdd}.log");
            var info = new FileInfo(path);
            if (info.Exists && info.Length >= MaxBytes)
            {
                // 切り替えた古い方は deskslate-<日付>-<時刻>.log として残す(日付で古さを判定できるように頭は同じ)
                File.Move(path, Path.Combine(dir, $"deskslate-{now:yyyyMMdd}-{now:HHmmssfff}.log"), overwrite: true);
            }

            File.AppendAllText(path, line + Environment.NewLine);
        }

        Written?.Invoke(line);
    }

    /// <summary>起動時に呼ぶ。名前の日付が今日から 14 日より前のログを消す(14 日前の物は残す)。</summary>
    public void DeleteOld()
    {
        if (!Directory.Exists(dir))
        {
            return;
        }

        var today = DateOnly.FromDateTime(clock.UtcNow.ToLocalTime().DateTime);
        foreach (var file in Directory.EnumerateFiles(dir, "deskslate-*.log"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (name.Length >= Prefix.Length + 8
                && DateOnly.TryParseExact(name.AsSpan(Prefix.Length, 8), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                && date < today.AddDays(-KeepDays))
            {
                File.Delete(file);
            }
        }
    }
}
