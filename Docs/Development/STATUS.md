# Pet Offline Development Status

更新时间：2026-07-15  
总体状态：Milestone 0 Foundation 已通过真实 Unity 验收；完整游戏未完成。  
当前里程碑：Milestone 1 — Day 1。

## 已完成

- 已核对 AGENTS.md、三份现有 HTML、53 张 PNG 和两份 Excel 的资料角色。
- 已确认最终范围为两关垂直切片和两个结局。
- 已完成 Docs/Development/SOURCE_OF_TRUTH.md 的冲突裁决。
- 已完成 PLAN.md 的目录、Assembly、Scene、Hierarchy、状态机、系统、SO、资源、自动化、测试、构建、里程碑和回退方案。
- 已创建 DECISIONS.md、KNOWN_GAPS.md 和 TEST_REPORT.md 基线。
- 已确认 ProjectVersion 和本机 Editor 都是 Unity 6000.3.14f1。
- 已创建并编译六个 PetOffline asmdef、Input Actions、Bootstrap、Day 1、Day 2 与 UIRoot_Test Scene。
- Build Settings 顺序为 Bootstrap、Day 1、Day 2；UIRoot_Test 存在但禁用。
- Unity Test Runner：EditMode 1/1 Passed，PlayMode 3/3 Passed，两个日志均以 code 0 退出。
- `Tools/Pet Offline/Validate Project` 与 batch 验证报告均为 PASS。
- Bootstrap 已在真实 PlayMode 加载；当前 PetOffline Console Error=0。

## 尚未完成或未验证

- 未实现 Day 1、Day 2、UGUI、Art/Audio/VFX。
- 未运行 Day 1/Day 2 P0、UIRoot 断电或完整 standalone Smoke。
- Artifacts/TestResults 已有 Foundation 证据；Artifacts/Screenshots 尚未生成验收截图。
- 未生成或启动 Builds/Windows/PetOffline.exe。

## Milestone 状态

| Milestone | 状态 | 完成证据 |
| --- | --- | --- |
| 文档基线 | 完成 | PLAN.md 与 Docs/Development 五份文档 |
| Milestone 0 Foundation | 完成 | Setup.log、EditMode.xml 1/1、PlayMode.xml 3/3、ValidationReport PASS |
| Milestone 1 Day 1 | 未开始 | 无实现或 PlayMode 证据 |
| Milestone 2 Day 2 | 未开始 | 无实现或 PlayMode 证据 |
| Milestone 3 UGUI | 未开始 | 无 UIRoot_Test 或断电测试证据 |
| Milestone 4 Art/Audio/VFX | 未开始 | 无视觉验收截图 |
| Milestone 5 QA/Build | 未开始 | 无测试 XML、Build 或 standalone 日志 |

## 当前工作区注意事项

2026-07-15 文档基线前观察到：

- AGENTS.md 已修改。
- Assets/Scenes/SampleScene.unity 及其 meta 被删除。
- Assets/Scenes/Main.unity 及其 meta 为未跟踪文件。
- Docs/ 当前为未跟踪内容。

这些改动的所有权未确认。后续自动化不得删除、重置或把它们混入无关提交。

## 下一步

1. 提交 Milestone 0，只暂存 PetOffline 自有路径与明确 ProjectSettings/Package 改动。
2. 实现 Day 1 世界灰盒、输入、搬运、Camera A/B、机器人、两个任务、Final Bark、报告请求与测试。
3. 使用 PetOffline 专属 relay 或关闭 Editor 后的 batchmode 验证，禁止使用多实例下指向其他项目的全局 Console。
