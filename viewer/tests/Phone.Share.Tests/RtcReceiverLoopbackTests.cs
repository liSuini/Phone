using SIPSorcery.Net;
using Phone.Share.Rtc;
using Xunit;

namespace Phone.Share.Tests;

/// <summary>
/// 票据 04（C# 侧）：Sipsorcery DataChannel 自回环——本机双 pc 直连，无需网络与真机。
/// 验证三件事（票据 04 关键风险前置消化）：
/// 1. RtcReceiver 的 offer 生成与 non-trickle candidates 拼接被标准 WebRTC 端接受
/// 2. setRemoteDescription(answer) 建连成功
/// 3. DataChannel 控制消息双向互通
/// </summary>
public class RtcReceiverLoopbackTests
{
    [Fact]
    public async Task Offer含Candidates且DataChannel双向互通()
    {
        // ---- 观察端（被测）：生成含 candidates 的 offer ----
        var receiver = new RtcReceiver();
        var offerSdp = await receiver.CreateOfferAsync();

        Assert.Contains("m=application", offerSdp);                     // DataChannel 媒体行
        Assert.Contains("a=ice-ufrag", offerSdp);                        // SDP 完整性
        var candidateCount = offerSdp.Split('\n').Count(l => l.TrimStart().StartsWith("a=candidate"));
        Assert.True(candidateCount > 0, "non-trickle：offer 应内嵌 host candidates");

        // ---- 应答端（标准 Sipsorcery pc）：模拟共享端应答 ----
        var answerPc = new RTCPeerConnection(null);
        var tcs = new TaskCompletionSource<string>();
        answerPc.ondatachannel += dc =>
        {
            // OnDataChannelMessageDelegate(RTCDataChannel dc, DataChannelPayloadProtocols protocol, byte[] data)
            dc.onmessage += (channel, protocol, data) =>
                tcs.TrySetResult(System.Text.Encoding.UTF8.GetString(data));
        };

        var offerInit = new RTCSessionDescriptionInit { type = RTCSdpType.offer, sdp = offerSdp };
        Assert.Equal(SetDescriptionResultEnum.OK, answerPc.setRemoteDescription(offerInit));

        var answer = answerPc.createAnswer(null);
        answerPc.setLocalDescription(answer);

        // 应答端同样等待 candidates 收集完成后拼入（non-trickle 对称）
        var answerSdp = AwaitGathering(answerPc, answer.sdp ?? string.Empty);

        // ---- 观察端：应用 answer 建连 ----
        await receiver.ConnectAsync(answerSdp);

        // ---- 等待 DataChannel 打开并双向互通 ----
        var deadline = DateTime.UtcNow.AddSeconds(10);
        var sent = false;
        while (DateTime.UtcNow < deadline)
        {
            if (receiver.SendControl("""{"t":"tap","x":0.5,"y":0.5}"""))
            {
                sent = true;
                break;
            }
            await Task.Delay(200);
        }
        Assert.True(sent, "控制通道未在 10 秒内打开");

        var received = await Task.WhenAny(tcs.Task, Task.Delay(5000));
        Assert.Same(tcs.Task, received);
        Assert.Contains("\"t\":\"tap\"", tcs.Task.Result);
    }

    private static string AwaitGathering(RTCPeerConnection pc, string baseSdp)
    {
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
            if (pc.iceGatheringState == RTCIceGatheringState.complete) done.TrySetResult(true);
        };
        if (pc.iceGatheringState == RTCIceGatheringState.complete) done.TrySetResult(true);
        done.Task.Wait(3000);
        lock (lines)
        {
            return baseSdp.TrimEnd() + "\r\n" + string.Join("\r\n", lines) + (lines.Count > 0 ? "\r\n" : string.Empty);
        }
    }
}
