---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '487f0ce6-8e7c-4a21-bddc-d6f3044deca4'
  PropagateID: '487f0ce6-8e7c-4a21-bddc-d6f3044deca4'
  ReservedCode1: '6137736c-445e-4c56-a3b4-265200860089'
  ReservedCode2: '6137736c-445e-4c56-a3b4-265200860089'
---

# 03: 共享端信令服务器

**What to build:** 共享端（Android）内嵌轻量 HTTP 信令服务器：提供 GET /info、POST /pair、POST /offer 三个端点，含 4 位配对码的生成与 15 分钟失效校验。完成后与票据 02 的观看端客户端本机对跑，全部接口互通。

**Blocked by:** 02（本机对跑验收依赖其客户端）

**Status:** ready-for-agent

**实现细节：** 见开发计划 Task 3

- [ ] 配对码：4 位数字、生成即记时、15 分钟前签发的码验证拒绝、验证一次后失效
- [ ] GET /info 返回设备名/分辨率/电量/版本
- [ ] POST /pair 配对码正确返回会话标识，错误返回 403 与错误信息
- [ ] POST /offer 接收 SDP 返回 answer（回调由会话层注入，本票用测试桩）
- [ ] 与票据 02 客户端本机对跑全部通过并提交