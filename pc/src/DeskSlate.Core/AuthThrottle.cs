// 認証の失敗の遮断(TabSpan の仕様書 FR-3。DeskSlate では FR-1)。複数のスレッドから呼ばれてよい。
// TabSpan の実装(Policies.cs)から写した(仕様書 D-2・§8.9)。
namespace DeskSlate.Core;

/// <summary>同じリモートから認証の失敗が 5 回続いたら、60 秒の間 IsBlocked が真。成功で数え直す。</summary>
public sealed class AuthThrottle(IClock clock)
{
    public const int MaxFailures = 5;
    public const long BlockUs = 60_000_000;

    private readonly Dictionary<string, (int Failures, long? BlockedUntilUs)> _remotes = [];
    private readonly Lock _lock = new();

    public bool IsBlocked(string remote)
    {
        lock (_lock)
        {
            if (!_remotes.TryGetValue(remote, out var r) || r.BlockedUntilUs is not { } until)
            {
                return false;
            }

            if (clock.NowUs < until)
            {
                return true;
            }

            _remotes.Remove(remote); // 遮断が明けたら数え直し
            return false;
        }
    }

    public void Fail(string remote)
    {
        lock (_lock)
        {
            _remotes.TryGetValue(remote, out var r);
            var failures = r.Failures + 1;
            _remotes[remote] = failures >= MaxFailures ? (0, clock.NowUs + BlockUs) : (failures, r.BlockedUntilUs);
        }
    }

    public void Succeed(string remote)
    {
        lock (_lock)
        {
            _remotes.Remove(remote);
        }
    }
}
