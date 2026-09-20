---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: 'f092686c-0079-404c-a51d-c03514b2247a'
  PropagateID: 'f092686c-0079-404c-a51d-c03514b2247a'
  ReservedCode1: 'e2ae8276-f66a-4263-b0ed-688c772bc489'
  ReservedCode2: 'e2ae8276-f66a-4263-b0ed-688c772bc489'
---

# 反向控制采用无障碍手势注入，而非 adb input 注入

观看端点击需要在手机屏幕生效。scrcpy 类工具依赖 adb shell input（本质利用 adb 特权），但本项目定位 WiFi 直连、免数据线免 adb 依赖，故采用 AccessibilityService.dispatchGesture 注入点击与滑动。代价：需用户开启无障碍授权（采集三授权之一）、极少数应用检测到无障碍会拒绝运行、无法操作系统级区域；收益：零 adb 依赖、纯 WiFi 链路自洽。

## Consequences

- 无障碍未开启时反向控制不可用，但纯观看不受影响（PRD F08 引导策略）
- 返回键走 GLOBAL_ACTION_BACK，同受无障碍授权约束