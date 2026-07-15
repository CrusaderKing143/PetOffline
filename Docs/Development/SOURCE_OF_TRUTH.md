# Pet Offline Source of Truth



## 1. 当前参考资料清单

实际存在：

| 资料 | 角色 |
| --- | --- |
| AGENTS.md | 项目目标、硬性架构、流程、测试和 Definition of Done |
| Docs/Reference/02_UnityImplementationPlan.html | 最高优先级技术实现依据 |
| Docs/Reference/01_UnityDesignPlan.html | 最终玩法、体验、两关流程和表现依据 |
| Docs/Reference/03_WebPlayableReference.html | 行为、提示、时序和结局参考；不得复制架构 |
| Docs/Reference/images/*.png | 53 张视觉、地图、UI 和历史方案参考图 |

## 2. 权威优先级

发生冲突时按以下顺序裁决：

1. 当前用户指令与 AGENTS.md 的项目硬性契约。
2. Docs/Reference/02_UnityImplementationPlan.html。
3. Docs/Reference/01_UnityDesignPlan.html。
4. Docs/Reference/03_WebPlayableReference.html，仅作行为、时序和文案参考。
5. Docs/Reference/images/*.png，仅作视觉和历史方案参考。

当前仓库没有 `03_LatestDesignSource.pdf`、`04_WebPlayableReference.html` 或 `05_ArtSource.mg`；实际 Web 文件编号为 `03_WebPlayableReference.html`。因此无法独立核验 PDF 后期“第一关程序需求文档 / 第二关程序需求文档”，也无法解析或提取 `.mg` 分层美术。03 Web 原型只用于核对行为，不是 Unity 架构来源；不存在的文件不能被当作已经审阅的依据。

## 3. 最终范围

最终交付是 12–15 分钟、Windows x64、离线、固定对话、键鼠操作的 Unity 6 LTS 两关垂直切片：

    Title Screen
    → Day 1: 狗已上线
    → Day 1 Report
    → Day 1 Ending
    → Day 2: 偷偷安抚
    → Day 2 Report
    → Final Choice
    → Restore Connection 或 Keep Quiet Ending
    → Return to Title / Restart

必须包含：

- Day 1 拖鞋、抱枕、Camera A/B、Boss Call、香蕉滑区、机器人、Final Bark、报告和自动演出。
- Day 2 20 秒晒太阳、10 秒确认循环、投食器摄像头离线机关、备用摄像头错误路线、客厅正确路线、报告和两个结局。
- 保存/解锁、暂停/设置、返回标题/重开。
- 单一简体中文版本；`CAMERA OFFLINE`、`BACKUP CAMERA ACTIVE` 等英文是固定世界/系统文案，不代表需要多语言切换或语言存档。
- UIRoot_Test Mock 预览。
- EditMode、PlayMode、架构校验、Windows x64 standalone Build 和真实启动 Smoke。

明确排除：

- 被废弃的三章节完整版和第三章设备玩法。
- 问卷、研究页和早期实验页面。
- WebView、HTML 嵌入、联网依赖和运行时生成式 AI。
- 点击地面移动、触屏控制、完整商业版内容。
- Web 原型的 `R` 公开重置键、浏览器日志导出和 DOM/Canvas 调试能力；若保留重置，只能作为 Development Build 调试命令。
- DOTS、NavMesh、第三方 Quest 框架和无实际需求的通用抽象。

非 P0、不得反向扩大范围的表现参考：

- “期待值 / 安定值”、情绪曲线、Action Line、AI Analyse 等可作为报告或 HUD 风格参考，但不是通关条件，也不进入权威关卡状态。
- 标题页 Quit 文案、成就、额外 Toast、报告动态计数可在 P0 全流程稳定后再评估；缺少它们不阻断垂直切片验收。
- 99_Test_Playground、会议电视 RenderTexture、动态世界截图均为可选工具或表现，不得替代正式世界 Scene 与固定报告内容。

## 4. 工程、场景、输入与数据契约

工程固定为本机已安装的 Unity `6000.3.14f1`、Windows x64、1920×1080、16:9、URP 2D Renderer。使用 Input System、TextMeshPro、Cinemachine、Unity Test Framework、Assembly Definitions 和显式状态机；不增加 DOTS、NavMesh、第三方 Quest 框架或 Web 依赖。

必需 Scene：

1. `00_Bootstrap`
2. `10_Day1_Meeting`
3. `20_Day2_Sunbath`
4. `90_UIRoot_Test`

`99_Test_Playground` 可选。Release Build 只启用 Bootstrap 与两关世界 Scene；`90_UIRoot_Test` 必须存在并可单独打开，但不进入 Release。

`00_Bootstrap` 持有唯一且持久的：

- GameSession
- SceneFlowService
- InputRouter
- AudioService
- SaveService
- DialogueDirector
- Main Camera / Cinemachine Brain / AudioListener
- UIRoot / EventSystem

Bootstrap 常驻，每次只 additive 加载一个 World Scene。两个 World Scene 都必须包含以下明确根节点，不在其中放屏幕 HUD：

    WorldRoot
    Environment
    Collision
    Actors
    Interactables
    Devices
    Sensors
    Triggers
    Paths
    WorldVFX
    WorldAudio
    LevelFlow
    VirtualCamera

输入使用独立 Gameplay / UI Action Map：

| Action | 默认绑定 |
| --- | --- |
| Move | WASD / 方向键 |
| Interact / Pick Up / Drop | E |
| Bark | Space |
| Push | Q |
| Lie / Sunbathe | Left Shift |
| Pause | Escape |
| UI | 鼠标与键盘导航 |

报告、选择或暂停打开时切换到 UI Map；关闭后恢复 Gameplay Map。禁用 UIRoot 不得禁用 Gameplay Map。Release 不提供点击地面移动，也不公开 Web 的 `R` 重置键。

配置资产固定为六类：

- LevelConfigSO
- DialogueSequenceSO
- ReportDefinitionSO
- CameraScanConfigSO
- CarryableConfigSO
- AudioCueDefinitionSO

SO 只保存不可变配置。`SunTime`、`FoodCameraActive`、当前任务状态、携带关系、检测状态和完成标记属于运行时关卡/GameSession，禁止写回 SO。两个固定关卡不创建通用 Quest 框架或额外 TaskDefinitionSO。



## 5. 不可违反的世界/UI 边界

必须在 Game World：

- Latte、房间、家具、拖鞋、抱枕、香蕉、机器人、投食器。
- Camera A/B、Feeder Camera、Backup Camera、视野锥和遮挡。
- GoalZone、SunZone、Feeder Area、SideDoor Trigger、Waypoint。
- 世界碰撞、音频、VFX 和自动结局演出。

UGUI 只能显示：

- 标题、HUD、目标、进度、提示。
- 视频窗、对话、通知、字幕、Bark Prompt。
- 报告、最终选择、暂停、淡入淡出、Toast。

硬性禁止：

- Latte、房间、摄像头视野、机器人或触发区使用 UGUI。
- 用 RectTransform/anchoredPosition 移动玩法对象。
- 用 GraphicRaycaster 做检测。
- UI 持有权威任务状态或直接控制世界任务。
- 把完整 Main Camera 放进 RawImage 后在 UI 内玩。
- 把 HTML 通过 WebView 嵌入 Unity。

PetOffline.UI → PetOffline.Core；PetOffline.Gameplay → PetOffline.Core；UI 与 Gameplay 不互引。

关闭整个 UIRoot 后，世界移动、搬运、摄像头、机器人和任务逻辑仍须运行；这是自动化 P0 门槛。

## 6. 最终行为基线

Day 1：

    Opening → TaskShoes → TaskPillow → FinalBark → Report → Ending → Complete

- 拖鞋在 Camera A GoalArea 保持 2 秒完成。
- 拖鞋初始位于 Dog Bed 旁；Camera A 是目标相机且永不检测玩家。
- Boss Pillow 初始位于 Camera A 前方，TaskShoes 完成前锁定。
- Camera B 在配置的左右端点之间固定循环扫描。
- Latte 未携带当前任务物时可安全穿过 Camera B；只有携带当前拖鞋/抱枕才会触发对应失败。
- 抱枕进入 Dog Bed GoalArea 立即完成。
- Camera B 失败只重置 Latte、当前任务物、Camera B 默认角度和临时警戒/检测状态；不重播 Opening，不清除已完成前置任务。
- 轻物携带首轮默认约 85%，重物约 60%；携带抱枕 Bark 必须掉落。
- Boss Call 成功提供约 3 秒安全窗；错过后 Camera B 扫描速度、FOV 和 range 临时增强约 5–8 秒，自动恢复且绝不 Game Over。
- Day 1 香蕉是配置化固定 BananaSlipZone；机器人固定巡逻、可阻挡 Latte，并按配置推动落地抱枕。
- Final Bark 无失败。
- Final Bark 后先进入固定 Report，再由 Continue 命令启动世界 Ending；保存 DayOneCompleted 后加载 Day 2。

Day 2：

    Start → SunFirst → CameraCheck → Loop → DestroyCamera
    → Backup → FinalSun → Report → Choice → End

- SunZone + Lie 才累计。
- 唯一可见主目标固定为：`让拿铁晒满20秒太阳`。
- Feeder Camera 活跃时第 10 秒触发确认；确认暂停进度；回投食器完成确认并清零。
- 忽略确认不会 Game Over；确认扫描临时扩大并加速，SunTime 保持暂停。
- BananaPeel + Robot 只关闭 Feeder Camera，不关闭投食器本体；运行时必须记录 `FoodCameraActive = false`。
- 摄像头离线后显示 `CAMERA OFFLINE` 与 `当前画面：墙`。
- 侧门激活 Backup Camera；错误路线仍触发下一次确认。
- 必须先完成一次 Backup Camera 错误路线教学，再从客厅路线、无活动确认相机重捕获时累计到 20 秒。
- Day 2 固定报告必须表达：Sunbathe completed、Confirmation failed、Camera offline、Current image: wall、Unable to confirm whether Latte still misses the owner。
- Restore Connection 必须可见地重启确认循环，而不是只亮起摄像头后直接结束。
- Keep Quiet 关闭远程确认、播放睡眠演出并显示最终字幕：

    它不是不想你。
    它只是终于不用证明它在想你。

## 7. 视觉资料结论

Docs/Reference/images 共 53 张 PNG：

- 52 张 PNG Color Type 2（RGB）。
- 1 张 PNG Color Type 6（RGBA），完整 Alpha 扫描最小值仍为 255。
- 图片尺寸约为 1024–1672 宽、559–1536 高。
- 其中有 4 对解码像素完全相同但文件编码不同的重复图：`1e53.../4cca...`、`283b.../a90e...`、`67bc.../e2c6...`、`7e7e.../ac69...`；53 个文件实际代表 49 个独立画面。
- 多数透明棋盘格已烘焙为像素，不能直接作为透明角色/道具切片。

03 Web 文件另含 16 个 Data URI、6 个唯一 WebP：2 张 RGB 背景/房间图，以及主人、Latte、老板、AI 共 4 张真正带 Alpha 的头像。四张头像只能在来源记录和画风统一后作为可替换的 UGUI 原型素材，不能变成世界玩法对象；两张复合背景仍只作构图参考。

因此图片只用于：

- 斜 45 度房间布局、家具分区和色彩气氛。
- HUD、报告、视频窗、提示的视觉层级。
- 人物头像和演出方向参考。

统一视觉基线：温暖手绘质感的斜 45°公寓，琥珀阳光/黄昏光与青绿家具、蓝灰设备形成冷暖对比；UGUI 使用米色纸张、剪贴簿和圆角卡片层级；摄像头状态使用普通蓝、警戒红、安全/完成绿。最终 PC HUD 不采用参考图中覆盖大半屏幕的密集 Dashboard 或触屏动作栏。

生产分类：

- `4b9f737dc644643325fe3f4877875989.png` 仅可在来源、授权和 1920×1080 放大质量确认后，作为 Title UGUI 背景候选；图片内 START/QUIT 只是烘焙视觉，必须覆盖真实、可导航的 UGUI Button。
- 晚期执行参考以 `57f16e9b...`（Day 1 制作优先级）、`714492e2...`（Day 1 两个顺序任务）和 `75851b14...`（含 Backup Camera 的 Day 2 制作优先级）为主，但规则仍由 01/02 决定。
- `227bdf4a...` 的“两个任务可自由顺序”、`ee932dcc...` 的“任选 3/6 恶作剧”、`1e53d750...`/`4cca7d44...` 与 `e18bc6b6...` 的“无备用摄像头教学”、`aebb5562...` 的错误 Day 1 晒太阳报告均为明确旧方案。
- 主人、老板和 AI 的多组表情稿存在两套明显不同画风且棋盘格已烘焙，必须先统一角色设计，再重新输出真实 Alpha 头像；不能原样当生产 Sprite。
- 游戏画面 Mockup、流程板、程序优先级图、地图、报告和合成角色表全部只作参考；不得直接作为世界背景、Collider 依据或带烘焙文字的 UI。
- 所有正式文字由 TMP 重建。复合房间图不得附加 Collider 后冒充可交互世界。

视觉资料内部还混有多种成熟度：游戏画面/标题/日报 Mockup、第一关和第二关流程板、房间布局图、角色表情头像与早期总览。无论图面看起来多完整，全部都处于低于 01/02 HTML 的参考层级；图中的第三章、额外指标、旧数值、旧任务顺序或 UI 操作方式一律不得升级为需求。

已识别的旧图内容还包括：任选恶作剧任务、挡 PPT、玩具球/纸团/纸箱、Camera C、零食奖励、老板信任、抱枕送盲区、Camera A 敌对检测，以及错误的 Day 1 晒太阳报告。这些均不进入最终两关。

不得直接推导：

- 世界 Collider/Sensor 结构。
- 任务权威状态。
- 已废弃章节或图内早期数值。
- 可直接用于生产的透明 Sprite。

## 8. Web 原型允许迁移与禁止迁移

01 内嵌 Web payload 与 03 独立文件完全相同，因此只审计和验证一次；不能把相同原型的两份副本当作两条独立证据来推翻 01 外层正文或 02 技术方案。

允许迁移：

- 拖鞋 2 秒、重物减速、Bark 掉落、Camera B 警戒、安全窗口、10 秒确认、返回投食器清零、机器人撞歪摄像头、Backup Camera、两种结局等行为关系。
- 固定台词、通知语气、报告层级、房间分区和 12–15 分钟节奏目标。
- Web 数值可作为第一轮配置起点，但必须由 LevelConfigSO 承载并经 Unity Playtest 调整。

可作为第一轮调参种子的原型数值包括：玩家速度 3.25、轻/重携带约 0.83/0.58、Camera B range 7.1/FOV 约 54°、警戒 range 8.45/FOV 约 81°/7.2 秒、检测保持 0.24 秒、Bark 窗口 3.6 秒、安全窗 3.2 秒、首次 Boss Call 14 秒、Robot 速度 1.22、推动抱枕 0.65。它们不是最终需求，正式默认值写入 DECISIONS.md/SO 并由 Playtest 校准。

禁止迁移：

- 单个 HTML Canvas 的绘制、坐标、圆/矩形碰撞、DOM 与游戏状态混合、`setTimeout` 驱动关卡、`localStorage` 存档。
- 点击地面移动、Canvas 深度排序、UI 图片视野判定、运行时截图代替世界内容。
- Web 中与高优先级文件冲突的开场并行、Day 1 演出/报告顺序、Day 2 多主任务显示和随机 Boss Call 节奏。

## 9. 项目校验与测试基线

必须提供菜单命令 `Tools/Pet Offline/Validate Project`，至少检查：

1. PlayerController2D 不在任何 Canvas 下。
2. CarryController 不在任何 Canvas 下。
3. CameraVisionSensor2D 不在任何 Canvas 下。
4. RobotPatrol 不在任何 Canvas 下。
5. LevelFlowController 及 Day 1/Day 2 实现不在任何 Canvas 下。
6. Gameplay 世界对象不使用 RectTransform/UGUI Graphic 承载玩法。
7. PetOffline.UI 不引用 PetOffline.Gameplay。
8. PetOffline.Gameplay 不引用 PetOffline.UI。
9. 必需 Scene、Build Settings 顺序、唯一 Bootstrap/UIRoot/Camera/EventSystem 和所有必需引用完整，无 Missing Script/MissingReference。

指定的 19 项 P0 测试：

1. Architecture boundary test
2. UIRoot disabled gameplay test
3. UIRoot mock preview test
4. Day 1 shoe completion test
5. Day 1 detection reset test
6. Day 1 previous-task preservation test
7. Day 1 pillow and robot interaction test
8. Day 1 final report transition test
9. Day 2 first 10-second confirmation test
10. Day 2 feeder return resets progress test
11. Day 2 ignored confirmation pauses progress test
12. Day 2 feeder-camera disable test
13. Day 2 backup-camera activation test
14. Day 2 wrong-route confirmation test
15. Day 2 correct-route 20-second completion test
16. Restore Connection ending test
17. Keep Quiet ending test
18. Save/unlock test
19. Full title-to-ending smoke test

测试文件存在不等于通过。只有真实 Unity 命令返回成功、XML 为 Passed/Failed=0、日志无编译或引用异常时才可记为 PASS。

## 10. Build、Artifacts 与 Definition of Done

固定证据落点：

- `Artifacts/TestResults`：Setup、Validation、EditMode、PlayMode、Standalone Smoke 日志/XML/JSON。
- `Artifacts/Screenshots`：Title、Day 1、Day 1 Report、Day 2 Camera Offline、Backup Camera、Day 2 Report、两个结局等实际运行截图。
- `Builds/Windows/Development/PetOffline.exe`：Development Build。
- `Builds/Windows/PetOffline.exe`：Release Build。

最终完成必须同时满足：Unity 零编译错误、无 Missing Script/MissingReference、标题到两关可完整游玩、两个结局与 Return/Restart 可达、保存/解锁跨进程有效、全部 P0 与架构校验通过、关闭 UIRoot 后玩法仍运行、Windows x64 Build 已真实生成并启动、截图与测试结果已保存、README/Controls/Build/Test/Known Limitations 完整。Editor 内能运行、脚本存在或进程短暂存活均不能替代 Standalone 全流程验收。
