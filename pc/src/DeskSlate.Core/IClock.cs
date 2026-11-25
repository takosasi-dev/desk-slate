// 時計の差し替え口。DateTime.Now や Stopwatch を直接書かず、テストで時刻を固定できるようにする。
// TabSpan の実装から写した(仕様書 D-2・§8.9)。
using System.Diagnostics;

namespace DeskSlate.Core;

/// <summary>単調な µs(差だけに意味がある)と、壁時計。</summary>
public interface IClock
{
    long NowUs { get; }
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();

    // ts * 1e6 は 10 MHz の時計で起動から約 10 日であふれるので、割ってから掛ける
    public long NowUs
    {
        get
        {
            var ts = Stopwatch.GetTimestamp();
            var f = Stopwatch.Frequency;
            return ts / f * 1_000_000 + ts % f * 1_000_000 / f;
        }
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
