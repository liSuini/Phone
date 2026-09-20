using System.Collections.Concurrent;

namespace Phone.Share.Rtc;

/// <summary>
/// IRtcReceiver 的测试适配器（票据 04）：可编程状态序列、合成帧推送、控制消息记录。
/// 行为契约与 Sipsorcery 真实现一致是整个测试基建的生命线——差异即测试失真。
/// </summary>
public sealed class FakeRtcReceiver : IRtcReceiver
{
    public event Action<I420Frame>? OnVideoFrame;
    public event Action<PcmFrame>? OnAudioPcm;
    public event Action<RtcState>? OnStateChanged;

    /// <summary>测试观察用：已发出的全部控制消息。</summary>
    public ConcurrentQueue<string> SentControls { get; } = new();

    /// <summary>对拍用：收到的控制消息（模拟手机端回声）。</summary>
    public event Action<string>? OnControlReceived;

    private RtcState _state = RtcState.New;

    public Task<string> CreateOfferAsync()
    {
        SetState(RtcState.Connecting);
        return Task.FromResult("fake-offer-sdp");
    }

    public Task ConnectAsync(string answerSdp)
    {
        SetState(RtcState.Connected);
        return Task.CompletedTask;
    }

    public bool SendControl(string json)
    {
        if (_state != RtcState.Connected)
        {
            return false;
        }
        SentControls.Enqueue(json);
        OnControlReceived?.Invoke(json);
        return true;
    }

    public void FireVideoFrame(I420Frame frame) => OnVideoFrame?.Invoke(frame);

    public void FireAudioPcm(PcmFrame pcm) => OnAudioPcm?.Invoke(pcm);

    public void FireStateChanged(RtcState state) => SetState(state);

    private void SetState(RtcState next)
    {
        _state = next;
        OnStateChanged?.Invoke(next);
    }
}
