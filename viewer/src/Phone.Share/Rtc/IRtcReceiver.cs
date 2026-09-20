namespace Phone.Share.Rtc;

/// <summary>WebRTC 接收端状态（M-V2）。</summary>
public enum RtcState
{
    New,
    Connecting,
    Connected,
    Disconnected,
    Failed,
}

/// <summary>
/// 观看端 WebRTC 接收缝（票据 04 定义，票据 07/08/12 消费）。
/// 适配器一：Sipsorcery 真实现；适配器二：FakeRtcReceiver（测试）。
/// </summary>
public interface IRtcReceiver
{
    /// <summary>生成本地 offer（含全部 ICE candidates，non-trickle，见 ADR-0003）。</summary>
    Task<string> CreateOfferAsync();

    /// <summary>应用远端 answer，开始建连。</summary>
    Task ConnectAsync(string answerSdp);

    /// <summary>视频帧输出（I420，归一化帧类型）。</summary>
    event Action<I420Frame>? OnVideoFrame;

    /// <summary>音频帧输出（PCM）。</summary>
    event Action<PcmFrame>? OnAudioPcm;

    /// <summary>发送控制消息。DataChannel 未开启时返回 false，不抛异常（架构设计 M-S3 不变量同款）。</summary>
    bool SendControl(string json);

    /// <summary>连接状态变化事件。</summary>
    event Action<RtcState>? OnStateChanged;
}
