namespace Phone.Share.Rtc;

/// <summary>
/// I420（4:2:0）视频帧（票据 04，M-V2 帧归一：WebRTC 细节止步于 RtcReceiver，下游只见此类型）。
/// Y = w*h；U = V = ((w+1)/2)*((h+1)/2)。
/// </summary>
public sealed record I420Frame(int Width, int Height, byte[] Y, byte[] U, byte[] V, long TimestampUs)
{
    public int UWidth => (Width + 1) / 2;

    public int UHeight => (Height + 1) / 2;

    /// <summary>测试/假源工厂：填充纯色平面（票据 04 合成彩条的基础）。</summary>
    public static I420Frame CreateFilled(int width, int height, byte y, byte u, byte v, long timestampUs = 0)
    {
        var uw = (width + 1) / 2;
        var uh = (height + 1) / 2;
        var yPlane = new byte[width * height];
        Array.Fill(yPlane, y);
        var uPlane = new byte[uw * uh];
        Array.Fill(uPlane, u);
        var vPlane = new byte[uw * uh];
        Array.Fill(vPlane, v);
        return new I420Frame(width, height, yPlane, uPlane, vPlane, timestampUs);
    }
}

/// <summary>48kHz 音频帧（M-V2 帧归一）。</summary>
public sealed record PcmFrame(int SampleRate, int Channels, short[] Samples, long TimestampUs = 0);
