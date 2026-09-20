---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: 'c431bea8-53e4-4c1e-b95d-6a311bb81527'
  PropagateID: 'c431bea8-53e4-4c1e-b95d-6a311bb81527'
  ReservedCode1: '1ac64827-40f2-4713-9b8c-f63e4020da68'
  ReservedCode2: '1ac64827-40f2-4713-9b8c-f63e4020da68'
---

# 01: protocol 控制消息契约（双端解析器对拍）

**What to build:** 定义跨端控制消息契约（tap/swipe/key 三类 JSON，坐标归一化 0~1），共享端（Kotlin）与观看端（C#）各实现一个解析器与序列化器，并提供协议样例文件。完成后双端对同一组样例的解析→序列化往返结果完全一致。

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**实现细节：** 见开发计划 Task 1（docs/plans/2026-09-20-phone-screen-share-plan.md）

- [ ] 样例文件覆盖 tap / swipe / key 三类消息的合法与非法输入
- [ ] Kotlin 解析器 4 个单元测试通过（含非法输入返回 null）
- [ ] C# 解析器 4 个同构单元测试通过
- [ ] 双端对拍测试：全部样例 Parse→ToJson 往返输出与样例字节级一致
- [ ] 解析器接口签名与架构设计文档 M-S3/M-V2 一致（后续任务直接消费）
- [ ] 测试通过并提交