// 1 つの API を試して、値か、失敗の中身(例外の型と文)を文字で返す。
// Probe は「どの API が取れて、どれが取れないか」を出すのが仕事なので、ここだけは例外を広く受けて表示に変える。
namespace DeskSlate.Probe;

internal static class Attempt
{
    internal static string Read(Func<string> read)
    {
#pragma warning disable CA1031 // 握りつぶさず、失敗の中身をそのまま出力に出す
        try
        {
            return read();
        }
        catch (Exception ex)
        {
            return $"失敗: {ex.GetType().Name}: {ex.Message}";
        }
#pragma warning restore CA1031
    }
}
