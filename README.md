# Pet Offline / 《老板，我狗开会了》

Unity 6 Windows x64 双关垂直切片，完整流程为：

Title → Day 1 → Day 1 Report/Ending → Day 2 → Day 2 Report → Final Choice → Ending → Return/Restart。

当前功能版已经完成两关世界玩法、生产 UGUI、固定对话、音频、保存/Continue、两个结局、自动化测试和 Windows Standalone Build。它是可完整游玩的垂直切片，不代表正式商业美术已经完成。

## 当前验证状态

更新日期：2026-07-15（UTC+8）

| 范围 | 结果 | 证据 |
| --- | --- | --- |
| Project Validator | PASS | `Artifacts/TestResults/ValidationReport.txt` |
| EditMode | PASS 3/3 | `Artifacts/TestResults/EditMode_Final.xml` |
| PlayMode | PASS 33/33 | `Artifacts/TestResults/PlayMode_Final.xml` |
| Windows Player 双结局/截图冒烟 | PASS 2/2 | `Artifacts/TestResults/StandalonePlayMode_Screenshots_Final.xml` |
| 跨进程存档 Seed | PASS 1/1 | `Artifacts/TestResults/Standalone_CrossProcess_Seed.xml` |
| 下一进程 Continue Day 2 | PASS 1/1 | `Artifacts/TestResults/Standalone_CrossProcess_Continue.xml` |
| Development Build | Succeeded，0 error / 0 warning | `Artifacts/TestResults/WindowsBuild_Development.txt` |
| Release Build | Succeeded，0 error / 0 warning | `Artifacts/TestResults/WindowsBuild_Release.txt` |
| Release 启动检查 | PASS | `Artifacts/TestResults/Player_Release_Final.log` |

Release 启动检查证明 EXE 能正常启动、窗口响应且日志无运行时异常；它不替代人工正常时长的完整通关。

## 环境

- Unity：`6000.3.14f1 (d68c3f99a318)`
- 本机 Editor：`C:\Program Files\Unity 6000.3.14f1\Editor\Unity.exe`
- 目标平台：Windows x64
- Scripting Backend：Mono x64
- 分辨率：1920×1080，16:9
- 渲染：URP 2D
- 输入：Unity Input System
- 运行方式：离线，无 Web 或运行时生成式 AI 依赖

请使用与 `ProjectSettings/ProjectVersion.txt` 一致的 Editor，不要在交付期间升级 Unity 补丁。

## 直接运行 Release

运行：

`Builds/Windows/PetOffline.exe`

Development Build 位于：

`Builds/Windows/Development/PetOffline.exe`

BuildReport 记录的完整输出大小：

- Development：192,566,482 bytes
- Release：126,796,871 bytes

两个 Build 的 Managed 目录均不包含 `Unity.AI.*`。

## 操作

| 操作 | 按键 |
| --- | --- |
| 移动 | WASD 或方向键 |
| 互动、拾取、放下 | E |
| 汪叫 | Space |
| 推动 | Q |
| 躺下、晒太阳 | Left Shift |
| 暂停、返回 | Escape |
| UI 导航 | 鼠标、WASD 或方向键 |
| UI 确认 | Enter 或 Space |

## 在 Unity 中运行

1. 在 Unity Hub 中将仓库根目录作为现有项目添加。
2. 使用 Unity `6000.3.14f1` 打开项目。
3. 打开 `Assets/PetOffline/Scenes/00_Bootstrap.unity`。
4. 进入 Play Mode，从标题页选择“新游戏”或“继续游戏”。

运行 Build Settings Scene 顺序：

1. `Assets/PetOffline/Scenes/00_Bootstrap.unity`
2. `Assets/PetOffline/Scenes/10_Day1_Meeting.unity`
3. `Assets/PetOffline/Scenes/20_Day2_Sunbath.unity`

`Assets/PetOffline/Scenes/90_UIRoot_Test.unity` 用于 UI Mock/Test，不进入 Release Build。

## 架构

`00_Bootstrap` 持有常驻服务、Main Camera、输入、音频、存档和生产 UIRoot，两关世界 Scene 以 Additive 方式加载。

玩法对象位于普通世界 GameObject 中；UGUI 只呈现状态并发送高层命令。UI 与 Gameplay 都只依赖 Core，不互相引用。禁用 UIRoot 后，世界玩法仍可运行。

## 验证

Editor 菜单：

`Tools/Pet Offline/Validate Project`

PowerShell：

```powershell
$Unity = "C:\Program Files\Unity 6000.3.14f1\Editor\Unity.exe"
$Project = (Resolve-Path ".").Path

& $Unity `
  -batchmode -nographics -quit `
  -projectPath $Project `
  -executeMethod PetOffline.Editor.ProjectValidator.ValidateBatch `
  -logFile "$Project\Artifacts\TestResults\Validation_Final.log"
```

成功时 `Artifacts/TestResults/ValidationReport.txt` 必须明确包含 `Pet Offline validation: PASS`。

## 测试

也可在 Unity 的 `Window > General > Test Runner` 中分别运行全部 EditMode 和 PlayMode。

PowerShell：

```powershell
$Unity = "C:\Program Files\Unity 6000.3.14f1\Editor\Unity.exe"
$Project = (Resolve-Path ".").Path

& $Unity `
  -batchmode -nographics `
  -projectPath $Project `
  -runTests -testPlatform EditMode `
  -testResults "$Project\Artifacts\TestResults\EditMode_Final.xml" `
  -logFile "$Project\Artifacts\TestResults\EditMode_Final.log"

& $Unity `
  -batchmode -nographics `
  -projectPath $Project `
  -runTests -testPlatform PlayMode `
  -testResults "$Project\Artifacts\TestResults\PlayMode_Final.xml" `
  -logFile "$Project\Artifacts\TestResults\PlayMode_Final.log"
```

只有 XML 的 `result="Passed"`、`failed="0"` 和对应日志均正常时才能记录通过。Unity Test Runner 偶尔会在写完 XML 后保留 Editor 进程，应只处理本次命令启动的 Unity 实例。

## Windows Build

Editor 菜单：

- `Tools/Pet Offline/Build/Windows Development`
- `Tools/Pet Offline/Build/Windows Release`

PowerShell：

```powershell
& $Unity `
  -batchmode -nographics -quit `
  -projectPath $Project `
  -buildTarget StandaloneWindows64 `
  -executeMethod PetOffline.Editor.WindowsBuildAutomation.BuildDevelopmentBatch `
  -logFile "$Project\Artifacts\TestResults\BuildDevelopment_Final.log"

& $Unity `
  -batchmode -nographics -quit `
  -projectPath $Project `
  -buildTarget StandaloneWindows64 `
  -executeMethod PetOffline.Editor.WindowsBuildAutomation.BuildReleaseBatch `
  -logFile "$Project\Artifacts\TestResults\BuildRelease_Final.log"
```

构建自动化会先清理对应输出，避免旧 DLL 或文件残留。

## 重建自动生成内容

正常打开和运行项目不需要执行 Setup。只有需要重新生成 Scene、UI、视觉或音频时才使用：

- `Tools/Pet Offline/Setup Project`
- `Tools/Pet Offline/Setup Day 1 Greybox`
- `Tools/Pet Offline/Setup Day 2 Greybox`
- `Tools/Pet Offline/Setup Production UI`
- `Tools/Pet Offline/Setup World Visuals`
- `Tools/Pet Offline/Setup Generated Audio`

这些入口会改写自动生成的 Scene、配置或引用。执行前先检查 Git 状态，避免覆盖来源不明的用户修改。

## 交付物

- Release：`Builds/Windows/PetOffline.exe`
- Development：`Builds/Windows/Development/PetOffline.exe`
- 测试和构建证据：`Artifacts/TestResults`
- 九张 1920×1080 验收截图：`Artifacts/Screenshots`
- Player 原始截图：`Artifacts/Screenshots/Native`
- 开发状态：`Docs/Development/STATUS.md`
- 测试报告：`Docs/Development/TEST_REPORT.md`
- 已知缺口：`Docs/Development/KNOWN_GAPS.md`
- 资产来源：`Docs/Development/ASSET_PROVENANCE.md`

## 已知限制

- 世界、角色和音频达到功能垂直切片级，仍需正式美术替换和完整授权审查。
- 尚未完成首次玩家 12–15 分钟计时与教学理解度测试。
- 尚未形成目标机 60 FPS、1% low、GC 和 Profiler 报告。
- 长时间暂停、窗口失焦恢复和多显示器行为尚未系统验收。
- 尚未在第二台电脑、VM 或新 Windows 用户环境中断网运行 Release。
- 尚未人工以正常时长完整走完 Release 分支。
- 隐藏 Player 测试受虚拟显示限制，部分报告、选择和结局图由真实 1024×768 Player 帧居中裁切并缩放为 1920×1080，不能替代目标显示器上的人工视觉验收。

详细说明见 `Docs/Development/KNOWN_GAPS.md`。
