# Pet Offline Test Report

更新日期：2026-07-15 23:30（UTC+8）

结论：当前工作树的架构校验、全部 EditMode/PlayMode、Windows Player 双结局冒烟、跨进程 Continue、Development/Release 构建和 Release 启动均通过。最终 XML 均为 `Passed` 且 `Failed=0`；最终日志未发现编译错误、Missing Script、MissingReference 或运行时异常。

最终 Editor 回归时间为 23:20–23:22，晚于 `FullTitleToEndingSmokeTests.cs` 的 22:47 最后修改，因此本报告不沿用修改前的普通全量测试结果。

## 环境

| 项目 | 值 |
| --- | --- |
| Unity | `6000.3.14f1` |
| OS / Target | Windows x64 / StandaloneWindows64 |
| Scripting Backend | Mono x64（本机未安装 Windows IL2CPP variation） |
| Renderer | URP 2D |
| Input | Unity Input System |
| 测试日期 | 2026-07-15（UTC+8） |

## 最终结果

| 运行 | 结果 | 数量/构建摘要 | 证据 |
| --- | --- | --- | --- |
| Project Validator | PASS | 四场景、边界、引用、Build Settings、必需资产 | `ValidationReport.txt`、`Validation_Final.log` |
| EditMode | PASS | 3/3 | `EditMode_Final.xml`、`EditMode_Final.log` |
| PlayMode | PASS | 33/33 | `PlayMode_Final.xml`、`PlayMode_Final.log` |
| Standalone 双结局/截图 | PASS | 2/2 | `StandalonePlayMode_Screenshots_Final.xml`、对应 Player/Editor log |
| 跨进程 Seed | PASS | 1/1 | `Standalone_CrossProcess_Seed.xml` |
| 下一进程 Continue | PASS | 1/1 | `Standalone_CrossProcess_Continue.xml` |
| Development Build | PASS | 192,566,482 bytes；0 error / 0 warning | `WindowsBuild_Development.txt`、`BuildDevelopment_Final.log` |
| Release Build | PASS | 126,796,871 bytes；0 error / 0 warning | `WindowsBuild_Release.txt`、`BuildRelease_Final.log` |
| Release launch smoke | PASS | 窗口响应正常，D3D11 启动，无异常 | `Player_Release_Final.log` |

所有相对证据路径均位于 `Artifacts/TestResults`。

## 指定 19 项 P0 覆盖

| # | 要求 | 最终状态 | 主要覆盖 |
| --- | --- | --- | --- |
| 1 | Architecture boundary test | PASS | EditMode asmdef/世界 UI 边界 + Validator |
| 2 | UIRoot disabled gameplay test | PASS | Bootstrap/World PlayMode 断电测试 |
| 3 | UIRoot mock preview test | PASS | `90_UIRoot_Test` Mock/生产面板测试 |
| 4 | Day 1 shoe completion | PASS | 2 秒 Goal Hold |
| 5 | Day 1 detection reset | PASS | Camera B 当前任务局部重置 |
| 6 | Day 1 previous-task preservation | PASS | 抱枕失败不回退拖鞋 |
| 7 | Day 1 pillow and robot interaction | PASS | 重物 Bark 掉落、Robot 推动、Q 推动 |
| 8 | Day 1 final report transition | PASS | Final Bark → Report → Ending → Day 2 |
| 9 | Day 2 first 10-second confirmation | PASS | SunFirst/CameraCheck |
| 10 | Day 2 feeder return resets progress | PASS | 回投食器后 SunTime 清零 |
| 11 | Day 2 ignored confirmation pauses progress | PASS | 警戒增强且进度暂停 |
| 12 | Day 2 feeder-camera disable | PASS | BananaPeel + Robot，`FoodCameraActive=false` |
| 13 | Day 2 backup-camera activation | PASS | SideDoor 世界触发 |
| 14 | Day 2 wrong-route confirmation | PASS | Backup 下一次 10 秒确认不被跳过 |
| 15 | Day 2 correct-route 20-second completion | PASS | 客厅路线、无重捕获、FinalSun 完成 |
| 16 | Restore Connection ending | PASS | 真实按钮 + 10 秒确认循环重启 + Ending |
| 17 | Keep Quiet ending | PASS | 真实按钮 + 睡眠演出 + 固定字幕 |
| 18 | Save/unlock | PASS | PlayerPrefs 保存、Day 2 解锁、两个独立 Player 进程 Continue |
| 19 | Full title-to-ending smoke | PASS | Windows Player 中 New Game/Continue、两关、报告、Choice、Return/Restart |

## Standalone 说明

- `StandalonePlayMode_Screenshots_Final.xml` 来自 Unity Test Framework 构建并启动的真实 Windows Player，使用正式 Bootstrap、additive World Scene、Physics2D、状态机和生产 UGUI；只缩短等待并通过公开 Gameplay/API 推进，不直接写权威 Flow 状态。
- 跨进程证据分为两个独立 Player：第一个完成流程并保留 `DayOneCompleted`，第二个禁止测试内 Seed，直接读取已有 PlayerPrefs 后点击生产 `ContinueButton` 进入 Day 2。
- 最终 Release 另行启动 12 秒，窗口标题为 `Pet Offline`、进程响应正常，再通过 Alt+F4 正常关闭；该项是 Release launch smoke，不冒充人工 12–15 分钟完整分支。

## 截图证据

`Artifacts/Screenshots` 中以下文件为 1920×1080：

- `Title.png`
- `Day1_Opening.png`
- `Day1_Report.png`
- `Day2_CameraOffline.png`
- `Day2_BackupActive.png`
- `Day2_Report.png`
- `Day2_Choice.png`
- `Ending_KeepQuiet.png`
- `Ending_Restore.png`

测试机的隐藏 Player 会话被系统限制为 1024×768。完整 UGUI 原始帧保留在 `Artifacts/Screenshots/Native`；报告、选择和结局图由这些真实帧居中裁切为 16:9 并高质量缩放到 1920×1080。标题和世界状态图使用 1920×1080 相机渲染证据。该限制不影响逻辑测试，但不等同于目标显示器上的最终人工视觉验收。

## Build 与离线检查

- `Builds/Windows/Development/PetOffline.exe` 与 `Builds/Windows/PetOffline.exe` 均由 `WindowsBuildAutomation` 生成。
- 构建前会清理对应旧输出，避免 Managed DLL 残留。
- `Packages/manifest.json` / `packages-lock.json` 不含 `com.unity.ai.assistant`。
- 两个 Build 的 `*_Data/Managed` 均未发现 `Unity.AI.*`。
- Unity Connect、Diagnostics、Analytics/Ads 启动与 `submitAnalytics` 已关闭；仍需在断网干净机上完成外部验证。

## 未执行的人工/环境测试

- 首次玩家完整 12–15 分钟计时与教学理解度。
- 目标机 60 FPS、1% low、GC/Profiler 证据。
- 长时间暂停、窗口失焦/恢复和多显示器行为。
- 第二台电脑/VM 或新 Windows 用户的断网干净机运行。
- Release 人工完整走完一条正常时长分支。

这些项目记录在 `KNOWN_GAPS.md`，不应被本报告中的自动化 PASS 替代。
