// Probe が使う Win32 の宣言と、pid から exe 名(小文字)を引く小さな関数。
// 構造体の並びは Microsoft Learn の各 API のページのとおり(SendInput / INPUT / KEYBDINPUT / GetSystemTimes ほか)。
using System.Runtime.InteropServices;

namespace DeskSlate.Probe;

internal static class Native
{
    internal const uint EventSystemForeground = 0x0003;
    internal const uint InputKeyboard = 1;
    internal const uint KeyEventKeyUp = 0x0002;
    internal const uint KeyEventUnicode = 0x0004;
    private const uint ProcessQueryLimitedInformation = 0x1000;

    [StructLayout(LayoutKind.Sequential)]
    internal struct FILETIME
    {
        public uint Low, High;
        public readonly ulong Value => ((ulong)High << 32) | Low;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MEMORYSTATUSEX
    {
        public uint dwLength, dwMemoryLoad;
        public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag;
        public uint BatteryLifeTime, BatteryFullLifeTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public UIntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX, ptY;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData, dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort wVk, wScan;
        public uint dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    // 共用体はいちばん大きい MOUSEINPUT を入れておかないと、INPUT の大きさが足りなくなる。
    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    internal delegate void WinEventProc(IntPtr hook, uint evt, IntPtr hwnd, int idObject, int idChild, uint thread, uint time);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX status);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint pid);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageNameW(IntPtr process, uint flags, [Out] char[] name, ref uint size);

    [DllImport("user32.dll")]
    internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module, WinEventProc proc, uint pid, uint thread, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWinEvent(IntPtr hook);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PeekMessageW(out MSG msg, IntPtr hwnd, uint min, uint max, uint remove);

    [DllImport("user32.dll")]
    internal static extern IntPtr DispatchMessageW(ref MSG msg);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(uint count, INPUT[] inputs, int size);

    /// <summary>pid の exe 名を小文字で返す。開けないプロセス(保護されたプロセスなど)は null(仕様書 D-17 の「不明」)。</summary>
    internal static string? ExeNameOf(uint pid)
    {
        var handle = OpenProcess(ProcessQueryLimitedInformation, false, pid);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var buffer = new char[1024];
            var size = (uint)buffer.Length;
            return QueryFullProcessImageNameW(handle, 0, buffer, ref size)
                ? Path.GetFileName(new string(buffer, 0, (int)size)).ToLowerInvariant()
                : null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    /// <summary>前面のウィンドウの exe 名。取れなければ「不明」。</summary>
    internal static string ForegroundExe()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return "不明(前面のウィンドウなし)";
        }

        GetWindowThreadProcessId(hwnd, out var pid);
        return ExeNameOf(pid) ?? $"不明(pid={pid})";
    }
}
