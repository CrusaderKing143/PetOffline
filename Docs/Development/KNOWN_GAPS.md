# Pet Offline Known Gaps

更新日期：2026-07-15

当前两关功能垂直切片、两个结局、保存/Continue、架构校验、自动测试和 Windows Build 已完成。以下项目是剩余的资产、体验和外部环境验收，不应再描述为 Day 1、Day 2、UGUI 或 Build 尚未实现。

## 资料与资产

| 剩余缺口 | 当前影响 | 关闭条件 |
| --- | --- | --- |
| 仓库缺少原计划中的 `03_LatestDesignSource.pdf` | 无法重新独立核对晚期第一、第二关程序需求原文 | 补齐 PDF 后按既定优先级复审 `SOURCE_OF_TRUTH.md` |
| 仓库缺少 `05_ArtSource.mg` | 无法提取或核对正式分层美术 | 补齐并解析源文件，或由正式授权的新资产替代 |
| 实际 Web 文件为 `03_WebPlayableReference.html`，与原计划编号 `04_...` 不一致 | 只影响资料追踪，不影响当前实现 | 保持 `SOURCE_OF_TRUTH.md` 中的实际路径记录 |
| `TitleBackground.png` 的作者和授权范围未确认 | 不能据此声明已获得公开发行授权 | 补齐来源与许可，或替换为许可明确、项目自制的图像 |
| 中文字体二进制缺少可核对的下载地址、版本和许可证文本 | 离线运行正常，但公开分发授权链不完整 | 补齐准确来源和许可证，或替换字体 |
| 世界角色、家具和设备主要为程序化占位视觉 | 功能完整，但不代表正式商业美术品质 | 使用正式角色、家具、动画和特效替换表现子节点，并重新回归 |
| 七组 WAV 与 AudioCue 为项目生成占位音效 | 无第三方音频运行依赖，但最终听感尚未验收 | 完成人工混音验收，或换成来源和许可可追溯的正式音频 |

资产明细和哈希见 `Docs/Development/ASSET_PROVENANCE.md`。在来源和许可关闭前，不得把 Temporary/Unresolved 资产描述为已获公开发行授权。

## 人工体验验收

| 剩余缺口 | 当前影响 | 关闭条件 |
| --- | --- | --- |
| 未完成首次玩家正常流程 12–15 分钟计时 | 自动测试使用缩短等待，不能证明目标节奏 | 由未参与开发的玩家完整通关并记录分段时间 |
| 未系统验证首次玩家是否理解 Camera B、投食器摄像头和 Backup Camera 教学 | 自动测试证明规则正确，不证明教学清晰 | 观察首次游玩并记录卡点、误解和软锁 |
| 未人工以正常时长完成 Release 分支 | Release 已通过启动检查，但不等于人工完整通关 | 使用最终 Release EXE 正常时长完成至少一条完整分支并保存日志 |
| 两个结局主要由自动化 Windows Player 覆盖 | 逻辑已通过，最终演出节奏仍需人工确认 | 人工检查 Restore Connection 与 Keep Quiet 的字幕、音频和返回流程 |

## 性能与平台验收

| 剩余缺口 | 当前影响 | 关闭条件 |
| --- | --- | --- |
| 没有目标机 60 FPS、1% low、GC 和 Profiler 记录 | 不能给出正式性能预算结论 | 在目标配置上采集完整关卡 Profiler 和帧时间证据 |
| 长时间暂停、窗口失焦/恢复和多显示器未系统测试 | 极端桌面环境行为未知 | 覆盖 Alt+Tab、最小化、焦点恢复、显示器切换和暂停恢复 |
| 未在第二台电脑、VM 或新 Windows 用户下断网运行 | 仍可能存在机器缓存、权限或环境依赖 | 在干净环境断网启动 Release，完成启动、存档、Continue 和退出 |

## 截图限制

`Artifacts/Screenshots` 中九张交付图片均为 1920×1080：

- `Title.png`
- `Day1_Opening.png`
- `Day1_Report.png`
- `Day2_CameraOffline.png`
- `Day2_BackupActive.png`
- `Day2_Report.png`
- `Day2_Choice.png`
- `Ending_KeepQuiet.png`
- `Ending_Restore.png`

隐藏 Windows Player 会话受测试机虚拟显示限制，只能输出 1024×768。完整原始帧保留在 `Artifacts/Screenshots/Native`；报告、选择和结局图片由这些真实 Player 帧居中裁切为 16:9 并使用 bicubic 缩放至 1920×1080。标题和世界状态图片使用 1920×1080 渲染证据。

这些图片能证明真实 UI 和状态存在，但不能替代目标 1920×1080 显示器上的人工像素、字体、裁切和多显示器验收。

## 已完成且不再属于 Gap

- Title → Day 1 → Report/Ending → Day 2 → Report → Choice → 两个 Ending → Return/Restart。
- Day 1 鞋子、抱枕、Camera B、老板来电、机器人和最终报告。
- Day 2 晒太阳、10 秒确认、投食器摄像头离线、Backup Camera 教学和正确路线完成。
- Restore Connection 与 Keep Quiet 两个结局。
- 保存、Day 2 解锁以及两个独立 Player 进程之间的 Continue。
- 生产 UGUI、UIRoot Mock 和 UIRoot-disabled 世界玩法。
- Project Validator、EditMode 3/3、PlayMode 33/33。
- Windows Player 双结局冒烟和跨进程存档测试。
- Development 与 Release Windows Build。
- Release EXE 启动、响应和无运行时异常检查。
- 九张 1920×1080 验收图及 Player 原始帧。
- 移除 `com.unity.ai.assistant`，两个 Build 的 Managed 目录均无 `Unity.AI.*`。

## 工作区边界

仓库仍包含归属不同的 `AGENTS.md`、`Assets/Scenes/Main.unity`、SampleScene 删除、`Docs/Reference` 和 `ProjectSettings/SceneTemplateSettings.json` 改动。

提交交付里程碑时必须精确暂存 Pet Offline 文件，禁止使用 `git add -A`，也不要 reset、checkout 或覆盖这些用户、来源文件。
