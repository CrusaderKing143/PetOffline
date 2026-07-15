# Pet Offline Development Status

更新时间：2026-07-15 23:30（UTC+8）

总体状态：完整两关垂直切片已实现、可从标题进入并完成两个结局；当前工作树已通过最终架构、EditMode、PlayMode、Windows Player 冒烟、跨进程 Continue、Development/Release 构建与 Release 启动验证。剩余项集中在正式美术替换、首次玩家节奏、性能和干净机/断网外部验收，不影响当前功能版交付。

最终 Editor 回归于 23:20–23:22 重跑，晚于 `FullTitleToEndingSmokeTests.cs` 的最后修改；结果为 Validator PASS、EditMode 3/3、PlayMode 33/33。

## 最终机器证据

| 范围 | 结果 | 证据 |
| --- | --- | --- |
| Project Validator | PASS | `Artifacts/TestResults/ValidationReport.txt`、`Validation_Final.log` |
| EditMode | PASS 3/3 | `Artifacts/TestResults/EditMode_Final.xml`、`EditMode_Final.log` |
| PlayMode | PASS 33/33 | `Artifacts/TestResults/PlayMode_Final.xml`、`PlayMode_Final.log` |
| Windows Player 双结局/截图冒烟 | PASS 2/2 | `Artifacts/TestResults/StandalonePlayMode_Screenshots_Final.xml` |
| 跨进程存档 Seed | PASS 1/1 | `Artifacts/TestResults/Standalone_CrossProcess_Seed.xml` |
| 下一进程 Continue Day 2 | PASS 1/1 | `Artifacts/TestResults/Standalone_CrossProcess_Continue.xml` |
| Windows Development Build | Succeeded，0 error / 0 warning | `Artifacts/TestResults/WindowsBuild_Development.txt` |
| Windows Release Build | Succeeded，0 error / 0 warning | `Artifacts/TestResults/WindowsBuild_Release.txt` |
| Release 启动 | PASS | `Artifacts/TestResults/Player_Release_Final.log`；窗口 `Pet Offline` 响应正常，无运行时异常 |
| 运行时生成式 AI | 不包含 | Manifest/lock 已移除 `com.unity.ai.assistant`；两个 Build 的 Managed 目录均无 `Unity.AI.*` |

## 已完成范围

- `00_Bootstrap` 常驻服务、Main Camera/Cinemachine Brain、EventSystem 和生产 UIRoot。
- `10_Day1_Meeting` 完整流程：Opening → Shoes → Pillow → Final Bark → Report → 自动结尾 → Day 2。
- `20_Day2_Sunbath` 完整流程：10 秒确认、投食器摄像头离线、Backup Camera 错误路线教学、正确路线 20 秒、报告和双结局。
- Title、HUD、对话、报告、Choice、Pause/音量、Ending、Return Title、Restart 和 Continue 的生产 UGUI。
- World/UGUI 架构边界、UIRoot-disabled 世界运行、UIRoot Mock、显式状态机、Input System 和六类配置 SO。
- 搬运、重物减速、Bark 掉落、Q 推动、香蕉/机器人、摄像头扫描/警戒、晒太阳和自动结局表演。
- 固定中文对话、程序化离线音频、中文 TMP 字体和可替换的程序化世界表现。
- 九个 1920×1080 验收画面及对应真实 Player 原始帧。
- 干净 Development/Release 输出；Release 实际启动并正常关闭。

## Milestone 状态

| Milestone | 状态 | 说明 |
| --- | --- | --- |
| 0 Foundation | 完成 | 已提交于 `d08c995`，并由最终 Validator/测试重新覆盖 |
| 1 Day 1 | 完成 | 已提交于 `ecc27d5`，最终回归通过 |
| 2 Day 2 | 完成、当前工作树待精确提交 | 全状态机、世界机关、报告和两结局通过 |
| 3 UGUI | 完成、当前工作树待精确提交 | 生产面板、真实按钮命令和 Mock/断电测试通过 |
| 4 Art/Audio/VFX | 垂直切片级完成 | 标题资源、程序化世界视觉/音频和关键状态表现已接入；正式角色/家具美术仍可替换 |
| 5 QA/Build | 功能门槛完成 | 自动测试、Player 冒烟、跨进程 Continue、双 Build、启动和截图已完成；外部体验/性能验收见 KNOWN_GAPS |

## 交付位置

- Release：`Builds/Windows/PetOffline.exe`
- Development：`Builds/Windows/Development/PetOffline.exe`
- 截图：`Artifacts/Screenshots`
- 测试/构建证据：`Artifacts/TestResults`
- 控制、运行、测试和构建说明：`README.md`

## 工作区边界

- 当前仓库仍有用户或来源不同的 `AGENTS.md`、`Assets/Scenes/Main.unity`、SampleScene 删除、`Docs/Reference` 和 `SceneTemplateSettings.json` 改动。
- 未执行 reset/checkout，也未覆盖上述内容。
- 若创建提交，必须精确暂存 PetOffline 交付文件，禁止 `git add -A`。

## 尚需外部验收

详见 `Docs/Development/KNOWN_GAPS.md`：正式美术/授权、12–15 分钟首次玩家计时、性能/焦点恢复、断网干净机和 Release 人工完整分支仍需后续人工 QA。
