# Pet Offline Known Gaps

更新日期：2026-07-15

## 资料与资产

| Gap | 影响 | 当前回退 |
| --- | --- | --- |
| 缺少 03_LatestDesignSource.pdf | 无法核对晚期第一/第二关程序需求原文 | 以 AGENTS、02、01 和 Web 参考依次裁决；补齐后重审 |
| 缺少 05_ArtSource.mg | 无法提取正式分层美术 | 使用可替换灰盒，不伪造源资产 |
| 53 张 PNG 基本无透明通道 | 不能直接作为透明角色/道具 Sprite | 只作布局和视觉参考，后续重切或正式导出 |
| 03 Web 尚未在浏览器完整实玩记录 | 细节提示/时序可能仍需校准 | 已完成 HTML/JS 静态核对；当前无可用浏览器后端，恢复后补跑且不复制 Canvas 架构 |
| 正式音频、字体授权和动画源未验收 | Milestone 4 表现存在不确定性 | 使用离线、可替换、许可清晰的占位资源 |

## 工程状态

| Gap | 影响 | 完成证据要求 |
| --- | --- | --- |
| Day 1 未实现/未测试 | 无法完成第一关 | 对应 PlayMode 测试和截图 |
| Day 2 未实现/未测试 | 无法完成确认循环和结局 | 对应 PlayMode 测试和截图 |
| UIRoot_Test 仅有 Foundation Mock Host，UIRoot Disabled P0 尚未实现 | 完整架构独立性未证明 | Mock Preview 与 Day 1/Day 2 断电 PlayMode 通过 |
| Windows x64 Build 未生成/启动 | 无 standalone 交付 | PetOffline.exe、Player.log、Smoke 结果 |
| Artifacts/Screenshots 尚未生成 | 无视觉验收证据 | Milestone 4/5 保存关键状态截图 |
| README 尚未按最终项目更新 | 用户无法复现运行/测试/构建 | 控制、设置、测试、构建和已知限制齐全 |

## 工作区风险

- 当前有来源不明的 AGENTS.md 修改、SampleScene 删除和 Main.unity 未跟踪文件。
- Docs/Reference 与本轮文档目前可能仍是未跟踪内容。
- 自动化必须限定到 Assets/PetOffline 和明确配置项；禁止 reset、checkout 或覆盖用户资产。
- 当前 Packages 包含项目模板的额外包。除非它们造成实际问题，不为“清洁”而做无关删包；新增依赖也必须最小化。
- 当前同时打开其他 Unity 工程时，全局 MCP 的 Console 可能串实例；PetOffline 使用专属 instance-id relay 或 batchmode。

## 产品/实现风险

- Backup Camera 错误路线若状态拆分不清会软锁。使用 EverTriggered 与当前 Active 两种状态，并以 PlayMode 锁定恢复行为。
- 关闭 UIRoot 可能意外关闭 Input 或关卡流程。InputRouter 和状态机必须属于 Bootstrap/Core/Gameplay，断电测试为 P0。
- 视野视觉与实际检测若使用两套参数会漂移。两者读取同一 CameraScanConfigSO。
- 自动测试缩短计时可能掩盖真实节奏。测试配置只缩短时间，不绕过状态和物理；Release 仍做 12–15 分钟真实游玩。
- 灰盒美术替换可能破坏碰撞/引用。逻辑根与 Render 子节点分离，替换只动表现层。
