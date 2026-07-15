# Pet Offline Development Decisions

更新日期：2026-07-15

| ID | 决定 | 原因/约束 |
| --- | --- | --- |
| D-001 | 固定 Unity `6000.3.14f1` | ProjectVersion 与本机 Editor 一致；实现中途不升级补丁 |
| D-002 | 最终范围仅为 Day 1、Day 2 和两个结局 | 排除三章节、问卷、研究页和早期概念 |
| D-003 | Windows x64、1920×1080、16:9、键鼠、离线、固定中文对话 | 项目硬性目标，不预建本地化框架 |
| D-004 | World owns gameplay; UGUI presents state | Latte、房间、Camera、Robot、物品和 Trigger 不进入 UGUI |
| D-005 | Bootstrap 常驻，World Scene additive；任一时刻只激活一个 World Scene | 保持服务/UI 单实例和明确生命周期 |
| D-006 | Scene 固定为 `00_Bootstrap`、`10_Day1_Meeting`、`20_Day2_Sunbath`、`90_UIRoot_Test` | 统一实现、Build Settings 与测试命名；99_Test_Playground 默认不建 |
| D-007 | UI 和 Gameplay 只依赖 Core，互不引用 | 保证关闭 UIRoot 后世界玩法独立 |
| D-008 | 使用直接事件和显式状态机，不引入通用 EventBus/Quest 框架 | 两个固定关卡不需要额外抽象 |
| D-009 | 只创建 Level、Dialogue、Report、CameraScan、Carryable、AudioCue 六类必需 SO | 两个状态机已权威描述任务，不创建 TaskDefinitionSO 重复建模 |
| D-010 | Day 1 Camera A 永不检测；Camera B 只在携带当前任务物时失败 | 遵循最终任务规则并允许空手安全穿越 |
| D-011 | Day 1 香蕉是固定 BananaSlipZone；Day 2 香蕉皮才可搬 | 解决 Web 原型混用 |
| D-012 | 轻物倍率默认 0.85，重物 0.60；滑行默认 1.60、0.9 秒 | 作为 Level/Carryable 配置初值，最终由 Playtest 调整 |
| D-013 | 拖鞋目标保持 2 秒；抱枕进入狗窝立即完成 | 最终 Day 1 要求 |
| D-014 | Boss Call 默认 14/26 秒确定性时间表、响应窗 3.6 秒；成功安全窗 3 秒，错过警戒约 7 秒 | 可测试且符合 3 秒/5–8 秒范围；不使用 Web 随机节奏 |
| D-015 | Day 2 唯一可见主目标是“让拿铁晒满20秒太阳” | 内部教学状态不展示为并列主任务 |
| D-016 | 第 10 秒确认暂停进度；回投食器确认后清零；忽略时增强扫描且保持暂停 | 固定确认循环压力，无 Game Over |
| D-017 | Robot 撞 Feeder 只关闭摄像头并设置 `FoodCameraActive=false` | 投食器本体继续工作，世界锥同步离线 |
| D-018 | Backup Camera 是真实世界设备/Sensor；错误路线局部重置晒太阳尝试，Feeder 离线状态保留 | 下一次 10 秒确认必须发生，同时避免软锁 |
| D-019 | Restore Connection 必须可见重启确认循环；Keep Quiet 关闭远程确认并播放固定字幕 | 两个选择都必须成为可运行世界结果 |
| D-020 | Save v1 使用版本化 PlayerPrefs 键 | 少量解锁、选择和设置不需要额外存储层 |
| D-021 | 53 张 PNG 只作参考，不整体导入 Assets | 52 张 RGB，唯一 RGBA 也完全不透明；多数棋盘格已烘焙 |
| D-022 | 与 Pet Offline 无关的塔防 Newbie Guide 资料排除 | 不让无关项目扩大范围 |
| D-023 | 缺少 PDF/.mg 时使用可替换灰盒或新生成资源；资料补齐后在 Milestone 4 前重审 | 不阻塞玩法，也不伪造来源/授权结论 |
| D-024 | Unity/MCP 写入前必须确认 Project Root 为 `D:\UGit\PetOffline` | 防止连接到其他已打开 Unity 工程 |
| D-025 | Release 最终输出 `Builds/Windows/PetOffline.exe` 并实际启动 | Editor 内运行不满足 Definition of Done |
| D-026 | 每个 Milestone 必须真实导入、编译、测试、更新台账并使用精确文件集提交 | 禁止只凭脚本存在或旧证据声称完成 |
| D-027 | 多 Unity 实例时使用 PetOffline 专属 instance-id relay 或 batchmode | 全局 Unity Console/MCP 可能串实例 |
| D-028 | 当前 Windows standalone 使用已安装的 Mono x64 Player | 本机未安装 Windows IL2CPP variation；IL2CPP 不是当前 DoD |
| D-029 | Boss Call 成功窗口同时冻结 Camera B 扫描角和检测 | “安全窗”必须在世界逻辑与视觉上可读 |
| D-030 | Camera B 失败只清除当前任务、Camera B 角度和临时安全/警戒，不取消独立 Boss Call 时间表 | 保证局部重置边界 |
| D-031 | 滑行期间禁止方向、Interact、Push 和 Lie；CarryController 共享入口再次拒绝主动放物 | 防止输入、UI、测试或未来调用绕过规则 |
| D-032 | SceneFlowService 在卸载/返回标题时集中停止 DialogueDirector | 避免 Bootstrap 持久对话跨 Scene 串场 |
| D-033 | PlayMode 键盘测试使用独立虚拟 Keyboard、IgnoreFocus 和显式 device filter | batchmode 无 Game View 焦点，仍需证明真实 Input Action Map |
| D-034 | 用户 `/goal` 已通过 Approval Gate，后续按 PLAN 自主推进到 Definition of Done | 不再为普通可逆实现选择重复等待批准 |
| D-035 | Day 2 与 UGUI 可在当前工作树并行建设，但里程碑完成、测试证据和提交仍分别核算 | 代码在建不等于任一里程碑完成，避免旧 PASS 被误用 |
| D-036 | 从 `Packages/manifest.json` 和 `packages-lock.json` 移除 `com.unity.ai.assistant`，并把两个 Build 的 Managed 目录无 `Unity.AI.*` 作为构建检查 | 游戏要求离线且禁止生成式 AI runtime；开发期生成资源不应把联网 AI 包带入 Player |
| D-037 | Development 构建前删除整个 `Builds/Windows/Development`；Release 构建前清除旧 Release Player 文件和数据目录，同时保留 Development 子目录 | 防止旧 DLL、特别是已移除包的 Managed 程序集残留，使两个输出都能证明来自当前工程 |
| D-038 | 最终截图统一交付为 1920×1080；真实 Player 原始帧保留在 `Artifacts/Screenshots/Native` | 隐藏 Player 会话被系统限制为 1024×768；报告、选择和结局原始帧居中裁切为 16:9 后使用 bicubic 缩放，标题和世界状态使用 1920×1080 渲染；该处理不冒充目标显示器人工视觉验收 |
| D-039 | 跨进程存档验证拆分为独立 Seed 与 Continue Player；Seed 使用 `PETOFFLINE_KEEP_SAVE=1`，下一进程使用 `PETOFFLINE_REQUIRE_EXISTING_SAVE=1` | 第二个进程禁止测试内 Seed，必须读取前一进程写入的 PlayerPrefs 并点击生产 `ContinueButton`，避免把同进程状态误记为持久化通过 |
| D-040 | Standalone 全流程测试使用正式 Bootstrap、additive World Scene、Physics2D、状态机和生产 UGUI，只缩短等待并调用公开 Gameplay/API | 保持测试可重复且快速，同时禁止直接写权威 Flow State、伪造报告或绕过结局 |
| D-041 | Release 的短时真实启动只记为 launch smoke，不记为 12–15 分钟人工完整分支 | 窗口响应和无异常只能证明 Player 可启动；正常时长体验、节奏和目标显示器行为继续列为外部验收缺口 |

后续普通调参直接追加决定并继续；只有 Unity/License 阻塞、不可恢复资产损坏或真正不可逆产品选择才请求用户。
