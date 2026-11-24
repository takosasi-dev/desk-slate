// 自分の SID だけに読みを許した名前付きパイプ(サーバーは書くだけ)を作り、同じ利用者からつないで 1 行読めるかを試す(仕様書 §8.1、§9.9)。
// SID そのものは出力に出さない(出力を貼った先に残さないため)。
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace DeskSlate.Probe;

internal static class PipeProbe
{
    internal static int Run()
    {
        Console.WriteLine("API: NamedPipeServerStreamAcl.Create(PipeDirection.Out, PipeSecurity) / NamedPipeClientStream(PipeDirection.In)");
        Console.WriteLine(Attempt.Read(() =>
        {
            var sid = WindowsIdentity.GetCurrent().User ?? throw new InvalidOperationException("利用者の SID を取れません");
            var name = $"DeskSlate.Probe.{sid.Value}";
            var security = new PipeSecurity();
            security.AddAccessRule(new PipeAccessRule(sid, PipeAccessRights.Read | PipeAccessRights.Synchronize, AccessControlType.Allow));

            using var server = NamedPipeServerStreamAcl.Create(name, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 4096, security); // 出力バッファが 0 だと、相手が読むまで書き込みが返らない
            Console.WriteLine("作成: OK (名前 DeskSlate.Probe.<SID>、許可は自分の SID の Read だけ)");

            var waiting = server.WaitForConnectionAsync();
            using var client = new NamedPipeClientStream(".", name, PipeDirection.In);
            client.Connect(2000);
            if (!waiting.Wait(2000))
            {
                return "失敗: サーバー側が 2 秒待っても接続を受け取れませんでした";
            }

            Console.WriteLine("接続: OK");

            using var writer = new StreamWriter(server) { AutoFlush = true };
            writer.WriteLine("""{"probe":"ok"}""");
            using var reader = new StreamReader(client);
            return $"読めた 1 行: {reader.ReadLine()}";
        }));
        return 0;
    }
}
