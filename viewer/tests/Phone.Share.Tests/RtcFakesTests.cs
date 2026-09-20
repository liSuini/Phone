using Phone.Share.Rtc;
using Xunit;

namespace Phone.Share.Tests;

/// <summary>
/// 票据 04：帧类型与 FakeRtcReceiver 测试（观看端测试基建，技术规格 M-V2 缝）。
/// Fake 是 IRtcReceiver 缝的第二适配器（测试用），与 Sipsorcery 真实现满足同一接口。
/// </summary>
public class RtcFakesTests
{
    // ---- I420Frame 契约 ----

    [Fact]
    public void I420帧平面尺寸符合规范()
    {
        var f = new I420Frame(1920, 1080, new byte[1920 * 1080], new byte[480 * 270 * 4 / 4 * 4], new byte[0], 0);
        // 4:2:0：Y = w*h；U = V = w*h/4
        Assert.Equal(1920 * 1080, f.Y.Length);
    }

    [Fact]
    public void 创建纯色帧工厂方法填充全部平面()
    {
        var f = I420Frame.CreateFilled(64, 64, y: 200, u: 128, v: 128);
        Assert.All(f.Y, b => Assert.Equal(200, b));
        Assert.All(f.U, b => Assert.Equal(128, b));
        Assert.All(f.V, b => Assert.Equal(128, b));
        Assert.Equal(64 * 64, f.Y.Length);
        Assert.Equal(64 * 64 / 4, f.U.Length);
        Assert.Equal(64 * 64 / 4, f.V.Length);
    }

    // ---- PcmFrame 契约 ----

    [Fact]
    public void Pcm帧携带采样参数与样本()
    {
        var f = new PcmFrame(48000, 2, new short[] { 100, -100 });
        Assert.Equal(48000, f.SampleRate);
        Assert.Equal(2, f.Channels);
        Assert.Equal(2, f.Samples.Length);
    }

    // ---- FakeRtcReceiver 契约（与真实现行为一致是测试基建的生命线） ----

    [Fact]
    public async Task Fake生成Offer与回放状态序列()
    {
        var fake = new FakeRtcReceiver();
        var states = new List<RtcState>();
        fake.OnStateChanged += states.Add;

        var offer = await fake.CreateOfferAsync();
        Assert.Equal("fake-offer-sdp", offer);

        await fake.ConnectAsync("fake-answer-sdp");

        Assert.Equal(new[] { RtcState.Connecting, RtcState.Connected }, states);
    }

    [Fact]
    public void Fake未连接时SendControl返回false()
    {
        var fake = new FakeRtcReceiver();
        Assert.False(fake.SendControl("""{"t":"tap","x":0.5,"y":0.5}"""));
    }

    [Fact]
    public async Task Fake连接后SendControl记录消息并触发OnControl()
    {
        var fake = new FakeRtcReceiver();
        await fake.ConnectAsync("answer");
        string? echoed = null;
        fake.OnControlReceived += m => echoed = m;

        Assert.True(fake.SendControl("ctrl-1"));
        Assert.True(fake.SendControl("ctrl-2"));
        Assert.Equal(new[] { "ctrl-1", "ctrl-2" }, fake.SentControls);
        Assert.Equal("ctrl-2", echoed);
    }

    [Fact]
    public async Task Fake推送合成视频帧与音频帧()
    {
        var fake = new FakeRtcReceiver();
        var frames = new List<I420Frame>();
        var pcms = new List<PcmFrame>();
        fake.OnVideoFrame += frames.Add;
        fake.OnAudioPcm += pcms.Add;
        await fake.ConnectAsync("answer");

        fake.FireVideoFrame(I420Frame.CreateFilled(32, 32, 16, 128, 128));
        fake.FireAudioPcm(new PcmFrame(48000, 1, new short[480]));

        Assert.Single(frames);
        Assert.Single(pcms);
    }

    [Fact]
    public async Task Fake可模拟断连与失败()
    {
        var fake = new FakeRtcReceiver();
        var states = new List<RtcState>();
        fake.OnStateChanged += states.Add;
        await fake.CreateOfferAsync();
        await fake.ConnectAsync("answer");

        fake.FireStateChanged(RtcState.Disconnected);
        fake.FireStateChanged(RtcState.Failed);

        Assert.Equal(new[] { RtcState.Connecting, RtcState.Connected, RtcState.Disconnected, RtcState.Failed }, states);
        Assert.False(fake.SendControl("x")); // 断连后控制通道关闭
    }
}
