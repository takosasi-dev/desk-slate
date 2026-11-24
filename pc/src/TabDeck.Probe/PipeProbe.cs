// 自分の SID だけに読みを許した名前付きパイプ(サーバーは書くだけ)を作り、同じ利用者からつないで 1 行読めるかを試す(仕様書 §8.1、§9.9)。
// SID そのものは出力に出さない(出力を貼った先に残さないため)。
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace TabDeck.Probe;

internal static class PipeProbe
{
    internal static int Run()
    {
        Console.WriteLine("API: NamedPipeServerStreamAcl.Create(PipeDirection.Out, PipeSecurity) / NamedPipeClientStream(PipeDirection.In)");
        Console.WriteLine(Attempt.Read(() =>
        {
            var sid = WindowsIdentity.GetCurrent().User ?? throw new InvalidOperationException("利用者の SID を取れません");
            var name = $"TabDeck.Probe.{sid.Value}";
            var security = new PipeSecurity();
            security.AddAccessRule(new PipeAccessRule(sid, PipeAccessRights.Read | PipeAccessRights.Synchronize, AccessControlType.Allow));

            using var server = NamedPipeServerStreamAcl.Create(name, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, security);
            Console.WriteLine("作成: OK (名前 TabDeck.Probe.<SID>、許可は自分の SID の Read だけ)");

            var waiting = server.WaitForConnectionAsync();
            using var client = new NamedPipeClientStream(".", name, PipeDirection.In);
            client.Connect(2000);
            waiting.Wait(2000);
            Console.WriteLine("接続: OK");

            using var writer = new StreamWriter(server) { AutoFlush = true };
            writer.WriteLine("""{"probe":"ok"}""");
            using var reader = new StreamReader(client);
            return $"読めた 1 行: {reader.ReadLine()}";
        }));
        return 0;
    }
}
