---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: 'c5141782-8e93-4315-ab3b-0bb180986486'
  PropagateID: 'c5141782-8e93-4315-ab3b-0bb180986486'
  ReservedCode1: '7a8da288-0602-44f2-a0b9-cc83e92aa50b'
  ReservedCode2: '7a8da288-0602-44f2-a0b9-cc83e92aa50b'
---

# 09: 共享端权限引导与 UI 收尾

**What to build:** 共享端完整用户体验：冷启动后经三步授权引导（屏幕授权 → 声音授权 → 无障碍授权[可跳过]）进入就绪状态，展示二维码与 IP/配对码入口，状态页显示连接情况与采集状态。

**Blocked by:** 05, 06, 07（三授权分别对应画面/声音/控制三个已实现能力）

**Status:** ready-for-agent

**实现细节：** 见开发计划 Task 13（ShareCoordinator 与 Android UI）

- [ ] 引导流顺序单元测试通过：拒绝声音授权仍可进入就绪并标记 silentByPolicy（降级不阻断）
- [ ] 二维码生成（IP+端口+配对码）；无摄像头侧可手输的 IP/码数字展示（用户故事 1、2）
- [ ] 状态页：连接状态、采集三授权状态、电池优化白名单引导
- [ ] endSession(reason) 区分主动停止与断连，通知观看端（对齐 CONTEXT.md 术语）
- [ ] 真机冷启动全流程：首次安装 → 三引导 → 二维码出现 → 电脑可连
- [ ] 测试通过并提交