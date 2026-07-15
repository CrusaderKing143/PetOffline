# Pet Offline 两关垂直切片执行计划

更新日期：2026-07-15  
目标平台：Unity 6000.3.14f1，Windows x64，1920×1080，16:9，键盘和鼠标，离线运行。

## 0. 当前基线

本计划已于 2026-07-15 获确认，是后续实现依据；不代表功能已经完成。

当前工作区已经存在一批未提交的 Foundation 生成物，包括 C#、asmdef、四个 Scene、Input Actions、Package/ProjectSettings 改动和最小测试产物；这些内容超出了本轮“只做资料归一与计划”的范围，因此本轮全部冻结，不继续扩展、删除、提交或宣称完成。计划获确认后的第一项工作是逐文件对照本计划审计这些生成物，再由用户决定保留还是另行处理。

- 关卡可玩性、完整 UGUI、所有 P0 测试、截图和 Windows Build 均未验收。
- 当前最小 EditMode 1/1、PlayMode 1/1 与基础 Validation PASS 只证明 Foundation 的极小范围，不代表 Day 1、Day 2 或 Definition of Done。
- 任何里程碑只有在真实 Unity 命令成功、证据写入 Artifacts、开发台账更新后才能标记完成。
- 工作树包含用户已有改动与来源不同的生成物；禁止 `git add -A`、禁止自动恢复 SampleScene/Main/AGENTS.md 等所有权不明内容。
- 后续首次 Unity/MCP 写操作前必须确认 Project Root 为 `D:\UGit\PetOffline`。

完整产品流程固定为：

    Title
    → Day 1: 狗已上线
    → Day 1 Report
    → Day 1 自动结尾
    → Day 2: 偷偷安抚
    → Day 2 Report
    → Final Choice
    ├─ Restore Connection → 确认循环重启结局
    └─ Keep Quiet → 睡眠结局
    → Return to Title / Restart

正常首次流程目标时长为 12–15 分钟。测试可使用缩短计时的测试配置，但不得绕过真实 Scene、物理、状态机、报告、选择或结局。

## 1. 项目结构与 Assembly Definitions

计划目录：

    Assets/PetOffline/
    ├── Art/
    │   ├── Characters
    │   ├── Environment
    │   ├── Props
    │   ├── UI
    │   └── VFX
    ├── Audio/
    │   ├── Music
    │   ├── SFX
    │   └── Voice
    ├── Data/
    │   ├── Levels
    │   ├── Dialogue
    │   ├── Reports
    │   ├── Cameras
    │   ├── Carryables
    │   └── Audio
    ├── Prefabs/
    │   ├── World
    │   └── UI
    ├── Scenes
    ├── Scripts/
    │   ├── Core
    │   ├── Gameplay
    │   ├── UI
    │   └── Editor/
    │       ├── Automation
    │       ├── Validation
    │       └── Build
    ├── Settings/
    │   ├── Input
    │   ├── URP
    │   └── Audio
    └── Tests/
        ├── EditMode
        └── PlayMode

    Artifacts/
    ├── Screenshots
    └── TestResults

    Builds/Windows/

Assembly Definition 依赖固定如下：

| Assembly | 职责 | 允许引用 |
| --- | --- | --- |
| PetOffline.Core | 服务接口、只读状态、命令、DTO、通用配置 | Unity/Input System 等必要平台程序集 |
| PetOffline.Gameplay | 世界角色、物理、设备、传感器、关卡状态机 | PetOffline.Core |
| PetOffline.UI | UGUI Presenter/View、Mock UI 预览 | PetOffline.Core、UGUI、TMP |
| PetOffline.Editor | Setup、校验、构建和必要调试工具 | Core、Gameplay、UI；Editor only |
| PetOffline.Tests.EditMode | 架构、配置、纯逻辑和静态验证 | 被测程序集、Unity Test Framework |
| PetOffline.Tests.PlayMode | Scene、输入、物理、UI 和全流程测试 | 被测程序集、Unity Test Framework |

asmdef 落点与约束：

| asmdef | 目录 | 关键设置 |
| --- | --- | --- |
| PetOffline.Core | `Assets/PetOffline/Scripts/Core` | Root Namespace=`PetOffline.Core`；无 Gameplay/UI 引用 |
| PetOffline.Gameplay | `Assets/PetOffline/Scripts/Gameplay` | Root Namespace=`PetOffline.Gameplay`；仅引用 Core |
| PetOffline.UI | `Assets/PetOffline/Scripts/UI` | Root Namespace=`PetOffline.UI`；仅引用 Core、UGUI、TMP |
| PetOffline.Editor | `Assets/PetOffline/Scripts/Editor` | Include Platforms=`Editor`；引用 Core/Gameplay/UI |
| PetOffline.Tests.EditMode | `Assets/PetOffline/Tests/EditMode` | Include Platforms=`Editor`；Test Assemblies=true |
| PetOffline.Tests.PlayMode | `Assets/PetOffline/Tests/PlayMode` | Test Assemblies=true；引用 Core/Gameplay/UI |

强制规则：

- PetOffline.UI 与 PetOffline.Gameplay 互不引用。
- Core 不暴露 Gameplay/UI 类型，不使用跨边界 GameObject 或 Transform 作为消息载荷。
- Runtime asmdef 不引用 `UnityEditor`；Editor 工具不得被 Player Build 收入。
- Gameplay 通过只读事件和 ILevelViewModel 发布状态。
- UI 只通过 ICommandSink 发送开始、继续、报告确认、最终选择、返回标题和重开等高层命令。
- 不创建只有一个实现的工厂、通用任务框架或通用 EventBus；两个固定关卡使用明确状态机和直接事件。

## 2. Scene 列表、加载顺序与 Hierarchy

Build Settings：

| Index | Scene | Release 状态 |
| --- | --- | --- |
| 0 | 00_Bootstrap | 启用 |
| 1 | 10_Day1_Meeting | 启用 |
| 2 | 20_Day2_Sunbath | 启用 |
| 3 | 90_UIRoot_Test | 存在但禁用 |

99_Test_Playground 默认不创建；只有独立物理调试无法由正式 Scene 或测试解决时才添加。

加载规则：

1. 应用只从 00_Bootstrap 启动。
2. Bootstrap 常驻，持有服务、Main Camera、Cinemachine Brain 和唯一 UIRoot。
3. 标题页 New Game additive 加载 Day 1；已解锁的 Continue 可加载 Day 2。
4. World Scene 加载完成后设为 active；任一时刻最多一个 World Scene。
5. 切关先完成保存和淡出，再卸载旧 World Scene、加载新 World Scene、绑定 ViewModel、淡入。
6. 结局完成后卸载 Day 2，返回仍常驻的标题页。

00_Bootstrap：

    00_Bootstrap
    ├── App
    │   ├── GameSession
    │   ├── SceneFlowService
    │   ├── InputRouter
    │   ├── AudioService
    │   ├── SaveService
    │   └── DialogueDirector
    ├── Audio
    │   ├── WorldAudioSource
    │   └── UIAudioSource
    ├── Cameras
    │   └── Main Camera
    │       ├── Camera
    │       ├── CinemachineBrain
    │       └── AudioListener
    └── UIRoot
        ├── Canvas_HUD
        │   ├── ObjectivePanel
        │   ├── ProgressPanel
        │   ├── CameraStatus
        │   └── InteractionPrompt
        ├── Canvas_Overlay
        │   ├── TitlePanel
        │   ├── OwnerVideoPanel
        │   ├── BossDialoguePanel
        │   ├── AIMessagePanel
        │   ├── SubtitlePanel
        │   ├── BarkPrompt
        │   ├── ReportPanel
        │   ├── ChoicePanel
        │   ├── PausePanel
        │   ├── SettingsPanel
        │   ├── ScreenFade
        │   └── ToastPanel
        ├── EventSystem
        └── UIPanelRouter

10_Day1_Meeting：

    WorldRoot
    ├── Environment
    │   ├── Ground
    │   ├── BackFurniture
    │   └── FrontFurniture
    ├── Collision
    │   ├── Walls
    │   ├── FurnitureColliders
    │   └── VisionOccluders
    ├── Actors
    │   └── Latte
    ├── Interactables
    │   ├── OwnerSlipper
    │   ├── BossPillow
    │   └── BananaSlipZone
    ├── Devices
    │   ├── CameraA
    │   ├── CameraB
    │   ├── Speaker
    │   ├── RobotVacuum
    │   └── MeetingTV
    ├── Sensors
    │   └── CameraBVision
    ├── Triggers
    │   ├── PlayerSpawn
    │   ├── CameraAGoalArea
    │   ├── DogBedGoalArea
    │   └── EndingSpeakerPoint
    ├── Paths
    │   └── RobotPath_Day1
    ├── WorldVFX
    ├── WorldAudio
    ├── LevelFlow
    │   └── LevelOneFlowController
    └── VirtualCamera
        └── CM_Day1

Day 1 初始摆放契约：OwnerSlipper 在 Dog Bed 旁；BossPillow 在 Camera A 前且初始锁定；Camera A 没有 hostile sensor；Camera B 使用固定左右扫描端点。屏幕 HUD 不得出现在该 Scene。

20_Day2_Sunbath：

    WorldRoot
    ├── Environment
    │   ├── LivingRoom
    │   ├── Kitchen
    │   └── Balcony
    ├── Collision
    │   ├── Walls
    │   ├── FurnitureColliders
    │   └── VisionOccluders
    ├── Actors
    │   └── Latte
    ├── Interactables
    │   ├── BananaPeel
    │   └── OwnerSlipper
    ├── Devices
    │   ├── FutureFeeder
    │   ├── FeederCamera
    │   ├── RobotVacuum
    │   └── BackupCamera
    ├── Sensors
    │   ├── FeederCameraVision
    │   └── BackupCameraVision
    ├── Triggers
    │   ├── PlayerSpawn
    │   ├── BackupRetrySpawn
    │   ├── SunZone
    │   ├── FeederConfirmationArea
    │   └── SideDoorTrigger
    ├── Paths
    │   └── RobotPath_Day2
    ├── WorldVFX
    ├── WorldAudio
    ├── LevelFlow
    │   └── LevelTwoFlowController
    └── VirtualCamera
        └── CM_Day2

Day 2 初始摆放契约：BananaPeel 可搬，RobotPath 必须经过 Banana 目标点并能导向 FutureFeeder；FeederCamera 与 FutureFeeder 分为独立组件/子对象，以便只离线摄像头；SideDoorTrigger 与客厅安全路线在世界碰撞上可区分。屏幕 HUD 不得出现在该 Scene。

90_UIRoot_Test：

    90_UIRoot_Test
    ├── PreviewCamera
    ├── UIRoot
    ├── MockLevelViewModelHost
    └── UIWaitingStatePreview

UIRoot_Test 必须使用与 Bootstrap 相同的 UIRoot Prefab，并能通过 Mock ViewModel 预览标题、HUD、对话、确认、两日报告、最终选择、暂停、设置、Toast 和两个结局状态。没有加载 World Scene 或解绑 ViewModel 时，UI 进入“等待关卡数据”状态且不得抛出 MissingReference。

## 3. Game World 与 UGUI 硬边界

原则：World owns gameplay; UGUI presents state.

必须属于 Game World 的对象：

- Latte、地板、墙、家具、拖鞋、抱枕、香蕉区域/香蕉皮、机器人和投食器。
- Camera A、Camera B、Feeder Camera、Backup Camera、视野锥和遮挡。
- Camera A Goal、Dog Bed Goal、SunZone、Feeder Confirmation Area、Side Door Trigger。
- Robot Waypoints、碰撞、世界 AudioSource、世界 VFX 和自动结局演出。

这些对象使用普通 Transform、SpriteRenderer、Rigidbody2D、Collider2D、Animator、AudioSource 和世界空间效果。禁止：

- 把 Latte、房间、摄像头视野、机器人或任何触发区做成 UGUI。
- 用 Image、RectTransform 或 anchoredPosition 驱动玩法对象。
- 用 GraphicRaycaster 判定相机检测。
- 把公寓置于 Canvas 下。
- 用 WebView 嵌入 HTML。
- 把 Main Camera 全画面渲染到 RawImage 后在 UI 中完成玩法。
- 让 HUD、ReportPanel 或其他 UI 脚本直接启用摄像头、移动角色或推进任务。
- 把权威任务状态保存在 UI 脚本。

UGUI 只负责标题、HUD、目标、进度、交互提示、视频窗、对话、通知、字幕、Bark 提示、报告、选择、暂停、淡入淡出和 Toast。World Space Canvas 只允许小型提示或标记，不能持有玩法状态。RenderTexture 只允许真实世界显示器或摄像头画面。

自动化必须验证：关闭整个 UIRoot 后，Latte 移动、搬运、摄像头、机器人、触发区和关卡状态机仍然运行并可推进。

## 4. 输入

使用 Unity Input System，Gameplay 和 UI 使用独立 Action Map：

| Action | 默认绑定 |
| --- | --- |
| Move | WASD、方向键 |
| Interact / Pick Up / Drop | E |
| Bark | Space |
| Push | Q |
| Lie Down / Sunbathe | Left Shift |
| Pause | Escape |
| UI | 鼠标和键盘导航 |

UI 打开报告、选择或暂停时由 InputRouter 切到 UI Map；关闭后恢复 Gameplay Map。禁用 UIRoot 不得禁用 Gameplay Map。

Release 不提供鼠标点击地面移动，也不把 Web 原型的 `R` 设为玩家键位。若需要快速重置，`R` 只在 Development Build 的 Debug 开关开启时可用，且调用与正常失败相同的 `ResetCurrentTask` 边界。

## 5. Day 1 状态机

状态顺序：

    Opening → TaskShoes → TaskPillow → FinalBark → Report → Ending → Complete

| State | 权威行为和转移 |
| --- | --- |
| Opening | 锁定玩法输入，播放固定开场；只播一次，结束进入 TaskShoes |
| TaskShoes | 拖鞋在狗窝旁；BossPillow 位于 Camera A 前且锁定；Camera A 只作目标相机且永不检测；Camera B 在固定左右端点循环扫描，只在 Latte 携带当前任务拖鞋时造成失败；拖鞋在 Camera A GoalArea 连续停留 2 秒后锁定并解锁抱枕 |
| TaskPillow | 抱枕为重物，携带速度约 60%；携带时 Bark 必须掉落；Camera B 检测只重置当前抱枕任务；机器人沿固定路径移动并能阻挡 Latte、按配置推动掉落抱枕；抱枕进入 Dog Bed GoalArea 立即完成 |
| FinalBark | 关闭失败条件，等待一次 Bark；没有超时或失败 |
| Report | 锁定 Gameplay 输入，显示固定 Meeting Performance Report |
| Ending | 报告继续后播放世界空间的音响/狗窝自动演出 |
| Complete | 保存 DayOneCompleted，卸载 Day 1，加载 Day 2 |

失败只重置当前任务：

- Latte 回当前任务出生点。
- 当前任务物品回初始位置。
- Camera B 回基础角度。
- 清除临时警戒、安全窗和检测保持。
- 不重播 Opening，不撤销已完成任务。

Boss Call 在 TaskShoes 和 TaskPillow 生效：

- LevelConfigSO 中的确定性时间表产生 Bark 响应窗口，不使用随机间隔。
- 成功 Bark 提供短暂安全窗口。
- 错过只临时提高 Camera B 扫描速度、范围和视野角，不 Game Over。

Day 1 香蕉为配置化 BananaSlipZone，不是可搬物。默认调参记录在 DECISIONS.md，最终以 LevelConfigSO 和 Playtest 结果为准。

Day 1 固定报告至少包含：

- 远程指令响应：成功。
- 主人气味资产展示：1 次。
- 企业文化物料接触：1 次。
- 最终发言：已完成。
- 行为安全警报：已折叠。
- 情绪价值输出：优秀；主人贡献度：一般。
- 建议：明日继续接入会议。
- 固定笑点：“展开后影响阅读体验。”

Camera detections、Boss Call 回应数、机器人是否推动抱枕可以作为有限观测字段，但它们不改变报告结论、关卡通过或下一关解锁。

Day 1 对话资产按稳定 ID 拆分，触发点不得由 UI 决定：

| SequenceId | 世界触发 | 固定内容锚点 |
| --- | --- | --- |
| `D1.Opening` | Opening 进入 | 老板要求拖鞋上镜；AI 同时宣布“请搬运”与“禁止搬运”；主人指出矛盾 |
| `D1.ShoesComplete` | 鞋任务完成 | “主人气味资产”误读与主人仍被想念的解读 |
| `D1.PillowComplete` | 抱枕入狗窝 | “企业文化下沉到一线”；主人追问“一线是狗窝吗” |
| `D1.Call.*` | 配置化 Boss Call | 固定点名台词；成功/超时使用固定响应分支 |
| `D1.FinalBark` | FinalBark Bark | “建议停止当前会议”；“置信度：12%”；“但我同意” |
| `D1.Ending` | 报告 Continue 后 | 主人声音从世界 Speaker 发出；Latte 靠近再回狗窝；AI 误读为陪伴效果 |

## 6. Day 2 状态机

状态顺序：

    Start → SunFirst → CameraCheck → Loop → DestroyCamera → Backup → FinalSun → Report → Choice → End

唯一可见主目标固定为：让拿铁晒满20秒太阳。

| State | 权威行为和转移 |
| --- | --- |
| Start | 播放固定开场并显示唯一主目标 |
| SunFirst | Latte 同时处于世界 SunZone 且 Lie 时才累计；达到 10 秒且 Feeder Camera 活跃时进入 CameraCheck |
| CameraCheck | 确认期间太阳进度暂停；回到 Feeder Confirmation Area 完成确认并把太阳进度清零 |
| Loop | Feeder Camera 仍活跃时，每次累计到 10 秒都再次确认；忽略确认会扩大并加速扫描，进度保持暂停，不 Game Over |
| DestroyCamera | Latte 将可搬 BananaPeel 放到机器人路径；机器人打滑并撞击投食器；只关闭投食器摄像头，FoodCameraActive=false，投食器本体仍工作并显示 CAMERA OFFLINE / 当前画面：墙 |
| Backup | Feeder Camera 离线后，穿过厨房到阳台侧门激活 Backup Camera 并显示 BACKUP CAMERA ACTIVE；错误路线的下一次 10 秒确认仍会发生 |
| FinalSun | 玩家必须从客厅绕行，避免 SideDoor Trigger；当前尝试没有活动确认摄像头重新捕获 Latte 时才能越过 10 秒并累计到 20 秒 |
| Report | 显示固定 Day 2 报告 |
| Choice | 等待 Restore Connection 或 Keep Quiet |
| End | 播放对应结局，随后允许返回标题或重开 |

错误侧门路线必须可恢复，不能软锁：

- 记录 BackupCameraEverTriggered=true 供报告/调试。
- Backup Camera 在下一次 10 秒重新确认 Latte。
- 当前太阳进度清零，短淡出后把 Latte 重置到 BackupRetrySpawn。
- 当前尝试关闭 Backup Camera 并重新武装 SideDoor Trigger。
- Feeder Camera 保持离线，玩家可改走客厅路线。

20 秒完成条件：

    FoodCameraActive == false
    && BackupCameraActive == false
    && ConfirmationActive == false
    && SunTime >= 20 seconds

固定报告字段：

- Sunbathe completed
- Confirmation failed
- Camera offline
- Current image: wall
- Unable to confirm whether Latte still misses the owner

结局：

- Restore Connection：恢复远程确认并可见地重启主人点名/确认循环。
- Keep Quiet：关闭远程确认，播放 Latte 睡眠演出并显示：

    它不是不想你。
    它只是终于不用证明它在想你。

Day 2 对话资产按稳定 ID 拆分：

| SequenceId | 世界触发 | 固定内容锚点 |
| --- | --- | --- |
| `D2.Opening` | Start | 主人注意阳台；AI 判断“那边有太阳”；主人质疑连晒太阳也要管 |
| `D2.FirstConfirm` | 第一次 SunTime=10 | “开始点名”；主人要求 Latte 回来给他看 |
| `D2.ConfirmReturn` | 回 Feeder Area | “看到了，确认完成”；太阳进度 10→0 |
| `D2.FeederOffline` | Robot 撞 Feeder Camera | “当前画面：墙”；“正在认真观察墙” |
| `D2.BackupActive` | SideDoor Trigger | `BACKUP CAMERA ACTIVE`；系统确认侧门仍能看到 Latte |
| `D2.BackupConfirm` | 错误路线下一次 10 秒 | Backup 接管确认，进度暂停/本次尝试重置 |
| `D2.Complete` | 正确路线 SunTime=20 | “未检测到拿铁。检测到墙。”；主人意识到 Latte 睡着了 |
| `D2.Restore` | Restore Connection | 远程确认恢复，主人语音与确认循环重新开始 |
| `D2.KeepQuiet` | Keep Quiet | 关闭远程确认；主人选择让 Latte 安静一会儿 |

## 7. C# 系统职责

Core：

- GameSession：应用级流程、当前关卡、解锁状态和最终选择。
- SceneFlowService：additive 加载/卸载、active Scene 和 ViewModel 绑定，不判断任务。
- InputRouter：Gameplay/UI Action Map 切换。
- AudioService：Master、World、UI 总线和 AudioCue 播放。
- SaveService：版本化保存 DayOneCompleted、通关、选择和设置；测试时允许命令行覆盖独立存档路径。
- DialogueDirector：顺序播放固定 DialogueSequenceSO。
- ApplicationPauseController：处理 Escape、窗口失焦/恢复和输入状态恢复，不推进关卡。
- ILevelViewModel：只读发布 Level、Phase、Objective、Progress、CameraUiState 和 Changed 事件。
- ICommandSink：接收 StartNewGame、ContinueSavedGame、ContinueReport、SubmitChoice、ReturnToTitle、Restart。

Gameplay：

- PlayerController2D：八方向移动、朝向、Lie 和输入锁。
- InteractionSensor、CarryController、CarryableObject、GoalZone：交互、搬运、掉落和目标判定。
- CameraScanMotor、CameraVisionSensor2D、VisionConeRenderer：扫描、物理检测、遮挡和可视锥。
- RobotPatrol：Waypoint、Slip、阻挡和配置化推动。
- BananaSlipZone、BananaPeel：Day 1 固定滑区与 Day 2 可搬机关分离。
- SunbatheController、FeederConfirmationController、BackupCameraTrigger：Day 2 世界规则。
- LevelOneFlowController、LevelTwoFlowController：唯一关卡状态权威。
- WorldEndingController：固定 waypoint 自动演出。

UI：

- UIPanelRouter、TitlePresenter、LevelHudPresenter、DialoguePresenter、ReportPresenter、ChoicePresenter、PausePresenter、SettingsPresenter。
- MockLevelViewModelHost 只存在于 90_UIRoot_Test。
- Presenter 订阅 Core 状态并发送 ICommandSink 命令，不访问 Gameplay 组件。

Editor：

- ProjectAutomation：幂等创建/更新目录、配置、Prefab、Scene、SO 和 Build Settings。
- ProjectValidator：架构、Scene、引用、输入和 Missing Script 校验。
- WindowsBuild：Development/Release Windows x64 构建。
- StandaloneSmokeDriver：仅 Development Build/Editor 可用；读取命令行参数，驱动真实 Scene/输入/状态机，输出结果 JSON、截图和进程退出码；Release 中不暴露调试入口。

## 8. ScriptableObject 设计

| Asset | 关键字段 | 计划实例 |
| --- | --- | --- |
| LevelConfigSO | LevelId、ObjectiveText、目标/确认秒数、Spawn/Retry 点 ID、Boss Call 固定时间表、SafeWindow、AlertDuration、Banana/Robot 参数、引用到本关 Camera/Carryable/Dialogue/Report 配置 | `Level_Day1`、`Level_Day2`、测试用缩时副本 |
| DialogueSequenceSO | SequenceId、按顺序的 SpeakerId/Text/Duration、PauseGameplay、Skippable、完成事件 ID；文本固定且离线 | `D1.*`、`D2.*` 表中所有序列 |
| ReportDefinitionSO | ReportId、Title、固定字段、有限观测字段映射、固定警告、ContinueLabel、ChoicePrompt、RecommendedChoice | `Report_Day1`、`Report_Day2` |
| CameraScanConfigSO | CameraId、BaseAngle、Left/RightLimit、ScanSpeed、FOV、Range、SampleHz、DetectionHold、OcclusionMask、Alert speed/FOV/range multiplier、AlertDuration | `Camera_Day1_B`、`Camera_Day2_Feeder`、`Camera_Day2_Backup`；Camera A 不使用 hostile 配置 |
| CarryableConfigSO | ItemId、WeightClass、MoveMultiplier、PickupDistance、DropDistance、DropOnBark、RobotPushDistance、GoalHoldSeconds、允许目标 ID | `Carryable_Slipper`、`Carryable_Pillow`、`Carryable_BananaPeel` |
| AudioCueDefinitionSO | CueId、AudioClip、World/UI Bus、Volume、Pitch、SpatialBlend、Loop、MaxDistance、是否受暂停影响 | Bark、Robot、CameraAlert、FeederOffline、UIConfirm、UIReport、Ambience 等 |

所有 SO 只保存不可变配置；`SunTime`、`FoodCameraActive`、当前 State、携带关系、检测状态和已完成任务等运行时权威数据必须在关卡运行实例/GameSession 中，禁止写回 SO 资产。Stable ID 用常量或受校验的 catalog 解析，不在业务代码散落 Scene 名字符串。

不创建 TaskDefinitionSO：只有两个固定流程，额外数据驱动任务层会重复显式状态机。

## 9. 资源导入策略

- Docs/Reference/images 的 53 张 PNG 只作视觉、地图和 UI 层级参考，不整体复制到 Assets。
- 52 张是 RGB；唯一 RGBA 图片的 Alpha 也全部为 255。透明棋盘格大多已经烘焙，不能直接当透明 Sprite。
- 缺少 03_LatestDesignSource.pdf 和 05_ArtSource.mg 时，使用风格统一、结构可替换的灰盒 Sprite/Prefab，并记录缺口。
- 原始资料留在 `Docs/Reference`；只有经过来源确认、裁切/去底、命名和尺寸处理的派生资产才进入 `Assets/PetOffline/Art`，并在 `Docs/Development/ASSET_PROVENANCE.md` 记录来源文件、处理方式、许可/未知项和替换状态。
- World Prefab 将逻辑根和 Render 子节点分离，美术替换不得改变 Collider、Sensor、Path 或状态引用。
- 世界 Sprite 默认 100 PPU、Bilinear、无 Mipmap、底部 Pivot；UI Sprite 无 Mipmap。最终按像素密度和视觉测试调节。
- `AssetPostprocessor`/Import Preset 只作用于 `Assets/PetOffline`：World/UI Sprite 使用 sRGB、Sprite 类型、Mipmap off、Read/Write off；角色 Sprite Sheet 明确切片；Windows 压缩设置由视觉对比后锁定。
- 房间灰盒优先 SpriteRenderer/Tilemap 与独立 Collider2D；大型背景图不能携带任务碰撞。角色至少预留 Idle、Move、CarryLight、CarryHeavy、Bark、Slide、Lie、Sleep 的 Animator 状态，缺帧时用可替换静态姿势而不伪造完成度。
- Vision Cone 用世界 Mesh/SpriteRenderer 表现，检测和视觉读取同一 CameraScanConfigSO。
- 中文 TMP 字体必须离线随 Build 携带并确认授权/字符覆盖；音频按 Music/SFX/Voice 分类，世界设备使用 2D/有限空间化 AudioSource，UI 使用独立总线。音频缺失时使用确定性占位 Clip，不在运行时联网或生成。
- 两份 Newbie Guide/新手引导 Excel 属于另一塔防项目，不导入、不转化为本项目教程。
- 不使用运行时生成式 AI、网络下载、WebView、DOTS、NavMesh 或第三方 Quest 框架。

## 10. Editor 自动化与架构校验

Tools/Pet Offline/Setup Project：

- 校验 Project Root 与 Unity 6000.3.14f1。
- 幂等创建 PetOffline 自有目录、asmdef、Input Actions、Layers、Sorting Layers、URP 2D 配置、Scene、Prefab、默认 SO 和 Build Settings。
- 使用 Unity Editor API；禁止手工拼接大型 Scene YAML。
- 只更新已知 PetOffline 节点，不删除或改名来源不明的用户资产。
- 生成前记录 Git status；遇到已存在且非工具签名的同名 Scene/Prefab 时停止并报告，不静默覆盖。
- 每次运行后保存、刷新、等待编译，并回读 Build Settings、Scene Hierarchy、asmdef 和关键引用证明幂等。

Tools/Pet Offline/Validate Project：

- `PlayerController2D`、`CarryController`、`CameraVisionSensor2D`、`RobotPatrol`、`LevelFlowController` 不得位于任何 Canvas 子层级。
- Gameplay World 对象不得使用 RectTransform。
- Latte、房间、Camera/vision cone、Robot、Goal/Sun/Feeder/SideDoor Trigger 不得使用 UGUI Graphic 组件承载玩法。
- World Scene 不得包含 Screen Space HUD。
- World Space Canvas 不得持有任务状态。
- UI asmdef 不引用 Gameplay；Gameplay asmdef 不引用 UI。
- 必需 Scene 存在且 Build Settings 顺序正确。
- 必需序列化引用不为空。
- 无 Missing Script 或 MissingReference。
- Gameplay/UI Input Map 和键位完整。
- URP 2D、Main Camera、AudioListener 和 Cinemachine 配置有效。
- 必需 ScriptableObject 已创建并绑定。
- PetOffline Runtime 代码不得使用 `FindObjectOfType`/`FindFirstObjectByType` 作为业务依赖定位，也不得在 SceneNames/SceneCatalog 之外散落硬编码 Scene 名；由 EditMode 静态审计锁定。
- Camera A 不得挂 hostile `CameraVisionSensor2D`；Camera B 必须有扫描配置与遮挡 Mask；FeederCamera 必须可独立于 Feeder 本体禁用。

其他计划菜单/批处理入口：

- `Tools/Pet Offline/Capture Acceptance Screenshots`：只在指定验收状态拍图，输出到 `Artifacts/Screenshots`。
- `Tools/Pet Offline/Build/Windows Development` 与 `.../Windows Release`：统一 BuildPipeline 入口。
- `Tools/Pet Offline/Open Test Scenes`：只打开 90_UIRoot_Test/可选 Playground，不修改正式 Scene。

MCP 安全门：

- 首次写操作前查询并确认 Unity MCP 的 Project Root 正是 D:\UGit\PetOffline。
- 若 MCP 指向其他项目，禁止通过 MCP 写入；改用本项目 Editor 脚本和 Unity batchmode。

## 11. EditMode / PlayMode 测试计划

EditMode：

- Architecture boundary test：asmdef 依赖、五类 Gameplay 组件不在 Canvas 下、World 对象无 RectTransform、Runtime 无 UnityEditor、禁用 API/硬编码 Scene 名审计。
- Camera angle/range/occlusion test。
- GoalZone 2 秒、离开清零和即时完成 test。
- Save/unlock test。
- Config/reference validation test。

PlayMode：

1. UIRoot disabled Day 1 gameplay test。
2. UIRoot disabled Day 2 gameplay test。
3. UIRoot mock preview test。
4. UIRoot without World Scene waiting-state test。
5. Day 1 shoe completion test。
6. Day 1 detection reset test。
7. Day 1 previous-task preservation test。
8. Day 1 pillow and robot interaction test。
9. Day 1 boss-call success/timeout test。
10. Day 1 final report → ending → Day 2 transition test。
11. Day 2 first 10-second confirmation test。
12. Day 2 feeder return resets progress test。
13. Day 2 ignored confirmation pauses progress and enlarges/accelerates scan test。
14. Day 2 feeder-camera disable test。
15. Day 2 backup-camera activation test。
16. Day 2 wrong-route confirmation and recovery test。
17. Day 2 correct-route 20-second completion test。
18. Restore Connection ending test。
19. Keep Quiet ending test。
20. Save/unlock/Continue/Restart/ReturnToTitle test。
21. Pause、Settings、音量持久化和窗口失焦/恢复 test。
22. Full title-to-ending smoke test：两个分支各自从 Title 独立运行。

UIRoot 断电自动化步骤：

1. 加载 Bootstrap + Day 1。
2. 通过世界状态事件结束 Opening 并确认已进入 TaskShoes；不能依靠 UI 回调结束开场。
3. 禁用整个 UIRoot GameObject，并断言 Gameplay Action Map 仍启用。
4. 使用 Input System 测试键盘驱动 Latte，断言 Rigidbody2D 世界位置发生预期位移。
5. 拾取拖鞋，断言 CarryController/CarryableObject 的世界携带关系成立；将其送入 Camera A GoalArea，断言 2 秒后进入 TaskPillow。
6. 在携带当前抱枕时进入 Camera B 视野，断言 Camera B 扫描角实际变化、检测触发、仅 Latte/抱枕/Camera B 临时状态复位，拖鞋完成状态仍保留。
7. 断言 RobotPatrol waypoint index/世界位置推进，并让机器人推动落地抱枕；将抱枕送入 Dog Bed GoalArea，断言 Flow 进入 FinalBark。
8. 通过 Gameplay Bark 命令完成 FinalBark，断言世界状态可到 Report 请求点；测试期间不得读取、依赖或重新启用 UI。

Day 2 断电测试从独立 Bootstrap + Day 2 开始并保持 UIRoot 禁用：断言 SunZone+Lie 累计、10 秒确认暂停、Feeder Area 清零、BananaPeel/Robot 令 `FoodCameraActive=false`、SideDoor 激活 Backup、错误路线重试、客厅路线达到 20 秒并进入 Report 请求点。所有断言读取 Gameplay/Core 状态，不读取 UI 文本或 Presenter。

独立 UI 生命周期测试：卸载所有 World Scene、解绑 ILevelViewModel 后保持 UIRoot 激活，等待一个 Frame 并断言所有 Presenter 无 MissingReference/NullReference、Gameplay 命令禁用、显示“等待关卡数据”；随后绑定 Mock ViewModel，预览 HUD、报告、设置、选择和两结局。

完整链路 Smoke 必须实际证明：

- 标题可以进入 Day 1。
- Day 1 报告后进入 Day 2。
- Day 2 经历 10 秒确认循环。
- 香蕉皮和机器人让 Feeder Camera 离线。
- 侧门错误路线激活 Backup Camera，客厅路线可完成 20 秒。
- Day 2 报告和两个选择均可到达且实际执行各自结局。
- Keep Quiet 显示最终字幕。
- 两个结局均可返回标题或重开。

双结局必须是两次互相隔离的运行，不能在同一已推进实例中只切换按钮：

| Run | 初始存档 | 路径 | 必须输出的成功标记 |
| --- | --- | --- | --- |
| `Full_KeepQuiet` | 空白独立存档 | Title → New Game → Day1 → Day2 → Keep Quiet → Return to Title | `Day1ReportReached`、`DayOneSaved`、`Day2ReportReached`、`Ending=KeepQuiet`、最终字幕匹配、`ReturnedToTitle` |
| `Full_Restore` | 另一份空白独立存档 | Title → New Game → Day1 → Day2 → Restore Connection → Restart/Return | `Ending=RestoreConnection`、确认循环重启、Owner Call 再现、`Restarted` 或 `ReturnedToTitle` |
| `Continue_CrossProcess` | `SeedDay1` 专用 run 在 Day 1 保存完成后退出并写出的存档 | 关闭进程 → 使用同一存档启动 `ContinueDay2` → Title → Continue → Day2 | `ContinueVisible`、`Day2LoadedFromSave`、无 Day1 重播 |

每条 standalone 运行必须有独立 save/result/log/screenshot 目录，防止 PlayerPrefs 或上次流程污染结果。

Title → 双结局闭环审计：

| 转移 | 发起者 | 权威执行者 | 自动证据 |
| --- | --- | --- | --- |
| Bootstrap → Title | Bootstrap 完成 | GameSession/UIRoot 只显示标题 | Title 可交互；无 World Scene |
| Title → Day 1 | `StartNewGame` Command | SceneFlowService additive load；LevelOneFlowController 进入 Opening | `Full_*` 两条 run 均从 Title 进入 Day1 |
| Day 1 Report → Ending | `ContinueReport` Command | LevelOneFlowController 启动 WorldEndingController | 报告先于世界演出，UI 不直接移动 Latte |
| Day 1 Ending → Day 2 | 世界演出完成事件 | GameSession 保存 DayOneCompleted；SceneFlowService 卸载/加载 | Save/unlock 与 Day2 Scene 序列标记 |
| Day 2 Report → Choice | `ContinueReport` Command | LevelTwoFlowController 请求 Choice 状态 | ChoicePanel 仅提交命令 |
| Choice → Restore | `SubmitChoice(Restore)` | LevelTwoFlowController/EndingController 恢复确认设备并重启点名 | Owner Call/Confirmation 再次出现后才标记结局 |
| Choice → Keep Quiet | `SubmitChoice(KeepQuiet)` | LevelTwoFlowController 关闭远程确认；WorldEndingController 睡眠演出 | 两摄像头关闭、睡眠状态、最终字幕精确匹配 |
| 任一 Ending → Title/Restart | `ReturnToTitle` / `Restart` Command | GameSession + SceneFlowService | World Scene 正确卸载/重载，Bootstrap 不重复创建 |
| 重新启动 → Continue | Title Continue | SaveService + SceneFlowService | 跨进程加载 Day2，不重播 Day1 |

该表中的每个转移都有唯一权威执行者；不存在 ReportPanel、ChoicePanel 或其他 UI 直接移动角色、开关摄像头或设置关卡状态的缺口。

Web 参考核对不是 Unity 架构测试，但在 Milestone 0 结束前需记录一次行为对照：静态脚本核对已完成；浏览器后端可用时实际运行 Day 1、Day 2、报告和双结局，将与最终裁决不一致的行为标记为“参考但不迁移”。浏览器不可用时记录为已知工具缺口，不得把静态分析冒充实玩。

## 12. 构建与验收命令

计划批处理入口：

- PetOffline.Editor.ProjectAutomation.SetupBatch
- PetOffline.Editor.ProjectValidator.ValidateBatch
- PetOffline.Editor.WindowsBuild.BuildDevelopment
- PetOffline.Editor.WindowsBuild.BuildRelease
- Development Player 内的 `StandaloneSmokeDriver` 场景：`FullKeepQuiet`、`FullRestore`、`SeedDay1`、`ContinueDay2`

所有 batch 入口必须捕获异常、写明失败原因，并通过 `EditorApplication.Exit(0/1)` 返回可靠进程码；`WindowsBuild` 还必须检查 BuildReport Summary，失败时返回非零，不能只依赖 Unity 日志文本。

PowerShell 命令：

    $ErrorActionPreference = 'Stop'
    $Unity = 'C:\Program Files\Unity 6000.3.14f1\Editor\Unity.exe'
    $Project = 'D:\UGit\PetOffline'
    $Results = "$Project\Artifacts\TestResults"
    $Shots = "$Project\Artifacts\Screenshots"
    $Builds = "$Project\Builds\Windows"

    New-Item $Results -ItemType Directory -Force | Out-Null
    New-Item $Shots -ItemType Directory -Force | Out-Null
    New-Item $Builds -ItemType Directory -Force | Out-Null

    function Invoke-UnityChecked([string]$Name, [string[]]$Arguments) {
        & $Unity @Arguments
        if ($LASTEXITCODE -ne 0) { throw "$Name failed with exit code $LASTEXITCODE" }
    }

    function Assert-TestXml([string]$Path, [string]$Name) {
        if (-not (Test-Path $Path)) { throw "$Name did not create $Path" }
        [xml]$Xml = Get-Content $Path -Raw
        $Run = $Xml.'test-run'
        if ($null -eq $Run -or $Run.result -ne 'Passed' -or [int]$Run.failed -ne 0) {
            throw "$Name XML is not a zero-failure Passed run"
        }
    }

    Invoke-UnityChecked 'Setup' @('-batchmode','-nographics','-quit','-projectPath',$Project,'-executeMethod','PetOffline.Editor.ProjectAutomation.SetupBatch','-logFile',"$Results\Setup.log")
    Invoke-UnityChecked 'Validation' @('-batchmode','-nographics','-quit','-projectPath',$Project,'-executeMethod','PetOffline.Editor.ProjectValidator.ValidateBatch','-logFile',"$Results\Validation.log")
    Invoke-UnityChecked 'EditMode' @('-batchmode','-nographics','-quit','-projectPath',$Project,'-runTests','-testPlatform','EditMode','-testResults',"$Results\EditMode.xml",'-logFile',"$Results\EditMode.log")
    Assert-TestXml "$Results\EditMode.xml" 'EditMode'
    Invoke-UnityChecked 'PlayMode' @('-batchmode','-quit','-projectPath',$Project,'-runTests','-testPlatform','PlayMode','-testResults',"$Results\PlayMode.xml",'-logFile',"$Results\PlayMode.log")
    Assert-TestXml "$Results\PlayMode.xml" 'PlayMode'
    Invoke-UnityChecked 'BuildDevelopment' @('-batchmode','-nographics','-quit','-projectPath',$Project,'-buildTarget','StandaloneWindows64','-executeMethod','PetOffline.Editor.WindowsBuild.BuildDevelopment','-logFile',"$Results\BuildDevelopment.log")
    Invoke-UnityChecked 'BuildRelease' @('-batchmode','-nographics','-quit','-projectPath',$Project,'-buildTarget','StandaloneWindows64','-executeMethod','PetOffline.Editor.WindowsBuild.BuildRelease','-logFile',"$Results\BuildRelease.log")
    if (-not (Test-Path "$Builds\Development\PetOffline.exe")) { throw 'Development EXE missing' }
    if (-not (Test-Path "$Builds\PetOffline.exe")) { throw 'Release EXE missing' }

输出固定为：

    Builds/Windows/Development/PetOffline.exe
    Builds/Windows/PetOffline.exe

`WindowsBuild` 必须显式使用 `BuildTarget.StandaloneWindows64` 和 x86_64。Scripting Backend 以本机已安装且项目可编译的实际配置为准，在 BuildReport 中记录 Mono/IL2CPP；未验证前不写死，也不为本计划擅自安装平台模块。

Development standalone 自动 Smoke 命令约定：

    $DevExe = "$Builds\Development\PetOffline.exe"

    function Invoke-StandaloneSmoke([string]$Scenario, [string]$SaveId) {
        $RunId = "${Scenario}_$([guid]::NewGuid().ToString('N'))"
        $RunDir = "$Results\Standalone\$RunId"
        $SavePath = "$Results\Standalone\Saves\$SaveId.json"
        $ResultPath = "$RunDir\result.json"
        $LogPath = "$RunDir\Player.log"
        $ShotDir = "$Shots\Standalone\$RunId"
        @($RunDir,$ShotDir,(Split-Path -Parent $SavePath)) | ForEach-Object {
            New-Item $_ -ItemType Directory -Force | Out-Null
        }

        $Args = @('-batchmode','-force-d3d11','-screen-width','1920','-screen-height','1080',
            '-petOfflineSmoke',$Scenario,'-petOfflineSavePath',$SavePath,
            '-petOfflineResultPath',$ResultPath,'-petOfflineScreenshotPath',$ShotDir,
            '-logFile',$LogPath)
        $Process = Start-Process -FilePath $DevExe -ArgumentList $Args -PassThru -WindowStyle Hidden
        if (-not $Process.WaitForExit(180000)) {
            Stop-Process -Id $Process.Id -Force
            throw "$Scenario timed out after 180 seconds"
        }
        if ($Process.ExitCode -ne 0) { throw "$Scenario player exit code $($Process.ExitCode)" }
        if (-not (Test-Path $ResultPath)) { throw "$Scenario did not write result.json" }
        $Result = Get-Content $ResultPath -Raw | ConvertFrom-Json
        if (-not $Result.Success) { throw "$Scenario reported failure: $($Result.Failure)" }
    }

    $SmokeSession = [guid]::NewGuid().ToString('N')
    Invoke-StandaloneSmoke 'FullKeepQuiet' "fresh_keepquiet_$SmokeSession"
    Invoke-StandaloneSmoke 'FullRestore' "fresh_restore_$SmokeSession"
    Invoke-StandaloneSmoke 'SeedDay1' "continue_$SmokeSession"
    Invoke-StandaloneSmoke 'ContinueDay2' "continue_$SmokeSession"

每个场景由 `StandaloneSmokeDriver` 在真实 Bootstrap/additive World Scene 中完成；它只能注入正常 Gameplay 输入/高层 UI Command，禁止直接写 Flow State 或伪造报告/结局。Smoke 完成后由 `Application.Quit(0/非0)` 返回可靠退出码。

Standalone 验收不能由 Editor PlayMode 代替：

- 实际启动 Development 和 Release 两个 `PetOffline.exe`。
- Development Build 自动跑两个独立完整结局以及跨进程 Continue，并输出 Player.log、result.json 和状态截图。
- result.json 必须包含 Scenario、Success、实际经过的 Scene/State 序列、Ending、Return/Restart、SaveVersion、开始/结束 UTC 和失败原因。
- Release Build 使用全新测试存档、断网环境和普通窗口至少手动走一条完整分支，保存标题、Day 1、Day 2 Camera Offline、Backup Camera、两日报告及该分支结局截图。另一结局由第二次 Release run 或独立 Development run 证明；其截图和 JSON 必须标注实际 Build/Scenario 来源，不能只拍 ChoicePanel。
- Player.log 无异常；无 Missing Script/MissingReference。
- 键位、音量、暂停、窗口失焦/恢复、返回标题、重开和 1920×1080 显示均实测。
- 关键 Day 1/Day 2 段各采集 Profiler/FrameTiming 证据：当前开发机以 60 FPS 为目标、1% low 不低于 45 FPS；Camera/Robot/Sun 循环不得产生持续每帧 GC。若硬件差异导致未达标，记录机器与数据，不隐藏结果。
- README 写明运行、操作、测试和构建方法。

## 13. Approval Gate 与 Milestone 0–5

Approval Gate：已于 2026-07-15 通过。现有 Foundation 已完成只读审计；保留符合本计划的 PetOffline 自有内容，不删除用户的 AGENTS.md、Main/SampleScene 或 Reference 资料改动。

每个里程碑共同门槛：

- 在 Unity 6000.3.14f1 中重新导入并编译。
- 运行最小相关真实测试。
- 修复全部编译错误、Missing Script 和 MissingReference。
- 把日志/XML/截图保存到 Artifacts。
- 更新 STATUS.md、DECISIONS.md、KNOWN_GAPS.md 和 TEST_REPORT.md。
- Git status 审核后，只提交本里程碑归属文件并创建 commit。

| Milestone | 工作 | 完成条件 |
| --- | --- | --- |
| Milestone 0 Foundation | 审计并收敛已有 Foundation；包配置、目录、asmdef、Input、Bootstrap、四个 Scene、加载服务、Setup、Validator、Web 行为对照记录 | Unity 零编译错误；Scene 能打开；Build Settings 正确；架构校验通过；UIRoot 无 World 等待状态与基础 EditMode/PlayMode 通过；不相关工作树改动未被触碰 |
| Milestone 1 Day 1 | 世界灰盒、移动、搬运、Camera A/B 固定扫描与视野、香蕉滑区、机器人、拖鞋、抱枕、确定性 Boss Call、Final Bark、固定报告和自动演出 | Opening→Report→Ending→Complete 可玩；局部重置、前置保留、机器人交互、Boss Call 两分支和 Day 2 加载测试通过 |
| Milestone 2 Day 2 | SunZone、10 秒确认、暂停/清零、Feeder Camera、BananaPeel、Robot 撞击、Offline、Backup Camera、错误/正确路线、20 秒、固定报告和两结局 | 错误路线可恢复、正确路线可完成；Restore 真正重启确认、Keep Quiet 播放睡眠；全部 Day 2 P0 测试通过，无软锁 |
| Milestone 3 UGUI | 标题、HUD、唯一主目标、字幕、视频窗、提示、两日报告、选择、暂停、设置、音量、淡入淡出、UIRoot_Test | UI 只绑定 Core；Mock/无 World 两模式可预览；UIRoot Disabled 逐项断电测试、设置持久化和失焦恢复测试通过 |
| Milestone 4 Art/Audio/VFX | 导入可用美术、SpriteRenderer 排序、动画、音效、世界 VFX、视野表现、灯光和最终演出 | 1920×1080 视觉检查通过；世界/UGUI 边界不回退；关键状态截图齐全；正常流程 12–15 分钟 |
| Milestone 5 QA/Build | 全部 EditMode/PlayMode、三类 standalone Smoke、架构验证、性能/焦点/离线检查、Development/Release Windows x64 Build | 所有 P0 通过；两个独立标题到结局 run、跨进程 Continue、Return/Restart 均有 JSON/日志/截图；`Builds/Windows/PetOffline.exe` 可运行；README、Known Gaps 与 Artifacts 完整 |

## 14. 风险、默认假设和回退

| 风险/假设 | 默认决定 | 回退 |
| --- | --- | --- |
| 缺少 PDF 和 .mg | 继续使用现有最高优先级 HTML 与可替换灰盒 | 资料补齐后在美术精修前重做冲突审计 |
| 53 张 PNG 无可用透明切片 | 仅作视觉参考，不直接当正式 Sprite | 重新切图或由正式源资产导出 |
| Web 浏览器后端当前不可用 | 已完成 HTML/JS 静态核对，交互实玩列为 Milestone 0 证据项 | 后端恢复后补跑；不可用时保留明确缺口，不阻断世界/UI 架构裁决 |
| AGENTS 路由的多份 TEngine 参考文件不存在 | 不假设 TEngine 热更/模块/Luban/资源框架，按当前 Unity 项目和实际工具 schema 执行 | 文件补齐后只在不冲突时增补约定 |
| MCP 可能连接错误项目 | 写入前强制核对 Project Root | 使用 Editor 脚本 + batchmode |
| 当前仓库已有用户改动 | 不覆盖、不清理、不混入 milestone commit | 冲突时停在具体文件并报告 |
| 当前已存在超出本轮范围的未提交 Foundation | 全部冻结；获批后先审计，不把最小测试当作完成 | 用户决定保留、修订或另行清理；本计划不擅自回退 |
| Cinemachine 暂时不可用 | 固定正交 Camera 可支撑灰盒 | 最终验收前恢复所需 CinemachineBrain/VCam |
| Windows Scripting Backend/平台模块状态未最终核实 | 先查询实际 Build Support 与项目设置，选择已安装且能稳定构建的 x64 backend，并记录 BuildReport | 缺少 Windows x64 模块时才作为明确阻塞报告；不擅自安装 |
| 中文字体授权或字符覆盖不足 | 选择可随包分发的离线 TMP 字体并生成所需字符集 | 使用经授权的系统/开源字体替代，记录视觉差异 |
| Backup 路线可能软锁 | 当前尝试局部重置，Feeder Camera 离线状态保留 | 通过 PlayMode 测试锁定恢复规则 |
| UIRoot 关闭后 Opening/Report 可能等待 UI 回调 | Flow 只等待 Core Command/世界事件；断电测试在进入 TaskShoes 后逐项推进 | 为测试提供 Core 测试命令，不允许 UI 成为状态机依赖 |
| 12–15 分钟节奏受美术/对话/路线影响 | 第一轮按 Web 数值与 01/02 目标配置，Milestone 4 用首次玩家计时 | 只调 SO 数值、路线和对话时长，不删核心教学循环 |
| 自动 standalone 截图在隐藏窗口可能失真 | Smoke Driver 使用游戏 Camera/RenderTexture 保存 PNG，并保留 Release 普通窗口人工截图 | 自动图失败时功能 JSON 仍保留，视觉验收改用显式普通窗口运行并记录 |
| 复杂通用框架增加风险 | 直接事件、显式状态机、六类 SO | 只有真实重复需求出现后再抽象 |
