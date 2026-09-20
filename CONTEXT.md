---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: 'a5b0f48e-fc98-4f28-8ab3-7a92e6ef1dd9'
  PropagateID: 'a5b0f48e-fc98-4f28-8ab3-7a92e6ef1dd9'
  ReservedCode1: '61274c86-f47a-472d-9da1-3e30bef3a399'
  ReservedCode2: '61274c86-f47a-472d-9da1-3e30bef3a399'
---

# 手机屏幕共享助手（Phone Screen Share）

将 Android 手机屏幕画面与系统声音实时同步到电脑、并支持电脑鼠标反向控制的共享工具。本文档是全项目唯一的术语表，统一双端代码与文档用词。

## Language

### 角色与会话

**共享端（Sharer）**:
运行在 Android 手机上的一端，采集并发出屏幕与声音。信令服务器内嵌于此端。
_Avoid_: 服务端、发送端、手机客户端

**观看端（Viewer）**:
运行在 Windows 电脑上的一端，接收并呈现画面与声音，发出控制消息。
_Avoid_: 客户端、接收端、PC 端（口语可，代码与文档不用）

**共享会话（ShareSession）**:
从 WebRTC 建连成功到连接终止的一次完整投屏过程。一次会话对应一个 PeerConnection。
_Avoid_: 连接、投屏会话

**配对码（PairCode）**:
共享端生成的一次性短数字码，观看端在建连前提交以证明可连。随会话失效。
_Avoid_: 密码、验证码、口令

### 采集（共享端）

**屏幕采集（ScreenCapture）**:
经系统 MediaProjection 授权后，对手机屏幕的持续镜像采集。
_Avoid_: 录屏（口语含"录制保存"歧义）、投屏采集

**声音捕获（AudioCapture）**:
经 AudioPlaybackCapture 采集手机正在播放的媒体声音；音源 APP 可拒绝，此时无声属正常。
_Avoid_: 录音（易与麦克风录音混淆）、音频录制

**采集三授权（Three Grants）**:
开启一次完整共享所需的三个独立系统授权：屏幕授权、声音授权、无障碍授权。第三个缺省不影响纯观看。
_Avoid_: 权限三件套（口语可，文档不用）

### 传输与连接

**信令（Signaling）**:
建连前通过 HTTP 交换 SDP 与 ICE 的过程，由共享端内嵌服务器提供。会话建立后不再使用。
_Avoid_: 配对流程（配对只是信令的第一步）

**媒体流（MediaStream）**:
WebRTC 承载的视频轨道与音频轨道集合，经 SRTP 加密。
_Avoid_: 数据流、推流（口语可，文档不用）

**控制消息（ControlMessage）**:
会话中经 DataChannel 传输的点击、滑动、按键指令，与媒体流相互独立。
_Avoid_: 输入事件（指系统层面事件，含义更广）

**归一化坐标（NormalizedCoordinate）**:
0~1 的相对坐标，与双方分辨率及画面缩放解耦；共享端负责换算回屏幕像素。
_Avoid_: 相对坐标、百分比坐标

### 控制（共享端）

**手势注入（GestureInjection）**:
共享端把控制消息翻译成无障碍手势（dispatchGesture）派发到屏幕的动作。仅在无障碍授权开启时可用。
_Avoid_: 模拟点击、触控注入

### 终止与异常

**主动停止（Stop）**:
用户或系统有意结束会话：锁屏、撤销屏幕授权、观看端关闭。可提示但不应自动重连。
_Avoid_: 断开（与断连混淆）

**断连（Disconnect）**:
网络原因导致的非预期连接中断。触发观看端重连策略。
_Avoid_: 掉线、断开连接