// 30 秒の間、前面のウィンドウの切り替わりを SetWinEventHook で受けて、exe 名(小文字)と一緒に出す(仕様書 §8.1、D-17)。
// 管理者で動くアプリや保護されたプロセスが前面に来たときに「不明」になるかを、動かした人が見る。
using System.Diagnostics;

namespace TabDeck.Probe;

internal static class ForegroundProbe
{
    internal static int Run()
    {
        var watch = Stopwatch.StartNew();
        // 変数に持って、フックが生きている間に GC で回収されないようにする
        Native.WinEventProc proc = (_, _, hwnd, _, _, _, _) =>
        {
            Native.GetWindowThreadProcessId(hwnd, out var pid);
            Console.WriteLine($"{watch.Elapsed.TotalSeconds,5:0.0} 秒  exe={Native.ExeNameOf(pid) ?? "不明"}  pid={pid}");
        };

        var hook = Native.SetWinEventHook(Native.EventSystemForeground, Native.EventSystemForeground, IntPtr.Zero, proc, 0, 0, 0 /* WINEVENT_OUTOFCONTEXT */);
        if (hook == IntPtr.Zero)
        {
            Console.WriteLine("失敗: SetWinEventHook が 0 を返しました");
            return 1;
        }

        Console.WriteLine($"API: SetWinEventHook(EVENT_SYSTEM_FOREGROUND, WINEVENT_OUTOFCONTEXT) + QueryFullProcessImageNameW。今の前面: {Native.ForegroundExe()}");
        Console.WriteLine("30 秒の間、ウィンドウを切り替えてください(管理者で開いたアプリも 1 回前面にしてください)。");
        try
        {
            // 知らせは、このスレッドがメッセージを汲んだときに届く
            while (watch.Elapsed < TimeSpan.FromSeconds(30))
            {
                while (Native.PeekMessageW(out var msg, IntPtr.Zero, 0, 0, 1 /* PM_REMOVE */))
                {
                    Native.DispatchMessageW(ref msg);
                }

                Thread.Sleep(20);
            }
        }
        finally
        {
            Native.UnhookWinEvent(hook);
            GC.KeepAlive(proc);
        }

        Console.WriteLine("foreground: 終わりました");
        return 0;
    }
}
