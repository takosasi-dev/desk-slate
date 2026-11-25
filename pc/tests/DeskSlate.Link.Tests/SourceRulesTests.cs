// ソースと参照の決まり(DeskSlate の仕様書の番号): HTTP と WebSocket のクライアントは Monitors と Quake のフォルダの外に無い(AC-38・INV-8)、
// DeskSlate.Link が DeskSlate.Platform を参照しない(AC-40 の後半・INV-14)。TabSpan の同じ名前のテストを DeskSlate の決まりに合わせて書き直した。
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace DeskSlate.Link.Tests;

public sealed partial class SourceRulesTests
{
    [GeneratedRegex(@"\b(HttpClient|WebClient|HttpWebRequest|ClientWebSocket)\b")]
    private static partial Regex NetClients();

    private static string SrcDir([CallerFilePath] string here = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "src"));

    private static bool Under(string file, string folder) =>
        file.Contains($"{Path.DirectorySeparatorChar}{folder}{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    [Fact]
    public void AC38_HTTP_と_WebSocket_のクライアントは_Monitors_と_Quake_の外に無い()
    {
        var files = Directory.EnumerateFiles(SrcDir(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !Under(f, "obj") && !Under(f, "bin") && !Under(f, "Monitors") && !Under(f, "Quake"))
            .ToList();
        Assert.NotEmpty(files);
        Assert.DoesNotContain(files, f => NetClients().IsMatch(File.ReadAllText(f)));
    }

    [Fact]
    public void AC40_DeskSlate_Link_は_DeskSlate_Platform_を参照しない()
    {
        Assert.DoesNotContain(typeof(LinkServer).Assembly.GetReferencedAssemblies(), a => a.Name == "DeskSlate.Platform");
        Assert.DoesNotContain("DeskSlate.Platform.csproj", File.ReadAllText(Path.Combine(SrcDir(), "DeskSlate.Link", "DeskSlate.Link.csproj")), StringComparison.Ordinal);
    }
}
