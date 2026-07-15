# Pet Offline Development Decisions

更新日期：2026-07-15

| ID | 决定 | 原因/约束 |
| --- | --- | --- |
| D-001 | 固定 Unity 6000.3.14f1 | ProjectVersion 和本机 Editor 一致；不在实现中途升级补丁 |
| D-002 | 最终范围仅为 Day 1、Day 2 和两个结局 | 排除三章节、问卷、研究页和早期概念 |
| D-003 | Windows x64、1920×1080、16:9、键鼠、离线、固定对话 | 项目硬性目标 |
| D-004 | World owns gameplay; UGUI presents state | Latte、房间、摄像头视野、机器人和触发区绝不做成 UGUI |
| D-005 | Bootstrap 常驻，World Scene additive；任一时刻只激活一个 World Scene | 保持服务/UI 单实例和明确生命周期 |
| D-006 | Scene 固定为 00_Bootstrap、10_Day1_Meeting、20_Day2_Sunbath、90_UIRoot_Test | 统一实现与测试命名；99_Test_Playground 默认不建 |
| D-007 | UI 和 Gameplay 只依赖 Core，互不引用 | 保证关闭 UIRoot 后世界玩法独立 |
| D-008 | 使用直接事件和显式状态机，不引入通用 EventBus/Quest 框架 | 两个固定关卡不需要额外抽象 |
| D-009 | 只创建六类必需 SO，不创建 TaskDefinitionSO | 任务顺序由两套显式状态机权威管理 |
| D-010 | Day 1 Camera A 永不检测；Camera B 只重置当前任务 | 遵循最终任务规则并保留前置进度 |
| D-011 | Day 1 香蕉是固定 BananaSlipZone；Day 2 香蕉皮才可搬 | 解决资料混用 |
| D-012 | 轻物携带倍率默认 0.85，重物 0.60；滑行默认 1.60、0.9 秒 | 作为可调初值，不替代 Playtest |
| D-013 | 拖鞋目标保持 2 秒；抱枕入狗窝立即完成 | 最终 Day 1 要求 |
| D-014 | Boss Call 初始建议 14 秒、后续 26 秒、响应窗 3.6 秒；成功安全窗 3 秒，错过警戒约 7 秒 | 可测试且可在 LevelConfigSO 调参；错过不 Game Over |
| D-015 | Day 2 唯一主目标是“让拿铁晒满20秒太阳” | 不暴露内部子任务清单 |
| D-016 | 10 秒确认暂停进度；回投食器确认后清零；忽略时增强扫描且保持暂停 | 固定确认循环压力 |
| D-017 | Robot 撞 Feeder 只关闭摄像头，FoodCameraActive=false | 投食器本体和世界表现继续工作 |
| D-018 | Backup Camera 使用真实世界设备/Sensor；错误路线局部重置当前晒太阳尝试，Feeder 离线状态保留 | 下一次 10 秒确认必须发生，同时避免软锁 |
| D-019 | Restore Connection 必须可见重启确认循环；Keep Quiet 关闭远程确认并播放固定字幕 | 两个选择都要成为可运行结局 |
| D-020 | Save v1 使用版本化 PlayerPrefs 键 | 少量布尔值和设置不需要额外存储层 |
| D-021 | 53 张扁平 PNG 只作参考，不整体导入 Assets | 52 张 RGB，唯一 RGBA 也完全不透明 |
| D-022 | 两份塔防 Newbie Guide Excel 排除 | 内容与 Pet Offline 无关 |
| D-023 | 缺少 PDF/.mg 时使用可替换灰盒；资料补齐后在 Milestone 4 前重审 | 不因缺失来源阻塞基础玩法，又不伪造美术结论 |
| D-024 | MCP 写入前必须确认 Project Root 为 D:\UGit\PetOffline | 当前连接可能指向其他 Unity 项目 |
| D-025 | Windows Release 最终输出 Builds/Windows/PetOffline.exe，并实际启动 | Editor 内运行不满足 Definition of Done |
| D-026 | 每个 Milestone 真实导入、编译、相关测试、文档更新和独立 commit | 禁止只凭脚本存在声称完成 |
| D-027 | 多 Unity 实例时只使用 `relay_win.exe --mcp --instance-id <PetOffline PID>` 或 batchmode | 全局 Unity MCP 的 Console 可能路由到另一工程；Project Root 与目标实例必须同时核对 |
| D-028 | 当前 Windows standalone 使用已安装的 Mono x64 Player | 本机 Windows Build Support 有 Mono development/release player，未安装 Windows IL2CPP；x64 交付不要求额外模块 |
| D-029 | Boss Call 成功窗口同时冻结 Camera B 扫描角与检测，而不只是忽略检测结果 | 02 与 Web 行为都明确“安全窗/冻结扫描”；视觉锥保持显示并在窗口结束后继续扫描 |
| D-030 | Camera B 发现只清除当前任务的临时安全/警戒状态，不取消正在进行的 Boss Call 或重排下一次点名 | 局部重置不得改变独立的定时点名流程 |
| D-031 | 滑行期间禁止方向、Interact、Push 和 Lie；CarryController 在共享入口再次拒绝主动放物 | 在输入层和世界交互入口各守一次，避免 UI/测试或未来调用绕过规则 |
| D-032 | 统一由 SceneFlowService 卸载入口停止 DialogueDirector | Bootstrap 对话服务跨 Scene 持久；集中停止可避免返回标题或换关后旧回调串场 |
| D-033 | PlayMode 键盘测试使用独立虚拟 Keyboard、IgnoreFocus 和显式 device filter | batchmode 无 Game View 焦点；该设置证明真实 Input Action Map，而不是直接写角色状态 |

后续普通调参直接追加决定并继续；只有不可逆产品选择、Unity/License 阻塞或不可恢复资产损坏才请求用户。
