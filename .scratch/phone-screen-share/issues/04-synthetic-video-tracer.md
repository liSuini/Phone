---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '1a6812d0-869a-429f-aaff-a1f52313c9c1'
  PropagateID: '1a6812d0-869a-429f-aaff-a1f52313c9c1'
  ReservedCode1: 'ccf19f3b-c552-4c30-a1fe-2a37d1da9d00'
  ReservedCode2: 'ccf19f3b-c552-4c30-a1fe-2a37d1da9d00'
---

# 04: 合成画面纵贯线（WebRTC 双端最小子弹）

**What to build:** 打通最细的端到端纵贯线：共享端用合成视频假源（彩条动画）经真实 WebRTC 链路推流，观看端完成信令握手、建连、接收，并在窗口中渲染出该合成画面。这是最早暴露 WebRTC 双端联调风险的切片（Sipsorcery ↔ Android WebRTC 的 ICE/DTLS 兼容性）。

**Blocked by:** 01, 02, 03

**Status:** ready-for-agent

**实现细节:** 见开发计划 Task 5/6/7（假源、RtcSession 推流端、RtcReceiver 接收端、VideoRenderer 渲染）

- [ ] 共享端合成视频源：1080p 彩条动画帧，满足 MediaSource 缝接口（真源可替换）
- [ ] 共享端 WebRTC 推流会话：createOffer 返回含全部 candidates 的 SDP（non-trittle），acceptAnswer 建连
- [ ] 观看端 IRtcReceiver：CreateOffer/Connect 建连成功，OnVideoFrame 输出归一 I420Frame，DataChannel SendControl 可用（未开时返回 false）
- [ ] 观察端视频渲染：YuvConverter 单元测试通过；WriteableBitmap 渲染合成帧；写入 Write 非阻塞、保新弃旧
- [ ] Sipsorcery 自回环测试通过（无需网络的编解码环验证）
- [ ] **真机联调门禁**：电脑窗口实时看到手机假源彩条动画，延迟目测 <300ms
- [ ] 联调问题记录（若有：candidate 打包/DTLS 指纹排查过程）
- [ ] 全部测试通过并提交