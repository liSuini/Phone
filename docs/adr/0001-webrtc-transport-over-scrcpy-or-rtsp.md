---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '833bf18c-9b83-4306-b548-e56a2e0d43a0'
  PropagateID: '833bf18c-9b83-4306-b548-e56a2e0d43a0'
  ReservedCode1: 'e144493f-83f1-4749-9cc9-dd9f8263511f'
  ReservedCode2: 'e144493f-83f1-4749-9cc9-dd9f8263511f'
---

# 传输采用完全自研 WebRTC，而非 scrcpy 封装或 RTSP+LibVLC

需求核心是音画同步质量，备选方案中 scrcpy 封装工作量最小（免开发手机 APP）、RTSP+LibVLC 开发难度最低。决定完全自研：Android 端 MediaProjection + WebRTC 推流，C# 端自研接收，以换取全链路自主可控与后续深度定制空间，并接受约 5~10 倍于封装方案的工作量与 WebRTC 双端联调风险。音画同步由 WebRTC 内置 RTP 时间戳机制保证，局域网内 host candidate 直连、无需公网 STUN/TURN。

## Considered Options

- scrcpy 封装：工作量 20%，体验最佳，但放弃自研与定制空间（用户明确不选）
- RTSP + LibVLC：难度最低、同步开箱即用，延迟 100~300ms（用户明确不选）
- 自定义裸协议：同步与丢包恢复需全部自建，风险最高，排除

## Consequences

- 手机端必须开发并安装 APP；系统声音采集受 Android 10+ 与音源 APP 策略限制
- 局域网预期延迟 50~150ms；跨公网场景留待二期引入 TURN