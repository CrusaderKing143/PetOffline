# Pet Offline Development Status

更新时间：2026-07-15

总体状态：Milestone 0 Foundation 与 Milestone 1 Day 1 已通过真实 Unity 验收；完整游戏未完成。

当前里程碑：Milestone 2 — Day 2。

## 已完成

- 已完成资料归一、SOURCE_OF_TRUTH.md 与可执行 PLAN.md。
- Unity 与项目版本固定为 6000.3.14f1。
- Milestone 0：六个 asmdef、Input Actions、Bootstrap、additive SceneFlow、四个必需 Scene、UIRoot Mock 和基础 Validator 已完成并提交。
- Milestone 1 Day 1 世界灰盒已生成：Latte、拖鞋、老板抱枕、Camera A Goal、Camera B 扫描/遮挡/世界锥、BananaSlipZone、RobotPath、两个 GoalZone、世界触发和结尾路径均在 Game World。
- Day 1 显式状态机已完成：Opening → TaskShoes → TaskPillow → FinalBark → Report → Ending → Complete。
- 已实现轻物 85%、重物 60%、抱枕 Bark 掉落、拖鞋 2 秒、抱枕即时完成、当前任务局部重置与前置任务保留。
- Boss Call 使用确定性 14/26 秒时间表；成功产生 3 秒扫描冻结/安全窗，超时产生约 7 秒扩大警戒且不 Game Over。
- 已修复滑行期间转向/主动放物/Push/Lie、检测错误取消 Boss Call、返回标题后持久对话串场等问题。
- Day 1 报告后由 Core Command 启动世界结尾演出，保存 DayOneCompleted，卸载 Day 1 并加载已绑定的 Day 2 runtime skeleton。
- Project Validator 现递归检查 Missing Script，并要求 Day 1/Day 2 各有唯一正确 FlowController。
- 关闭整个 UIRoot 的 Day 1 自动化已证明：Gameplay Action Map、键盘移动、搬运、滑行锁、Camera/Robot FixedUpdate、真实 Goal Trigger、报告、结尾、保存和 Day 2 转场仍运行。

## 真实执行证据

| Gate | 结果 | 证据 |
| --- | --- | --- |
| Day 1 Setup / Unity Import | PASS | `Artifacts/TestResults/DayOneSetup.log`、`ProjectSetup_M1_Fixed.log` |
| EditMode | PASS 2/2 | `Artifacts/TestResults/EditMode_M1.xml`、`EditMode_M1.log` |
| PlayMode | PASS 11/11 | `Artifacts/TestResults/PlayMode_M1.xml`、`PlayMode_M1.log` |
| Architecture Validation | PASS | `Artifacts/TestResults/ValidationReport.txt`、`Validation_M1.log` |
| Compile / MissingReference scan | PASS | 四份最终日志无 CS error、NullReference、MissingReference 或 Missing Script |

## 尚未完成

- Milestone 2：Day 2 完整 Sun/Confirm/Feeder/Backup/双结局世界逻辑及测试。
- Milestone 3：生产 UGUI 标题、HUD、字幕、报告、选择、暂停、设置和完整 UIRoot_Test 面板。
- Milestone 4：统一美术、动画、音频、VFX、灯光和最终演出截图。
- Milestone 5：全 P0、两个标题到结局 standalone Smoke、性能、Windows Development/Release Build 与启动验收。
- `Builds/Windows/PetOffline.exe` 尚未生成；`Artifacts/Screenshots` 尚无最终验收截图。

## Milestone 状态

| Milestone | 状态 | 完成证据 |
| --- | --- | --- |
| 文档基线 | 完成 | PLAN.md 与 Docs/Development 文档 |
| Milestone 0 Foundation | 完成 | commit `d08c995`；M0 Setup/EditMode/PlayMode/Validation |
| Milestone 1 Day 1 | 完成 | EditMode 2/2、PlayMode 11/11、Validation PASS、Day 1 Scene/配置资产 |
| Milestone 2 Day 2 | 进行中 | 当前只有可绑定 runtime skeleton，无 Day 2 P0 测试 |
| Milestone 3 UGUI | 未开始 | 仅 Foundation Mock Host |
| Milestone 4 Art/Audio/VFX | 未开始 | 无视觉验收截图 |
| Milestone 5 QA/Build | 未开始 | 无完整 Smoke 或 Windows Build |

## 工作区边界

以下现有改动不归 Milestone 1 所有，提交时不得混入：用户修改的 AGENTS.md、SampleScene 删除、未跟踪的 Assets/Scenes/Main.unity、Docs/Reference 与 SceneTemplateSettings.json。

## 下一步

1. 创建并提交 Milestone 1 的精确文件集。
2. 生成 Day 2 配置资产和真实世界灰盒 Scene。
3. 实现 10 秒确认循环、Feeder Camera Offline、不可跳过的 Backup 教学、20 秒完成和两个结局。
4. 运行 Day 2 EditMode/PlayMode、UIRoot-disabled 与 Validator 后提交 Milestone 2。
