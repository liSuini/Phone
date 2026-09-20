---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: 'f05dc8c3-0019-4543-b9dc-38b5f9ebf96e'
  PropagateID: 'f05dc8c3-0019-4543-b9dc-38b5f9ebf96e'
  ReservedCode1: '0c70ca2f-28b6-4bc5-9834-0d9f0bc9b61b'
  ReservedCode2: '0c70ca2f-28b6-4bc5-9834-0d9f0bc9b61b'
---

# 05: 真实屏幕接入（M1 门禁）

**What to build:** 把票据 04 的合成假源替换为真实手机屏幕：MediaProjection 授权 → VirtualDisplay 采集 → 同一 WebRTC 链路推流，电脑窗口看到真实手机画面。含采集前台服务与用户撤销授权的生命周期回调。

**Blocked by:** 04

**Status:** ready-for-agent

**实现细节：** 见开发计划 Task 4（ScreenSource 与前台服务）

- [ ] 屏幕授权弹窗引导 → 开始采集 → 电脑窗口显示真实手机画面（M1 联调门禁达成）
- [ ] 采集运行于 mediaProjection 类型前台服务，通知栏常驻
- [ ] 用户撤销授权或锁屏时 onRevoked 回调触发（主动停止语义，供票据 08 消费）
- [ ] 手机旋转后画面随之适配（PRD F13 / 用户故事 8）
- [ ] 满足 MediaSource 缝接口，假源仍可用于测试回归
- [ ] 真机冒烟通过并提交