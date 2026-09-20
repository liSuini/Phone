---
AIGC:
  ContentProducer: '001191110102MAD55U9H0F10002'
  ContentPropagator: '001191110102MAD55U9H0F10002'
  Label: '1'
  ProduceID: '746236f8-1b8c-43ce-903c-03c9e3bd4659'
  PropagateID: '746236f8-1b8c-43ce-903c-03c9e3bd4659'
  ReservedCode1: 'dfc4b918-0ed7-4e18-b3b8-c58aaef98427'
  ReservedCode2: 'dfc4b918-0ed7-4e18-b3b8-c58aaef98427'
---

# 06: 声音端到端与音画同步（M2 门禁）

**What to build:** 共享端采集手机正在播放的媒体声音（AudioPlaybackCapture）经 WebRTC Opus 推流，观看端经 NAudio 播放，音画同步达到主观无感知差异。含"音源拒绝被采集"的静默降级提示。

**Blocked by:** 05（音频捕获依赖 MediaProjection token 与屏幕授权流程）

**Status:** ready-for-agent

**实现细节：** 见开发计划 Task 8/9（AudioCapture、AudioPlayer）

- [ ] 共享端声音捕获接入 WebRTC 音轨（Opus）；策略判定纯函数单元测试通过
- [ ] 观看端环形缓冲单元测试通过；NAudio WasapiOut 播放 48k PCM，欠载率可观测
- [ ] 真机同步验收：播放口型明显的视频，主观无音画偏差，验收记录落盘
- [ ] 音源拒绝采集的 APP（银行/部分视频 APP）：无声且状态栏提示"该应用不允许采集声音"，无报错不崩溃
- [ ] PRD 验收标准 1 达成；提交