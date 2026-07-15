# Pet Offline 两关垂直切片执行计划

更新日期：2026-07-15  
目标平台：Unity 6000.3.14f1，Windows x64，1920×1080，16:9，键盘和鼠标，离线运行。

## 0. 最终执行结果

截至 2026-07-15，用户通过 `/goal` 要求的完整两关 Unity 垂直切片已实现。当前版本可从标题页完成 Day 1、Day 2、两日报告、最终选择、两个结局、返回标题、重开和跨进程 Continue。

| 验收范围 | 最终结果 | 证据/交付 |
| --- | --- | --- |
| 完整产品流程 | PASS | Title → Day 1 → Report/Ending → Day 2 → Report → Choice → 两个 Ending → Return/Restart |
| 架构与 Editor 测试 | PASS | Project Validator；EditMode 3/3；PlayMode 33/33 |
| Windows Player 冒烟 | PASS | 双结局/截图 2/2；跨进程 Seed 1/1；下一进程 Continue Day 2 1/1 |
| Development Build | PASS | `Builds/Windows/Development/PetOffline.exe`；192,566,482 bytes；0 error / 0 warning |
| Release Build | PASS | `Builds/Windows/PetOffline.exe`；126,796,871 bytes；0 error / 0 warning |
| Release 启动 | PASS | 窗口响应正常，D3D11 启动，最终 Player 日志无运行时异常 |
| 截图 | PASS | `Artifacts/Screenshots` 下 9 张 1920×1080 验收图；真实 Player 原始帧保留于 `Artifacts/Screenshots/Native` |
| 离线运行时 | PASS | Manifest/lock 已移除 `com.unity.ai.assistant`；两个 Build 的 Managed 目录均无 `Unity.AI.*` |

详细证据见 `Docs/Development/STATUS.md`、`Docs/Development/TEST_REPORT.md` 与 `Artifacts/TestResults`。

尚未完成的外部人工验收包括：正式美术替换及授权确认、首次玩家 12–15 分钟计时、性能/Profiler、窗口失焦与多显示器、断网干净机，以及 Release 正常时长人工完整分支。这些项目保留在 `Docs/Development/KNOWN_GAPS.md`，不得由自动化 PASS 替代。

工作树仍包含用户或其他来源的 `AGENTS.md`、SampleScene 删除、`Assets/Scenes/Main.unity`、`Docs/Reference` 和 `ProjectSettings/SceneTemplateSettings.json` 改动。提交时必须精确暂存 Pet Offline 交付文件，禁止 `git add -A`、reset、checkout 或覆盖这些内容。

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

Package 基线：

- 锁定 `ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json` 和 `Packages/packages-lock.json`。
- P0 只依赖 URP 2D Renderer、Input System、UGUI/TextMeshPro、Cinemachine、Unity Test Framework，以及 Unity 官方 2D Sprite/Tilemap/Animation 能力。
- Timeline 只有在固定结尾演出确实比 Animator/Coroutine 更简单时才使用；不因“以后可能需要”增加包。
- M0 先把现有 Package 分为“本项目必需”和“模板/Editor 附带”，未经所有权审计不擅自删除；后续不新增 DOTS、NavMesh、第三方 Quest/EventBus 或联网运行时依赖。
- 本切片固定简体中文，不引入本地化框架或语言切换存档；设备英文标签作为固定文案随 TMP 字体资产离线打包。

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
    └── UIRoot                              # Bootstrap 常驻且全局唯一
        ├── Canvas_HUD [Screen Space - Overlay]
        │   ├── ObjectivePanel
        │   ├── ProgressPanel
        │   ├── CameraStatus
        │   └── InteractionPrompt
        ├── Canvas_Overlay [Screen Space - Overlay, sortingOrder 100]
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
        ├── EventSystem [InputSystemUIInputModule]
        └── UIPanelRouter

Bootstrap Scene 本身从启动到退出都不卸载，因此服务和 UIRoot 依靠常驻 Scene 保持生命周期，不再把同一对象额外迁入 DontDestroyOnLoad Scene。Validator 必须断言 Bootstrap、Main Camera、AudioListener、EventSystem 和 UIRoot 各自唯一，连续切换 Day 1/Day 2/Title 不产生重复实例。

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
    │   └── MeetingTV                    # 可选表现，不是 P0；不得承载玩法
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

Physics Layer 与碰撞矩阵：

| Layer | 用途 | 允许的关键交互 |
| --- | --- | --- |
| Player | Latte | WorldStatic、Robot、WorldTrigger |
| WorldStatic | 墙和家具 | Player、Robot、Carryable |
| Carryable | 拖鞋、抱枕、香蕉皮 | WorldStatic、Robot、WorldTrigger |
| Robot | 清洁机器人 | Player、WorldStatic、Carryable、Feeder |
| Sensor | 摄像头逻辑查询 | 不做实体碰撞，只查询 Player/目标并执行遮挡 Linecast |
| VisionOccluder | 视野遮挡 | 只进入摄像头遮挡 Mask |
| WorldTrigger | Goal、Sun、Feeder、SideDoor | Player 或 Carryable，按 Trigger 类型限定 |
| WorldUI | 小型世界提示 | 不参与 Physics2D |

Sorting Layer 固定为：`Ground → GroundDecal → FurnitureBack → Actor → Carried → FurnitureFront → WorldFX → WorldUI`。Latte 与携带物使用 SortingGroup/Y-sort；屏幕 UGUI 不进入这些 Sorting Layer。

PlayerSettings 默认目标为 Windows x64、1920×1080、16:9、Full Screen Window；验收还要用命令行强制 1920×1080 窗口模式，检查缩放、焦点恢复和 UI 安全区。

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
| Opening | 锁定移动、搬运、Push 和 Lie，播放固定开场；保留一次无失败的 Bark 教学输入，不依赖 UGUI 回调；只播一次，完成后进入 TaskShoes |
| TaskShoes | 拖鞋在狗窝旁；轻物携带速度约 85%；BossPillow 位于 Camera A 前且锁定；Camera A 只作目标相机且永不检测；Camera B 在固定左右端点循环扫描，只在 Latte 携带当前任务拖鞋时造成失败；拖鞋在 Camera A GoalArea 连续停留 2 秒后锁定并解锁抱枕 |
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

Day 1 香蕉为配置化 BananaSlipZone，不是可搬物。首轮默认滑行 0.8–1.0 秒、约 160% 移速，滑行期间禁止转向和主动放下物品；默认调参记录在 DECISIONS.md，最终以 LevelConfigSO 和 Playtest 结果为准。

Camera B 的最小可读世界视野锥属于 Day 1 P0：普通蓝、警戒红，范围和角度读取与检测相同的 CameraScanConfigSO。Milestone 4 只精修材质、扫描线和闪烁，不得把“是否有世界锥”推迟到美术阶段，也不得让视觉锥承担权威检测。

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
| `D1.Opening` | Opening 进入与无失败教学 Bark | 老板要求拖鞋上镜；AI 同时宣布“请搬运”与“禁止搬运”；主人指出矛盾；一次 Bark 后进入 TaskShoes |
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
| Backup | Feeder Camera 离线后，第一次返回阳台必须完成侧门错误路线教学：穿过厨房到阳台侧门，激活 Backup Camera 并显示 BACKUP CAMERA ACTIVE；错误路线的下一次 10 秒确认仍会发生，完成恢复后记录 `BackupLessonCompleted=true` |
| FinalSun | `BackupLessonCompleted` 后，玩家必须从客厅绕行并避免 SideDoor Trigger；当前尝试没有活动确认摄像头重新捕获 Latte 时才能越过 10 秒并累计到 20 秒 |
| Report | 显示固定 Day 2 报告 |
| Choice | 等待 Restore Connection 或 Keep Quiet |
| End | 播放对应结局，随后允许返回标题或重开 |

Feeder Camera 和 Backup Camera 的最小世界视野锥同样属于 Day 2 P0，并与各自逻辑 FOV/range/active 状态同步；Feeder Camera 离线时世界锥必须同时消失或切换为明确离线表现，投食器本体仍保持工作。

错误侧门路线必须可恢复，不能软锁：

- 记录 BackupCameraEverTriggered=true 供报告/调试。
- Backup Camera 在下一次 10 秒重新确认 Latte。
- 当前太阳进度清零，短淡出后把 Latte 重置到 BackupRetrySpawn。
- 当前尝试关闭 Backup Camera 并重新武装 SideDoor Trigger。
- Feeder Camera 保持离线，玩家可改走客厅路线。

20 秒完成条件：

    BackupLessonCompleted == true
    && FoodCameraActive == false
    && BackupCameraActive == false
    && ConfirmationActive == false
    && SunTime >= 20 seconds

Backup 不能靠隐藏 UI 条件硬卡。灰盒布局应自然把 Feeder 事故后的第一次回程引向侧门；若 Playtest 证明仍可直接跳过，回退为可见的世界阻挡/机器人占位，在第一次 Backup 重试后清除。任何方案都必须由世界碰撞和 Flow 执行，不能由 UGUI 传送玩家或改状态。

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
- InputRouter：Gameplay/UI Action Map 切换，并处理 Escape、窗口失焦/恢复；禁用 UIRoot 不得关闭 Gameplay Map。
- AudioService：Master、World、UI 总线和 AudioCue 播放。
- SaveService：版本化保存 DayOneCompleted、通关、选择和设置；测试时允许命令行覆盖独立存档路径。
- DialogueDirector：顺序播放固定 DialogueSequenceSO。
- ILevelViewModel：只读发布 Level、Phase、Objective、Progress、CameraUiState 和 Changed 事件。
- ICommandSink：接收 StartNewGame、ContinueSavedGame、ContinueReport、SubmitChoice、ReturnToTitle、Restart。

Gameplay：

- PlayerController2D：八方向移动、朝向、Lie 和输入锁。
- CarryController、CarryableObject、CarryGoalZone2D：用最小 Physics2D 查询完成拾取、搬运、掉落与两个目标区判定；不建立通用交互/任务框架。
- CameraVisionSensor2D：在一个组件内完成扫描角、距离/FOV/遮挡检测、约 10Hz 采样和 P0 世界锥子节点同步；先不拆 CameraScanMotor/VisionConeRenderer，视觉节点不参与权威判定。
- RobotPatrol：Waypoint、Slip、阻挡和配置化推动。
- BananaSlipZone、BananaPeel：Day 1 固定滑区与 Day 2 可搬机关分离。
- SunZone、FeederConfirmationArea、SideDoorTrigger：只报告世界进入/离开；SunTime、确认循环与 Backup 教学全部由 LevelTwoFlowController 持有。
- LevelSceneContext：每个 World Scene 唯一序列化注册锚点，暴露 ILevelRuntime/ILevelViewModel 和必需引用；SceneFlowService 从新加载 Scene 的 Root 精确取得该锚点并绑定，不使用 FindObjectOfType。
- LevelOneFlowController、LevelTwoFlowController：唯一关卡状态权威。
- 固定结尾演出先由各关 Flow 直接驱动 Animator/Coroutine；只有出现真正共享的第三段演出后才抽取独立播放器。

UI：

- UIPanelRouter、TitlePresenter、LevelHudPresenter、DialoguePresenter、ReportPresenter、ChoicePresenter、PausePresenter、SettingsPresenter。
- MockLevelViewModelHost 只存在于 90_UIRoot_Test。
- Presenter 订阅 Core 状态并发送 ICommandSink 命令，不访问 Gameplay 组件。

UI 生命周期固定为：Task/HUD 在世界运行时显示，在报告、暂停和结尾隐藏；BarkPrompt 在成功、超时或状态切换时关闭；Report/Choice 打开时切换到 UI Action Map；Toast/Subtitle 使用短队列且状态切换时可安全清空。WorldToScreen 提示只持有稳定 Anchor ID 或短期只读坐标，不保存任务状态或反向控制世界对象。

Editor：

- ProjectAutomation：幂等创建/更新目录、配置、Prefab、Scene、SO 和 Build Settings。
- ProjectValidator：架构、Scene、引用、输入和 Missing Script 校验。
- WindowsBuildAutomation：清理旧输出后生成 Development/Release Windows x64 构建，并保存 BuildReport 摘要。
- FullTitleToEndingSmokeTests：由 Unity Test Framework 构建并启动真实 Windows Player，驱动正式 Scene、输入、状态机和生产 UGUI；环境变量只控制截图与跨进程存档验证，不进入 Release 业务逻辑。

固定运行顺序：

1. Input System 回调只记录玩家意图或发送高层 UI Command。
2. FixedUpdate 执行 Rigidbody2D 移动、搬运跟随、Robot、Trigger 与 10Hz 视野采样。
3. LevelOne/TwoFlowController 消费世界事件并推进唯一权威状态。
4. Animator、WorldVFX、Audio 响应状态变化，但不反向决定任务成功。
5. ILevelViewModel 广播只读快照，UGUI Presenter 事件驱动刷新。

跨 Core 边界的事件/DTO 只允许稳定 ID、枚举和数值快照，不携带 Gameplay 的 GameObject、Transform、Collider2D 或 UI 类型。Bark 属于 Gameplay Action Map；BarkPrompt 只显示窗口，不替玩家发送 Bark。

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
- 导入候选先按解码像素去重；当前 53 个 PNG 含 4 对像素级重复，实际只有 49 个独立画面。重复编码不得产生两套 Unity 资产或两条来源记录。
- 视觉基线采用温暖手绘斜 45°公寓、琥珀阳光、青绿家具/蓝灰设备，以及米色纸张/剪贴簿式 UGUI；Camera 状态使用蓝/红/绿。参考图中的密集 Dashboard、触屏动作栏和烘焙英文不进入 PC HUD。
- `4b9f737dc644643325fe3f4877875989.png` 只作为 Title 背景候选；必须先确认来源/授权、1920×1080 放大和裁切质量，并在其上叠加真实 UGUI Button。其余复合 Mockup、地图、流程板和报告只作参考。
- 03 Web 中可提取 4 张真正带 Alpha 的主人/Latte/老板/AI WebP 头像；它们只可作为有来源记录的临时 UGUI 原型素材，待统一角色画风后替换，绝不用于世界碰撞或玩法状态。
- 主人/老板/AI 表情稿混有写实动漫与扁平矢量两套风格，且棋盘格已烘焙；Milestone 4 前必须确定一套角色设计并重制真实 Alpha 头像。Latte 的品种、轮廓和配色也必须统一。
- 所有正式文字用 TMP 重建；禁止把带烘焙文字的 Mockup 当 UI，禁止把复合房间图当世界背景后附加 Collider 冒充可交互关卡。
- 缺少 03_LatestDesignSource.pdf 和 05_ArtSource.mg 时，使用风格统一、结构可替换的灰盒 Sprite/Prefab，并记录缺口。
- 原始资料留在 `Docs/Reference`；只有经过来源确认、裁切/去底、命名和尺寸处理的派生资产才进入 `Assets/PetOffline/Art`，并在 `Docs/Development/ASSET_PROVENANCE.md` 记录来源文件、处理方式、许可/未知项和替换状态。
- World Prefab 将逻辑根和 Render 子节点分离，美术替换不得改变 Collider、Sensor、Path 或状态引用。
- 世界 Sprite 默认 100 PPU、Bilinear、无 Mipmap、底部 Pivot；UI Sprite 无 Mipmap。最终按像素密度和视觉测试调节。
- 优先用 Unity Import Preset 和 Editor Setup 对 `Assets/PetOffline` 统一导入：World/UI Sprite 使用 sRGB、Sprite 类型、Mipmap off、Read/Write off；角色 Sprite Sheet 明确切片；Windows 压缩设置由视觉对比后锁定。只有 Preset 无法稳定覆盖重复导入时才增加最小 AssetPostprocessor，不为未来资产预建复杂管线。
- 短 SFX 默认 `Decompress On Load`，长音乐默认 Vorbis + `Streaming`，较长语音默认 Vorbis + `Compressed In Memory`；空间化世界音频优先单声道。只有内存或加载采样证明不合适时再改。SpriteAtlas 等最终 Sprite 集稳定后按 World/UI 分开建立，不提前为灰盒资源建通用图集系统。
- 房间灰盒优先 SpriteRenderer/Tilemap 与独立 Collider2D；大型背景图不能携带任务碰撞。角色至少预留 Idle、Move、CarryLight、CarryHeavy、Bark、Slide、Lie、Sleep 的 Animator 状态，缺帧时用可替换静态姿势而不伪造完成度。
- Vision Cone 用世界 Mesh/SpriteRenderer 表现，检测和视觉读取同一 CameraScanConfigSO。
- 中文 TMP 字体必须离线随 Build 携带并确认授权/字符覆盖；音频按 Music/SFX/Voice 分类，世界设备使用 2D/有限空间化 AudioSource，UI 使用独立总线。音频缺失时使用确定性占位 Clip，不在运行时联网或生成。
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

- `PlayerController2D`、`CarryController`、`CameraVisionSensor2D`、`RobotPatrol`、`LevelFlowController` 及其 Day 1/Day 2 派生/实现类型不得位于任何 Canvas 子层级。
- Gameplay World 对象不得使用 RectTransform。
- Latte、房间、Camera/vision cone、Robot、Goal/Sun/Feeder/SideDoor Trigger 不得使用 UGUI Graphic 组件承载玩法。
- World Scene 不得包含 Screen Space HUD。
- World Space Canvas 不得持有任务状态。
- UI asmdef 不引用 Gameplay；Gameplay asmdef 不引用 UI。
- 必需 Scene 存在且 Build Settings 顺序正确。
- Bootstrap、UIRoot、Main Camera、AudioListener、EventSystem 各自唯一；Canvas_HUD/Overlay 模式、Overlay sortingOrder=100 和 InputSystemUIInputModule 正确。
- 必需序列化引用不为空。
- 无 Missing Script 或 MissingReference。
- Gameplay/UI Input Map 和键位完整。
- Physics Layer、碰撞矩阵、Sorting Layer 和顺序完整。
- URP 2D、Main Camera、AudioListener 和 Cinemachine 配置有效。
- ProjectVersion、必要 Package 锁定且 PlayerSettings 为 Windows x64 / 1920×1080。
- 必需 ScriptableObject 已创建并绑定。
- 每个 World Scene 恰有一个 LevelSceneContext（或等价注册锚点），所有必需世界引用可由 SceneFlowService 精确绑定。
- PetOffline Runtime 代码不得使用 `FindObjectOfType`/`FindFirstObjectByType` 作为业务依赖定位，也不得在 SceneNames/SceneCatalog 之外散落硬编码 Scene 名；由 EditMode 静态审计锁定。
- Camera A 不得挂 hostile `CameraVisionSensor2D`；Camera B 必须有扫描配置与遮挡 Mask；FeederCamera 必须可独立于 Feeder 本体禁用。

Debug 开关只服务调试和验收，不进入 Release 玩家流程：

| 开关 | Development/Editor | Release |
| --- | --- | --- |
| ShowVisionLogic | 显示逻辑锥、采样点和遮挡射线 | 强制关闭 |
| ShowTriggerGizmos | 显示 Goal/Sun/Feeder/SideDoor/Path | 强制关闭 |
| ForceLevelState | 测试/开发菜单跳转到指定显式状态 | 不编译或不可用 |
| DisableUI | 一键关闭整个 UIRoot 执行断电测试 | 不暴露 |
| MockUI | 仅 90_UIRoot_Test 绑定 Mock ViewModel | 不进入 Build Settings |

Validator 必须检查这些开关的 Release 默认值和 Scene 归属，防止调试路径泄漏进正式 Build。

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
- Camera angle/range/occlusion test，并验证普通蓝/警戒红最小世界锥读取同一配置且不参与权威检测。
- CarryGoalZone2D 2 秒、离开清零和即时完成 test。
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
8. Day 1 carry/banana/pillow/robot interaction test：轻物约 85%、重物约 60%、抱枕 Bark 必掉；BananaSlip 0.8–1.0 秒且期间不可转向/主动放物；Robot 可推动落地抱枕。
9. Day 1 boss-call success/timeout test：成功安全窗约 3 秒；失败强化扫描/FOV/range 5–8 秒并自动恢复。
10. Day 1 final report → ending → Day 2 transition test。
11. Day 2 first 10-second confirmation test。
12. Day 2 feeder return resets progress test。
13. Day 2 ignored confirmation pauses progress and enlarges/accelerates scan test。
14. Day 2 feeder-camera disable test：只关闭摄像头，Feeder 本体仍工作，世界锥与逻辑 active 状态同步。
15. Day 2 backup-camera activation test。
16. Day 2 wrong-route confirmation and recovery test。
17. Day 2 Backup 教学不可跳过 + correct-route 20-second completion test。
18. Restore Connection ending test：选择后必须再次累计到 10 秒，观察 `ConfirmationActive=true` 与主人点名重现，不能只断言摄像头启用。
19. Keep Quiet ending test。
20. Save/unlock/Continue/Restart/ReturnToTitle test。
21. Pause、Settings、音量持久化和窗口失焦/恢复 test。
22. Full title-to-ending smoke test：两个分支各自从 Title 独立运行。

另做一次无说明文档的首次玩家 UX 记录：玩家应能从世界锥和失败反馈理解 Camera B、从 10→0 理解确认死循环、从 Backup 提示与动线理解必须绕客厅。该项是人工可用性验收，不用自动测试伪装“玩家理解”。

UIRoot 断电自动化步骤：

1. 加载 Bootstrap + Day 1。
2. 通过 Gameplay Bark 完成 Opening 的无失败教学点名并确认进入 TaskShoes；不能依靠 UI 回调结束开场。
3. 禁用整个 UIRoot GameObject，并断言 Gameplay Action Map 仍启用。
4. 使用 Input System 测试键盘驱动 Latte，断言 Rigidbody2D 世界位置发生预期位移。
5. 拾取拖鞋，断言 CarryController/CarryableObject 的世界携带关系成立；将其送入 Camera A GoalArea，断言 2 秒后进入 TaskPillow。
6. 在携带当前抱枕时进入 Camera B 视野，断言 Camera B 扫描角实际变化、检测触发、仅 Latte/抱枕/Camera B 临时状态复位，拖鞋完成状态仍保留。
7. 断言 RobotPatrol waypoint index/世界位置推进，并让机器人推动落地抱枕；将抱枕送入 Dog Bed GoalArea，断言 Flow 进入 FinalBark。
8. 通过 Gameplay Bark 完成 FinalBark，断言世界状态到达 Report；随后直接向 Core ICommandSink 发送 ContinueReport，保持 UIRoot 禁用并验证世界自动演出、DayOneCompleted 保存和 Day 2 additive 加载。测试期间不得读取、依赖或重新启用 UI。

Day 2 断电测试从独立 Bootstrap + Day 2 开始并保持 UIRoot 禁用：断言 SunZone+Lie 累计、10 秒确认暂停、Feeder Area 清零、BananaPeel/Robot 令 `FoodCameraActive=false`、SideDoor 激活 Backup、错误路线重试、`BackupLessonCompleted=true` 后客厅路线达到 20 秒并进入 Report。随后用 Core 命令进入 Choice；Restore/Keep Quiet 在两个隔离测试中各自验证世界设备与演出结果。所有断言读取 Gameplay/Core 状态，不读取 UI 文本或 Presenter。

独立 UI 生命周期测试：卸载所有 World Scene、解绑 ILevelViewModel 后保持 UIRoot 激活，等待一个 Frame 并断言所有 Presenter 无 MissingReference/NullReference、Gameplay 命令禁用、显示“等待关卡数据”；随后绑定 Mock ViewModel，预览 HUD、报告、设置、选择和两结局。

完整链路 Smoke 必须实际证明：

- 标题可以进入 Day 1。
- Day 1 报告后进入 Day 2。
- Day 2 经历 10 秒确认循环。
- 香蕉皮和机器人让 Feeder Camera 离线。
- 侧门错误路线激活 Backup Camera 且完成教学，客厅路线才可完成 20 秒。
- Day 2 报告和两个选择均可到达且实际执行各自结局。
- Keep Quiet 显示最终字幕。
- 两个结局均可返回标题或重开。

双结局必须是两次互相隔离的运行，不能在同一已推进实例中只切换按钮：

| Run | 初始存档 | 路径 | 必须输出的成功标记 |
| --- | --- | --- | --- |
| `Full_KeepQuiet` | 空白独立存档 | Title → New Game → Day1 → Day2 → Keep Quiet → Return to Title | `Day1ReportReached`、`DayOneSaved`、`Day2ReportReached`、`Ending=KeepQuiet`、最终字幕匹配、`ReturnedToTitle` |
| `Full_Restore` | 另一份空白独立存档 | Title → New Game → Day1 → Day2 → Restore Connection → Restart/Return | `Ending=RestoreConnection`、新一轮 SunTime 达到 10、`ConfirmationActive=true`、Owner Call 再现、`Restarted` 或 `ReturnedToTitle` |
| `Continue_CrossProcess` | `SeedDay1` 专用 run 在 Day 1 保存完成后退出并写出的存档 | 关闭进程 → 使用同一存档启动 `ContinueDay2` → Title → Continue → Day2 | `ContinueVisible`、`Day2LoadedFromSave`、无 Day1 重播 |

每条 standalone 运行必须有独立 save/result/log/screenshot 目录，防止 PlayerPrefs 或上次流程污染结果。

Title → 双结局闭环审计：

| 转移 | 发起者 | 权威执行者 | 自动证据 |
| --- | --- | --- | --- |
| Bootstrap → Title | Bootstrap 完成 | GameSession/UIRoot 只显示标题 | Title 可交互；无 World Scene |
| Title → Day 1 | `StartNewGame` Command | SceneFlowService additive load；LevelOneFlowController 进入 Opening | `Full_*` 两条 run 均从 Title 进入 Day1 |
| Day 1 Report → Ending | `ContinueReport` Command | LevelOneFlowController 启动本关固定世界演出 | 报告先于世界演出，UI 不直接移动 Latte |
| Day 1 Ending → Day 2 | 世界演出完成事件 | GameSession 保存 DayOneCompleted；SceneFlowService 卸载/加载 | Save/unlock 与 Day2 Scene 序列标记 |
| Day 2 Report → Choice | `ContinueReport` Command | LevelTwoFlowController 请求 Choice 状态 | ChoicePanel 仅提交命令 |
| Choice → Restore | `SubmitChoice(Restore)` | LevelTwoFlowController 恢复确认设备并重启点名 | 新一轮 SunTime 到 10 秒、`ConfirmationActive=true` 且 Owner Call 再次出现后才标记结局 |
| Choice → Keep Quiet | `SubmitChoice(KeepQuiet)` | LevelTwoFlowController 关闭远程确认并驱动睡眠演出 | 两摄像头关闭、睡眠状态、最终字幕精确匹配 |
| 任一 Ending → Title/Restart | `ReturnToTitle` / `Restart` Command | GameSession + SceneFlowService | World Scene 正确卸载/重载，Bootstrap 不重复创建 |
| 重新启动 → Continue | Title Continue | SaveService + SceneFlowService | 跨进程加载 Day2，不重播 Day1 |

该表中的每个转移都有唯一权威执行者；不存在 ReportPanel、ChoicePanel 或其他 UI 直接移动角色、开关摄像头或设置关卡状态的缺口。

执行闭环审计结论：当前 Unity 工程与最终 Player 证据已覆盖 `Title → Day 1 → Day 1 Report/Ending → Day 2 → Day 2 Report → Choice → Restore/Keep Quiet → Return/Restart`、两个独立结局和跨进程 Continue。自动化证据不替代 12–15 分钟首次玩家及外部环境验收。

Web 参考核对不是 Unity 架构测试。静态 HTML/JS 已完整核对，且通过本地只读 HTTP 抽样实跑了 Title→Day 1、拖鞋拾取与一次 Boss Call 成功安全窗；完整 Day 1、Day 2、报告和双结局仍是补充参考缺口。无论是否补跑，都不得把静态分析或局部实玩冒充完整流程验收，也不得让 Web 行为覆盖更高优先级裁决。

## 12. 构建与验收命令

Setup、Validation、EditMode、PlayMode 与 WindowsBuildAutomation 入口均已实现并实际运行。Standalone 证据由 `FullTitleToEndingSmokeTests` 产生；Unity Test Runner 命令不依赖 `-quit`，如出现残留进程只处理本次启动的精确 PID，不能关闭其他 Unity 项目。

性能实现预算：常驻世界相机只有 Main Camera；CaptureCamera 仅按需启用；摄像头感知约 10Hz；禁止运行时生成大纹理；Update/FixedUpdate 热路径避免 LINQ 和字符串拼接。对象池只用于经 Profiler 证明会频繁生成的粒子或 World UI，不预建通用池框架。

计划批处理入口：

- PetOffline.Editor.ProjectAutomation.SetupBatch
- PetOffline.Editor.ProjectValidator.ValidateBatch
- PetOffline.Editor.WindowsBuildAutomation.BuildDevelopmentBatch
- PetOffline.Editor.WindowsBuildAutomation.BuildReleaseBatch
- `FullTitleToEndingSmokeTests` 的 Windows Player 两结局、Seed 与 Continue 独立运行

所有 batch 入口捕获异常、写明失败原因并返回可靠进程码；`WindowsBuildAutomation` 还检查 BuildReport Summary，失败时返回非零，不能只依赖 Unity 日志文本。

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

    function Invoke-UnityTestChecked(
        [string]$Name,
        [string]$Platform,
        [string]$XmlPath,
        [string]$LogPath,
        [bool]$NoGraphics
    ) {
        Remove-Item -LiteralPath $XmlPath,$LogPath -Force -ErrorAction SilentlyContinue
        $Args = @('-batchmode','-projectPath',$Project,'-runTests','-testPlatform',$Platform,
            '-testResults',$XmlPath,'-logFile',$LogPath)
        if ($NoGraphics) { $Args = @('-batchmode','-nographics') + $Args[1..($Args.Count-1)] }

        $Process = Start-Process -FilePath $Unity -ArgumentList $Args -PassThru -WindowStyle Hidden
        $Deadline = [DateTime]::UtcNow.AddMinutes(10)
        $XmlComplete = $false

        while ([DateTime]::UtcNow -lt $Deadline) {
            $Process.Refresh()
            if (Test-Path $XmlPath) {
                try {
                    [xml]$Probe = Get-Content $XmlPath -Raw
                    if ($null -ne $Probe.'test-run'.result) { $XmlComplete = $true; break }
                } catch { }
            }
            if ($Process.HasExited) { break }
            Start-Sleep -Milliseconds 500
        }

        if (-not $XmlComplete) {
            if (-not $Process.HasExited) { Stop-Process -Id $Process.Id -Force }
            throw "$Name did not finish with a complete test XML"
        }

        Start-Sleep -Seconds 2
        $Process.Refresh()
        if (-not $Process.HasExited) {
            Stop-Process -Id $Process.Id -Force
            $Process.WaitForExit()
        } elseif ($Process.ExitCode -ne 0) {
            throw "$Name player exit code $($Process.ExitCode)"
        }
        Assert-TestXml $XmlPath $Name
    }

    Invoke-UnityChecked 'Setup' @('-batchmode','-nographics','-quit','-projectPath',$Project,'-executeMethod','PetOffline.Editor.ProjectAutomation.SetupBatch','-logFile',"$Results\Setup.log")
    Invoke-UnityChecked 'Validation' @('-batchmode','-nographics','-quit','-projectPath',$Project,'-executeMethod','PetOffline.Editor.ProjectValidator.ValidateBatch','-logFile',"$Results\Validation.log")
    Invoke-UnityTestChecked 'EditMode' 'EditMode' "$Results\EditMode.xml" "$Results\EditMode.log" $true
    Invoke-UnityTestChecked 'PlayMode' 'PlayMode' "$Results\PlayMode.xml" "$Results\PlayMode.log" $false
    Invoke-UnityChecked 'BuildDevelopment' @('-batchmode','-nographics','-quit','-projectPath',$Project,'-buildTarget','StandaloneWindows64','-executeMethod','PetOffline.Editor.WindowsBuildAutomation.BuildDevelopmentBatch','-logFile',"$Results\BuildDevelopment.log")
    Invoke-UnityChecked 'BuildRelease' @('-batchmode','-nographics','-quit','-projectPath',$Project,'-buildTarget','StandaloneWindows64','-executeMethod','PetOffline.Editor.WindowsBuildAutomation.BuildReleaseBatch','-logFile',"$Results\BuildRelease.log")
    if (-not (Test-Path "$Builds\Development\PetOffline.exe")) { throw 'Development EXE missing' }
    if (-not (Test-Path "$Builds\PetOffline.exe")) { throw 'Release EXE missing' }

输出固定为：

    Builds/Windows/Development/PetOffline.exe
    Builds/Windows/PetOffline.exe

`WindowsBuildAutomation` 显式使用 `BuildTarget.StandaloneWindows64` 和 x86_64，并检查 `BuildReport.summary.result == BuildResult.Succeeded`。本机只安装 Windows x64 Mono Player variation，因此本垂直切片构建为 Mono x64；最终 Development 与 Release BuildReport 均为 Succeeded、0 error、0 warning。

Standalone 自动 Smoke 使用 Unity Test Framework 的 `FullTitleToEndingSmokeTests`。测试构建并启动真实 Windows Player，只缩短对话、Goal Hold、Boss Call 和 Sun/Confirm 等等待，并通过公开 Gameplay/API 与生产 UI 按钮推进；禁止直接写权威 Flow State、伪造报告或绕过结局。

- `StandalonePlayMode_Screenshots_Final.xml`：两个独立完整结局及截图，2/2 PASS。
- `Standalone_CrossProcess_Seed.xml`：设置 `PETOFFLINE_KEEP_SAVE=1`，完成前一进程并保留 `DayOneCompleted`，1/1 PASS。
- `Standalone_CrossProcess_Continue.xml`：设置 `PETOFFLINE_REQUIRE_EXISTING_SAVE=1`，禁止测试内 Seed，下一进程点击生产 `ContinueButton` 进入 Day 2，1/1 PASS。
- `PETOFFLINE_CAPTURE_SCREENSHOTS` 与 `PETOFFLINE_SCREENSHOT_DIR` 只控制证据输出，不改变业务状态。

缩时测试仍经过真实 Bootstrap/additive World Scene、Input、Physics2D、世界 Trigger、状态机、报告、选择和结局。正常 Release 配置继续按 12–15 分钟人工验收。

Standalone 验收不能由普通 Editor PlayMode 代替。当前已完成：

- Windows Player 两个独立完整结局、Return/Restart 和状态截图。
- 两个独立 Player 进程之间的 Day 1 Seed 与生产 Continue。
- Development/Release 两个干净 Build；Release 另行可见启动，窗口响应且 Player 日志无运行时异常。
- README 的运行、操作、测试、构建和已知限制说明。

仍保留在 `KNOWN_GAPS.md`：Release 正常时长人工完整分支、首次玩家教学与 12–15 分钟计时、目标机 Profiler/FrameTiming、窗口失焦/多显示器以及第二台机器或新用户断网验证。

最终文档证据固定落点：`Docs/Development/STATE_MACHINES.md` 保存两关状态/转移图，`Docs/Development/ARCHITECTURE_AUDIT.md` 保存 UI/Game 分层与 Validator 结果，`Docs/Development/TEST_REPORT.md` 汇总自动/人工回归，`Docs/Development/KNOWN_GAPS.md` 记录未满足项。离线兼容优先在第二台 Windows x64 机器或干净 VM 验证；若当前没有该环境，至少在新建 Windows 用户、断网状态下运行 Release，并在 KNOWN_GAPS 明确“未完成干净机验证”，不能用本机普通启动冒充。

Release 可见窗口启动命令：

    $ReleaseExe = "$Builds\PetOffline.exe"
    $ReleaseLog = "$Results\ReleasePlayer.log"
    $ReleaseProcess = Start-Process -FilePath $ReleaseExe -ArgumentList @(
        '-force-d3d11','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080',
        '-logFile',$ReleaseLog
    ) -PassThru

该命令只负责真实启动；验收人员必须在可见窗口走完整分支并正常退出。单纯“进程存活 10 秒”只能记为 launch smoke，不能替代标题、两关、报告、选择和结局证据。

## 13. Approval Gate 与 Milestone 0–5

Approval Gate：已通过。用户通过 `/goal` 要求从当前基线继续并完成两关。该批准不授权删除、回退或混入 AGENTS.md、Main/SampleScene、Reference 资料及其他所有权不明改动。

当前执行位置：Milestone 0–3 已完成；Milestone 4 达到垂直切片级交付；Milestone 5 的自动化、Windows Player、双构建和启动门槛已完成。正式美术、人工节奏、性能、焦点和干净机验收保留在 `KNOWN_GAPS.md`；本任务交付已精确提交为 `2538dab`。

每个里程碑共同门槛：

- 在 Unity 6000.3.14f1 中重新导入并编译。
- 运行最小相关真实测试。
- 修复全部编译错误、Missing Script 和 MissingReference。
- 把日志/XML/截图保存到 Artifacts。
- 更新 STATUS.md、DECISIONS.md、KNOWN_GAPS.md 和 TEST_REPORT.md。
- Git status 审核后，只提交本里程碑归属文件并创建 commit。

| Milestone | 工作 | 完成条件 |
| --- | --- | --- |
| Milestone 0 Foundation | 审计并收敛已有 Foundation commit/工作树；包配置、目录、asmdef、Input、Bootstrap、四个 Scene、加载/绑定、Setup、Validator、Web 行为对照记录 | Unity 零编译错误；Scene 能打开；Build Settings、Layer/Sorting/PlayerSettings 正确；架构校验通过；UIRoot 无 World 等待状态与基础 EditMode/PlayMode 通过；不相关工作树改动未被触碰 |
| Milestone 1 Day 1 | 世界灰盒、移动、85%/60% 搬运、Camera A/B 固定扫描/检测/P0 世界锥、0.8–1 秒香蕉滑区、机器人、拖鞋、抱枕、确定性 Boss Call、Final Bark、ReportDefinition 和自动演出 | 世界 Flow 可到 Report，并由测试 Core Command 继续到 Ending→Complete→Day2；局部重置、前置保留、滑行不可转向/放物、机器人交互、3 秒安全窗/5–8 秒警戒、视野锥同步和 UIRoot-disabled 测试通过；此阶段不要求生产 ReportPanel |
| Milestone 2 Day 2 | SunZone、10 秒确认、暂停/清零、Feeder/Backup Camera 与 P0 世界锥、BananaPeel、Robot 撞击、Offline、不可跳过的 Backup 教学、错误/正确路线、20 秒、ReportDefinition 和两种世界结局 | 世界 Flow 可到 Report/Choice；Feeder 离线只关闭摄像头且锥同步；隔离测试验证 Restore 真正重启确认、Keep Quiet 播放睡眠；全部 Day 2 P0 世界测试通过，无软锁；此阶段不要求生产 ChoicePanel |
| Milestone 3 UGUI | 标题、HUD、唯一主目标、字幕、视频窗、提示、两日报告、选择、暂停、设置、音量、淡入淡出、UIRoot_Test | UI 只绑定 Core；Mock/无 World 两模式可预览；玩家可从 Title 操作到两种结局；UIRoot Disabled 逐项断电测试、设置持久化和失焦恢复测试通过 |
| Milestone 4 Art/Audio/VFX | 导入可用美术、统一 Latte/主人/老板/AI 设计、SpriteRenderer 排序、动画、音效、世界 VFX、视野表现、灯光和最终演出 | 1920×1080 视觉检查通过；Title 背景无错误裁切；关键 Sprite 无烘焙棋盘底；人物画风一致；世界/UGUI 边界不回退；关键状态截图齐全；正常流程 12–15 分钟 |
| Milestone 5 QA/Build | 全部 EditMode/PlayMode、三类 standalone Smoke、首次玩家 UX、架构验证、性能/焦点/离线检查、Development/Release Windows x64 Build | 所有 P0 通过；两个独立标题到结局 run、跨进程 Continue、Return/Restart 均有 JSON/日志/截图；首次玩家能理解三段核心教学；`Builds/Windows/PetOffline.exe` 可运行；完成干净机/VM 离线验证或诚实记录缺口；README、状态机图、架构审计、Known Gaps 与 Artifacts 完整 |

## 14. 风险、默认假设和回退

| 风险/假设 | 默认决定 | 回退 |
| --- | --- | --- |
| 缺少 PDF 和 .mg | 继续使用现有最高优先级 HTML 与可替换灰盒 | 资料补齐后在美术精修前重做冲突审计 |
| 53 张 PNG 无可用透明切片 | 仅作视觉参考，不直接当正式 Sprite | 重新切图或由正式源资产导出 |
| Web 原型仅完成局部交互抽样 | 已完整静态审计，并通过本地 HTTP 实跑到 Day 1 拾鞋/Boss Call；不把它当 Unity 验收 | 需要节奏核对时再补跑完整 Web 双结局；冲突仍由 02/01 裁决 |
| AGENTS 路由的多份 TEngine 参考文件不存在 | 不假设 TEngine 热更/模块/Luban/资源框架，按当前 Unity 项目和实际工具 schema 执行 | 文件补齐后只在不冲突时增补约定 |
| MCP 可能连接错误项目 | 写入前强制核对 Project Root | 使用 Editor 脚本 + batchmode |
| 当前仓库已有用户改动 | 不覆盖、不清理、不混入 milestone commit | 冲突时停在具体文件并报告 |
| Foundation/Day 1 已提交，Day 2/UI 和 QA 交付提交于 `2538dab` | 最终 Validator、EditMode、PlayMode、Player 与 Build 证据已覆盖该交付 | 后续提交继续精确暂存，不回退用户或其他代理的无关改动 |
| Cinemachine 3.1.7 已安装，但实际关卡镜头尚未验收 | 优先使用正交 Main Camera + 必要 VCam | 若集成增加风险，灰盒先用固定正交 Camera，最终仍满足镜头体验与架构校验 |
| Windows 模块仅有 Mono x64，无 IL2CPP variation | 本垂直切片默认 Mono x64，并把 backend 写入 BuildReport | IL2CPP 不是当前 DoD；只有用户另行要求时才评估安装 |
| 中文字体授权或字符覆盖不足 | 选择可随包分发的离线 TMP 字体并生成所需字符集 | 使用经授权的系统/开源字体替代，记录视觉差异 |
| 02 P1 Backlog 提到语言持久化 | 本切片固定简体中文，只保存音量、进度和必要设置 | 用户明确要求本地化时再评估，不预建语言框架 |
| Backup 路线可能软锁 | 当前尝试局部重置，Feeder Camera 离线状态保留 | 通过 PlayMode 测试锁定恢复规则 |
| Backup 教学可能被直接走客厅跳过 | `BackupLessonCompleted` 是 FinalSun 前置，先用地图动线自然引导 | 若 Playtest 仍可跳过，使用可见世界阻挡并在 Backup 重试后清除；禁止 UI 门控 |
| UIRoot 关闭后 Opening/Report 可能等待 UI 回调 | Opening 由 Gameplay Bark 完成，Report/Choice 只等待 Core Command；断电测试全程不重新启用 UIRoot | 测试直接发送正常 Gameplay 输入/高层 Command，不允许 UI 成为状态机依赖 |
| 12–15 分钟节奏受美术/对话/路线影响 | 第一轮按 Web 数值与 01/02 目标配置，Milestone 4 用首次玩家计时 | 只调 SO 数值、路线和对话时长，不删核心教学循环 |
| 隐藏 Player 会话只能输出 1024×768 | 原始帧保留在 `Artifacts/Screenshots/Native`；报告、选择和结局图居中裁切为 16:9 并 bicubic 缩放，标题和世界图使用 1920×1080 渲染 | 这些图片只作状态证据；目标显示器像素、字体和裁切仍由人工视觉验收关闭 |
| 复杂通用框架增加风险 | 直接事件、显式状态机、六类 SO | 只有真实重复需求出现后再抽象 |
