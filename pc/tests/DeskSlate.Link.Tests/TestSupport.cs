// テストの道具: 一時フォルダ、偽の時計、port 0 で立てるサーバー、C# で書いた偽の端末(TLS でつなぎ、枠を読み書きする)。
// 実物の %APPDATA%\DeskSlate と 47816 には触らない。
// TabSpan のテスト(takosasi-dev/tab-span b656208)から写した(仕様書 D-2・§8.9)。コメントとテスト名の FR・INV・AC の番号は、断りが無ければ TabSpan の仕様書の物(DeskSlate の AC-2 が指す)。
using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using DeskSlate.Core;

namespace DeskSlate.Link.Tests;

internal sealed class TempDir : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("deskslate-test-").FullName;

    public void Dispose() => Directory.Delete(Path, recursive: true);
}

internal sealed class FakeClock : IClock
{
    public long NowUs { get; set; }

    public DateTimeOffset UtcNow { get; set; } = new(2026, 11, 24, 3, 0, 0, TimeSpan.Zero);
}

internal sealed class Harness : IAsyncDisposable
{
    private readonly TempDir _dir = new();

    private Harness(LinkOptions options, Func<LinkSession, CancellationToken, Task>? onSession, IClock clock)
    {
        Identity = Identity.LoadOrCreate(System.IO.Path.Combine(_dir.Path, "identity.bin"), clock).Identity;
        Throttle = new AuthThrottle(clock);
        Server = new LinkServer(options, Identity, Registry, () => Saves++, Throttle, clock, Logs.Enqueue, onSession ?? ((_, ct) => Task.Delay(Timeout.Infinite, ct)));
        Server.Start();
    }

    public Identity Identity { get; }
    public DeviceRegistry Registry { get; } = new([]);
    public AuthThrottle Throttle { get; }
    public LinkServer Server { get; }
    public ConcurrentQueue<string> Logs { get; } = new();
    public int Saves { get; private set; }

    public static Harness Start(Func<LinkSession, CancellationToken, Task>? onSession = null, LinkOptions? options = null, IClock? clock = null) =>
        new((options ?? new LinkOptions()) with { Port = 0 }, onSession, clock ?? SystemClock.Instance);

    public Task<FakeDevice> ConnectAsync() => FakeDevice.ConnectAsync(Server.Port);

    /// <summary>台帳に端末を 1 台入れ、その token を返す。</summary>
    public byte[] AddDevice(string deviceId)
    {
        var token = RandomNumberGenerator.GetBytes(32);
        Registry.Upsert(deviceId, "偽の端末", token, DateTimeOffset.UtcNow);
        return token;
    }

    public async ValueTask DisposeAsync()
    {
        await Server.DisposeAsync();
        Identity.Dispose();
        _dir.Dispose();
    }
}

internal sealed class FakeDevice : IAsyncDisposable
{
    private readonly TcpClient _client;

    private FakeDevice(TcpClient client, SslStream stream, byte[] certDer)
    {
        _client = client;
        Stream = stream;
        CertDer = certDer;
    }

    public SslStream Stream { get; }

    /// <summary>PC の証明書の DER(ペアリングの 6 桁の計算に使う)。</summary>
    public byte[] CertDer { get; }

    public static async Task<FakeDevice> ConnectAsync(int port)
    {
        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        byte[]? der = null;
        var ssl = new SslStream(client.GetStream(), false);
        await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = "localhost",
            EnabledSslProtocols = SslProtocols.Tls12,
            RemoteCertificateValidationCallback = (_, cert, _, _) =>
            {
                der = cert!.GetRawCertData();
                return true;
            },
        });
        return new FakeDevice(client, ssl, der!);
    }

    public Task SendAsync(MessageType type, byte[] payload) => FrameCodec.WriteAsync(Stream, (byte)type, payload, CancellationToken.None).AsTask();

    public Task SendJsonAsync<T>(MessageType type, T msg) => SendAsync(type, Messages.ToJson(msg));

    /// <summary>枠を 1 つ読む。閉じられたら null(切断の例外も null にする)。</summary>
    public async Task<Frame?> ReadAsync(int timeoutMs = 5000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            return await FrameCodec.ReadAsync(Stream, cts.Token);
        }
        catch (IOException)
        {
            return null;
        }
    }

    public async Task<T> ReadJsonAsync<T>(MessageType expected)
    {
        var f = await ReadAsync() ?? throw new Xunit.Sdk.XunitException($"{expected} を待っていたが閉じられた");
        Assert.Equal((byte)expected, f.Type);
        return Messages.FromJson<T>(f.Payload.Span);
    }

    public static HelloMsg Hello(string deviceId, byte[]? token, int version = 1, string? app = Messages.App) =>
        new(version, app, deviceId, token is null ? null : Convert.ToBase64String(token), "偽の端末", "0.0.1", new ScreenInfo(1280, 800, 213));

    public async ValueTask DisposeAsync()
    {
        await Stream.DisposeAsync();
        _client.Dispose();
    }
}
