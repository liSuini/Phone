---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '239259cd-c611-4e8b-810d-ae281ffdd74f'
  PropagateID: '239259cd-c611-4e8b-810d-ae281ffdd74f'
  ReservedCode1: 'bc479a24-c01e-4dcc-9143-ba8e2e2984ec'
  ReservedCode2: 'bc479a24-c01e-4dcc-9143-ba8e2e2984ec'
---

# 08: 会话状态机与观看端 UI

**What to build:** 观看端会话生命周期管理：SessionManager 状态机（含原型验证补入的"弱信号自愈 ★"转换弧）驱动完整 UI——连接面板、视频区、状态栏（设备信息/弱信号/欠载提示）、断线提示与一键重连。

**Blocked by:** 04（消费其 IRtcReceiver 缝与信令客户端）

**Status:** ready-for-agent

**实现细节：** 见开发计划 Task 12（状态机与状态测试）；状态机转换规则以 docs/specs/2026-09-20-prototype-validation.md 为准

- [ ] 状态机单元测试覆盖原型 5 剧本直译（≥8 用例）：正常建连/配对错码/建连超时/**弱信号自愈（★弧必须覆盖）**/断连重连成功/重连失败回 Idle/重连中用户取消/手机锁屏不重连
- [ ] 转换表显式数据驱动实现；计时器可注入缩短测试时间
- [ ] Sharing 中断连：保持 Sharing + 弱信号提示，10s 恢复取消计时器，10s 未恢复进单次重连
- [ ] 主动停止（手机锁屏/撤销/用户停止）永不自动重连；断连才可重连
- [ ] 主窗口 MVVM：状态绑定、一键重连按钮、设备信息展示（PRD F07/F11/F12 手动重连部分）
- [ ] 全部测试通过并提交