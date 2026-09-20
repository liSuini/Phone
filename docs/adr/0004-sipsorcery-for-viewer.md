---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '710e43dd-1f55-442e-b96e-a40fcc71b2d0'
  PropagateID: '710e43dd-1f55-442e-b96e-a40fcc71b2d0'
  ReservedCode1: '5d437a80-3105-4ae7-8600-51193769408a'
  ReservedCode2: '5d437a80-3105-4ae7-8600-51193769408a'
---

# 观看端 WebRTC 采用 Sipsorcery 库

.NET 生态成熟的 WebRTC 实现仅有 Sipsorcery 一家（PeerConnection、媒体轨道、DataChannel 完整支持），无真正的备选。半年后有人问"为什么不用 XXX"时答案在此：不是偏好，是唯一。视频渲染由 Sipsorcery 输出 I420 帧后自行处理（WriteableBitmap），音频经解码回调输出 PCM 交给 NAudio。

> **版本事实（2026-09-20 编码期补充）**：项目锁定 SIPSorcery 10.0.16。10.x 相对 8.x 有两处破坏性变化，已在本仓库踩坑记录：
> 1. 命名空间从 `Sipsorcery.Net` 改为全大写 `SIPSorcery.Net`（编译错误 CS0246，且 C# 命名空间大小写敏感）
> 2. `createOffer/createAnswer/setLocalDescription/setRemoteDescription` 为同步方法；`createDataChannel` 为异步（Task<RTCDataChannel>）；DataChannel 消息委托为三参数 `(RTCDataChannel dc, DataChannelPayloadProtocols protocol, byte[] data)`，文本需自行按 UTF8 解码
> 升级版本时这三处是回归重点。

## Consequences

- 库质量与维护节奏绑定 Sipsorcery 社区；若其停更，迁移成本约等于重写观看端接收模块
- I420→RGB 转换性能需在 M2.4 阶段尽早压测，必要时换 SkiaSharp 或 D3D 渲染路径
- 本轮已验证：SIPSorcery DataChannel 自回环通过（offer 生成 + non-trickle candidates 内嵌 + 建连 + 控制消息双向互通），票据 04 的 C# 侧信令层风险已消化