# Pet Offline Test Report

更新日期：2026-07-15

报告状态：Milestone 0 Foundation 与 Milestone 1 Day 1 已执行真实 Unity 测试。

结论：Day 1 功能灰盒、架构边界和 Day 1 UIRoot 断电链路通过；Day 2、生产 UI、完整 Smoke 与 Build 尚未通过。

## 环境

| 项目 | 结果 | 证据 |
| --- | --- | --- |
| ProjectVersion | PASS | Unity 6000.3.14f1 (d68c3f99a318) |
| Unity Editor | PASS | `C:\Program Files\Unity 6000.3.14f1\Editor\Unity.exe` |
| 运行方式 | PASS | PetOffline 无锁时使用 batchmode；未使用指向其他项目的全局 Unity MCP |
| 编译 | PASS | 最终 Setup/EditMode/PlayMode/Validation 日志无 `error CS` |

## Milestone 1 实际命令

```powershell
Unity.exe -batchmode -nographics -quit -projectPath D:\UGit\PetOffline `
  -executeMethod PetOffline.Editor.DayOneAutomation.SetupDayOneBatch

Unity.exe -batchmode -nographics -quit -projectPath D:\UGit\PetOffline `
  -executeMethod PetOffline.Editor.ProjectAutomation.SetupBatch

Unity.exe -batchmode -nographics -projectPath D:\UGit\PetOffline `
  -runTests -testPlatform EditMode -testResults Artifacts\TestResults\EditMode_M1.xml

Unity.exe -batchmode -projectPath D:\UGit\PetOffline `
  -runTests -testPlatform PlayMode -testResults Artifacts\TestResults\PlayMode_M1.xml

Unity.exe -batchmode -nographics -projectPath D:\UGit\PetOffline `
  -executeMethod PetOffline.Editor.ProjectValidator.ValidateBatch
```

Unity 6 Test Runner 完成后在本机保留了 Editor 进程；自动化按 ProjectPath 只关闭 PetOffline 实例并确认 `Temp/UnityLockfile` 清除，未触碰另一个已打开的 Unity 项目。

## 最终执行结果

| Gate | 状态 | 结果文件 |
| --- | --- | --- |
| Day 1 Setup / Scene generation | PASS | `DayOneSetup.log`、`ProjectSetup_M1_Fixed.log` |
| EditMode | PASS 2/2、Failed 0、Skipped 0 | `EditMode_M1.xml`、`EditMode_M1.log` |
| PlayMode | PASS 11/11、Failed 0、Skipped 0 | `PlayMode_M1.xml`、`PlayMode_M1.log` |
| Architecture Validator | PASS | `ValidationReport.txt`、`Validation_M1.log` |
| Missing Script / RequiredReference | PASS（当前 M0/M1 Scene） | Validator 递归 Scene 层级并检查必需引用 |
| Full title-to-Restore smoke | NOT RUN | Milestone 2/3/5 |
| Full title-to-Quiet smoke | NOT RUN | Milestone 2/3/5 |
| Windows x64 Development/Release | NOT RUN | `Builds/Windows/PetOffline.exe` 尚不存在 |
| Screenshots | NOT RUN | Milestone 4/5 |

## 通过的 EditMode

- `ProjectValidatorPasses`：asmdef、Scene、Build Settings、Layer、世界/UI 边界、Missing Script、RequiredReference、唯一 Day1/Day2 runtime。
- `DayOneCameraAndCarryConfigsMatchPlayableBaseline`：拖鞋 2 秒、Camera B 7.1/54°/0.24 秒、轻物 0.85、重物 0.60、Bark 掉落与 Robot push 0.65。

## 通过的 PlayMode

- Bootstrap 持久服务、无 World 等待状态。
- 同时切关请求只加载一个 World Scene。
- 返回标题停止持久 DialogueDirector 且不执行旧 completion callback。
- UIRoot_Test 在无 World 时绑定 Mock ViewModel。
- 拖鞋 Goal 需要 2 秒。
- Camera B 只重置当前拖鞋任务。
- 抱枕失败保留已完成拖鞋任务。
- 检测不会取消正在进行的 Boss Call。
- Boss Call 成功冻结 Camera B 扫描 3 秒；超时进入约 7 秒扩大警戒并自动恢复。
- 重抱枕 Bark 掉落，Robot 推动 0.65，狗窝 Goal 立即完成。
- 关闭整个 UIRoot 后仍通过真实 Gameplay Action Map 完成移动、搬运、滑行锁、Camera/Robot FixedUpdate、真实 Goal Trigger、Final Bark、Report→Ending、DayOneCompleted 保存、Day 1 卸载和 Day 2 runtime 绑定。

## P0 清单状态

- [x] Architecture boundary test（M0/M1 范围）
- [ ] UIRoot disabled gameplay test（Day 1 已通过；Day 2 待 Milestone 2）
- [x] UIRoot mock preview test（当前 Mock 范围）
- [x] Day 1 shoe completion test
- [x] Day 1 detection reset test
- [x] Day 1 previous-task preservation test
- [x] Day 1 pillow and robot interaction test
- [x] Day 1 final report transition test
- [ ] Day 2 first 10-second confirmation test
- [ ] Day 2 feeder return resets progress test
- [ ] Day 2 ignored confirmation pauses progress test
- [ ] Day 2 feeder-camera disable test
- [ ] Day 2 backup-camera activation test
- [ ] Day 2 wrong-route confirmation test
- [ ] Day 2 correct-route 20-second completion test
- [ ] Restore Connection ending test
- [ ] Keep Quiet ending test
- [ ] Save/unlock test（DayOneCompleted + Day2 load 已覆盖；跨进程 Continue 待 M5）
- [ ] Full title-to-ending smoke：Restore Connection
- [ ] Full title-to-ending smoke：Keep Quiet

## 失败与修复记录

首次 M1 PlayMode 基线为 8/9，失败于 UIRoot-disabled 测试的合成键盘移动。根因是 batchmode 无 Game View 焦点，而测试复用/新增 Keyboard 时未设置 Input System 的 Editor/background 行为。修复为独立虚拟 Keyboard、`IgnoreFocus`、`AllDeviceInputAlwaysGoesToGameView` 和显式 device filter；重跑后完整 PlayMode 11/11。

测试结果只有在 XML 为 Passed、Failed=0、最终日志无编译/引用异常且 Unity 进程退出后才记为通过。
