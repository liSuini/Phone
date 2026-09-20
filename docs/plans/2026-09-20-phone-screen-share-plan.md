---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '96732d7b-a963-45a4-bff9-592683b51c7c'
  PropagateID: '96732d7b-a963-45a4-bff9-592683b51c7c'
  ReservedCode1: '3df35fbf-ea02-4f93-8037-596a6509e98f'
  ReservedCode2: '3df35fbf-ea02-4f93-8037-596a6509e98f'
---

# 手机屏幕共享助手 实施计划

> **For agentic workers:** 本计划按任务逐个执行，步骤用复选框（`- [ ]`）跟踪。执行每个任务前先读 `docs/specs/2026-09-20-technical-spec.md` 与本文档对应任务；任务间可派发子代理并行无依赖任务。

**Goal:** 实现手机屏幕共享助手——Android 屏幕与声音经 WebRTC 同步到 C# WPF 客户端，支持鼠标反向控制。

**Architecture:** 三包结构（android / viewer / protocol）。WebRTC P2P 媒体+DataChannel 控制；信令为手机内嵌 HTTP（non-trickle ICE）；观看端以状态机管理会话。深模块接口见架构设计文档，本计划的任务即按模块推进。

**Tech Stack:** Kotlin + stream-webrtc-android + NanoHTTPD + ZXing（Android）；.NET 8 + Sipsorcery + NAudio + WPF（C#）；JUnit / xUnit。

**Spec:** `docs/specs/2026-09-20-technical-spec.md`（实现决策与测试缝）、`docs/specs/2026-09-20-prototype-validation.md`（状态机含弱信号恢复转换 ★）

## Global Constraints

- 手机端 Android 10+（API 29）为最低支持；采集运行于 `mediaProjection` 类型前台服务
- 观看端 .NET 8，Windows 10/11 x64
- 控制消息坐标一律归一化 0~1（CONTEXT.md 术语），像素换算只发生在 GestureInjector
- 视频编码 H.264 硬编优先（VP8 回退），音频 Opus；帧率上限 30fps
- 每个任务结束必须：测试通过 + `git commit`（消息含任务号）
- 禁止引入范围外功能（技术规格 Out of Scope 清单）
- 双端共用协议样例必须从 `protocol/` 出，禁止手写两份不一致的字面量

---

## 里程碑 M1：画面链路（Task 1~7）

### Task 1: protocol 契约包与双端解析器

**Files:**
- Create: `protocol/schema/control-message.json`（控制消息 JSON Schema 文档）
- Create: `protocol/samples/*.json`（tap/swipe/key 正反样例）
- Create: `android/app/src/main/java/com/limw/phone/share/protocol/ControlMessage.kt`
- Create: `android/app/src/test/java/com/limw/phone/share/protocol/ControlMessageTest.kt`
- Create: `viewer/src/Phone.Share.Protocol/ControlMessage.cs`
- Create: `viewer/tests/Phone.Share.Protocol.Tests/ControlMessageTests.cs`

**Interfaces:**
- Produces（双端一致，后续任务直接使用）:
  - Kotlin: `data class ControlMessage(val t: String, val x: Float? , val y: Float?, val x0: Float?, val y0: Float?, val x1: Float?, val y1: Float?, val dur: Int?, val k: String?)` + `fun parse(json: String): ControlMessage?` + `fun ControlMessage.toJson(): String`
  - C#: `sealed record ControlMessage(string T, float? X, float? Y, float? X0, float? Y0, float? X1, float? Y1, int? Dur, string? K)` + `static ControlMessage? Parse(string json)` + `string ToJson()`

- [ ] **Step 1: 写样例与 Kotlin 失败测试**

```kotlin
// ControlMessageTest.kt
class ControlMessageTest {
    @Test fun `tap 消息解析`() {
        val m = ControlMessage.parse("""{"t":"tap","x":0.5,"y":0.3}""")
        assertNotNull(m); assertEquals("tap", m!!.t); assertEquals(0.5f, m.x); assertEquals(0.3f, m.y)
    }
    @Test fun `swipe 消息解析`() {
        val m = ControlMessage.parse("""{"t":"swipe","x0":0.1,"y0":0.2,"x1":0.8,"y1":0.9,"dur":300}""")
        assertNotNull(m); assertEquals(300, m!!.dur)
    }
    @Test fun `key 消息解析`() {
        val m = ControlMessage.parse("""{"t":"key","k":"back"}""")
        assertEquals("back", m!!.k)
    }
    @Test fun `非法输入返回 null`() { assertNull(ControlMessage.parse("not-json")) }
}
```

- [ ] **Step 2: 运行确认失败** `./gradlew :app:testDebugUnitTest --tests "*ControlMessage*"` → 编译失败（类不存在）
- [ ] **Step 3: 实现 Kotlin 解析器**（org.json 内置即可，禁引第三方）
- [ ] **Step 4: 运行确认通过**
- [ ] **Step 5: C# 同构（xUnit 同样 4 个用例**，样例字面量从 `protocol/samples/` 内容复制，禁止手打）
- [ ] **Step 6: 双端对拍**——把 `protocol/samples/` 全部文件分别喂给两端的 Parse→ToJson 往返测试，输出必须一致
- [ ] **Step 7: Commit** `git commit -m "feat(protocol): 控制消息契约与双端解析器 Task1"`

### Task 2: viewer 骨架与 SignalingClient

**Files:**
- Create: `viewer/src/Phone.Share/Phone.Share.csproj`、`viewer/src/Phone.Share/Signaling/SignalingClient.cs`
- Create: `viewer/tests/Phone.Share.Tests/SignalingClientTests.cs`

**Interfaces:**
- Produces: `sealed class SignalingClient(HttpClient http)`:
  - `async Task<DeviceInfo> GetInfoAsync(string ip, int port, CancellationToken ct)` → `record DeviceInfo(string DeviceName, int Width, int Height, int Battery, string Version)`
  - `async Task<PairResult> PairAsync(string ip, int port, string code, CancellationToken ct)` → `record PairResult(bool Ok, string? Session, string? Error)`
  - `async Task<string> ExchangeOfferAsync(string ip, int port, string sdp, CancellationToken ct)`（返回 answer SDP）

- [ ] **Step 1: 失败测试**（xUnit，用本地 HttpListener 起 stub 服务器返回固定 JSON）

```csharp
[Fact]
public async Task PairAsync_错误码返回Ok失败() {
    using var stub = new SignalingStub("/pair", """{"ok":false,"error":"配对码错误"}""");
    var client = new SignalingClient(new HttpClient());
    var r = await client.PairAsync("127.0.0.1", stub.Port, "9999", default);
    Assert.False(r.Ok); Assert.Equal("配对码错误", r.Error);
}
```

- [ ] **Step 2: 跑测试确认失败** `dotnet test` → 编译失败
- [ ] **Step 3: 实现**（HttpClient + System.Text.Json；超时 5s；返回结果不产生副作用）
- [ ] **Step 4: 跑测试通过**（补 3 个用例：GetInfo 解析、Pair 成功、ExchangeOffer 往返）
- [ ] **Step 5: Commit** `git commit -m "feat(viewer): 信令客户端与骨架 Task2"`

### Task 3: android 骨架与 SignalingServer

**Files:**
- Create: `android/settings.gradle.kts`、`android/app/build.gradle.kts`、`android/app/src/main/AndroidManifest.xml`
- Create: `android/app/src/main/java/com/limw/phone/share/signaling/SignalingServer.kt`、`PairCode.kt`
- Create: `android/app/src/test/java/com/limw/phone/share/signaling/{SignalingServerTest,PairCodeTest}.kt`

**Interfaces:**
- Consumes: Task 1 无关；独立可测
- Produces: `class SignalingServer(val port: Int = 18080)`：`fun start()`；`fun stop()`；`var onPair: (code: String) -> Boolean`；`var onOffer: (sdp: String) -> String`；`fun info(): DeviceInfo`
- Produces: `object PairCode { fun generate(): String; fun verify(code: String, issued: String, issuedAt: Long): Boolean }`（4 位数字，15 分钟失效）

- [ ] **Step 1: PairCode 失败测试**（生成 4 位、验证正确码通过、15 分钟前签发的码拒绝）
- [ ] **Step 2: 跑失败 → 实现 → 跑通过**
- [ ] **Step 3: SignalingServer 失败测试**（Robolectric + OkHttp 对打：GET /info 返回 JSON；POST /pair 错码 403；POST /offer 回显 answer）
- [ ] **Step 4: 实现 NanoHTTPD 路由**（依赖 `org.nanohttpd:nanohttpd:2.3.1`）
- [ ] **Step 5: 与 Task 2 的 C# SignalingClient 本机对跑**（集成脚本：起 Kotlin 测试服务器进程 → dotnet test 集成用例指向它）
- [ ] **Step 6: Commit** `git commit -m "feat(android): 信令服务器与配对码 Task3"`

### Task 4: android ScreenSource（MediaProjection 采集）

**Files:**
- Create: `android/app/src/main/java/com/limw/phone/share/capture/ScreenSource.kt`、`capture/CaptureService.kt`
- Create: `android/app/src/main/res/values/foreground.xml`（前台服务声明）
- Test: 真机手动（本任务无 JVM 可测面，接口由 Task 5 消费验证）

**Interfaces:**
- Consumes: Android 系统 API
- Produces: `class ScreenSource`：`fun requestAndStart(activity: Activity, w: Int, h: Int, cb: (Surface) -> Unit)`；`fun stop()`；`var onRevoked: () -> Unit`
- 行为契约：授权弹窗 → VirtualDisplay 渲染到 cb 给出的 Surface；用户撤销或锁屏时回调 onRevoked（触发主动停止语义）

- [ ] **Step 1: 实现 CaptureService**（`foregroundServiceType="mediaProjection"`，notificationChannel "share"）
- [ ] **Step 2: 实现 ScreenSource**（MediaProjectionManager.requestProjection → startForeground → createVirtualDisplay）
- [ ] **Step 3: 真机冒烟**：临时 Activity 里 start 后用 SurfaceView 显示自采集画面（App 内自预览），验证旋转/撤销回调
- [ ] **Step 4: Commit** `git commit -m "feat(android): 屏幕采集源 Task4"`

### Task 5: android RtcSession 推流端

**Files:**
- Create: `android/app/src/main/java/com/limw/phone/share/rtc/RtcSession.kt`、`rtc/FakeVideoSource.kt`
- Create: `android/app/src/test/java/com/limw/phone/share/rtc/RtcSessionOfferTest.kt`

**Interfaces:**
- Consumes: `MediaSource` 缝（Task 4 的 ScreenSource 满足；测试用 FakeVideoSource 合成帧）
- Produces: `class RtcSession`：
  - `fun createOffer(video: VideoSinkSource, audio: AudioSource?): String`（返回含全部 candidates 的 SDP，non-trickle）
  - `fun acceptAnswer(sdp: String)`
  - `fun sendControl(msg: String): Boolean`（DataChannel 未开返回 false 不抛）
  - `val onControl: (String) -> Unit`
  - `fun close()`

- [ ] **Step 1: FakeVideoSource**（VideoCapturer 接口，定时推送合成 NV12 帧 1080p，供测试与后续无真机联调）
- [ ] **Step 2: 失败测试**——`createOffer` 返回的 SDP 含 `a=candidate` 且 m=video 存在；Fake 源注入后 VideoTrack 可用
- [ ] **Step 3: 实现流式 WebRTC**（依赖 `io.getstream:stream-webrtc-android:1.1.1`；PeerConnectionFactory + DefaultVideoEncoderFactory(H264=true) + DataChannel "control"；ICE 完成等待 `iceGatheringState==COMPLETE` 后打包进 SDP）
- [ ] **Step 4: 跑测试通过**
- [ ] **Step 5: Commit** `git commit -m "feat(android): WebRTC推流会话 Task5"`

### Task 6: viewer RtcReceiver（Sipsorcery 接收端）

**Files:**
- Create: `viewer/src/Phone.Share/Rtc/RtcReceiver.cs`、`Rtc/FakeRtcReceiver.cs`、`Rtc/FrameTypes.cs`
- Create: `viewer/tests/Phone.Share.Tests/RtcReceiverTests.cs`

**Interfaces:**
- Consumes: Task 2 SignalingClient 的 answer；信令由调用方完成
- Produces: `interface IRtcReceiver`（SessionManager 注入缝，Task 12 消费）:
  - `Task<string> CreateOfferAsync()`（含 candidates 的本地 SDP）
  - `Task ConnectAsync(string answerSdp)`
  - `event Action<I420Frame>? OnVideoFrame`；`event Action<PcmFrame>? OnAudioPcm`
  - `bool SendControl(string json)`
  - `event Action<RtcState>? OnStateChanged`（enum RtcState { New, Connecting, Connected, Disconnected, Failed }）
- Produces: `record I420Frame(int Width, int Height, byte[] Y, byte[] U, byte[] V, long TimestampUs)`、`record PcmFrame(int SampleRate, int Channels, short[] Samples)`

- [ ] **Step 1: FrameTypes + 纯函数测试**（帧 record 构造/拷贝语义）
- [ ] **Step 2: FakeRtcReceiver**（可编程状态转移 + 合成帧事件，供 Task 7/12 测试与状态机全路径用）
- [ ] **Step 3: 失败测试**——用 Sipsorcery 自 loopback：单个 RTCPeerConnection 自连（offer 自己 consume）验证 OnVideoFrame 能收帧（编码回环，无需网络）
- [ ] **Step 4: 实现 RtcReceiver**（Sipsorcery：RTCPeerConnection + OnVideoFrame I420 回调转 FrameTypes + OnReceiveRtp→PCM 重采样 48k + SctpSession DataChannel）
- [ ] **Step 5: 跑测试通过**
- [ ] **Step 6: Commit** `git commit -m "feat(viewer): WebRTC接收端 Task6"`

### Task 7: viewer VideoRenderer（I420→WPF）

**Files:**
- Create: `viewer/src/Phone.Share/Render/IFrameSink.cs`、`Render/WpfFrameSink.cs`、`Render/YuvConverter.cs`
- Create: `viewer/tests/Phone.Share.Tests/YuvConverterTests.cs`

**Interfaces:**
- Consumes: Task 6 `I420Frame`
- Produces: `interface IFrameSink { void Write(I420Frame frame); }`；`sealed class WpfFrameSink(Image image) : IFrameSink`（WriteableBitmap，保新弃旧丢帧，Dispatcher 调度）

- [ ] **Step 1: YuvConverter 失败测试**（4x4 已知输入 YUV → 期望 RGB 字节；黑/白/灰三组边界）
- [ ] **Step 2: 实现 YuvConverter**（BT.709 有限范围，无 unsafe 优先，unsafe 可选优化）
- [ ] **Step 3: WpfFrameSink**（无 UI 测试：WriteableBitmap 用 WriteableBitmap(4,4) 断言像素字节）
- [ ] **Step 4: 压测**——FakeRtcReceiver 推 1080p@30fps 合成帧 60 秒，统计丢帧率与 CPU，写 `docs/specs/perf-video-render.md`（若 CPU>15%，记录改用 SkiaSharp 适配器决定）
- [ ] **Step 5: Commit** `git commit -m "feat(viewer): 视频渲染与压测 Task7"`

**M1 联调门禁**：手机开 APP → 电脑输入 IP+码 → 电脑窗口出现手机画面。至此走通主链路。

---

## 里程碑 M2：音画同步（Task 8~9）

### Task 8: android AudioCapture 接入 RtcSession

**Files:**
- Create: `android/app/src/main/java/com/limw/phone/share/capture/AudioCapture.kt`、`capture/FakeAudioSource.kt`
- Modify: `android/app/src/main/AndroidManifest.xml`（RECORD_AUDIO 权限）
- Test: `android/app/src/test/java/com/limw/phone/share/capture/AudioCapturePolicyTest.kt`

**Interfaces:**
- Consumes: Task 5 RtcSession 的 AudioSource 参数位
- Produces: `class AudioCapture`：`fun start(): AudioSource`（AudioPlaybackCaptureConfiguration，44.1k/双声道/PCM16）；`val silentByPolicy: StateFlow<Boolean>`；`fun stop()`

- [ ] **Step 1: 失败测试**——策略判定纯函数：`AudioCapture.isPolicyDenied(usage: Int, contentTypes: IntArray): Boolean`（USAGE_MEDIA+CONTENT_TYPE_MOVIE 允许、usage=USAGE_VOICE_COMMUNICATION 拒绝）——JVM 可测
- [ ] **Step 2: 实现 isPolicyDenied + AudioCapture**（MediaProjection token 传入 buildAudioPlaybackCaptureConfig）
- [ ] **Step 3: RtcSession 接入音轨**（createOffer 带 audio，Opus）
- [ ] **Step 4: 真机验证**——播放视频，`adb logcat` 观察 silentByPolicy 状态；对拒绝采集的 APP（银行类）确认状态栏提示而非报错
- [ ] **Step 5: Commit** `git commit -m "feat(android): 声音捕获接入 Task8"`

### Task 9: viewer AudioPlayer 与同步验收

**Files:**
- Create: `viewer/src/Phone.Share/Audio/AudioPlayer.cs`、`Audio/RingBuffer.cs`
- Create: `viewer/tests/Phone.Share.Tests/{RingBufferTests,AudioPlayerTests}.cs`

**Interfaces:**
- Consumes: Task 6 `PcmFrame`
- Produces: `sealed class AudioPlayer`：`void Open(int sampleRate, int channels)`；`void Write(PcmFrame frame)`；`void Close()`；`double UnderrunRatio { get }`
- Produces: `sealed class RingBuffer(int capacityFrames)`：`void Push(short[] pcm)`；`short[]? Pull(int samples)`；`int Count`

- [ ] **Step 1: RingBuffer 失败测试**（推拉计数、满时阻塞丢旧策略、空返回 null）
- [ ] **Step 2: 实现 → 通过**
- [ ] **Step 3: AudioPlayer**（NAudio.WasapiOut 共享模式，PlaybackStopped 兜底，欠载计数暴露）
- [ ] **Step 4: 同步验收（真机）**——播放含明显口型的视频，主观校验音画偏差；记录 `docs/specs/m2-sync-acceptance.md`（通过标准：无感知差异）
- [ ] **Step 5: Commit** `git commit -m "feat(viewer): 音频播放与同步验收 Task9"`

**M2 门禁**：音画同步验收记录通过，PRD 验收标准 1 达成。

---

## 里程碑 M3：反向控制（Task 10~11）

### Task 10: android GestureInjector（无障碍手势注入）

**Files:**
- Create: `android/app/src/main/java/com/limw/phone/share/control/GestureInjector.kt`、`control/CoordinateMapper.kt`、`control/ShareAccessibilityService.kt`、`res/xml/accessibility.xml`
- Create: `android/app/src/test/java/com/limw/phone/share/control/CoordinateMapperTest.kt`

**Interfaces:**
- Consumes: Task 1 ControlMessage（t/x/y 字段）
- Produces: `object CoordinateMapper { fun normalizeToPixel(x: Float, y: Float, w: Int, h: Int): Pair<Float, Float> }`（纯函数，clamp 0~1）
- Produces: `class GestureInjector(service: ShareAccessibilityService?)`：`fun injectTap(x: Float, y: Float)`；`fun injectSwipe(x0: Float, y0: Float, x1: Float, y1: Float, dur: Long)`；`fun pressBack()`；`fun isAvailable(): Boolean`（service 非 null 且已连接）

- [ ] **Step 1: CoordinateMapper 失败测试**（中心点/角点/越界 clamp 三组用例）
- [ ] **Step 2: 实现 → 通过**
- [ ] **Step 3: ShareAccessibilityService**（dispatchGesture tap 100ms、swipe 连续路径、GLOBAL_ACTION_BACK；静态单例供 Service 绑定）
- [ ] **Step 4: RtcSession.onControl 接线**——DataChannel 消息 → ControlMessage.parse → GestureInjector 分发；isAvailable()==false 时 Toast 引导（PRD F08）
- [ ] **Step 5: 真机验证**——安装并开启无障碍，APP 内自测按钮点击自身界面
- [ ] **Step 6: Commit** `git commit -m "feat(android): 手势注入 Task10"`

### Task 11: viewer 控制发送端到端

**Files:**
- Modify: `viewer/src/Phone.Share/Rtc/RtcReceiver.cs`（SendControl 已有，本任务接鼠标事件）
- Create: `viewer/src/Phone.Share/Control/ControlSender.cs`
- Create: `viewer/tests/Phone.Share.Tests/ControlSenderTests.cs`

**Interfaces:**
- Consumes: Task 6 `IRtcReceiver.SendControl`、Task 1 C# ControlMessage
- Produces: `sealed class ControlSender(IRtcReceiver rtc)`：`void Tap(float nx, float ny)`；`void Swipe(float nx0, float ny0, float nx1, float ny1, int durMs)`；`void Back()`；`event Action<string>? OnRejected`（DataChannel 未开时触发，UI 提示）

- [ ] **Step 1: 失败测试**——FakeRtcReceiver 断言 Tap(0.5,0.3) 发出的 JSON 等于 `{"t":"tap","x":0.5,"y":0.3}`；通道关闭时 OnRejected
- [ ] **Step 2: 实现 → 通过**
- [ ] **Step 3: WPF 接线**——视频 Image 控件 MouseLeftButtonDown/Up（按下→抬起区间 <200ms 判 Tap）与 MouseMove（按住拖拽 >20px 判 Swipe），坐标按控件渲染区归一化
- [ ] **Step 4: 真机端到端**——电脑点手机计算器按钮，验证 100ms 内响应（PRD 验收 2）
- [ ] **Step 5: Commit** `git commit -m "feat(viewer): 控制发送链路 Task11"`

**M3 门禁**：PRD 验收 2、6、16（旋转后坐标）通过。

---

## 里程碑 M4：会话管理与打磨（Task 12~14）

### Task 12: viewer SessionManager 状态机

**Files:**
- Create: `viewer/src/Phone.Share/Session/SessionManager.cs`、`Session/SessionState.cs`
- Create: `viewer/tests/Phone.Share.Tests/SessionManagerTests.cs`

**Interfaces:**
- Consumes: Task 2 SignalingClient、Task 6 IRtcReceiver（FakeRtcReceiver 注入）、Task 7 IFrameSink、Task 9 AudioPlayer
- Produces: `sealed class SessionManager(SignalingClient sig, Func<IRtcReceiver> rtcFactory)`：
  - `Task StartAsync(string ip, int port, string code)`；`Task StopAsync()`；`Task ReconnectAsync()`
  - `IObservable<SessionState> State`（enum：Idle, Pairing, Connecting, Sharing, Reconnecting, Stopping）
- 转换规则（原型验证版，必须含 ★ 恢复弧）：

```csharp
// 转换表（与 docs/specs/2026-09-20-prototype-validation.md 一致，共 14 条 + 忽略路径）
// ★ Sharing 中 ICE 恢复：保持 Sharing、取消 10s 计时器 —— 单元测试必须覆盖
// Sharing 断连：保持 Sharing + 弱信号提示，10s 未恢复 → Reconnecting（单次尝试）
// 重连成功 → Sharing；失败 → Idle；主动停止（PeerStop/UserStop）→ Stopping → Idle，永不自动重连
```

- [ ] **Step 1: 失败测试**——用 FakeRtcReceiver 走全路径（≥8 用例）：正常建连 / 配对错码 / 建连超时 / **弱信号自愈（含 ★）** / 断连重连成功 / 重连失败回 Idle / 重连中用户取消 / 手机锁屏不重连——用例即原型 5 剧本的直译
- [ ] **Step 2: 实现**（显式转换表驱动而非 switch 散写；10s/30s 计时器可注入 IClock 缩短）
- [ ] **Step 3: 全部通过 → Commit** `git commit -m "feat(viewer): 会话状态机 Task12"`

### Task 13: android ShareCoordinator 与 UI

**Files:**
- Create: `android/app/src/main/java/com/limw/phone/share/ui/MainActivity.kt`、`ui/ShareViewModel.kt`、`ui/GrantStepAdapter.kt`
- Create: `android/app/src/main/java/com/limw/phone/share/ShareCoordinator.kt`
- Test: `android/app/src/test/java/com/limw/phone/share/ShareCoordinatorTest.kt`

**Interfaces:**
- Consumes: Task 3/4/5/8/10 全部模块
- Produces: `class ShareCoordinator`：`fun beginShare(): Flow<GrantStep>`（enum：ScreenGrant, AudioGrant, AccessibilityGrant, Ready）；`fun endSession(reason: Termination)`（enum：Stop, Disconnect）

- [ ] **Step 1: 失败测试**——权限引导流的顺序与降级（拒绝声音授权仍可进入 Ready 并标记 silentByPolicy）
- [ ] **Step 2: 实现 Coordinator + ViewModel**（Material 3 界面：二维码（ZXing）、状态页、三步引导卡）
- [ ] **Step 3: 真机走全流程**——首次安装冷启动到可连接
- [ ] **Step 4: Commit** `git commit -m "feat(android): 共享协调器与UI Task13"`

### Task 14: 稳定性、重连与最终验收

**Files:**
- Modify: `viewer/src/Phone.Share/Session/SessionManager.cs`（重连按钮事件暴露）
- Create: `viewer/src/Phone.Share/App.xaml(.cs)`、`viewer/src/Phone.Share/MainWindow.xaml(.cs)`
- Create: `docs/acceptance/2026-XX-XX-acceptance.md`（验收记录，执行时填）

**Interfaces:**
- Consumes: 全部既有模块
- Produces: 完整观看端应用（MVVM：连接面板 + 视频区 + 状态栏）

- [ ] **Step 1: MainWindow MVVM**（连接面板绑定 SessionManager.State；视频区绑 IFrameSink；状态栏显示设备信息/电量/欠载率/弱信号）
- [ ] **Step 2: 断线演练**——飞行模式切换 WiFi 验证：10s 内提示、一键重连恢复（PRD 验收 4）
- [ ] **Step 3: 2 小时稳定性**——连续投屏，观察内存曲线与崩溃（PRD 验收 7）；修复发现的问题并回归
- [ ] **Step 4: 验收清单**——逐条核对 PRD 第 6 节 7 条 + 国产 ROM 差异（MIUI/ColorOS 声音策略），结果写入 `docs/acceptance/`
- [ ] **Step 5: Commit** `git commit -m "feat: M4打磨与验收 Task14"`

---

## 自审记录（Self-Review）

1. **规格覆盖**：技术规格 10 项实现决策 → Task 1（契约）、2/3（信令）、4/5/8（采集编码）、6/9（接收播放）、10/11（控制）、12（状态机含★）、13（前台服务与三授权）、14（断线语义与验收）。24 个用户故事 → P0 故事全被任务覆盖；P1 故事（F09 滑动/F10 返回/F11 设备信息/F12 自动重连/F13 旋转适配）中 F12 自动重连与 F11 在 Task 14 有落点，滑动/返回已含在 Task 10/11。无缺口。
2. **占位符扫描**：无 TBD/TODO；每步均含命令与预期。
3. **类型一致性**：`I420Frame/PcmFrame/IRtcReceiver` 在 Task 6 定义、Task 7/9/11/12 消费；`ControlMessage` 双端签名在 Task 1 定义、Task 10/11 消费；`GrantStep/Termination` 在 Task 13 内闭环。已核对。

## 执行说明

- Task 1→2→3 有序；Task 4/5 依赖 android 骨架（Task 3）；Task 6 依赖 Task 2；M1 门禁后 Task 8/9 与 Task 10/11 可并行；Task 12 依赖 6 的 IRtcReceiver；Task 13 依赖 3/4/5/8/10；Task 14 收尾
- 双端联调风险（Sipsorcery ↔ Android WebRTC）的验证点即 M1 门禁，若握手失败优先排查 non-trickle candidate 打包与 DTLS 指纹