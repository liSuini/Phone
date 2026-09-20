using System.Net;
using System.Net.Http;
using Phone.Share.Signaling;
using Xunit;

namespace Phone.Share.Tests;

/// <summary>
/// 票据 03 双端本机对拍：C# SignalingClient（票据 02）↔ Kotlin SignalingServer（票据 03）。
/// 依赖 protocol/parity/run-parity.ps1 先启动对拍服务器（默认 127.0.0.1:18099，码 1234）。
/// 单跑 dotnet test 请用 --filter "Category!=Parity" 排除。
/// </summary>
[Trait("Category", "Parity")]
public class ParityTests
{
    private static int Port =>
        int.TryParse(Environment.GetEnvironmentVariable("PARITY_PORT"), out var p) ? p : 18099;

    private static string Code => Environment.GetEnvironmentVariable("PARITY_CODE") ?? "1234";

    private static SignalingClient NewClient() => new(new HttpClient());

    [Fact]
    public async Task 对拍_GetInfo返回Kotlin服务端设备信息()
    {
        var info = await NewClient().GetInfoAsync("localhost", Port);
        Assert.NotNull(info);
        Assert.Equal("对拍机", info!.DeviceName);
        Assert.Equal(1080, info.Width);
        Assert.Equal(2400, info.Height);
        Assert.Equal(88, info.Battery);
        Assert.Equal("1.0-test", info.Version);
    }

    [Fact]
    public async Task 对拍_正确配对码获得会话()
    {
        var result = await NewClient().PairAsync("localhost", Port, Code);
        Assert.True(result.Ok);
        Assert.NotNull(result.Session);
    }

    [Fact]
    public async Task 对拍_错误配对码被403拒绝()
    {
        var result = await NewClient().PairAsync("localhost", Port, "9999");
        Assert.False(result.Ok);
        Assert.Equal("配对码错误", result.Error);
    }

    [Fact]
    public async Task 对拍_Offer交换获得Answer回显()
    {
        const string offer = "v=0\r\no=- 0 0 IN IP4 127.0.0.1\r\ns=parity";
        var answer = await NewClient().ExchangeOfferAsync("localhost", Port, offer);
        Assert.Equal("answer::" + offer, answer);
    }
}
