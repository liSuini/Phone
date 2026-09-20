---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '8239b488-4993-4001-a8f8-5995892732b7'
  PropagateID: '8239b488-4993-4001-a8f8-5995892732b7'
  ReservedCode1: 'b2a288f7-74c6-4894-a538-19c80ca29251'
  ReservedCode2: 'b2a288f7-74c6-4894-a538-19c80ca29251'
---

# 原型验证：观看端 SessionManager 状态模型

- 日期：2026-09-20
- 原型代码：丢弃分支 `prototype/session-state-machine`（文件 `prototype/session-state-machine.html`，不进 master）
- 验证方式：单文件 HTML 可交互状态机（14 条转换表 + 5 个关键难例剧本），浏览器驱动全部剧本实跑

## 回答的问题

观看端 SessionManager 状态模型（技术规格 M-V5）是否成立——特别是：
1. "断连"与"主动停止"的分叉是否正确（断连才重连，主动停止永不重连）
2. Sharing 中 WiFi 闪断 10 秒内的恢复与 10 秒超时的竞争
3. 重连途中用户取消/手机锁屏的处置
4. 配对/建连阶段超时兜底

## 验证结果

| 剧本 | 预期 | 实跑结果 |
|------|------|----------|
| 正常建连 | Idle→Pairing→Connecting→Sharing | ✅ 通过，计时器正确清理 |
| 弱信号自愈 | 断连后 10s 内恢复，保持 Sharing 不重连 | ❌ **首次失败** → 修复后 ✅ |
| 断连重连 | Sharing→(10s)→Reconnecting→(成功)→Sharing | ✅ 通过，单次尝试策略生效 |
| 手机锁屏 | Sharing→Stopping→Idle，不自动重连 | ✅ 通过 |
| 重连中用户取消 | Reconnecting→Stopping→Idle | ✅ 通过 |
| 无效事件（如 Idle 时点 ICE 建连） | 忽略不崩溃 | ✅ 通过 |

## 发现并修复的设计缺陷

**缺陷：转换表缺失 `Sharing + ICE恢复` 弧。**
Sharing 中收到断连信号后启动 10 秒计时；若连接在计时窗口内自愈，"ICE 建连成功"事件在 Sharing 状态下无转换可走、被当作无效事件忽略——连接实际已恢复，10 秒计时器却在继续跑，最终**误杀健康连接、错误进入 Reconnecting**。

**修复**：补入转换 `Sharing + EV_ICE_CONNECTED → Sharing`（清除计时、提示"信号已恢复"）。修复后复验通过：自愈后 3 秒（超演示计时窗口）状态保持 Sharing。

> 这是典型"纸上推理容易漏、实跑一遍就暴露"的状态机边界——原型价值得到确认。该决策已折入 master 技术规格（M-V5）。

## 验证后的状态机（最终版）

```
Idle ──用户连接──► Pairing ──/pair成功──► Connecting ──ICE建连──► Sharing
 ▲                    │失败/超时              │超时        │断连(保持+弱信号提示,10s计时)
 │                    ▼                      ▼           ├─恢复──► Sharing(取消计时) ★新增
 │                  Idle                    Idle        └─10s未恢复──► Reconnecting
 │                                                          │成功──► Sharing
 └────────────── Stopping ◄──手机锁屏/撤销授权/用户停止──────────┘│失败──► Idle
     （主动停止永不自动重连）
```

## 结论

1. 状态模型在补入"弱信号恢复"转换后成立，**作为编码基准**；SessionManager 实现必须包含该转换，状态机单元测试须覆盖全部 14 条转换 + 忽略路径
2. 原型自身按丢弃处理：结论折入技术规格后，分支保留仅为可追溯，不合并进 master
3. 最大技术风险（Sipsorcery ↔ Android WebRTC 双端联调）无法在纯设计阶段验证，按需求拆分计划在 **M1 里程碑用最小编码链路验证**，不阻塞当前设计定稿

## 本阶段未验证项（如实记录）

- WebRTC 真实链路（需 Android 真机 + 双端工程骨架，属 M1 编码期）
- I420 渲染性能（M2.4 压测，IFrameSink 缝已备 SkiaSharp 后备适配器）
- 信令 HTTP 时序（M1 集成测试：M-V1 ↔ M-S4 本机对跑）