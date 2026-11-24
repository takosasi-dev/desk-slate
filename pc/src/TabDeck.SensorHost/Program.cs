// TabDeck.SensorHost の入口。管理者で動き、センサーの値を読むだけのプロセス(仕様書 INV-1・INV-2)。
// Phase 0 の版は --probe(ハードウェアとセンサーの一覧を標準出力に出して終了)だけを持つ。
// パイプへの書き出しとタスクの登録は、§8.3 が埋まった後の Phase 7 で足す。
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.PawnIo;

if (args is not ["--probe"])
{
    Console.Error.WriteLine("Phase 0 の版です。使えるのは --probe だけです(管理者の PowerShell から動かしてください)。");
    return 2;
}

Console.WriteLine($"LibreHardwareMonitorLib {typeof(Computer).Assembly.GetName().Version}");
Console.WriteLine($"PawnIO: IsInstalled={PawnIo.IsInstalled} Version={PawnIo.Version}");

// 温度とファンに要る 3 種だけを開く。ストレージやコントローラーは読まない。
var computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true, IsMotherboardEnabled = true };
computer.Open();
try
{
    // 負荷やファンは 2 回目の読みから値が出る物があるので、1 回空読みしてから出す
    Walk(computer.Hardware, 0, print: false);
    Thread.Sleep(1000);
    Walk(computer.Hardware, 0, print: true);
}
finally
{
    computer.Close();
}

return 0;

static void Walk(IEnumerable<IHardware> hardware, int depth, bool print)
{
    var indent = new string(' ', depth * 2);
    foreach (var hw in hardware)
    {
        hw.Update();
        if (print)
        {
            Console.WriteLine($"{indent}[{hw.HardwareType}] {hw.Name}  ({hw.Identifier})");
            foreach (var s in hw.Sensors)
            {
                Console.WriteLine($"{indent}  {s.SensorType} #{s.Index} {s.Name} = {s.Value?.ToString("0.##") ?? "—"}  ({s.Identifier})");
            }
        }

        Walk(hw.SubHardware, depth + 1, print);
    }
}
