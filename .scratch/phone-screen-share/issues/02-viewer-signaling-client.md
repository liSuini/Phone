---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '6ef01b98-3d7c-4778-8430-c32e0e36224a'
  PropagateID: '6ef01b98-3d7c-4778-8430-c32e0e36224a'
  ReservedCode1: 'e405ccda-c520-4f55-a9eb-ac70974594d2'
  ReservedCode2: 'e405ccda-c520-4f55-a9eb-ac70974594d2'
---

# 02: 观看端信令客户端

**What to build:** 观看端（C#）的信令客户端：输入共享端 IP/端口/配对码，完成设备信息查询、配对校验、SDP offer 交换并取回 answer。完成后可对本地 stub 服务器走完全部三个信令接口。

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**实现细节：** 见开发计划 Task 2

- [ ] GetInfo 返回设备名/分辨率/电量/版本并正确解析
- [ ] 配对成功返回会话标识；配对码错误返回带错误信息的失败结果
- [ ] ExchangeOffer 提交 offer 返回 answer（SDP 字符串原样往返）
- [ ] 全部为纯 HTTP 调用，超时 5 秒，失败返回结果不抛异常
- [ ] stub 服务器单元测试全部通过并提交