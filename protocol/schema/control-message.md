---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '9889aeaa-609a-404f-906f-72c436baad86'
  PropagateID: '9889aeaa-609a-404f-906f-72c436baad86'
  ReservedCode1: 'cd012c4d-825f-40f2-828e-28e5d26c2e66'
  ReservedCode2: 'cd012c4d-825f-40f2-828e-28e5d26c2e66'
---

# 控制消息契约（ControlMessage Contract）

- 版本：1.0（票据 01）
- 双端实现：`protocol/kotlin/`（共享端 Kotlin）与 `viewer/src/Phone.Share.Protocol/`（观看端 C#）

## 消息类型

DataChannel 上传输的 JSON 文本。坐标一律为**归一化坐标**（0~1，见 CONTEXT.md），契约层为 double，下游换算 float。

| t | 字段 | 说明 |
|---|------|------|
| `tap` | `x`, `y` | 单击 |
| `swipe` | `x0,y0,x1,y1`, `dur`(ms) | 滑动 |
| `key` | `k` | 按键（当前仅 `back`） |

## 双端一致规则

1. **解析**：非法 JSON、未知类型、字段缺失 → 一律返回 null（不抛异常）
2. **序列化**：只输出非 null 字段；字段按 t,x,y,x0,y0,x1,y1,dur,k 顺序输出
3. **对拍标准（语义级）**：对每个样例，parse → toJson → 再 parse，字段值须一致（数值容差 1e-9）；非法样例双端均返回 null。不做字节级对拍（双端浮点文本化格式有平台差异，已刻意规避）
4. 未知字段：解析时忽略（向前兼容）
5. 样例文件（`protocol/samples/`）是双端测试的共同真理之源，测试必须读文件而不是手写副本