---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '1163b050-c2dc-4421-b2f2-07b8b52c38ec'
  PropagateID: '1163b050-c2dc-4421-b2f2-07b8b52c38ec'
  ReservedCode1: '94ad9f67-44ce-4180-8071-f1a38a5d1e7b'
  ReservedCode2: '94ad9f67-44ce-4180-8071-f1a38a5d1e7b'
---

# 架构设计文档：手机屏幕共享助手

- 日期：2026-09-20
- 依据：`tasks/prd-phone-screen-share.md`、`docs/requirements/2026-09-20-requirement-breakdown.md`、`CONTEXT.md`、`docs/adr/0001~0004`
- 术语：遵循 codebase-design 词汇——模块（Module）、接口（Interface）、缝（Seam）、适配器（Adapter）、深度（Depth）

## 1. 总体形态

双端独立交付物，一个仓库三个模块包：

```
Phone/
├─ android/          共享端 Sharer（Kotlin APP）
├─ viewer/           观看端 Viewer（C# WPF，.NET 8）
└─ protocol/         跨端契约：信令报文与控制消息的 Schema + 双端各自生成/解析实现
```

`protocol` 是唯一的跨端共享模块（纯数据契约，零依赖），双端各自实现解析器——一个契约、两个适配器，天然防漂移。

## 2. 共享端（Android）模块设计

### M-S1 ScreenSource（屏幕采集）

- **接口**：`fun start(w: Int, h: Int): Surface`；`fun stop()`；`val onRevoked: callback`
- **深度来源**：调用者只拿一块 Surface 去喂 WebRTC；系统授权弹窗、`mediaProjection` 前台服务、VirtualDisplay 建销、用户撤销（onRevoked→触发主动停止语义）全部藏在实现里
- **缝**：`MediaSource` 适配器位——真实适配器是 ScreenSource，测试适配器是合成帧的 FakeVideoSource，两者满足同一接口

### M-S2 AudioCapture（声音捕获）

- **接口**：`fun start(): CallbackFlow<PcmFrame>`；`val silentByPolicy: StateFlow<Boolean>`
- **深度来源**：AudioPlaybackCaptureConfiguration、采样率协商、音源拒绝采集（静默但可观测，喂给 UI 状态栏）全部内置
- **错误模式**：`silentByPolicy=true` 表示音源 APP 禁止采集，非故障不重试

### M-S3 RtcSession（WebRTC 会话）

- **接口**：`createOffer(videoSrc, audioSrc): Sdp`；`acceptAnswer(sdp)`；`sendControl(msg)`；`onControl: Flow<ControlMessage>`；`close()`；`state: Flow<SessionState>`
- **深度来源**：这是共享端最深的模块——PeerConnectionFactory、H.264 硬编协商、Opus、ICE 收集（含 non-trickle 等待完成）、DTLS、DataChannel 开关全部内部化。调用方看不到 WebRTC 任何类名
- **不变量**：`acceptAnswer` 必须在 `createOffer` 之后；`sendControl` 在 DataChannel 开启前调用返回 false（不抛异常）

### M-S4 SignalingServer（信令服务）

- **接口**：`start(port)`；`onPair: callback(code)`；`onOffer: callback(sdp) -> answer`
- **实现**：NanoHTTPD，路由 GET /info、POST /pair、POST /offer；配对码生成（4 位，15 分钟失效）
- **删除测试**：删掉它，配对与 SDP 交换逻辑会在 RtcSession 与 UI 各处重现——earns its keep

### M-S5 GestureInjector（手势注入）

- **接口**：`injectTap(x: Float, y: Float)`；`injectSwipe(x0,y0,x1,y1,dur)`；`pressBack()`；`isAvailable(): Boolean`
- **坐标语义**：入参即归一化坐标（CONTEXT.md 定义），像素换算内聚于此，输入换算一处改、处处改（locality）
- **缝**：`isAvailable()==false`（无障碍未授权）时 UI 降级提示，不阻断观看——PRD F08 的实现落点

### M-S6 ShareCoordinator（共享会话协调器）

- **接口**：`beginShare(): Flow<GrantStep>`（权限引导流）；`startSession()`；`endSession(reason: Termination)`
- 职责：编排 M-S1~S5 的生命周期与三授权引导；唯一知道全部模块的模块
- `Termination` 枚举区分主动停止（Stop）与断连（Disconnect），对应 CONTEXT.md 术语，驱动不同 UI 与重连策略

## 3. 观看端（C#）模块设计

### M-V1 SignalingClient

- **接口**：`GetInfo(ip, port)`；`Pair(ip, port, code)`；`ExchangeOffer(sdp) -> answer`
- 纯 HTTP，返回结果不产生副作用，直接可测

### M-V2 RtcReceiver

- **接口**：`Connect(answer)`；`OnVideoFrame: event<I420Frame>`；`OnAudioPcm: event<PcmFrame>`；`SendControl(msg)`；`OnStateChanged: event<RtcState>`
- **缝**：视频/音频输出是事件流（帧回调），渲染与播放模块订阅；测试注入合成帧即可测下游全链路，无需真机
- **深度来源**：Sipsorcery 全部细节止步于此（ADR-0004）；帧格式归一为内部 `I420Frame`/`PcmFrame`，下游不感知 WebRTC

### M-V3 VideoRenderer

- **接口**：`Attach(Image control)`；`Write(I420Frame)`
- **缝**：`IFrameSink` 适配器位——第一适配器 WriteableBitmap 实现；若压测不达标（>15% CPU），第二适配器换 SkiaSharp/D3D，接口与上游零改动
- **性能特征**（接口的一部分）：Write 必须非阻塞，帧排队丢弃策略（保新弃旧）内聚于实现

### M-V4 AudioPlayer

- **接口**：`Open(sampleRate, channels)`；`Write(PcmFrame)`；`Close()`
- 环形缓冲与欠载处理（插值补偿）内置；欠载率暴露给状态栏

### M-V5 SessionManager（状态机）

- **状态**：`Idle → Pairing → Connecting → Sharing → (Reconnecting|Stopping) → Idle`
- **接口**：`StartAsync(ip, code)`；`Stop()`；`State: IObservable<SessionState>`
- 10s ICE 断连检测与一键重连（P1 自动重连）策略内聚于此；以 `IRtcReceiver` 接口注入，测试用 Fake 走状态机全路径
- 断连（Disconnect）→ Reconnecting；主动停止（Stop）→ Stopping——不重连，语义对齐 CONTEXT.md

### M-V6 App Shell（MVVM）

ViewModel 只依赖 M-V1~V5 的接口；MainWindow = 连接面板 + 视频区（Image 控件）+ 状态栏。无业务逻辑。

## 4. 关键数据流

```
[共享端] MediaProjection ─Surface─► RtcSession ─SRTP─►
                                              ◄─DataChannel─ 控制消息
[观看端] RtcReceiver ─I420Frame─► VideoRenderer ─► Image 控件
                  └─PcmFrame──► AudioPlayer  ─► 扬声器
SessionManager ─状态事件─► ViewModel ─► UI
```

## 5. 缝与适配器总表

| 缝 | 适配器一 | 适配器二（真实存在） |
|----|----------|----------------------|
| MediaSource（S3 输入） | ScreenSource+AudioCapture | FakeVideoSource/FakeAudioSource（测试） |
| IFrameSink（V2→V3） | WriteableBitmap 渲染 | SkiaSharp/D3D 渲染（性能后备） |
| IRtcReceiver（V5 依赖） | Sipsorcery 真实现 | FakeReceiver（状态机测试） |
| 控制消息契约（protocol） | Kotlin 解析器 | C# 解析器 |

> 只在确有两个适配器处设缝：GestureInjector、SignalingServer 均单一实现、单一用途，不设抽象接口（防止为假想变化付深度税）。

## 6. 测试面规划

- **单元**（xUnit / JUnit）：坐标归一化↔像素（M-S5 纯函数）、控制消息编解码（protocol 双端对拍）、SessionManager 状态机（Fake RtcReceiver）、VideoRenderer 帧转换（合成帧）
- **集成**：信令握手（M-V1 ↔ M-S4 本机对跑）
- **真机验收**：按 PRD 第 6 节 7 条验收标准执行，重点覆盖国产 ROM 差异（MIUI/ColorOS 音频捕获行为）

## 7. 不做的事（YAGNI）

- 不做多观看端并发（ADR-0003 已定，P2 重估）
- 不做公网穿透（二期 TURN）
- 不做录屏保存（P2）
- 不设多余抽象层：凡单一实现处，接口即具体类型