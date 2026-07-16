# Pet Offline Unity Project Instructions

## 1. Project Goal

Build a complete, playable Unity vertical slice of:

Pet Offline / 《老板，我狗开会了》

The deliverable is a 12–15 minute PC game containing:

Title Screen
→ Day 1: 狗已上线
→ Day 1 Report
→ Day 2: 偷偷安抚
→ Day 2 Report
→ Final Choice
→ Ending
→ Return to Title / Restart

This is a complete two-level vertical slice, not a commercial full game.

Target:
- Unity 6 LTS
- Windows x64
- 1920×1080, 16:9
- 2D isometric / 斜45度俯视
- Keyboard and mouse
- Offline
- Fixed dialogue
- No generative AI runtime
- No web dependency

Do not implement the discarded three-chapter version, questionnaires, research pages,
or unrelated early concepts from the source PDF.

## 2. Source of Truth

Read all files before implementation.

Priority order:

1. Docs/Reference/02_UnityImplementationPlan.html
   Technical architecture, scenes, C# responsibilities, backlog, tests and build rules.

2. Docs/Reference/01_UnityDesignPlan.html
   Final gameplay, experience, level flow, theme and UGUI/Game separation.

3. Docs/Reference/03_LatestDesignSource.pdf
   Original design source, visual diagrams, dialogue and final program requirements.
   When reading this PDF, prioritize the late-stage sections named:
   - 第一关程序需求文档
   - 第二关程序需求文档

4. Docs/Reference/04_WebPlayableReference.html
   Interaction, timing and behavioral reference only.
   Never copy its single-Canvas architecture into Unity.

5. Docs/Reference/05_ArtSource.mg
   Art and visual reference. Extract usable assets when possible.
   If the format cannot be parsed, use clean replaceable placeholders and document the gap.

If an older file named 朱佳琪项目一.pdf exists, treat it only as historical reference.

When sources conflict:
02_UnityImplementationPlan.html
> 01_UnityDesignPlan.html
> final program requirement sections in the latest PDF
> web demo
> early drafts.

Create Docs/Development/SOURCE_OF_TRUTH.md summarizing every resolved conflict.

## 3. Non-Negotiable Architecture Contract

World owns gameplay; UGUI presents state.

### Game World must contain

Use normal world-space GameObjects with Transform, SpriteRenderer,
Rigidbody2D, Collider2D, Animator, AudioSource and world-space effects for:

- Latte player character
- Apartment floor, walls and furniture
- Owner slipper
- Boss pillow
- Banana peel / banana slip zone
- Cleaning robot
- Feeder
- Camera A
- Camera B
- Feeder camera
- Backup camera
- Camera vision cones
- Vision occlusion
- Dog bed goal area
- Camera A goal area
- SunZone
- Feeder confirmation area
- Side-door trigger
- Robot waypoint paths
- World collision
- World audio
- Automatic ending performances

### UGUI may contain only

- Title menu
- HUD
- Current objective
- Progress bars
- Interaction prompts
- Owner video panel
- Boss dialogue panel
- AI notifications
- Subtitles
- Bark timing prompt
- Daily reports
- Final choice
- Pause/settings
- Screen fades
- Toasts and achievements

World Space Canvas is allowed only for small prompts or markers.
It must not own gameplay state.

### Forbidden implementations

- Do not implement Latte as a UI Image.
- Do not move gameplay objects through RectTransform or anchoredPosition.
- Do not implement camera detection using GraphicRaycaster.
- Do not put the apartment inside a Canvas.
- Do not embed the HTML game through WebView.
- Do not render the entire Main Camera into one RawImage and perform gameplay there.
- Do not let ReportPanel, HUD or other UI scripts directly enable cameras,
  move characters or advance mission state.
- Do not store authoritative mission state inside UI scripts.

RenderTexture is allowed only for a real in-world monitor, camera capture,
or optional meeting screen.

### Assembly dependency rule

PetOffline.UI → PetOffline.Core
PetOffline.Gameplay → PetOffline.Core

PetOffline.UI and PetOffline.Gameplay must not reference one another.

Gameplay exposes read-only events and ILevelViewModel.
UI sends high-level commands through ICommandSink.

## 4. Required Scenes

Create:

- 00_Bootstrap
- 10_Day1_Meeting
- 20_Day2_Sunbath
- 90_UIRoot_Test
- 99_Test_Playground, optional

00_Bootstrap contains persistent services:

- GameSession
- SceneFlowService
- InputRouter
- AudioService
- SaveService
- DialogueDirector
- Main Camera / Cinemachine Brain
- UIRoot

Load each world level additively.
Only one gameplay world level may be active at a time.

World level scenes must contain:

- WorldRoot
- Environment
- Collision
- Actors
- Interactables
- Devices
- Sensors
- Triggers
- Paths
- WorldVFX
- WorldAudio
- LevelFlow
- VirtualCamera

They must not contain the screen HUD.

## 5. Input

Default bindings:

- Move: WASD and arrow keys
- Interact / pick up / drop: E
- Bark: Space
- Push: Q
- Lie down / sunbathe: Left Shift
- Pause: Escape
- UI: mouse and keyboard navigation

Use the Unity Input System with separate Gameplay and UI action maps.

## 6. Day 1 Required Flow

State order:

Opening
→ TaskShoes
→ TaskPillow
→ FinalBark
→ Report
→ Ending
→ Complete

### TaskShoes

- Owner slipper starts beside the dog bed.
- Camera A is a goal camera and never detects the player.
- Camera B is the only hostile camera.
- Camera B scans left and right on a fixed loop.
- Latte may safely cross the cone while not carrying the current mission item.
- Carrying the slipper inside Camera B detection triggers failure.
- Failure resets only Latte, the slipper, Camera B angle and temporary alert state.
- The opening must not replay.
- The slipper must remain in Camera A GoalZone for two seconds.
- On completion, lock the slipper and unlock the boss pillow.

### TaskPillow

- Boss pillow starts in front of Camera A.
- It is a heavy carryable.
- Heavy carry speed is approximately 60 percent.
- Barking while carrying the pillow drops it.
- Camera B detection resets only the pillow task.
- Day 1 banana behavior is a configured BananaSlipZone.
- The cleaning robot follows a fixed waypoint path.
- The robot may block Latte and push a dropped pillow a configured distance.
- The pillow completes immediately when it enters the dog-bed goal area.

### Boss calls

- Timed calls create a bark response window.
- Successful response provides a brief safe window.
- Missing the response temporarily increases Camera B scan speed and range.
- Missing a call never causes Game Over.

### Day 1 ending

- Final bark has no failure state.
- Show the fixed Meeting Performance Report.
- Play the automatic speaker / dog-bed ending.
- Save DayOneCompleted.
- Load Day 2.

## 7. Day 2 Required Flow

State order:

Start
→ SunFirst
→ CameraCheck
→ Loop
→ DestroyCamera
→ Backup
→ FinalSun
→ Report
→ Choice
→ End

### Sunbathing

- The only visible main objective is:
  让拿铁晒满20秒太阳
- Latte must enter the world SunZone and lie down.
- At 10 seconds, while the feeder camera is active, trigger confirmation.
- During confirmation, sun progress pauses.
- Returning to the feeder completes confirmation but resets sun progress to zero.
- Ignoring confirmation temporarily enlarges and accelerates the scan.
- No Game Over.

### Destroying feeder confirmation

- Day 2 uses a movable BananaPeel.
- Latte can place the banana peel on the cleaning robot path.
- The robot slips and can collide with the feeder.
- The collision disables the feeder camera, not the whole feeder.
- The feeder displays CAMERA OFFLINE / 当前画面：墙.
- Gameplay state must record FoodCameraActive = false.

### Backup camera lesson

- Passing through the kitchen-to-balcony side door activates the backup camera.
- Display BACKUP CAMERA ACTIVE.
- If the player uses this incorrect route, the next 10-second confirmation loop still occurs.
- The player must learn to disable the feeder camera and return through the living room,
  avoiding the side-door trigger.
- Completion requires no active confirmation camera to have reacquired Latte.
- Latte then reaches 20 seconds and completes the level.

### Report and ending

Show the fixed Day 2 report, including:

- Sunbathe completed
- Confirmation failed
- Camera offline
- Current image: wall
- Unable to confirm whether Latte still misses the owner

Final choices:

- Restore Connection:
  show that the confirmation loop restarts.

- Keep Quiet:
  recommended ending.
  Disable remote confirmation and play the sleep ending.

Final subtitle:

它不是不想你。
它只是终于不用证明它在想你。

## 8. Engineering Requirements

Use:

- Unity 6 LTS patch actually installed on this machine
- URP 2D Renderer
- Input System
- TextMeshPro
- Cinemachine where useful
- Unity Test Framework
- Assembly Definitions
- ScriptableObject configuration
- Event-driven UI presentation
- Explicit level state machines

Do not add DOTS, NavMesh, a third-party quest framework or unnecessary packages.

Prefer Editor scripts and Unity APIs to generate scenes, prefabs and assets.
Do not manually fabricate large Unity scene YAML files when Editor automation is available.

Create configurable assets for:

- LevelConfigSO
- DialogueSequenceSO
- ReportDefinitionSO
- CameraScanConfigSO
- CarryableConfigSO
- AudioCueDefinitionSO

Avoid FindObjectOfType-based architecture and fragile scene-name string logic.

## 9. Required Validation Tools

Create an Editor validation command:

Tools/Pet Offline/Validate Project

It must check:

- No PlayerController2D exists below any Canvas.
- No CarryController exists below any Canvas.
- No CameraVisionSensor2D exists below any Canvas.
- No RobotPatrol exists below any Canvas.
- No LevelFlowController exists below any Canvas.
- Gameplay world objects do not use RectTransform.
- UI assembly does not reference Gameplay.
- Gameplay assembly does not reference UI.
- Required scenes are in Build Settings.
- Required references are not missing.

## 10. Tests

Create EditMode and PlayMode tests.

Required tests:

- Architecture boundary test
- UIRoot disabled gameplay test
- UIRoot mock preview test
- Day 1 shoe completion test
- Day 1 detection reset test
- Day 1 previous-task preservation test
- Day 1 pillow and robot interaction test
- Day 1 final report transition test
- Day 2 first 10-second confirmation test
- Day 2 feeder return resets progress test
- Day 2 ignored confirmation pauses progress test
- Day 2 feeder-camera disable test
- Day 2 backup-camera activation test
- Day 2 wrong-route confirmation test
- Day 2 correct-route 20-second completion test
- Restore Connection ending test
- Keep Quiet ending test
- Save/unlock test
- Full title-to-ending smoke test

## 11. Definition of Done

The project is not done merely because scripts exist.

Done means:

- Unity opens with zero compile errors.
- No missing script or missing reference exists.
- Title Screen starts the game.
- Day 1 is playable from start to report.
- Day 2 is playable from start to final choice.
- Both endings work.
- Save/unlock and restart work.
- All P0 tests pass.
- Architecture validation passes.
- Disabling UIRoot leaves world gameplay functional.
- A standalone desktop build is produced.
- The build has been launched and smoke-tested.
- Screenshots are saved under Artifacts/Screenshots.
- Test results are saved under Artifacts/TestResults.
- Build is saved under Builds/Windows.
- README contains controls, setup, build and test instructions.
- Known limitations are documented honestly.

## 12. Autonomous Work Policy

Do not stop after producing a plan, file tree or code skeleton.

Work milestone by milestone until the Definition of Done is met.

For routine design choices:
- choose a reasonable default;
- record it in Docs/Development/DECISIONS.md;
- continue working.

Ask the user only when blocked by:
- missing Unity installation;
- missing Unity license activation;
- permission required to launch Unity;
- unrecoverable source asset corruption;
- a genuinely irreversible product decision.

Maintain:

- PLAN.md
- Docs/Development/STATUS.md
- Docs/Development/DECISIONS.md
- Docs/Development/KNOWN_GAPS.md
- Docs/Development/TEST_REPORT.md

Update STATUS.md after each milestone.

Commit completed milestones to Git when possible.
Do not overwrite user-created work without checking Git status.
Do not claim something was tested unless the command or Unity run actually succeeded.

## Reference 路由

优先读取 `.codex/skills/tengine-dev/references/` 中的文档：

| 场景 | 必读文档 |
| --- | --- |
| UI 开发 | `ui-lifecycle.md`，需要进阶模式时读 `ui-patterns.md` |
| 资源加载 | `resource-api.md`，需要生命周期/泄漏分析时读 `resource-patterns.md` |
| 热更代码 | `hotfix-workflow.md` |
| 事件系统 | `event-system.md`，排查问题时读 `event-antipatterns.md` |
| 模块使用 | `modules.md` |
| Luban 配置 | `luban-config.md`，复杂配置表任务再读 `.codex/skills/luban-dev` |
| 代码规范 | `naming-rules.md` |
| 项目结构 | `architecture.md` |
| 问题排查 | `troubleshooting.md` |



