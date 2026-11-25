// 置き場の読み書き(identity、台帳、ログ FR-28。設定は DeskSlate.Core の SettingsTests)と、adb の引数(INV-10)・見回り(FR-1・FR-7)・探し方(FR-29)を確かめる。
// adb は偽の runner だけを使い、本物は呼ばない。
// TabSpan のテスト(takosasi-dev/tab-span b656208)から写した(仕様書 D-2・§8.9)。コメントとテスト名の FR・INV・AC の番号は、断りが無ければ TabSpan の仕様書の物(DeskSlate の AC-2 が指す)。
using System.Text;
using DeskSlate.Core;

namespace DeskSlate.Link.Tests;

public sealed class StoreTests
{
    [Fact]
    public void Identity_is_kept_and_a_broken_file_is_replaced()
    {
        using var dir = new TempDir();
        var clock = new FakeClock();
        var path = Path.Combine(dir.Path, "identity.bin");
        var (first, created, _) = Identity.LoadOrCreate(path, clock);
        using (first)
        {
            Assert.True(created);
            Assert.True(first.Certificate.HasPrivateKey);
            var (again, createdAgain, _) = Identity.LoadOrCreate(path, clock);
            using (again)
            {
                Assert.False(createdAgain);
                Assert.Equal(first.PcInstanceId, again.PcInstanceId);
                Assert.Equal(first.CertDer, again.CertDer);
            }
        }

        File.WriteAllBytes(path, [1, 2, 3]);
        var (fresh, recreated, brokenCopy) = Identity.LoadOrCreate(path, clock);
        using (fresh)
        {
            Assert.True(recreated);
            Assert.NotNull(brokenCopy);
            Assert.True(File.Exists(brokenCopy));
        }
    }

    [Fact]
    public void Devices_round_trip_and_broken()
    {
        using var dir = new TempDir();
        var clock = new FakeClock();
        var path = Path.Combine(dir.Path, "devices.json");
        var registry = new DeviceRegistry([]);
        registry.Upsert("dev-1", "dtab d-01J", new byte[32], clock.UtcNow);
        DeviceStore.Save(path, registry.Entries);
        Assert.Contains("\"tokenSha256\"", File.ReadAllText(path), StringComparison.Ordinal);
        var (entries, broken) = DeviceStore.Load(path, clock);
        Assert.Null(broken);
        Assert.Equal(registry.Entries, entries);

        File.WriteAllText(path, "[{]");
        (entries, broken) = DeviceStore.Load(path, clock);
        Assert.Empty(entries);
        Assert.NotNull(broken);
    }

    [Fact]
    public void Log_rotates_at_5_MB_and_keeps_14_days()
    {
        using var dir = new TempDir();
        var clock = new FakeClock { UtcNow = new DateTimeOffset(2026, 11, 24, 3, 0, 0, TimeSpan.Zero) };
        var log = new FileLog(dir.Path, clock);
        var today = clock.UtcNow.ToLocalTime();
        File.WriteAllBytes(Path.Combine(dir.Path, $"deskslate-{today:yyyyMMdd}.log"), new byte[FileLog.MaxBytes]);
        log.Write("次の行");
        Assert.Equal(2, Directory.GetFiles(dir.Path, $"deskslate-{today:yyyyMMdd}*.log").Length);

        var old15 = Path.Combine(dir.Path, $"deskslate-{today.AddDays(-15):yyyyMMdd}.log");
        var old14 = Path.Combine(dir.Path, $"deskslate-{today.AddDays(-14):yyyyMMdd}.log");
        File.WriteAllText(old15, "x");
        File.WriteAllText(old14, "x");
        log.DeleteOld();
        Assert.False(File.Exists(old15));
        Assert.True(File.Exists(old14));
    }
}

public sealed class AdbTests
{
    private sealed class FakeRunner(Func<string, IReadOnlyList<string>, AdbResult> answer) : IAdbRunner
    {
        public List<(string Path, string[] Args)> Calls { get; } = [];

        public Task<AdbResult> RunAsync(string adbPath, IReadOnlyList<string> args, CancellationToken ct)
        {
            lock (Calls)
            {
                Calls.Add((adbPath, [.. args]));
            }

            return Task.FromResult(answer(adbPath, args));
        }
    }

    [Fact]
    public void Commands_have_the_table_shapes_and_the_serial_stays_one_argument()
    {
        const string serial = "AB 12;&rm -rf";
        Assert.Equal(["version"], AdbCommands.Version());
        Assert.Equal(["devices"], AdbCommands.Devices());
        Assert.Equal(["-s", serial, "reverse", "tcp:47816", "tcp:47816"], AdbCommands.Reverse(serial, 47816));
        Assert.Equal(["-s", serial, "reverse", "--remove", "tcp:47816"], AdbCommands.ReverseRemove(serial, 47816));
        Assert.Equal(["-s", serial, "shell", "am", "start", "-n", "io.github.takosasidev.deskslate/.MainActivity"], AdbCommands.AmStart(serial));
    }

    [Fact]
    public async Task Watcher_reverses_once_launches_once_and_removes_at_exit()
    {
        const string serial = "DE 12;&x";
        var devicesOutput = $"List of devices attached\r\n{serial}\tdevice\r\nUN99\tunauthorized\r\n\r\n";
        var runner = new FakeRunner((_, args) => args[0] == "devices" ? new AdbResult(0, devicesOutput) : new AdbResult(0, ""));
        var logs = new List<string>();
        var watcher = new AdbWatcher(runner, "adb.exe", 47816, () => true, logs.Add);

        await watcher.PollOnceAsync(CancellationToken.None);
        await watcher.PollOnceAsync(CancellationToken.None);
        Assert.Equal([serial], watcher.Reversed);
        Assert.Equal(["UN99"], watcher.Unauthorized);

        devicesOutput = "List of devices attached\r\n\r\n"; // 抜いた
        await watcher.PollOnceAsync(CancellationToken.None);
        Assert.Empty(watcher.Reversed);
        devicesOutput = $"List of devices attached\r\n{serial}\tdevice\r\n"; // 挿し直した
        await watcher.PollOnceAsync(CancellationToken.None);
        await watcher.RemoveAllAsync(CancellationToken.None);

        string[][] expected =
        [
            ["devices"], AdbCommands.Reverse(serial, 47816), AdbCommands.AmStart(serial),
            ["devices"],
            ["devices"],
            ["devices"], AdbCommands.Reverse(serial, 47816), AdbCommands.AmStart(serial),
            AdbCommands.ReverseRemove(serial, 47816),
        ];
        Assert.Equal(expected, runner.Calls.Select(c => c.Args));
        Assert.DoesNotContain(logs, l => l.Contains(serial, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Watcher_reports_three_reverse_failures()
    {
        var runner = new FakeRunner((_, args) => args[0] == "devices" ? new AdbResult(0, "List of devices attached\nX1\tdevice\n") : new AdbResult(1, "error"));
        var watcher = new AdbWatcher(runner, "adb.exe", 47816, () => false, _ => { });
        for (var i = 0; i < 3; i++)
        {
            Assert.False(watcher.ReverseFailing);
            await watcher.PollOnceAsync(CancellationToken.None);
        }

        Assert.True(watcher.ReverseFailing);
        Assert.DoesNotContain(runner.Calls, c => c.Args.Contains("am"));
    }

    [Fact]
    public async Task Locator_order_and_skipping()
    {
        using var dir = new TempDir();
        string Make(string sub)
        {
            var p = Path.Combine(dir.Path, sub, "adb.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(p)!);
            File.WriteAllText(p, "");
            return p;
        }

        var configured = Make("configured");
        var onPath = Make("onpath");
        var local = Path.Combine(dir.Path, "local");
        var older = Make(@"local\Microsoft\WinGet\Packages\Google.PlatformTools_A\platform-tools");
        var newer = Make(@"local\Microsoft\WinGet\Packages\Google.PlatformTools_B\platform-tools");
        File.SetLastWriteTimeUtc(older, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(newer, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var path = $@"C:\nowhere;""{Path.GetDirectoryName(onPath)}""";

        Assert.Equal([configured, onPath, newer, older], AdbLocator.Candidates(configured, path, local));

        var failing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { configured, onPath };
        var runner = new FakeRunner((p, _) => new AdbResult(failing.Contains(p) ? 1 : 0, ""));
        Assert.Equal(newer, await AdbLocator.FindAsync(configured, path, local, runner, CancellationToken.None));
        Assert.All(runner.Calls, c => Assert.Equal(["version"], c.Args));

        failing.Add(newer);
        failing.Add(older);
        Assert.Null(await AdbLocator.FindAsync(configured, path, local, runner, CancellationToken.None));
        Assert.Null(await AdbLocator.FindAsync("", null, Path.Combine(dir.Path, "none"), runner, CancellationToken.None));
    }

    [Fact]
    public void Parse_devices_matches_the_real_output_shape()
    {
        var parsed = AdbWatcher.ParseDevices(Encoding.UTF8.GetString("List of devices attached\r\nDE0123\tdevice\r\n\r\n"u8));
        Assert.Equal([("DE0123", "device")], parsed);
    }
}
