---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '8696d902-763f-4dba-94df-f366800fbf40'
  PropagateID: '8696d902-763f-4dba-94df-f366800fbf40'
  ReservedCode1: '5fe1db24-3453-4a52-be7d-a06e58229de3'
  ReservedCode2: '5fe1db24-3453-4a52-be7d-a06e58229de3'
---

# 信令由共享端内嵌 HTTP 服务器承担，采用 non-trickle ICE，不设独立信令服务

常规 WebRTC 实践是独立信令服务器（WebSocket 长连接 + trickle ICE），但本项目仅在局域网内、一对一连接，独立服务是不必要的基础设施。决定：共享端内嵌 NanoHTTPD 提供配对与 SDP 交换的 3 个 HTTP 接口；ICE candidate 等收集完毕随 offer/answer 一次性交换（non-trickle），握手仅 2 次请求、无长连接维护。

## Considered Options

- 独立信令服务器（Node/Go + WebSocket）：为多对多/跨网络设计，本项目用不上，违背完全自研的零依赖原则
- 二维码直接携带全部连接信息（免信令）：SDP 过大无法编入二维码，不可行

## Consequences

- 连接并发为 1（一个共享端同时只服务一个观看端），多设备留待 P2 功能重估
- 若二期需要跨公网或 TURN，信令协议需升级为长连接形态