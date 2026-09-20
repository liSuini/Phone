---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '26232d81-5715-45e5-bc70-a8c0810756ff'
  PropagateID: '26232d81-5715-45e5-bc70-a8c0810756ff'
  ReservedCode1: 'b71457d8-d128-4bcc-82dd-7a164a4a3783'
  ReservedCode2: 'b71457d8-d128-4bcc-82dd-7a164a4a3783'
---

# Phone Screen Share Assistant

手机屏幕共享助手：在电脑上实时查看手机屏幕画面与音频，支持鼠标反向控制。

- **共享端**（Android，Kotlin）：屏幕/声音采集 + WebRTC 推流 + 无障碍手势注入
- **观看端**（Windows，C# WPF/.NET 8）：Sipsorcery 接收渲染 + NAudio 播放 + 会话状态机
- **protocol**：双端共享契约（控制消息、信令报文），样例文件是对拍真理之源

## 项目状态

阶段 0~4（需求/领域模型/架构/技术规格/原型验证/开发计划/任务票据）已完成，见 `docs/` 与 `CONTEXT.md`。
阶段 5（编码）进行中：票据 01（protocol 契约）与票据 02（观看端信令客户端）已完成，双端测试全绿。

## 构建与测试

环境：.NET SDK `D:\developTools\dotnet`（8.0）、Gradle `D:\developTools\gradle`（8.10.2）、JDK 17（`D:\developTools\JDK\jdk17.0.16`）。

```powershell
# C# 端（viewer）
dotnet test viewer\tests\Phone.Share.Protocol.Tests
dotnet test viewer\tests\Phone.Share.Tests

# Kotlin 契约模块（protocol/kotlin）
#  wrapper 下载源已配置腾讯镜像
protocol\kotlin\gradlew.bat -p protocol\kotlin test
```

## 关键文档

- `docs/specs/2026-09-20-technical-spec.md`：技术规格（实现决策与测试缝）
- `docs/plans/2026-09-20-phone-screen-share-plan.md`：实施计划（14 任务）
- `.scratch/phone-screen-share/issues/`：任务票据（纵贯切片，带阻塞边）
- `protocol/schema/`：跨端契约（控制消息、信令报文）