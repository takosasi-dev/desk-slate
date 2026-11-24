// 今の時刻の差し替え口。DateTime.Now を直接書かず、テストで時刻を固定できるようにする。
namespace DeskSlate.Core;

public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
