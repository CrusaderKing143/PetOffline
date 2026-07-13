# Unity 官方 MCP 工具指南

> **适用场景**：场景、GameObject、UI Prefab、脚本、Editor、Console、包管理与 Profiler | **视觉操作**：[mcp-visual.md](mcp-visual.md)

## 目录

- [工具名称与事实来源](#工具名称与事实来源)
- [52 个工具索引](#52-个工具索引)
- [工具选择顺序](#工具选择顺序)
- [脚本与资源读取](#脚本与资源读取)
- [场景与 GameObject](#场景与-gameobject)
- [UI Prefab](#ui-prefab)
- [Editor、菜单与 Console](#editor菜单与-console)
- [项目、包与 Profiler](#项目包与-profiler)
- [连接与排查](#连接与排查)

## 工具名称与事实来源

Unity 官方包为 `com.unity.ai.assistant`。当前项目基线是 Unity `6000.3.14f1`、Assistant `2.6.0-pre.1`。

同一个工具有三层名称：

| 层级 | 示例 |
|---|---|
| 官方 canonical ID | `Unity.ManageScene` |
| MCP 暴露名 | `Unity_ManageScene` |
| Codex 完整注册名 | `mcp__unity_mcp__Unity_ManageScene` |

文档使用 canonical ID；实际调用必须从当前工具清单选择完整注册名。长名称可能被截断并追加哈希，例如 `Unity_AssetGeneration_ConvertSpri_dca62520`，**不得硬编码哈希后缀**。

优先级：**当前注册 schema > 当前包源码/文档 > 本 reference 示例**。参数大小写不可自行统一：

- 核心 `Unity.Manage*` 多用 PascalCase。
- `Unity.ManageGameObject` 使用 lower snake_case。
- `Unity.AssetGeneration.*`、Capture 与 Profiler 多用 lower camelCase。

当前 relay 不实现标准 MCP `resources/list`、`resources/read`；读取项目资源应调用 `Unity.ListResources`、`Unity.ReadResource`、`Unity.FindInFile`。

## 52 个工具索引

### 原生核心工具（20）

| 分类 | canonical ID |
|---|---|
| 脚本创建/删除 | `Unity.CreateScript`、`Unity.DeleteScript` |
| 脚本读取/搜索/SHA | `Unity.ListResources`、`Unity.ReadResource`、`Unity.FindInFile`、`Unity.GetSha` |
| 脚本编辑/验证 | `Unity.ApplyTextEdits`、`Unity.ScriptApplyEdits`、`Unity.ValidateScript` |
| 兼容路由 | `Unity.ManageScript`、`Unity.ManageScript_capabilities` |
| 场景/对象 | `Unity.ManageScene`、`Unity.ManageGameObject` |
| 资产/Shader/模型 | `Unity.ManageAsset`、`Unity.ManageShader`、`Unity.ImportExternalModel` |
| Editor/菜单/Console | `Unity.ManageEditor`、`Unity.ManageMenuItem`、`Unity.ReadConsole` |
| 通用兜底 | `Unity.RunCommand` |

### Assistant 适配工具（32）

| 分类 | canonical ID |
|---|---|
| 资产生成 | `Unity.AssetGeneration.GenerateAsset`、`Unity.AssetGeneration.GetModels`、`Unity.AssetGeneration.GetCompositionPatterns`、`Unity.AssetGeneration.ManageInterrupted` |
| 非生成转换 | `Unity.AssetGeneration.ConvertToMaterial`、`Unity.AssetGeneration.ConvertToTerrainLayer`、`Unity.AssetGeneration.ConvertSpriteSheetToAnimationClip`、`Unity.AssetGeneration.CreateAnimatorControllerFromClip` |
| 动画/音频处理 | `Unity.AssetGeneration.EditAnimationClipTool`、`Unity.AudioClip.Edit` |
| 视觉查询/捕获 | `Unity.FindProjectAssets`、`Unity.Camera.Capture`、`Unity.SceneView.Capture2DScene`、`Unity.SceneView.CaptureMultiAngleSceneView` |
| 项目上下文 | `Unity.GetProjectData`、`Unity.GetUserGuidelines`、`Unity.Grep`、`Unity.GetConsoleLogs` |
| 包管理 | `Unity.PackageManager.GetData`、`Unity.PackageManager.ExecuteAction` |
| Profiler 耗时 | `Unity.Profiler.GetFrameRangeTopTimeSummary`、`Unity.Profiler.GetFrameTopTimeSamplesSummary`、`Unity.Profiler.GetFrameSelfTimeSamplesSummary`、`Unity.Profiler.GetSampleTimeSummary`、`Unity.Profiler.GetBottomUpSampleTimeSummary`、`Unity.Profiler.GetSampleTimeSummaryByMarkerPath`、`Unity.Profiler.GetRelatedSamplesTimeSummary` |
| Profiler GC | `Unity.Profiler.GetOverallGcAllocationsSummary`、`Unity.Profiler.GetFrameGcAllocationsSummary`、`Unity.Profiler.GetFrameRangeGcAllocationsSummary`、`Unity.Profiler.GetSampleGcAllocationSummary`、`Unity.Profiler.GetSampleGcAllocationSummaryByMarkerPath` |

视觉工具的参数、限制与完整示例统一见 [mcp-visual.md](mcp-visual.md)，本文件不重复维护。

## 工具选择顺序

1. `.codex/`、`repowiki/` 等 `Assets/` 外文件使用工作区文件工具，不走 Unity MCP。
2. `Assets/` 内只读查询优先 `ListResources`、`ReadResource`、`FindInFile`、`FindProjectAssets`。
3. 场景、GameObject、资产和脚本优先对应专用工具。
4. 依赖前一步结果的修改必须顺序调用；不存在官方 `batch_execute`。
5. 只有专用工具无法表达时才使用 `Unity.RunCommand`。
6. `RunCommand` 会编译临时代码；非 GOAL 模式禁止调用，也不得用它绕过项目编译限制。

## 脚本与资源读取

### 资源定位

| 工具 | 用途 | 关键参数 |
|---|---|---|
| `Unity.ListResources` | 列举 `Assets/` 下文件 | `Pattern`、`Under`、`Limit` |
| `Unity.ReadResource` | 按 URI/行范围读取 | `Uri`、`HeadBytes`、`TailLines`、`StartLine`、`LineCount` |
| `Unity.FindInFile` | 正则查找并返回 1-based 范围 | `Uri`、`Pattern`、`IgnoreCase`、`MaxResults` |
| `Unity.Grep` | 在 `Assets/` 内使用 rg | `args`、`path`；非 C# 文件显式传 `--glob`/`--type` |

`Unity.ReadResource` 当前 schema 中 `HeadBytes`、`TailLines` 为必填，未使用时显式传 `0`。

### 新建与删除

```json
{
  "tool": "Unity.CreateScript",
  "arguments": {
    "Path": "Assets/GameScripts/HotFix/GameLogic/UI/Battle/BattleMainUI.cs",
    "Namespace": "GameLogic",
    "ScriptType": "MonoBehaviour",
    "Contents": "using TEngine;\nnamespace GameLogic\n{\n    [Window(UILayer.UI, \"BattleMainUI\")]\n    public class BattleMainUI : UIWindow { }\n}\n"
  }
}
```

- `Path` 必须包含 `Assets/`、文件名和 `.cs`。
- 删除使用 `Unity.DeleteScript` 的 `Uri`，不要用 `ManageAsset` 删除脚本。
- `Unity.ManageScript` 是 legacy compatibility router，不作为新代码首选。

### 结构化编辑

先读取正文/SHA，再用 `ScriptApplyEdits` 预览：

```json
{
  "tool": "Unity.ScriptApplyEdits",
  "arguments": {
    "Name": "BattleMainUI",
    "Path": "Assets/GameScripts/HotFix/GameLogic/UI/Battle",
    "PreconditionSha256": "<ReadResource 返回的 SHA256>",
    "Preview": true,
    "Edits": [
      {
        "op": "insert_method",
        "className": "BattleMainUI",
        "position": "end",
        "replacement": "protected override void OnRefresh()\n{\n}\n"
      }
    ],
    "Options": {}
  }
}
```

支持的主要 `op`：`replace_class`、`delete_class`、`replace_method`、`delete_method`、`insert_method`、`anchor_insert`、`anchor_delete`、`anchor_replace`。可先调用无参数的 `Unity.ManageScript_capabilities` 获取当前操作与 payload 限制。

确认预览后以相同 SHA、`Preview=false` 应用。发生 stale SHA 时重新读取，不覆盖并发修改。

### 精确文本编辑

结构化工具不适用时使用 `Unity.ApplyTextEdits`：

```json
{
  "tool": "Unity.ApplyTextEdits",
  "arguments": {
    "Uri": "unity://path/Assets/GameScripts/HotFix/GameLogic/UI/Battle/BattleMainUI.cs",
    "PreconditionSha256": "<ReadResource 返回的 SHA256>",
    "Strict": true,
    "Edits": [
      {
        "startLine": 12,
        "startCol": 1,
        "endLine": 12,
        "endCol": 1,
        "newText": "    // inserted text\n"
      }
    ]
  }
}
```

行列从 1 开始；多编辑区域不可重叠。编辑后回读目标范围。`Unity.ValidateScript` 接受 `Uri`、`Level=basic|standard`、`IncludeDiagnostics`，但非 GOAL 模式不主动进行 C# 验证。

### TEngine 路径

| 类型 | 路径 |
|---|---|
| UIWindow/UIWidget | `Assets/GameScripts/HotFix/GameLogic/UI/<模块>/` |
| UI 生成绑定 | `Assets/GameScripts/HotFix/GameLogic/UI/Gen/` |
| 模块 | `Assets/GameScripts/HotFix/GameLogic/Module/<模块>/` |
| 事件 | `Assets/GameScripts/HotFix/GameLogic/Event/` |
| Luban 生成代码 | `Assets/GameScripts/HotFix/GameProto/`，禁止手改 |

## 场景与 GameObject

### Unity.ManageScene

参数使用 PascalCase：

| Action | 参数/说明 |
|---|---|
| `GetActive` | 获取当前场景 |
| `GetHierarchy` | `Depth=-1` 全量、`0` 仅根、`1+` 限深 |
| `GetBuildSettings` | 获取 Build Settings 场景 |
| `Create` | `Name`、`Path` |
| `Load` | `Name`/`Path`/`BuildIndex` |
| `Save` | 保存当前场景 |

切换或覆盖场景前先确认未保存改动，不要把工具返回成功等同于场景内容正确。

### Unity.ManageGameObject

这是大小写例外，全部使用 lower snake_case：

| action | 用途 |
|---|---|
| `create`、`modify`、`delete`、`find` | GameObject CRUD |
| `get_component`、`get_components` | 读取组件 |
| `add_component`、`remove_component` | 组件增删 |
| `set_component_property` | 设置组件属性 |

定位方式仅有 `by_name`、`by_id`、`by_path`。修改/删除优先 InstanceID 或唯一层级路径；删除实现可能匹配所有同名对象，不使用模糊名称删除。

资产引用使用 `Assets/...` 路径字符串；场景对象/组件引用使用 `{ "find": "Player", "method": "by_name", "component": "HealthComponent" }`。

```json
{
  "tool": "Unity.ManageGameObject",
  "arguments": {
    "action": "set_component_property",
    "target": "GameRoot/Player",
    "search_method": "by_path",
    "component_name": "Animator",
    "component_properties": {
      "Animator": {
        "runtimeAnimatorController": "Assets/AssetRaw/Animations/Hero.controller"
      }
    }
  }
}
```

读取大型对象或 UI 节点时优先 `get_component`；避免对 Canvas、CanvasScaler、GraphicRaycaster、RectTransform 或大型 UI 层级无差别调用 `get_components`。

### TEngine 场景约定

- 场景中保留 `UIRoot`，由 `UIModule.OnInit()` 查找。
- 场景放在 `Assets/Scenes/` 或 `Assets/AssetRaw/Scenes/`。
- 推荐层级：`GameRoot/Logic`、`GameRoot/UI`、`GameRoot/Effect`。
- 业务 UI 通过 `GameModule.UI.ShowUIAsync` 动态加载，不直接固化在业务场景。

## UI Prefab

沿用项目现有节点命名。官方 MCP 没有独立 `manage_ui` 或 `manage_prefabs`。

简单节点可用 `Unity.ManageGameObject` 创建并添加 `Canvas`、`CanvasScaler`、`GraphicRaycaster`、`Image`、`Button`、TMP、LayoutGroup 等组件；依赖关系按顺序调用，不虚构批处理工具。

Prefab 根节点要求：

```text
XxxUI.prefab
├── Canvas
├── CanvasScaler（1920×1080，Match=0.5）
├── GraphicRaycaster
└── m_btn_/m_tmp_/m_rect_/m_vlay_/...
```

创建新 GO 时可传 `save_as_prefab=true` 与 `prefab_path`。现有 Prefab 资产仅适合修改根节点已有组件属性；复杂子层级编辑、Add/Remove Component、Apply Overrides 需要 `Unity.RunCommand`，并遵守 GOAL 限制。

存放路径统一为 `Assets/AssetRaw/UI/Prefabs/<Name>.prefab`。

## Editor、菜单与 Console

### Unity.ManageEditor

`Action`：`Play`、`Pause`、`Stop`、`GetState`、`GetProjectRoot`、`GetWindows`、`GetActiveTool`、`GetSelection`、`GetPrefabStage`、`SetActiveTool`、`AddTag`、`RemoveTag`、`GetTags`、`AddLayer`、`RemoveLayer`、`GetLayers`。

修改前可用 `GetState` 检查 `IsCompiling/IsUpdating`。非 GOAL 模式不进入 Play Mode 做功能验证。

### Unity.ManageMenuItem

参数为 `Action`、必填 `Refresh`、`MenuPath`、`Search`。未知菜单先 `List`，再 `Exists`，最后 `Execute`；`Action=Refresh` 刷新的是菜单缓存，不等同于 `Assets/Refresh`。

常用菜单路径在执行前仍需动态确认：

- `Assets/Refresh`
- `File/Save Project`
- `Tools/UIScriptGenerator/Generate Selected`
- `Tools/Luban/Generate`
- `HybridCLR/Generate/All`

### Console

优先使用功能完整的 `Unity.ReadConsole`：

```json
{
  "tool": "Unity.ReadConsole",
  "arguments": {
    "Action": "Get",
    "Types": ["Error"],
    "Count": 30,
    "Format": "Detailed",
    "IncludeStacktrace": true
  }
}
```

可用 `FilterText`、`SinceTimestamp` 限定新增消息。`Unity.GetConsoleLogs` 是简化入口。除非用户明确要求，不清空 Console。

当前官方 MCP 没有独立 `RunTests/get_test_job/CompileProject`。不要声称存在；需要测试时必须遵守项目 GOAL 工作流。

## 项目、包与 Profiler

### 项目上下文

- `Unity.GetProjectData`：参数 `maxAssetItems`、`maxOutputChars`、`maxTaxonomyDepth`。
- `Unity.GetUserGuidelines`：读取 Unity 项目指南；本项目仍以根目录 `AGENTS.md` 和已选 Skill 为最高项目约束。
- `Unity.FindProjectAssets`：名称与视觉语义搜索，展示结果时遵循返回的 `ResponseGuidance`。

### Package Manager

- `Unity.PackageManager.GetData`：只读包信息，参数 `installedOnly`、`packageID`。
- `Unity.PackageManager.ExecuteAction`：`Add|Remove|Embed|Unembed|Sample`；只有用户明确要求包变更时调用。

### Profiler

12 个工具覆盖：跨帧耗时、单帧 Total/Self Time、Bottom-up、相关线程、总体/单帧/跨帧 GC、按 sampleId 或 markerIdPath 下钻。

| 分析 | canonical ID |
|---|---|
| 跨帧耗时 | `Unity.Profiler.GetFrameRangeTopTimeSummary` |
| 单帧 Total/Self | `Unity.Profiler.GetFrameTopTimeSamplesSummary`、`Unity.Profiler.GetFrameSelfTimeSamplesSummary` |
| Sample/Bottom-up | `Unity.Profiler.GetSampleTimeSummary`、`Unity.Profiler.GetBottomUpSampleTimeSummary`、`Unity.Profiler.GetSampleTimeSummaryByMarkerPath`、`Unity.Profiler.GetRelatedSamplesTimeSummary` |
| GC 总体/帧/区间 | `Unity.Profiler.GetOverallGcAllocationsSummary`、`Unity.Profiler.GetFrameGcAllocationsSummary`、`Unity.Profiler.GetFrameRangeGcAllocationsSummary` |
| GC Sample | `Unity.Profiler.GetSampleGcAllocationSummary`、`Unity.Profiler.GetSampleGcAllocationSummaryByMarkerPath` |

Profiler 工具只分析已经存在或已加载的数据，不负责开始录制。流程：

1. 确认 Profiler 已有数据。
2. 先用 frame/range 汇总定位问题帧。
3. 从返回值取得 `sampleId`、`bottomUpId`、`threadName` 或 `markerIdPath`。
4. 再调用对应下钻工具。

长工具名带运行时哈希，按描述和当前 schema 选择，禁止复制固定哈希到长期文档。

## 连接与排查

Windows relay：`%USERPROFILE%\.unity\relay\relay_win.exe --mcp`。多 Unity 实例时追加 `--project-path <绝对项目路径>` 或设置 `UNITY_PROJECT_PATH`。

排查顺序：

1. Unity **Project Settings > AI > Unity MCP**（新版本可能显示 Unity MCP Server）确认 Bridge Running。
2. 首次直连时批准 Pending Connection。
3. 检查 Tools 中目标工具已启用；工具清单是动态的。
4. 检查 Unity 是否正在编译、是否存在项目编译错误。
5. 重连 MCP 客户端，再读取实际 schema。
6. 工具缺失时不要退回第三方 lowercase 名称。

## 常见错误

| 错误 | 正确处理 |
|---|---|
| 使用 `manage_scene/manage_ui/manage_prefabs` | 使用当前注册的 `Unity.ManageScene/ManageGameObject` 或必要时 `RunCommand` |
| 使用 `batch_execute` | 独立只读/生成任务可并行，依赖修改顺序调用 |
| 自行统一参数大小写 | 严格复制当前 schema |
| 硬编码 InstanceID、SHA、modelId 或哈希工具名 | 从前序查询结果动态取得 |
| 用标准 MCP resources API 读取 Unity 文件 | 使用 `Unity.ListResources/ReadResource/FindInFile` |
| 成功返回后不回读 | 按资产、组件、层级、截图、Console 做针对性检查 |
