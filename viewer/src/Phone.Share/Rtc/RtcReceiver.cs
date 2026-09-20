// 注意：SIPSorcery 10.x 命名空间为全大写 SIPSorcery.Net（历史 8.x 为 Sipsorcery.Net）；
// 且 createOffer/setLocalDescription/setRemoteDescription 均为同步方法，事件字段名为小写（onicecandidate）。
using SIPSorcery.Net;

namespace Phone.Share.Rtc;

/// <summary>
/// 票据 04（C# 侧）：Sipsorcery WebRTC 接收端——第一适配器（真实实现）。
/// 本版范围（如实标注）：SDP/ICE/DTLS 信令层 + DataChannel 控制通道；
/// 视频帧（OnVideoFrame）/音频帧（OnAudioPcm）解码管线待媒体解码器（FFmpeg）集成，暂不触发。
/// non-trickle：等 candidates 收集完成后拼入 SDP（ADR-0003）。
/// </summary>
public sealed class RtcReceiver : IRtcReceiver
{
    private const string ControlChannelLabel = "control";
    private static readonly TimeSpan GatherTimeout = TimeSpan.FromSeconds(3);

    private RTCPeerConnection? _pc;
    private RTCDataChannel? _control;

    public event Action<I420Frame>? OnVideoFrame;
    public event Action<PcmFrame>? OnAudioPcm;
    public event Action<RtcState>? OnStateChanged;

    public async Task<string> CreateOfferAsync()
    {
        _pc = new RTCPeerConnection(null); // 局域网：无需 STUN，host candidate 直连
        _pc.oniceconnectionstatechange += state => OnStateChanged?.Invoke(Map(state));
        _control = await _pc.createDataChannel(ControlChannelLabel);
        _control.onopen += () => OnStateChanged?.Invoke(RtcState.Connected);

        var offer = _pc.createOffer(null);
        _pc.setLocalDescription(offer);

        var fullSdp = WithCandidates(offer.sdp ?? string.Empty);
        OnStateChanged?.Invoke(RtcState.Connecting);
        return fullSdp;
    }

    public Task ConnectAsync(string answerSdp)
    {
        if (_pc is null)
        {
            throw new InvalidOperationException("必须先 CreateOfferAsync");
        }
        var answer = new RTCSessionDescriptionInit
        {
            type = RTCSdpType.answer,
            sdp = answerSdp,
        };
        var result = _pc.setRemoteDescription(answer);
        if (result != SetDescriptionResultEnum.OK)
        {
            OnStateChanged?.Invoke(RtcState.Failed);
            throw new InvalidOperationException($"setRemoteDescription 失败: {result}");
        }
        return Task.CompletedTask;
    }

    public bool SendControl(string json)
    {
        if (_control is null || _control.readyState != RTCDataChannelState.open)
        {
            return false;
        }
        _control.send(json);
        return true;
    }

    // ---- 内部 ----

    /// <summary>等待 ICE 收集完成并把候选行拼入 SDP（non-trickle，握手仅 2 次请求）。</summary>
    private string WithCandidates(string baseSdp)
    {
        var pc = _pc!;
        var lines = new List<string>();
        var done = new TaskCompletionSource<bool>();
        pc.onicecandidate += cand =>
        {
            if (cand != null && !string.IsNullOrEmpty(cand.candidate))
            {
                lock (lines) lines.Add("a=" + cand.candidate);
            }
        };
        pc.onicegatheringstatechange += _ =>
        {
            if (pc.iceGatheringState == RTCIceGatheringState.complete)
            {
                done.TrySetResult(true);
            }
        };
        if (pc.iceGatheringState == RTCIceGatheringState.complete)
        {
            done.TrySetResult(true);
        }
        done.Task.Wait(GatherTimeout); // 超时保底：不阻塞调用方超过 3 秒
        lock (lines)
        {
            return baseSdp.TrimEnd() + "\r\n" + string.Join("\r\n", lines) + (lines.Count > 0 ? "\r\n" : string.Empty);
        }
    }

    private static RtcState Map(RTCIceConnectionState state) => state switch
    {
        RTCIceConnectionState.connected => RtcState.Connected,
        RTCIceConnectionState.disconnected => RtcState.Disconnected,
        RTCIceConnectionState.failed or RTCIceConnectionState.closed => RtcState.Failed,
        _ => RtcState.Connecting,
    };
}
