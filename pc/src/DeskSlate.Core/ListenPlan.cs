// 待ち受けの計画(FR-1・INV-5): lanEnabled が偽なら 127.0.0.1 の TCP だけ、真なら 0.0.0.0 の TCP と UDP(探索)。
// TabSpan の実装から写した(仕様書 D-2・§8.9)。
using System.Net;

namespace DeskSlate.Core;

public sealed record ListenPlan(IPAddress TcpAddress, bool Udp);

public static class ListenPlanner
{
    public static ListenPlan For(bool lanEnabled) => lanEnabled ? new(IPAddress.Any, true) : new(IPAddress.Loopback, false);
}
