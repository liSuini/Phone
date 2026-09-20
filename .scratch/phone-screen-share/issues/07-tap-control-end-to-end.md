---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '3073a24f-fe5c-4e3d-b585-e88b7f0808e6'
  PropagateID: '3073a24f-fe5c-4e3d-b585-e88b7f0808e6'
  ReservedCode1: '302b101f-4e0d-4fd6-a258-a68de590e8f3'
  ReservedCode2: '302b101f-4e0d-4fd6-a258-a68de590e8f3'
---

# 07: 点击控制端到端（M3 门禁）

**What to build:** 观看端鼠标点击视频画面 → 归一化坐标经 DataChannel 发送 → 共享端无障碍服务注入手势，手机对应位置响应点击；含拖拽滑动与返回键。无障碍未开启时降级引导不影响观看。

**Blocked by:** 05（控制坐标映射依赖真实画面与旋转适配）

**Status:** ready-for-agent

**实现细节：** 见开发计划 Task 10/11（GestureInjector、ControlSender）

- [ ] 坐标归一化↔像素换算纯函数单元测试通过（中心/角点/越界 clamp）
- [ ] 无障碍服务：单击/滑动手势注入、返回键全局动作；isAvailable() 状态正确暴露
- [ ] 观看端：按下→抬起 <200ms 判 Tap、拖拽 >20px 判 Swipe 的判定逻辑单元测试通过
- [ ] DataChannel 消息用票据 01 的契约解析器（禁手写字面量）
- [ ] 无障碍未开启：点击时手机端引导提示，观看不中断（PRD F08 / 用户故事 14、15）
- [ ] 真机端到端：电脑点手机计算器 100ms 内响应；旋转后坐标依然准确（PRD 验收 2、6）
- [ ] 全部测试通过并提交