---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '8f1af351-daec-4aff-8b99-5f1e2f0e9177'
  PropagateID: '8f1af351-daec-4aff-8b99-5f1e2f0e9177'
  ReservedCode1: '6f17ca8d-06b2-44a1-8683-af693ac0b689'
  ReservedCode2: '6f17ca8d-06b2-44a1-8683-af693ac0b689'
---

# 技术规格：手机屏幕共享助手

- 日期：2026-09-20
- 依据：PRD v1.0、需求拆分、架构设计文档（已确认）
- 状态：待评审
- 说明：因环境限制（api.github.com 不可用），本规格落盘于 docs/specs/ 而非发布至 GitHub Issues

## Problem Statement

我在电脑前工作时，手机上播放的视频/直播内容无法方便地在电脑大屏上观看：手机屏幕小、声音外放打扰他人、带耳机又隔绝环境。现有工具（scrcpy 等）需要数据线或 adb 依赖，且我希望能完全掌控工具的形态与功能演进。

## Solution

一套完全自研的无线屏幕共享工具：手机 APP（共享端）与电脑客户端（观看端）经同一 WiFi 直连，屏幕画面与声音实时同步到电脑（WebRTC，音画同步内置），鼠标点击画面即可反向操作手机。二维码配对，全程无需数据线与外部服务器。

## User Stories

**连接与配对**
1. 作为用户，我想打开手机 APP 即看到连接二维码，以便电脑端扫码建立共享
2. 作为用户，我想在电脑端手动输入 IP 与配对码连接，以便摄像头不可用时仍可配对
3. 作为用户，我想在电脑端扫码前看到设备名，以便确认连接的是我的手机
4. 作为用户，我输错配对码时希望得到明确拒绝提示，以防他人误连
5. 作为用户，我想配对码 15 分钟自动失效，以降低被蹭连风险

**观看与聆听**
6. 作为用户，我想在电脑窗口里流畅看到手机屏幕的一切内容，以便观看视频/直播
7. 作为用户，我想电脑端听到的手机声音与画面同步，以便获得完整影音体验
8. 作为用户，我想手机旋转后电脑画面随之适配，以便横屏内容正常观看
9. 作为用户，我想看到声音来自哪个手机 APP 的状态，以便无声时判断原因
10. 作为用户，当某 APP 禁止声音被采集时，我想得到"该应用不允许采集声音"的提示，以便区分故障与策略

**反向控制**
11. 作为用户，我想在电脑画面上单击某位置时手机同步响应点击，以便不用拿起手机操作
12. 作为用户，我想在电脑画面上拖拽产生滑动效果，以便滚动列表与翻页
13. 作为用户，我想在电脑端按返回键时手机执行返回，以便快速导航
14. 作为用户，无障碍服务未开启时我想被引导开启，以便控制功能可用
15. 作为用户，我不想开启无障碍服务时仍能正常观看，以便按需选择功能
16. 作为用户，我想画面旋转后点击位置依然准确，以便任意方向下操作

**权限与引导**
17. 作为用户，我想首次使用时被引导完成屏幕授权，以便理解系统要求
18. 作为用户，我想声音授权与屏幕授权分开引导，以便分别处理拒绝场景
19. 作为用户，我想共享期间手机保持前台服务通知，以便知道采集正在进行

**稳定性与异常**
20. 作为用户，手机锁屏后我想电脑端 3 秒内看到"已停止共享"，以便了解会话状态
21. 作为用户，WiFi 闪断时我想被提示并可一键重连，以便快速恢复观看
22. 作为用户，我想区分"主动停止"与"断连"，以便只在后者自动重连
23. 作为用户，我想电脑端显示手机电量与分辨率，以便掌握设备状态
24. 作为用户，我想共享两小时后工具依然稳定（不崩、不卡、不涨内存），以便长时间观看

## Implementation Decisions

1. **三包结构**：`android/`（共享端 Kotlin）、`viewer/`（观看端 .NET 8 WPF）、`protocol/`（跨端契约：控制消息与信令报文的 Schema，双端各自实现解析器）
2. **共享端模块接口**（详见架构设计文档 M-S1~S6）：
   - ScreenSource：`start(w,h) -> Surface`、`stop()`、撤销回调
   - AudioCapture：`start() -> PcmFrame 流`、`silentByPolicy 状态`
   - RtcSession：`createOffer(video,audio) -> Sdp`、`acceptAnswer`、`sendControl -> bool`、控制消息流、会话状态流
   - SignalingServer：`start(port)`、配对回调、offer→answer 回调
   - GestureInjector：`injectTap(x,y)`、`injectSwipe(...)`、`pressBack()`、`isAvailable()`
   - ShareCoordinator：`beginShare() -> 权限引导流`、`endSession(reason)`；reason 区分主动停止/断连
3. **观看端模块接口**（M-V1~V6）：
   - SignalingClient：`GetInfo`、`Pair`、`ExchangeOffer`
   - RtcReceiver：`Connect(answer)`、帧事件（I420Frame/PcmFrame）、`SendControl`、状态事件
   - VideoRenderer：`Attach(Image)`、`Write(I420Frame)`，非阻塞、保新弃旧
   - AudioPlayer：`Open(rate,ch)`、`Write(PcmFrame)`，环形缓冲内置
   - SessionManager：`Idle → Pairing → Connecting → Sharing → Reconnecting|Stopping → Idle` 状态机，断连检测 10 秒；Sharing 中 ICE 断连先保持 Sharing 并提示弱信号，10 秒内恢复则取消计时、不重连（该转换由原型验证补入），10 秒未恢复进入 Reconnecting 单次尝试，失败回 Idle
4. **信令协议**（HTTP，共享端监听默认 18080）：`GET /info` 返回设备名/分辨率/电量/版本；`POST /pair` 提交配对码返回会话标识；`POST /offer` 提交 SDP+全部 candidates，返回 SDP answer+全部 candidates（non-trickle，见 ADR-0003）
5. **控制消息协议**（DataChannel，JSON）：`{t:"tap",x,y}`、`{t:"swipe",x0,y0,x1,y1,dur}`、`{t:"key",k:"back"}`；坐标为归一化 0~1（CONTEXT.md 定义），共享端负责像素换算
6. **编码协商**：视频 H.264 硬编优先（VP8 回退），音频 Opus；分辨率跟随手机原生，帧率上限 30fps
7. **帧归一**：WebRTC 细节止步于 RtcSession/RtcReceiver，对下游暴露统一 I420Frame/PcmFrame（架构深模块原则）
8. **系统约束落地**：采集运行于 mediaProjection 类型前台服务；声音采集要求 Android 10+；无障碍 dispatchGesture 承载全部手势注入（ADR-0002）
9. **配对码**：4 位数字，生成后 15 分钟失效，随会话一次性使用
10. **断线语义**：ICE 断连 10 秒 → 断连（触发重连 UI）；锁屏/撤销授权 → 主动停止（不重连）——与 CONTEXT.md 术语一致

## Testing Decisions

- **只测外部行为**：测试穿越模块接口，不触碰实现内部（深模块原则）；模块内部构件仅经其自身接口测试
- **测试缝**（4 个，与架构设计第 5 节一致）：
  1. MediaSource 缝：FakeVideoSource/FakeAudioSource 注入 RtcSession，测会话逻辑无需真机
  2. IFrameSink 缝：合成 I420 帧驱动 VideoRenderer，测转换与丢帧策略
  3. IRtcReceiver 缝：FakeReceiver 驱动 SessionManager 状态机全路径（含断连/主动停止分叉）
  4. protocol 契约缝：Kotlin 与 C# 解析器对同一组样例报文对拍，防双端漂移
- **最高缝优先**：能经 SessionManager/ShareCoordinator 验证的编排逻辑不在下层模块重复测
- **先例**：新仓库无既有测试，本规格测试即为基线先例；双端分别用 xUnit（viewer）与 JUnit（android）
- **真机验收**：PRD 第 6 节 7 条标准 + 国产 ROM 差异清单（MIUI/ColorOS 音频捕获），作为 M4 里程碑门禁

## Out of Scope

- 多观看端并发（P2 重估）
- 跨公网穿透与 TURN（二期）
- 录屏/截图保存、全屏模式、多设备管理、自定义密码（PRD P2）
- iOS 平台
- 观看端自动重连（P1，先做手动一键重连）

## Further Notes

- 里程碑映射：M1 画面链路 → M2 音画同步 → M3 反向控制 → M4 稳定性打磨与验收
- 最大技术风险为 WebRTC 双端联调（Sipsorcery ↔ Android WebRTC），原型验证阶段优先回答该问题
- 工作量基线 24.5 人日 + 30% 联调缓冲（需求拆分文档）