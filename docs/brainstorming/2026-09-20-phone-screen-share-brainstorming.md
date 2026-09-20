---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '2e454765-4dc4-4a4a-9f55-20ef5fd0af72'
  PropagateID: '2e454765-4dc4-4a4a-9f55-20ef5fd0af72'
  ReservedCode1: '9850e0ad-18da-462b-9f22-4628af1dc1d7'
  ReservedCode2: '9850e0ad-18da-462b-9f22-4628af1dc1d7'
---

# 头脑风暴记录：手机屏幕共享助手

- 日期：2026-09-20
- 参与者：李鸣武（需求方）、TeleAgent（协助设计）
- 仓库：github.com/liSuini/Phone

## 1. 背景与目标

用户需要将 Android 手机的屏幕画面与声音实时同步到电脑上，主要用途是观看手机上的视频/直播内容，同时支持在电脑端用鼠标对手机做基础控制（点击等），连接方式为 WiFi 无线。

## 2. 需求澄清记录

| 提问 | 结论 |
|------|------|
| 手机平台？ | Android |
| 主要用途？ | 手机影音到电脑（音画同步为最高优先级） |
| 是否需要反向控制？ | 需要基础控制（鼠标点击） |
| 连接方式？ | WiFi 无线 |
| 电脑端形态？ | 桌面客户端（C# WPF） |
| 自研程度？ | 完全自研（Android APP + C# 客户端），不基于 scrcpy 封装 |
| 传输方案？ | WebRTC（延迟最低 50~100ms） |

## 3. 方案对比

| 方案 | 说明 | 结论 |
|------|------|------|
| A：scrcpy 封装 | 免开发手机 APP，音画同步最佳，工作量最小 | 未选择（用户希望完全自研自主可控） |
| B：完全自研（MediaProjection + WebRTC + C# 客户端） | 全链路自控，可深度定制，可学习价值高 | **选定** |
| C：DLNA/流媒体推送 | 只能推媒体内容，不能完整镜像屏幕 | 排除 |

传输方案对比（在 B 方案内二次决策）：

| 传输方案 | 延迟 | 开发难度 | 结论 |
|----------|------|----------|------|
| WebRTC | 50~100ms | 高（双端联调复杂） | **选定** |
| RTSP + LibVLC | 100~300ms | 低 | 未选 |
| 自定义裸协议 | 不可控 | 最高 | 排除 |

## 4. 关键设计决策

1. **WebRTC 作为音视频传输**：音画同步由 RTP 时间戳机制内置保证，局域网内 host candidate 直连，无需 STUN/TURN 公网服务器。
2. **信令自建**：APP 内嵌 NanoHTTPD 轻量 HTTP 服务器，二维码（IP+端口+配对码）完成配对，non-trickle ICE 一次性交换 SDP+candidates，握手仅 2 次请求。
3. **反向控制走 DataChannel**：归一化坐标（0~1）JSON 消息 → Android 无障碍服务 `dispatchGesture` 注入点击，不依赖 adb。
4. **音频采集用 AudioPlaybackCapture**（Android 10+ 硬性门槛），仅能采集允许录制的媒体类声音。
5. **C# 端技术栈**：.NET 8 + Sipsorcery（WebRTC）+ NAudio（音频）+ WriteableBitmap（视频渲染）。
6. **Android 端技术栈**：Kotlin + stream-webrtc-android（Google WebRTC 官方库封装）+ NanoHTTPD + ZXing + Material 3。

## 5. 系统架构

```
Android APP (Kotlin)                 WiFi 局域网            C# WPF 客户端 (.NET 8)
├─ MediaProjection 屏幕采集  ──视频(H.264硬编)──►  Sipsorcery 接收 → 视频渲染
├─ AudioPlaybackCapture 音频 ──音频(Opus)──────►  Sipsorcery 接收 → NAudio 播放
├─ NanoHTTPD 信令服务        ◄──HTTP(SDP/ICE)───  HttpClient 信令客户端
├─ AccessibilityService      ◄──DataChannel────  控制消息发送（归一化坐标）
└─ ZXing 二维码配对展示         ──────────────►   扫码/手输 IP 连接
```

三条链路：
- 信令链路（HTTP，一次性）：配对 → SDP offer/answer 交换
- 媒体链路（WebRTC SRTP）：视频 + 音频，音画同步内置
- 控制链路（DataChannel）：tap / swipe / key 消息

## 6. 风险与约束（已向用户明示）

- 手机要求 Android 10+（AudioPlaybackCapture 硬性要求）
- 部分 APP 可禁止被采集音频（系统限制，无解）
- 反向控制需开启无障碍服务；不开启则纯观看
- 采集需 mediaProjection 类型前台服务常驻
- WiFi 质量影响延迟与稳定性（预期局域网 50~150ms）