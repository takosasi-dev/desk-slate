// 5 秒待ってから、前面のウィンドウに 0〜9 を仮想キーで、ABC-123 を KEYEVENTF_UNICODE で打つ(仕様書 §8.1、D-10)。
// キーを注入するので、自動では動かさず、人が手で実行する。打つ前に確認を求める。
using System.Runtime.InteropServices;

namespace TabDeck.Probe;

internal static class KeysProbe
{
    internal static int Run()
    {
        // shortcut: x64 だけを確かめている。x86 や arm64 で動かすなら、この大きさ(40)を見直す
        if (Marshal.SizeOf<Native.INPUT>() != 40)
        {
            Console.WriteLine($"失敗: INPUT の大きさが {Marshal.SizeOf<Native.INPUT>()} です(x64 では 40 のはず)。宣言を直してください");
            return 1;
        }

        Console.Write("5 秒後に、前面のウィンドウへ 0123456789 と ABC-123 を打ちます。メモ帳を開いて前面にしてください。続けますか [y/N]: ");
        if (Console.ReadLine()?.Trim().ToLowerInvariant() != "y")
        {
            Console.WriteLine("やめました");
            return 0;
        }

        for (var i = 5; i > 0; i--)
        {
            Console.Write($"{i} ");
            Thread.Sleep(1000);
        }

        Console.WriteLine();
        Console.WriteLine($"打つ直前の前面: {Native.ForegroundExe()}");

        // 数字の仮想キーは VK_0(0x30)〜VK_9(0x39)で、文字コードと同じ値(Microsoft Learn「Virtual-Key Codes」)
        var sentVk = "0123456789".Sum(c => Tap(new Native.KEYBDINPUT { wVk = c }));
        // KEYEVENTF_UNICODE は wVk を 0、wScan に文字を入れる(Microsoft Learn「KEYBDINPUT」)
        var sentUnicode = "ABC-123".Sum(c => Tap(new Native.KEYBDINPUT { wScan = c, dwFlags = Native.KeyEventUnicode }));

        Console.WriteLine($"SendInput が受け取った入力の数: 仮想キー {sentVk}/20、Unicode {sentUnicode}/14");
        Console.WriteLine("メモ帳に 0123456789ABC-123 と入ったかを見てください。管理者で開いたウィンドウには届きません(届かなかったことは、この数字からは分かりません)。");
        return 0;
    }

    /// <summary>1 つのキーを押して離す。SendInput が受け取った入力の数(2 が正常)を返す。</summary>
    private static int Tap(Native.KEYBDINPUT key)
    {
        var up = key;
        up.dwFlags |= Native.KeyEventKeyUp;
        var inputs = new[]
        {
            new Native.INPUT { type = Native.InputKeyboard, U = new Native.InputUnion { ki = key } },
            new Native.INPUT { type = Native.InputKeyboard, U = new Native.InputUnion { ki = up } },
        };
        var sent = (int)Native.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Native.INPUT>());
        Thread.Sleep(2);
        return sent;
    }
}
