# Pet Offline Source of Truth

更新日期：2026-07-15  
状态：资料裁决与实现计划已确认；Milestone 0 Foundation 已有真实 Unity 证据，完整游戏仍在实施中。

## 1. 当前参考资料清单

实际存在：

| 资料 | 角色 |
| --- | --- |
| AGENTS.md | 项目目标、硬性架构、流程、测试和 Definition of Done |
| Docs/Reference/02_UnityImplementationPlan.html | 最高优先级技术实现依据 |
| Docs/Reference/01_UnityDesignPlan.html | 最终玩法、体验、两关流程和表现依据 |
| Docs/Reference/03_WebPlayableReference.html | 行为、提示、时序和结局参考；不得复制架构 |
| Docs/Reference/images/*.png | 53 张视觉、地图、UI 和历史方案参考图 |
| Docs/Reference/Newbie Guide.xlsx | 另一塔防项目的新手引导，排除 |
| Docs/Reference/新手引导.xlsx | 另一塔防项目的新手引导，排除 |

计划中提到但当前不存在：

- Docs/Reference/03_LatestDesignSource.pdf
- Docs/Reference/04_WebPlayableReference.html；当前对应文件名为 03_WebPlayableReference.html
- Docs/Reference/05_ArtSource.mg
- 旧文件 朱佳琪项目一.pdf

AGENTS.md 的“Reference 路由”还列出了 `ui-lifecycle.md`、`architecture.md`、`naming-rules.md` 等 TEngine 参考文件；当前仓库的 `.codex/skills/tengine-dev/references/` 实际只有 `mcp-tools.md` 和 `mcp-visual.md`。因此本项目不能假设不存在的 TEngine 模块、热更、资源或 Luban 约定，后续只采用当前 Unity 项目、AGENTS.md 和实际可用工具 schema 能证明的规则。

缺失资料不能被假装已经阅读或提取。补齐后必须在 Milestone 4 之前重新审计本文件；新增内容只有在不违反更高优先级来源时才能改变实现。

## 2. 权威优先级

发生冲突时按以下顺序裁决：

1. AGENTS.md 中的项目硬性契约和当前已确认目标。
2. Docs/Reference/02_UnityImplementationPlan.html。
3. Docs/Reference/01_UnityDesignPlan.html。
4. 若未来补齐，03_LatestDesignSource.pdf 中晚期的第一关/第二关程序需求文档。
5. Docs/Reference/03_WebPlayableReference.html 的行为和时序。
6. 视觉图片和早期草案。

03 Web 原型只用于核对行为，不是 Unity 架构来源。两份 Excel 内容是主城、箭塔、英雄、刷怪等塔防教程，与 Pet Offline 无关，明确排除。

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
- UIRoot_Test Mock 预览。
- EditMode、PlayMode、架构校验、Windows x64 standalone Build 和真实启动 Smoke。

明确排除：

- 被废弃的三章节完整版和第三章设备玩法。
- 问卷、研究页、早期实验页面、塔防新手引导。
- WebView、HTML 嵌入、联网依赖和运行时生成式 AI。
- 点击地面移动、触屏控制、完整商业版内容。
- Web 原型的 `R` 公开重置键、浏览器日志导出和 DOM/Canvas 调试能力；若保留重置，只能作为 Development Build 调试命令。
- DOTS、NavMesh、第三方 Quest 框架和无实际需求的通用抽象。

非 P0、不得反向扩大范围的表现参考：

- “期待值 / 安定值”、情绪曲线、Action Line、AI Analyse 等可作为报告或 HUD 风格参考，但不是通关条件，也不进入权威关卡状态。
- 标题页 Quit 文案、成就、额外 Toast、报告动态计数可在 P0 全流程稳定后再评估；缺少它们不阻断垂直切片验收。
- 99_Test_Playground、会议电视 RenderTexture、动态世界截图均为可选工具或表现，不得替代正式世界 Scene 与固定报告内容。

## 4. 已裁决冲突

| 主题 | 冲突/旧方案 | 最终裁决 |
| --- | --- | --- |
| 关卡数量 | 早期三章或更大范围 | 只做 Day 1 + Day 2 两关垂直切片 |
| Web 架构 | HTML 单 Canvas 可运行 | 只参考交互和时序；Unity 必须 World owns gameplay |
| Web 开场 | 原型进入 Day 1 后立即处于 `taskShoes`，开场对话与玩法并行 | Unity 使用独立 Opening 状态，锁 Gameplay 输入，开场只播放一次后才进入 TaskShoes |
| Web 输入 | 鼠标点击地面移动、`R` 重置当前任务 | Release 只使用 AGENTS.md 指定键位；鼠标仅用于 UI，`R` 仅可作 Development 调试 |
| Latte | HTML/草案可能是 UI 图层 | 普通世界 GameObject，禁止 Image/RectTransform |
| 房间 | HTML Canvas 内绘制 | 世界 SpriteRenderer/Collider2D，禁止放入 Canvas |
| 摄像头检测 | 原型视觉区域或 UI 判定 | 世界 Sensor、Collider/Raycast 和遮挡；禁止 GraphicRaycaster |
| Day 1 Camera A | 可能像敌对摄像头 | 只作 Goal Camera，永不检测 Latte |
| Day 1 Camera B | 通用穿锥失败或任意扫描实现 | 固定左右循环扫描；只在携带当前任务物品时触发当前任务重置 |
| Day 1 香蕉 | 可搬香蕉皮/固定区混用 | Day 1 固定 BananaSlipZone；Day 2 才是可搬 BananaPeel |
| 搬运速度 | 原型约 83%/58% 等数值 | 轻物约 85%，重物约 60%，最终由配置和 Playtest 校准 |
| 滑行速度 | 原型更快 | 默认 160%、0.9 秒，保留配置旋钮 |
| 报告与演出 | 原型可能先演出后报告 | Day 1 固定先报告，继续后自动音响/狗窝演出 |
| 报告内容 | Web 原型按计数器生成多项动态字段 | 两日报告为固定定义；只允许少量不影响流程的观测字段，核心文案和结论不得动态生成 |
| Boss Call 失败 | 可能理解为 Game Over | 只临时提高 Camera B 速度、范围、FOV，不 Game Over |
| Day 2 任务显示 | 原型可能显示多目标 | 唯一主目标为“让拿铁晒满20秒太阳” |
| 10 秒确认 | 只提示或仍累计 | 确认期间太阳进度暂停；回投食器确认后清零 |
| 忽略确认 | 只加速或继续累计 | 扫描加速、范围/FOV 增大，进度保持暂停，不 Game Over |
| Feeder 撞击 | 关闭整个投食器 | 只关闭 Feeder Camera；FoodCameraActive=false，投食器仍工作 |
| Backup Camera | 只记录布尔值 | 真实世界设备和 Sensor；侧门激活，下一次 10 秒仍确认 |
| 错误路线恢复 | 可能造成永久软锁 | 保留 Feeder 离线，重置当前晒太阳尝试，允许从客厅重试 |
| Day 2 完成 | 只看 SunTime | 还要求没有活动确认相机重新捕获 Latte |
| Restore Connection | 静态坏结局 | 必须可见地重启确认循环 |
| Keep Quiet | 普通成功页 | 关闭远程确认并播放睡眠结局及固定最终字幕 |
| 保存 | Web 使用 `localStorage` | Unity 使用 SaveService 的版本化本地离线存档；测试可覆盖独立存档路径，不依赖浏览器数据 |
| Boss Call 时序 | Web 使用带随机区间的下一次点名 | 正式流程使用 LevelConfigSO 中可复现的固定时间表；Web 数值仅作调参起点 |
| UI 测试 Scene | 90_Testbed 等旧名 | 使用 90_UIRoot_Test；99_Test_Playground 默认不建 |
| 参考 PNG | 看似透明切片 | 52 张 RGB；唯一 RGBA 也完全不透明，只作参考 |
| Excel | 新手引导资料 | 内容属于另一塔防项目，完全排除 |

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
- Boss Pillow 初始位于 Camera A 前方，TaskShoes 完成前锁定。
- Camera B 在配置的左右端点之间固定循环扫描。
- 抱枕进入 Dog Bed GoalArea 立即完成。
- Camera B 失败只重置当前任务。
- Final Bark 无失败。
- 保存 DayOneCompleted 后加载 Day 2。

Day 2：

    Start → SunFirst → CameraCheck → Loop → DestroyCamera
    → Backup → FinalSun → Report → Choice → End

- SunZone + Lie 才累计。
- 10 秒触发确认；确认暂停进度；回投食器清零。
- BananaPeel + Robot 只关闭 Feeder Camera。
- 侧门激活 Backup Camera；错误路线仍触发下一次确认。
- 客厅路线、无活动确认相机重捕获时累计到 20 秒。
- Restore Connection 重启确认循环。
- Keep Quiet 最终字幕：

    它不是不想你。
    它只是终于不用证明它在想你。

## 7. 视觉资料结论

Docs/Reference/images 共 53 张 PNG：

- 52 张 PNG Color Type 2（RGB）。
- 1 张 PNG Color Type 6（RGBA），完整 Alpha 扫描最小值仍为 255。
- 图片尺寸约为 1024–1672 宽、559–1536 高。
- 多数透明棋盘格已烘焙为像素，不能直接作为透明角色/道具切片。

因此图片只用于：

- 斜 45 度房间布局、家具分区和色彩气氛。
- HUD、报告、视频窗、提示的视觉层级。
- 人物头像和演出方向参考。

视觉资料内部还混有多种成熟度：游戏画面/标题/日报 Mockup、第一关和第二关流程板、房间布局图、角色表情头像与早期总览。无论图面看起来多完整，全部都处于低于 01/02 HTML 的参考层级；图中的第三章、额外指标、旧数值、旧任务顺序或 UI 操作方式一律不得升级为需求。

不得直接推导：

- 世界 Collider/Sensor 结构。
- 任务权威状态。
- 已废弃章节或图内早期数值。
- 可直接用于生产的透明 Sprite。

## 8. Web 原型允许迁移与禁止迁移

允许迁移：

- 拖鞋 2 秒、重物减速、Bark 掉落、Camera B 警戒、安全窗口、10 秒确认、返回投食器清零、机器人撞歪摄像头、Backup Camera、两种结局等行为关系。
- 固定台词、通知语气、报告层级、房间分区和 12–15 分钟节奏目标。
- Web 数值可作为第一轮配置起点，但必须由 LevelConfigSO 承载并经 Unity Playtest 调整。

禁止迁移：

- 单个 HTML Canvas 的绘制、坐标、圆/矩形碰撞、DOM 与游戏状态混合、`setTimeout` 驱动关卡、`localStorage` 存档。
- 点击地面移动、Canvas 深度排序、UI 图片视野判定、运行时截图代替世界内容。
- Web 中与高优先级文件冲突的开场并行、Day 1 演出/报告顺序、Day 2 多主任务显示和随机 Boss Call 节奏。

## 9. 验证状态

已验证：

- ProjectSettings/ProjectVersion.txt 锁定 Unity 6000.3.14f1。
- 本机存在 C:\Program Files\Unity 6000.3.14f1\Editor\Unity.exe，ProductVersion 为 6000.3.14f1。
- 三份 HTML 和 53 张 PNG 当前存在。
- 三份 HTML 的可见内容均已提取阅读；03 Web 原型的完整脚本已静态核对关键状态、时序、报告和结局。
- 两份 Excel 的非空单元格和内嵌图数量已检查，内容均属于另一塔防项目。
- 53 张 PNG 已逐张检查尺寸、颜色模式、Alpha，并通过联系表复核视觉内容。

未验证：

- 03 Web 原型的浏览器实际交互、完整时序和两个结局录屏/记录；当前浏览器后端不可用，不能伪造运行结果。
- 全部 P0 Unity 测试、两关可玩性、双结局 standalone Smoke 和最终 Windows Build。
- 工作区中已有未提交 Foundation 生成物只出现过最小 EditMode 1/1、PlayMode 1/1 与基础 Validation PASS；这些证据不等于本计划已获确认，也不等于关卡、UI 或 Definition of Done 已完成。
- 任何后续 Unity/MCP 写操作前仍必须确认目标 Project Root 为 `D:\UGit\PetOffline`。
