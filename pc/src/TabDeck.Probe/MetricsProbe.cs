// 管理者なしで取れる基本の値(CPU・メモリ・ディスク・ネットワーク・バッテリー・GPU)を、試した API の名前と一緒に 5 秒分出す(仕様書 §8.1、D-4)。
// どの API を本体で使うかは、この出力とタスク マネージャーの値を比べて決める。
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace TabDeck.Probe;

internal static class MetricsProbe
{
    internal static int Run()
    {
        // 1 回だけ出す物
        Console.WriteLine("ディスク [System.IO.DriveInfo]: " + Attempt.Read(() => string.Join(" / ",
            DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
                .Select(d => $"{d.Name} 空き {d.AvailableFreeSpace / 1e9:0.0}/{d.TotalSize / 1e9:0.0} GB"))));
        Console.WriteLine("バッテリー [GetSystemPowerStatus]: " + Attempt.Read(() =>
            Native.GetSystemPowerStatus(out var p)
                ? $"ACLineStatus={p.ACLineStatus} BatteryFlag={p.BatteryFlag} BatteryLifePercent={p.BatteryLifePercent} (Flag 128=電池なし, Percent 255=不明)"
                : $"失敗: GetLastError={Marshal.GetLastWin32Error()}"));

        // 1 秒ごとに出す物。最初の 1 回は差を取るための空読み。
        var samplers = new (string Api, Func<string> Read)[]
        {
            ("cpu [GetSystemTimes]", CpuFromSystemTimes()),
            ("cpu [PerfCounter Processor Information\\% Processor Utility\\_Total]", Counter("Processor Information", "% Processor Utility", "_Total")),
            ("cpu [PerfCounter Processor\\% Processor Time\\_Total]", Counter("Processor", "% Processor Time", "_Total")),
            ("mem [GlobalMemoryStatusEx]", Memory),
            ("net [NetworkInterface.GetIPStatistics]", Network()),
            ("gpu [PerfCounter GPU Engine\\Utilization Percentage, engtype_3D の合計]", Gpu()),
        };
        foreach (var s in samplers)
        {
            Attempt.Read(s.Read);
        }

        for (var t = 1; t <= 5; t++)
        {
            Thread.Sleep(1000);
            Console.WriteLine($"--- {t} 秒");
            foreach (var s in samplers)
            {
                Console.WriteLine($"  {s.Api}: {Attempt.Read(s.Read)}");
            }
        }

        return 0;
    }

    private static Func<string> CpuFromSystemTimes()
    {
        ulong lastIdle = 0, lastTotal = 0;
        return () =>
        {
            if (!Native.GetSystemTimes(out var idle, out var kernel, out var user))
            {
                return $"失敗: GetLastError={Marshal.GetLastWin32Error()}";
            }

            // kernel の時間は idle を含む
            var total = kernel.Value + user.Value;
            var (dIdle, dTotal) = (idle.Value - lastIdle, total - lastTotal);
            (lastIdle, lastTotal) = (idle.Value, total);
            return dTotal == 0 ? "—" : $"{100.0 * (dTotal - dIdle) / dTotal:0.0} %";
        };
    }

    private static Func<string> Counter(string category, string counter, string instance)
    {
        PerformanceCounter? pc = null;
        return () => $"{(pc ??= new PerformanceCounter(category, counter, instance, readOnly: true)).NextValue():0.0} %";
    }

    private static string Memory()
    {
        var m = new Native.MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<Native.MEMORYSTATUSEX>() };
        return Native.GlobalMemoryStatusEx(ref m)
            ? $"使用 {(m.ullTotalPhys - m.ullAvailPhys) / 1e9:0.0}/{m.ullTotalPhys / 1e9:0.0} GB ({m.dwMemoryLoad} %)"
            : $"失敗: GetLastError={Marshal.GetLastWin32Error()}";
    }

    private static Func<string> Network()
    {
        long lastRx = 0, lastTx = 0;
        return () =>
        {
            var stats = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(n => n.GetIPStatistics()).ToList();
            var (rx, tx) = (stats.Sum(s => s.BytesReceived), stats.Sum(s => s.BytesSent));
            var text = $"受信 {(rx - lastRx) / 1e3:0.0} kB/s 送信 {(tx - lastTx) / 1e3:0.0} kB/s (インターフェース {stats.Count} 個の合計)";
            (lastRx, lastTx) = (rx, tx);
            return text;
        };
    }

    private static Func<string> Gpu()
    {
        List<PerformanceCounter>? counters = null;
        return () =>
        {
            counters ??= new PerformanceCounterCategory("GPU Engine").GetInstanceNames()
                .Where(n => n.Contains("engtype_3D", StringComparison.Ordinal))
                .Select(n => new PerformanceCounter("GPU Engine", "Utilization Percentage", n, readOnly: true)).ToList();
            var sum = 0f;
            var gone = 0;
            foreach (var c in counters)
            {
                try
                {
                    sum += c.NextValue();
                }
                catch (InvalidOperationException)
                {
                    gone++; // 測っている間に終わったプロセスのインスタンス
                }
            }

            return $"{sum:0.0} % (インスタンス {counters.Count} 個、うち消えた物 {gone} 個)";
        };
    }
}
