---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: 'ff38ef17-bdea-47d6-ba32-2f25efcd9a6f'
  PropagateID: 'ff38ef17-bdea-47d6-ba32-2f25efcd9a6f'
  ReservedCode1: 'c5d22f1a-9db1-4469-b507-1da19ec56642'
  ReservedCode2: 'c5d22f1a-9db1-4469-b507-1da19ec56642'
---

# 信令报文契约（Signaling Contract）

- 版本：1.0（票据 02 定义，票据 03 共享端照此实现）
- 传输：HTTP 1.1，JSON body（UTF-8），共享端默认监听 18080 端口

## 端点

### GET /info → 200

```json
{"deviceName":"测试机","width":1080,"height":2400,"battery":88,"version":"1.0"}
```

### POST /pair

请求：`{"code":"1234"}`

成功 → 200：`{"ok":true,"session":"<uuid>"}`
失败 → 403：`{"ok":false,"error":"配对码错误"}`

### POST /offer

请求：`{"sdp":"<offer SDP 文本>"}`
成功 → 200：`{"answer":"<answer SDP 文本>"}`

## 双端一致规则

1. 字段名严格小写驼峰，双端解析器对未知字段忽略
2. 观看端对网络失败/超时（5s）返回失败结果，不抛异常；共享端对畸形请求返回 400
3. 配对码错误用 403 + error 文案区分于网络故障