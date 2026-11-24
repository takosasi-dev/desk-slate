// PadInput(0x90)のバイナリを読む(仕様書 §9.3)。整数はビッグエンディアン(接続の決まりのとおり)。
// 知らない kind、長さの合わない payload、範囲の外の値は null を返す。呼ぶ側が捨てて、ログに 1 行残す。
using System.Buffers.Binary;
using System.Text;

namespace DeskSlate.Core;

public abstract record PadEvent
{
    public sealed record Move(short Dx, short Dy) : PadEvent;

    public sealed record Scroll(short Dx, short Dy) : PadEvent;

    /// <param name="Which">1 = 左、2 = 右。</param>
    public sealed record Button(byte Which, bool Down) : PadEvent;

    public sealed record Click(byte Which) : PadEvent;

    /// <summary>テンキーのキーの名前。表にある名前かどうかは、ここでは見ない(無ければ後段が unknown_key で断る)。</summary>
    public sealed record NumpadKey(string Name) : PadEvent;

    public static PadEvent? Parse(ReadOnlySpan<byte> payload) => payload switch
    {
        [1, _, _, _, _] => new Move(BinaryPrimitives.ReadInt16BigEndian(payload[1..]), BinaryPrimitives.ReadInt16BigEndian(payload[3..])),
        [2, _, _, _, _] => new Scroll(BinaryPrimitives.ReadInt16BigEndian(payload[1..]), BinaryPrimitives.ReadInt16BigEndian(payload[3..])),
        [3, 1 or 2, 0 or 1] => new Button(payload[1], payload[2] == 1),
        [4, 1 or 2] => new Click(payload[1]),
        [5, var length, .. var name] when name.Length == length => new NumpadKey(Encoding.ASCII.GetString(name)),
        _ => null,
    };
}
