// 既定の再生デバイスと音のセッションを NAudio で読み、指定した exe の音量を 1 回変えて戻せるかを確かめる(仕様書 §8.1、R-2)。
// API の名前は NAudio.Wasapi 3.3.0 の実物(NAudio.CoreAudioApi)から引いた。
using NAudio.CoreAudioApi;

namespace DeskSlate.Probe;

internal static class AudioProbe
{
    internal static int Run(string[] args)
    {
        Console.WriteLine($"API: {typeof(MMDeviceEnumerator).Assembly.GetName().Name} {typeof(MMDeviceEnumerator).Assembly.GetName().Version}");
        Console.WriteLine(Attempt.Read(() => args is ["--set", var exe, var percent] ? Set(exe, percent) : List()));
        return 0;
    }

    private static string List()
    {
        using var enumerator = new MMDeviceEnumerator();
        if (!enumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
        {
            return "既定の再生デバイス: なし";
        }

        using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        Console.WriteLine($"既定の再生デバイス(Render / Multimedia): {device.FriendlyName}");
        var endpoint = device.AudioEndpointVolume;
        Console.WriteLine($"全体(AudioEndpointVolume): MasterVolumeLevelScalar={endpoint.MasterVolumeLevelScalar:0.00} Mute={endpoint.Mute}");

        var sessions = device.AudioSessionManager.Sessions;
        Console.WriteLine($"セッション(AudioSessionManager.Sessions): {sessions.Count} 個");
        for (var i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            // セッションごとに試す。管理者で動くアプリのセッションだけ読めない、という場合を 1 行ずつ見分けるため。
            Console.WriteLine($"  [{i}] " + Attempt.Read(() =>
                $"pid={session.GetProcessID} exe={Native.ExeNameOf(session.GetProcessID) ?? "不明"} system={session.IsSystemSoundsSession} state={session.State} " +
                $"SimpleAudioVolume.Volume={session.SimpleAudioVolume.Volume:0.00} Mute={session.SimpleAudioVolume.Mute} peak={session.AudioMeterInformation.MasterPeakValue:0.00}"));
        }

        return "audio: 一覧を出しました";
    }

    private static string Set(string exe, string percentText)
    {
        if (!int.TryParse(percentText, out var percent) || percent is < 0 or > 100)
        {
            return "音量は 0〜100 の整数で指定してください";
        }

        using var enumerator = new MMDeviceEnumerator();
        using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        var sessions = device.AudioSessionManager.Sessions;
        var targets = Enumerable.Range(0, sessions.Count).Select(i => sessions[i])
            .Where(s => string.Equals(Native.ExeNameOf(s.GetProcessID), exe, StringComparison.OrdinalIgnoreCase)).ToList();
        if (targets.Count == 0)
        {
            return $"{exe} のセッションがありません(audio で一覧を見てください)";
        }

        Console.Write($"{exe} のセッション {targets.Count} 個の音量を {percent}% に 2 秒だけ変えて、元に戻します。続けますか [y/N]: ");
        if (Console.ReadLine()?.Trim().ToLowerInvariant() != "y")
        {
            return "やめました";
        }

        var before = targets.Select(s => s.SimpleAudioVolume.Volume).ToList();
        try
        {
            foreach (var s in targets)
            {
                s.SimpleAudioVolume.Volume = percent / 100f;
            }

            Console.WriteLine("変えた直後の読み直し: " + string.Join(", ", targets.Select(s => s.SimpleAudioVolume.Volume.ToString("0.00"))));
            Thread.Sleep(2000);
        }
        finally
        {
            for (var i = 0; i < targets.Count; i++)
            {
                targets[i].SimpleAudioVolume.Volume = before[i];
            }
        }

        return "戻した後の読み直し: " + string.Join(", ", targets.Select(s => s.SimpleAudioVolume.Volume.ToString("0.00")));
    }
}
