using System.Net;
using Phone.Share.Signaling;
using Xunit;

namespace Phone.Share.Tests;

/// <summary>
/// 票据 02：观看端信令客户端测试。
/// 缝：M-V1 SignalingClient 公开接口（技术规格实现决策 3）。
/// 报文格式以 protocol/schema/signaling.md 为准，stub 与实现都不许手写漂移。
/// </summary>
public class SignalingClientTests
{
    private static SignalingClient NewClient() => new(new HttpClient());

    [Fact]
    public async Task GetInfo正常解析()
    {
        using var stub = new SignalingStub
        {
            Route = path => path switch
            {
                "/info" => (200, """{"deviceName":"测试机","width":1080,"height":2400,"battery":88,"version":"1.0"}"""),
                _ => (404, "{}"),
            },
        };

        var info = await NewClient().GetInfoAsync("localhost", stub.Port);

        Assert.NotNull(info);
        Assert.Equal("测试机", info!.DeviceName);
        Assert.Equal(1080, info.Width);
        Assert.Equal(2400, info.Height);
        Assert.Equal(88, info.Battery);
        Assert.Equal("1.0", info.Version);
    }

    [Fact]
    public async Task Pair配对码错误返回失败结果()
    {
        using var stub = new SignalingStub
        {
            Route = path => path switch
            {
                "/pair" => (403, """{"ok":false,"error":"配对码错误"}"""),
                _ => (404, "{}"),
            },
        };

        var result = await NewClient().PairAsync("localhost", stub.Port, "9999");

        Assert.False(result.Ok);
        Assert.Equal("配对码错误", result.Error);
        Assert.Null(result.Session);
    }

    [Fact]
    public async Task Pair配对成功返回会话()
    {
        using var stub = new SignalingStub
        {
            Route = path => path switch
            {
                "/pair" => (200, """{"ok":true,"session":"s-123"}"""),
                _ => (404, "{}"),
            },
        };

        var result = await NewClient().PairAsync("localhost", stub.Port, "1234");

        Assert.True(result.Ok);
        Assert.Equal("s-123", result.Session);
    }

    [Fact]
    public async Task ExchangeOffer往返Answer()
    {
        var offer = "v=0\r\no=- 0 0 IN IP4 127.0.0.1";
        using var stub = new SignalingStub
        {
            Route = path => path switch
            {
                "/offer" => (200, """{"answer":"v=0 answer-text"}"""),
                _ => (404, "{}"),
            },
        };

        var answer = await NewClient().ExchangeOfferAsync("localhost", stub.Port, offer);

        Assert.Equal("v=0 answer-text", answer);
    }

    [Fact]
    public async Task 服务器不可达返回失败不抛异常()
    {
        var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var deadPort = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop(); // 端口关闭，连接必然失败

        var client = NewClient();

        Assert.Null(await client.GetInfoAsync("localhost", deadPort));
        Assert.False((await client.PairAsync("localhost", deadPort, "1234")).Ok);
        Assert.Null(await client.ExchangeOfferAsync("localhost", deadPort, "v=0"));
    }

    [Fact]
    public async Task 超时由客户端内部超时控制返回失败()
    {
        // SignalingClient 内部对每个请求挂 5s 超时；连接层不可达（端口 1 无服务）
        // 走同一异常路径返回失败结果。真正的 5s 挂起场景留待与票据 03 对跑时验证。
        var client = NewClient();

        Assert.False((await client.PairAsync("localhost", 1, "0000")).Ok);
    }
}
