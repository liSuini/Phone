---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: 'eaabcaa7-0348-4e08-883d-e2a700d81d36'
  PropagateID: 'eaabcaa7-0348-4e08-883d-e2a700d81d36'
  ReservedCode1: '75e81d3d-32ce-4d60-94e8-2e1b82e2d196'
  ReservedCode2: '75e81d3d-32ce-4d60-94e8-2e1b82e2d196'
---

# 观看端 WebRTC 采用 Sipsorcery 库

.NET 生态成熟的 WebRTC 实现仅有 Sipsorcery 一家（PeerConnection、媒体轨道、DataChannel 完整支持），无真正的备选。半年后有人问"为什么不用 XXX"时答案在此：不是偏好，是唯一。视频渲染由 Sipsorcery 输出 I420 帧后自行处理（WriteableBitmap），音频经解码回调输出 PCM 交给 NAudio。

## Consequences

- 库质量与维护节奏绑定 Sipsorcery 社区；若其停更，迁移成本约等于重写观看端接收模块
- I420→RGB 转换性能需在 M2.4 阶段尽早压测，必要时换 SkiaSharp 或 D3D 渲染路径