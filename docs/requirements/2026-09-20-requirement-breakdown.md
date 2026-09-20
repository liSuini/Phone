---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: 'e9a87b51-9078-4d10-bb3d-fe4963cbb8cb'
  PropagateID: 'e9a87b51-9078-4d10-bb3d-fe4963cbb8cb'
  ReservedCode1: 'fd2bd318-8253-42dc-88b9-012ab954b02f'
  ReservedCode2: 'fd2bd318-8253-42dc-88b9-012ab954b02f'
---

# 需求拆分：手机屏幕共享助手

- 日期：2026-09-20
- 依据：`tasks/prd-phone-screen-share.md` v1.0

## 拆分原则

按"可独立交付的功能模块"拆分，双端（Android APP / C# 客户端）模块成对出现。每个模块标注优先级、依赖、预估工作量（人日，含联调）。

## 模块清单

### M1 信令与配对模块（P0）

| 子模块 | 端 | 内容 | 依赖 | 工作量 |
|--------|-----|------|------|--------|
| M1.1 信令服务器 | Android | NanoHTTPD：/info、/pair、/offer 接口，配对码生成与校验 | 无 | 2d |
| M1.2 二维码展示 | Android | ZXing 生成含 IP+端口+配对码的二维码 | M1.1 | 0.5d |
| M1.3 信令客户端 | C# | HttpClient 提交配对码与 SDP offer，解析 answer | M1.1 | 1d |
| M1.4 地址输入界面 | C# | 手输 IP 或扫码（ZXing.Net 摄像头） | M1.3 | 1d |

### M2 屏幕采集与视频传输模块（P0）

| 子模块 | 端 | 内容 | 依赖 | 工作量 |
|--------|-----|------|------|--------|
| M2.1 屏幕采集 | Android | MediaProjection 授权流程 + VirtualDisplay + 前台服务 | 无 | 2d |
| M2.2 WebRTC 推流端 | Android | PeerConnectionFactory、H.264 硬编、Offer/Answer、ICE | M2.1 | 3d |
| M2.3 WebRTC 接收端 | C# | Sipsorcery PeerConnection 建连、轨道订阅 | M1.3 | 2d |
| M2.4 视频渲染 | C# | I420→RGB 转换 + WriteableBitmap 帧调度 | M2.3 | 3d |

### M3 音频采集与播放模块（P0）

| 子模块 | 端 | 内容 | 依赖 | 工作量 |
|--------|-----|------|------|--------|
| M3.1 音频采集 | Android | AudioPlaybackCaptureConfiguration + AudioSource 接入 | M2.2 | 2d |
| M3.2 音频播放 | C# | Sipsorcery 音频轨道 + NAudio WasapiOut 环形缓冲 | M2.3 | 2d |

### M4 反向控制模块（P0 基础点击 / P1 滑动返回）

| 子模块 | 端 | 内容 | 依赖 | 工作量 |
|--------|-----|------|------|--------|
| M4.1 无障碍服务 | Android | AccessibilityService.dispatchGesture 点击/滑动注入 | 无 | 2d |
| M4.2 控制消息接收 | Android | DataChannel 监听 + 归一化坐标→像素映射 | M2.2, M4.1 | 1d |
| M4.3 控制消息发送 | C# | 鼠标事件 → 归一化坐标 JSON → DataChannel | M2.3 | 1d |
| M4.4 滑动与返回键 | 双端 | swipe/key 消息（P1） | M4.2, M4.3 | 1d |

### M5 连接管理与状态模块（P0）

| 子模块 | 端 | 内容 | 依赖 | 工作量 |
|--------|-----|------|------|--------|
| M5.1 采集生命周期 | Android | 锁屏/撤销授权 onStopped 检测与通知 | M2.1 | 1d |
| M5.2 断线检测重连 | C# | ICE 断连 10s 检测、提示、一键重连（P1 自动） | M2.3 | 2d |
| M5.3 设备信息展示 | C# | /info 数据展示（P1） | M1.1 | 0.5d |

### M6 权限引导与 UI 打磨模块（P0）

| 子模块 | 端 | 内容 | 依赖 | 工作量 |
|--------|-----|------|------|--------|
| M6.1 Android UI | Android | 权限三步引导、状态页、Material 3 | M2.1, M3.1, M4.1 | 2d |
| M6.2 C# 主界面 | C# | 视频区 + 状态栏 + 连接面板 MVVM | M2.4 | 2d |

## 依赖关系图

```
M1.1 ──► M1.2/M1.3/M1.4 ──┐
M2.1 ──► M2.2 ────────────┼──► M2.3 ──► M2.4 / M3.2 / M4.3 ──► M6.2
M4.1 ──► M4.2 ◄── M4.3    │
M3.1 ◄── M2.2             │
M5.x 依赖对应端核心模块    ┘
```

关键路径：**M2.1 → M2.2 → M2.3 → M2.4**（屏幕→显示的主链路），先打通它即达成 M1 里程碑。

## 里程碑映射

| 里程碑 | 包含模块 | 验收点 |
|--------|----------|--------|
| M1 跑通画面 | M1 全部 + M2.1~M2.4 + M6.2 基础 | 电脑看到手机画面 |
| M2 音画同步 | M3 全部 + M5.1 | 视频+声音同步播放 |
| M3 反向控制 | M4.1~M4.3 + M6.1 | 电脑点击手机响应 |
| M4 打磨交付 | M4.4 + M5.2 + M5.3 + M6.1 | PRD 验收标准全通过 |

## 工作量汇总

- P0 合计约 24.5 人日（不含联调缓冲，建议加 30%）
- P1 追加约 4.5 人日
- P2 二期另估

## 风险提示

1. **WebRTC 双端联调**是最大技术风险（Sipsorcery 与 Android 端 ICE/DTLS 兼容性），建议 M1 里程碑即做最小编解码链路验证
2. I420→RGB 渲染性能需尽早压测，避免 M2.4 后期返工
3. AudioPlaybackCapture 在国产 ROM（MIUI/ColorOS 等）行为差异需真机覆盖测试