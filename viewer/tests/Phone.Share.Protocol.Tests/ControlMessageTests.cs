using System.Text.Json;
using Phone.Share.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Phone.Share.Protocol.Tests;

/// <summary>
/// 票据 01：控制消息契约测试（与 Kotlin 端 ControlMessageTest 同构）。
/// 缝：protocol 契约缝（技术规格 Testing Decisions 第 4 缝）。
/// 样例文件是共同真理之源，对拍用例必须读 protocol/samples/ 而非手写副本。
/// </summary>
public class ControlMessageTests
{
    private static readonly string SamplesDir = Path.Combine(AppContext.BaseDirectory, "samples");

    private static string Sample(string name) =>
        File.ReadAllText(Path.GetFullPath(Path.Combine(SamplesDir, name))).Trim();

    // ---- 基础解析 ----

    [Fact]
    public void Tap消息解析()
    {
        var m = ControlMessage.Parse("""{"t":"tap","x":0.5,"y":0.3}""");
        Assert.NotNull(m);
        Assert.Equal("tap", m.T);
        Assert.Equal(0.5, m.X!.Value, 9);
        Assert.Equal(0.3, m.Y!.Value, 9);
    }

    [Fact]
    public void Swipe消息解析()
    {
        var m = ControlMessage.Parse("""{"t":"swipe","x0":0.1,"y0":0.2,"x1":0.8,"y1":0.9,"dur":300}""");
        Assert.NotNull(m);
        Assert.Equal("swipe", m.T);
        Assert.Equal(0.1, m.X0!.Value, 9);
        Assert.Equal(0.9, m.Y1!.Value, 9);
        Assert.Equal(300, m.Dur);
    }

    [Fact]
    public void Key消息解析()
    {
        var m = ControlMessage.Parse("""{"t":"key","k":"back"}""");
        Assert.NotNull(m);
        Assert.Equal("back", m.K);
    }

    [Fact]
    public void 非法输入返回Null() => Assert.Null(ControlMessage.Parse("not-json"));

    [Fact]
    public void 未知类型返回Null() => Assert.Null(ControlMessage.Parse("""{"t":"volume"}"""));

    [Fact]
    public void Tap缺坐标返回Null() => Assert.Null(ControlMessage.Parse("""{"t":"tap"}"""));

    // ---- 对拍：样例全集 ----

    [Fact]
    public void 合法样例全部可解析并往返一致()
    {
        foreach (var name in new[] { "01-tap.json", "02-swipe.json", "03-key-back.json" })
        {
            var m = ControlMessage.Parse(Sample(name)) ?? throw new Exception($"样例 {name} 解析失败");
            var round = ControlMessage.Parse(m.ToJson()) ?? throw new Exception($"样例 {name} 往返失败");
            Assert.Equal(m.T, round.T);
            Assert.Equal(m.X, round.X);
            Assert.Equal(m.Y, round.Y);
            Assert.Equal(m.X0, round.X0);
            Assert.Equal(m.Y0, round.Y0);
            Assert.Equal(m.X1, round.X1);
            Assert.Equal(m.Y1, round.Y1);
            Assert.Equal(m.Dur, round.Dur);
            Assert.Equal(m.K, round.K);
        }
    }

    [Fact]
    public void 非法样例全部返回Null()
    {
        foreach (var name in new[]
                 {
                     "04-invalid-not-json.json",
                     "05-invalid-unknown-type.json",
                     "06-invalid-missing-coords.json",
                 })
        {
            Assert.Null(ControlMessage.Parse(Sample(name)));
        }
    }

    // ---- 序列化规则 ----

    [Fact]
    public void ToJson只输出非Null字段且顺序固定()
    {
        var m = new ControlMessage("tap", 0.5, 0.3, null, null, null, null, null, null);
        Assert.Equal("""{"t":"tap","x":0.5,"y":0.3}""", m.ToJson());
    }

    [Fact]
    public void 未知字段解析时被忽略()
    {
        var m = ControlMessage.Parse("""{"t":"tap","x":0.25,"y":0.75,"future":"x"}""");
        Assert.NotNull(m);
        Assert.Equal(0.25, m.X!.Value, 9);
        Assert.Equal(0.75, m.Y!.Value, 9);
    }
}
