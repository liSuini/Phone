---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: 'e824d37d-6e08-40a7-9976-78386609b037'
  PropagateID: 'e824d37d-6e08-40a7-9976-78386609b037'
  ReservedCode1: '0bf4b56d-d00f-4f48-822b-5a164e38bfd6'
  ReservedCode2: '0bf4b56d-d00f-4f48-822b-5a164e38bfd6'
---

# Phone Screen Share Assistant

手机屏幕共享助手：在电脑上实时查看手机屏幕画面与音频，支持鼠标反向控制。

- **共享端**（Android，Kotlin）：屏幕/声音采集 + WebRTC 推流 + 无障碍手势注入
- **观看端**（Windows，C# WPF/.NET 8）：Sipsorcery 接收渲染 + NAudio 播放 + 会话状态机
- **protocol**：双端共享契约（控制消息、信令报文），样例文件是对拍真理之源

## 项目状态

阶段 0~4（需求/领域模型/架构/技术规格/原型验证/开发计划/任务票据）已完成，见 `docs/` 与 `CONTEXT.md`。
阶段 5（编码）进行中：票据 01（protocol 契约）、票据 02（观看端信令客户端）、票据 03（共享端信令服务器）已完成，含双端本机对拍全绿。

## 构建与测试

环境：.NET SDK `D:\developTools\dotnet`（8.0）、Gradle `D:\developTools\gradle`（8.10.2）、JDK 17（`D:\developTools\JDK\jdk17.0.16`）。

```powershell
# C# 端（viewer；Parity 用例需先起对拍服务器，日常用 --filter "Category!=Parity" 排除）
dotnet test viewer\tests\Phone.Share.Protocol.Tests
dotnet test viewer\tests\Phone.Share.Tests --filter "Category!=Parity"

# Kotlin 契约模块（protocol/kotlin）
#  wrapper 下载源已配置腾讯镜像
protocol\kotlin\gradlew.bat -p protocol\kotlin test

# 共享端 signaling 模块（android/signaling）
android\gradlew.bat -p android\signaling test

# 双端本机对拍：Kotlin 信令服务器 ↔ C# 信令客户端（自动构建、启停与测试）
powershell -File protocol\parity\run-parity.ps1
```

## 关键文档

- `docs/specs/2026-09-20-technical-spec.md`：技术规格（实现决策与测试缝）
- `docs/plans/2026-09-20-phone-screen-share-plan.md`：实施计划（14 任务）
- `.scratch/phone-screen-share/issues/`：任务票据（纵贯切片，带阻塞边）
- `protocol/schema/`：跨端契约（控制消息、信令报文）