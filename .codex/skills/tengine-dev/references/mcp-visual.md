# Unity 官方 MCP 材质与视觉操作

> **适用场景**：材质、Shader、纹理导入、生成式资产、3D 模型、Prefab、动画、音频、VFX 与视觉验证 | **通用 MCP**：[mcp-tools.md](mcp-tools.md)

## 目录

- [调用规则](#调用规则)
- [视觉工具选择矩阵](#视觉工具选择矩阵)
- [材质与 Renderer](#材质与-renderer)
- [纹理导入](#纹理导入)
- [Shader](#shader)
- [生成式资产](#生成式资产)
- [非生成式转换](#非生成式转换)
- [动画与音频](#动画与音频)
- [3D 模型与 Prefab](#3d-模型与-prefab)
- [VFX 与 LineRenderer](#vfx-与-linerenderer)
- [视觉验证](#视觉验证)
- [常见错误与边界](#常见错误与边界)

## 调用规则

文档使用官方 canonical ID，例如 `Unity.AssetGeneration.GenerateAsset`。Codex 中实际名称带 `mcp__unity_mcp__` 前缀；长名称可能被截断并追加哈希。调用时从当前工具清单按描述匹配，禁止硬编码哈希后缀。

参数大小写：

- `Unity.ManageAsset`、`Unity.ManageShader`、`Unity.ImportExternalModel`、`Unity.RunCommand` 使用 PascalCase。
- `Unity.ManageGameObject` 使用 lower snake_case。
- `Unity.AssetGeneration.*`、`Unity.AudioClip.Edit`、Camera/SceneView Capture 使用 lower camelCase。

当前 schema 永远优先于本文示例。不存在官方 `manage_material`、`manage_texture`、`manage_animation`、`manage_vfx`、`manage_prefabs` 或 `batch_execute`。

## 视觉工具选择矩阵

| 任务 | 首选工具 | 回退/说明 |
|---|---|---|
| 按名称或画面语义找资产 | `Unity.FindProjectAssets` | 展示结果时遵循 `ResponseGuidance` |
| 资产搜索、预览、创建和修改 | `Unity.ManageAsset` | 不支持直接创建 Texture/Prefab |
| Renderer、Animator、简单组件/Prefab | `Unity.ManageGameObject` | 复杂 Prefab/VFX 用 `RunCommand` |
| `.shader` CRUD | `Unity.ManageShader` | 不用于 `.shadergraph` |
| 查询生成模型 | `Unity.AssetGeneration.GetModels` | `modelId` 不可猜测 |
| 查询材质/地形构图模板 | `Unity.AssetGeneration.GetCompositionPatterns` | 生成 Material/TerrainLayer 前调用 |
| 生成或 AI 编辑资产 | `Unity.AssetGeneration.GenerateAsset` | 依赖结果时 `waitForCompletion=true` |
| 管理中断生成 | `Unity.AssetGeneration.ManageInterrupted` | `List` 后再 `Resume/Discard` |
| 非生成式转换 | `ConvertToMaterial/ConvertToTerrainLayer/ConvertSpriteSheetToAnimationClip/CreateAnimatorControllerFromClip` | 不需要 `modelId` |
| Humanoid Clip 后处理 | `Unity.AssetGeneration.EditAnimationClipTool` | 只支持两种命令 |
| AudioClip 后处理 | `Unity.AudioClip.Edit` | 输出新资产 |
| 外部 FBX/ZIP 导入 | `Unity.ImportExternalModel` | 仅可信 URL/本地来源 |
| 相机/2D/3D 多视角检查 | Camera/SceneView Capture | 都有较高开销 |
| Animator 状态机、复杂 VFX/Prefab | `Unity.RunCommand` | 非 GOAL 模式禁止调用 |

## 材质与 Renderer

### Unity.ManageAsset 的边界

`Action=Create` 当前明确支持：Folder、Material、PhysicsMaterial、ScriptableObject。Texture 和 Prefab 不能直接创建。

Material 的专用 `Properties` 仅支持：

| 键 | 结构 | 用途 |
|---|---|---|
| `shader` | 字符串 | 完整 Shader 名称 |
| `color` | `{ "name": "_BaseColor", "value": [r,g,b,a] }` | 单个颜色属性 |
| `float` | `{ "name": "_Smoothness", "value": 0.5 }` | 单个 float 属性 |
| `texture` | `{ "name": "_BaseMap", "path": "Assets/...png" }` | 单个纹理属性 |

简单颜色数组会固定写 `_Color`，不适合本项目 URP 材质。Keyword、Vector、Int、RenderQueue、多组属性批量修改等使用 `Unity.RunCommand`。

### 创建 URP 材质

```json
{
  "tool": "Unity.ManageAsset",
  "arguments": {
    "Action": "Create",
    "Path": "Assets/AssetRaw/Materials/HeroBlue.mat",
    "AssetType": "Material",
    "GeneratePreview": false,
    "Properties": {
      "shader": "Universal Render Pipeline/Lit",
      "color": {
        "name": "_BaseColor",
        "value": [0.1, 0.35, 1.0, 1.0]
      }
    }
  }
}
```

Shader 推荐值：

| 用途 | Shader |
|---|---|
| URP 受光材质 | `Universal Render Pipeline/Lit` |
| URP 无光照 | `Universal Render Pipeline/Unlit` |
| 2D Sprite | `Sprites/Default` |
| UGUI | `UI/Default` |

常用 URP 属性：`_BaseColor`、`_BaseMap`、`_Smoothness`、`_Metallic`、`_EmissionColor`。写入 `_EmissionColor` 不会自动启用 emission keyword；需要完整自发光效果时使用 `RunCommand`。

### 赋给 Renderer

使用 `sharedMaterial` 资产引用，避免访问 `material` 时产生实例：

```json
{
  "tool": "Unity.ManageGameObject",
  "arguments": {
    "action": "set_component_property",
    "target": "GameRoot/HeroModel",
    "search_method": "by_path",
    "component_name": "MeshRenderer",
    "component_properties": {
      "MeshRenderer": {
        "sharedMaterial": "Assets/AssetRaw/Materials/HeroBlue.mat"
      }
    }
  }
}
```

修改材质资产本身使用 `ManageAsset`。不要通过 `sharedMaterial.color` 修改颜色，否则会影响所有引用该材质的对象。

## 纹理导入

使用 `Unity.ManageAsset` 的 `Action=Modify` 修改 `TextureImporter` 公共属性。属性名必须是 Unity 实际 API 名，旧参数 `maxSize`、`generateMipMaps` 不可靠。

```json
{
  "tool": "Unity.ManageAsset",
  "arguments": {
    "Action": "Modify",
    "Path": "Assets/AssetRaw/UI/Icons/item_sword.png",
    "GeneratePreview": false,
    "Properties": {
      "textureType": "Sprite",
      "spriteImportMode": "Single",
      "mipmapEnabled": false,
      "alphaIsTransparency": true,
      "sRGBTexture": true,
      "isReadable": false,
      "maxTextureSize": 512
    }
  }
}
```

常用属性：

| 场景 | 建议属性 |
|---|---|
| UI/Sprite | `textureType=Sprite`、`mipmapEnabled=false`、`alphaIsTransparency=true` |
| 法线贴图 | `textureType=NormalMap`、`sRGBTexture=false` |
| 运行时读像素 | `isReadable=true`，仅确实需要时开启 |
| 尺寸限制 | `maxTextureSize` |

平台 Override、复杂压缩格式、Sprite 多切片等使用 `RunCommand` 或项目既有导入流程。

修改后读取信息和预览：

```json
{
  "tool": "Unity.ManageAsset",
  "arguments": {
    "Action": "GetInfo",
    "Path": "Assets/AssetRaw/UI/Icons/item_sword.png",
    "GeneratePreview": true
  }
}
```

`GetInfo` 只适合确认基础元数据、InstanceID 和预览，不返回完整 TextureImporter 配置。精确核对导入参数时读取对应 `.meta`（`Unity.ReadResource`），或在 GOAL 模式用 `RunCommand` 查询 `AssetImporter`。`AssetPreview` 可能尚未就绪而返回空，这不代表导入失败。

## Shader

### Unity.ManageShader

| Action | 关键参数 |
|---|---|
| `Create` | `Name`、`Path`、`Contents`/`EncodedContents`、`ContentsEncoded` |
| `Read` | `Name`、`Path`、`ContentsEncoded` |
| `Update` | `Name`、`Path`、完整内容、`ContentsEncoded` |
| `Delete` | `Name`、`Path`、`ContentsEncoded` |

- `Name` 不含 `.shader`，只能使用字母、数字、下划线且不能以数字开头。
- `Path` 是 `Assets/` 下目录；普通文本显式传 `ContentsEncoded=false`。
- 无 `Contents` 时工具生成旧 CG 模板，不适合本 URP 项目，因此创建时始终传完整 URP Shader 源码。
- `Update` 是整文件覆盖；编辑前先 `Read`，避免覆盖并发改动。
- `.shadergraph` 不是普通文本 Shader，不使用此工具 CRUD。

```json
{
  "tool": "Unity.ManageShader",
  "arguments": {
    "Action": "Create",
    "Name": "HeroFlatColor",
    "Path": "AssetRaw/Shaders",
    "ContentsEncoded": false,
    "Contents": "<完整的 URP ShaderLab/HLSL 源码>"
  }
}
```

工具只确认文件写入和导入，不保证 Shader 编译成功。之后读取 Console 中的 Error/Warning。

## 生成式资产

### 标准流程

1. 调用 `Unity.AssetGeneration.GetModels`。
2. 返回 `Models=[]` 时停止并报告当前账号/服务没有可用模型。
3. Material/TerrainLayer 基础生成前调用 `GetCompositionPatterns`。
4. 若存在中断任务，调用 `ManageInterrupted` 的 `List`；返回非空时停止后续生成，等待用户选择 `Resume` 或 `Discard`。
5. 调用 `GenerateAsset`；后续依赖结果时传 `waitForCompletion=true`。
6. 回读资产信息并做截图/组件验证。

`modelId` 必须复制自 `GetModels` 返回值：

```json
{
  "tool": "Unity.AssetGeneration.GetModels",
  "arguments": {
    "includeAllModels": false
  }
}
```

默认使用 `includeAllModels=false` 获取推荐模型；`true` 成本较高，仅在用户明确要求查看全部模型时使用。

### GenerateAsset command

| 类别 | command |
|---|---|
| Humanoid 动画 | `GenerateHumanoidAnimation` |
| Cubemap | `GenerateCubemap`、`UpscaleCubemap` |
| 材质 | `GenerateMaterial`、`AddPbrToMaterial` |
| 3D | `GenerateMesh` |
| 音频 | `GenerateSound` |
| 2D | `GenerateSprite`、`GenerateImage`、`GenerateSpritesheet` |
| 去背景 | `RemoveSpriteBackground`、`RemoveImageBackground` |
| Prompt 编辑 | `EditSpriteWithPrompt`、`EditImageWithPrompt` |
| 地形 | `GenerateTerrainLayer`、`AddPbrToTerrainLayer` |

```json
{
  "tool": "Unity.AssetGeneration.GenerateAsset",
  "arguments": {
    "command": "GenerateSprite",
    "modelId": "<GetModels 返回的 ModelId>",
    "prompt": "a clean fantasy sword icon, centered, game UI style",
    "savePath": "Assets/AssetRaw/UI/Generated/sword.png",
    "width": 1024,
    "height": 1024,
    "waitForCompletion": true,
    "forceGeneration": false
  }
}
```

约束：

- `UpscaleCubemap`、去背景、Prompt 编辑和 `AddPbr*` 必须提供 `targetAssetPath`。
- `GenerateSpritesheet` 必须同时提供 `prompt` 与 `referenceImageInstanceId`。
- `GenerateMesh` 接受 prompt 或参考图，输出带 Mesh 和 Material 的自包含 Prefab。
- `GenerateSprite` 后官方建议再执行去背景命令。
- `waitForCompletion=false` 返回占位资产，只用于能处理未完成资产的专家流程。
- 独立生成任务可由客户端并行发起；存在数据依赖的步骤必须顺序执行。

### Composition Pattern 的 2.6 接口差异

`GetCompositionPatterns` 返回 `AssetPath/DisplayName/Keywords`，而 `GenerateAsset` 接受 `referenceImageInstanceId`。选择 pattern 后，先用 `FindProjectAssets` 或 `ManageAsset/GetInfo` 找到当前资产并取得 InstanceID，再传给生成器；不要把路径字符串塞进数字字段。

### 中断任务

```json
{
  "tool": "Unity.AssetGeneration.ManageInterrupted",
  "arguments": {
    "command": "List"
  }
}
```

`command` 为 `List|Resume|Discard`。`forceGeneration=true` 会绕过中断保护，只能在用户明确确认放弃恢复流程时使用。

## 非生成式转换

这些工具不调用生成模型，不需要 `modelId`：

| canonical ID | 输入 | 输出 |
|---|---|---|
| `Unity.AssetGeneration.ConvertToMaterial` | Texture2D/Cubemap 路径 | `.mat` |
| `Unity.AssetGeneration.ConvertToTerrainLayer` | Texture2D 路径 | `.terrainlayer` |
| `Unity.AssetGeneration.ConvertSpriteSheetToAnimationClip` | 已切片 Texture2D SpriteSheet | `.anim` |
| `Unity.AssetGeneration.CreateAnimatorControllerFromClip` | 现有 `.anim` | 单默认状态 `.controller` |

```json
{
  "tool": "Unity.AssetGeneration.ConvertToMaterial",
  "arguments": {
    "referenceImagePath": "Assets/AssetRaw/Textures/stone.png",
    "savePath": "Assets/AssetRaw/Materials/Stone.mat"
  }
}
```

`savePath` 必须位于 `Assets/`，包含目录、文件名和正确扩展名。源资产仍在生成时应等待完成。

## 动画与音频

### AnimationClip

- Humanoid 动画生成：`GenerateAsset(command=GenerateHumanoidAnimation)`。
- SpriteSheet 动画：先确保纹理已切片，再调用 `ConvertSpriteSheetToAnimationClip`。
- 简单控制器：`CreateAnimatorControllerFromClip` 只创建一个默认状态，不能代替完整状态机编辑器。
- Humanoid 后处理：`Unity.AssetGeneration.EditAnimationClipTool` 总是生成唯一的新 Clip，不覆盖原文件。

后处理命令：

| command | 用途 | 相关参数 |
|---|---|---|
| `MakeStationary` | 去除 root motion | 无额外必填参数 |
| `TrimToBestLoop` | 搜索最佳循环区间 | `loopSearchWindowStart/End`、`minimumLoopDurationRatio`、`minimumMotionCoverage`、`muscleMatchingTolerance` |

多状态、参数、Transition、BlendTree 使用 `Unity.RunCommand` 与 `UnityEditor.Animations`。响应型战斗/移动状态通常设置 `hasExitTime=false`。

将控制器赋给场景对象：

```json
{
  "tool": "Unity.ManageGameObject",
  "arguments": {
    "action": "set_component_property",
    "target": "GameRoot/Hero",
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

### AudioClip

`Unity.AudioClip.Edit` 命令：`TrimSilence`、`TrimSound`、`ChangeVolume`、`LoopSound`。工具生成新资产；创建循环前建议先去除首尾静音。

```json
{
  "tool": "Unity.AudioClip.Edit",
  "arguments": {
    "inputAudioClipPath": "Assets/AssetRaw/Audio/Ambience.wav",
    "command": "LoopSound",
    "crossfadeDurationMs": 100
  }
}
```

`TrimSound` 使用 `startTime/endTime` 秒数；`ChangeVolume` 使用 `factor`。

## 3D 模型与 Prefab

### 生成 Mesh

`GenerateAsset(command=GenerateMesh)` 输出包含 Mesh 和 Material 的 Prefab。使用返回的资产路径、bounds 和 center 决定场景放置，不假设原点和尺寸。

### 导入外部模型

```json
{
  "tool": "Unity.ImportExternalModel",
  "arguments": {
    "Name": "HeroKnight",
    "FbxUrl": "<可信的本地路径或 HTTPS FBX/ZIP URL>",
    "Height": 2.0,
    "AlbedoTextureUrl": "<可选的 PNG/JPG/JPEG 路径或 URL>"
  }
}
```

- `Name` 必须是单个单词/ID，不能有空格。
- 仅接受 FBX 或包含 FBX 的 ZIP；ZIP 只处理首个 FBX。
- 会创建 `Assets/ExternalModels/<Name>`、场景实例和 Prefab，并按 Height 缩放落地。
- 外部材质可能 fallback 到 Standard，且实现只尝试根 MeshRenderer；URP 或多 Renderer 模型导入后必须检查并修正。
- 只使用用户授权的可信 URL/本地源。

### Prefab

创建新 GameObject 时可同时保存 Prefab：

```json
{
  "tool": "Unity.ManageGameObject",
  "arguments": {
    "action": "create",
    "name": "HitEffect",
    "parent": "GameRoot/Effect",
    "components_to_add": ["ParticleSystem"],
    "save_as_prefab": true,
    "prefab_path": "Assets/AssetRaw/Effects/HitEffect.prefab"
  }
}
```

现有 Prefab 资产只适合修改根节点已有组件属性。复杂子层级、Add/Remove Component、Apply Overrides 使用 `RunCommand`。修改和删除对象优先 InstanceID 或唯一 `by_path`；按重名目标删除可能影响所有匹配对象。

## VFX 与 LineRenderer

当前没有专用 VFX MCP。简单组件增删和普通公开属性使用 `Unity.ManageGameObject`：

```json
{
  "tool": "Unity.ManageGameObject",
  "arguments": {
    "action": "add_component",
    "target": "GameRoot/Effect/HitEffect",
    "search_method": "by_path",
    "component_name": "ParticleSystem"
  }
}
```

以下操作使用 `Unity.RunCommand`：

- ParticleSystem Burst、MinMaxCurve、Gradient、Shape、Renderer Material。
- 复杂 LineRenderer 点数组和曲线。
- VFX Graph 编辑。
- 多组件、跨资产的原子化 Editor 操作。

`RunCommand` 必须遵守官方模板和 Undo 登记：

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var target = GameObject.Find("GameRoot/Effect/HitEffect");
        if (target == null)
        {
            result.LogError("HitEffect not found");
            return;
        }

        var particles = target.GetComponent<ParticleSystem>();
        if (particles == null)
        {
            particles = target.AddComponent<ParticleSystem>();
            result.RegisterObjectCreation(particles);
        }
        else
        {
            result.RegisterObjectModification(particles);
        }
        var main = particles.main;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = particles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });
        EditorUtility.SetDirty(particles);
        result.Log("Configured {0}", target);
    }
}
```

创建 GameObject 后调用 `result.RegisterObjectCreation`；修改前调用 `RegisterObjectModification`；删除使用 `result.DestroyObject`。禁止顶层语句。**非 GOAL 模式不得实际调用 `Unity.RunCommand`。**

## 视觉验证

### 资产与组件

1. `FindProjectAssets` 定位资产，并遵循返回的 `ResponseGuidance`。
2. `ManageAsset/GetInfo` 核对类型、路径、InstanceID 和预览；需要时 `GeneratePreview=true`。TextureImporter 精确值读取 `.meta`，不从 GetInfo 推断。
3. `ManageGameObject/get_component` 核对 Renderer、Animator、ParticleSystem 等目标组件。
4. 大型/UI 对象不无差别读取全部组件，尤其避免对 Canvas、CanvasScaler、GraphicRaycaster、RectTransform 层级调用 `get_components`。

### Camera Capture

```json
{
  "tool": "Unity.Camera.Capture",
  "arguments": {
    "cameraInstanceID": 12345
  }
}
```

传带 Camera 组件 GameObject 的当前 InstanceID；省略时捕获已打开的 Scene View。输出固定约 1920×1080，调用成本较高。

### 2D Capture

```json
{
  "tool": "Unity.SceneView.Capture2DScene",
  "arguments": {
    "worldX": 0,
    "worldY": 0,
    "worldWidth": 20,
    "worldHeight": 12,
    "pixelsPerUnit": 64,
    "backgroundColor": "#00000000"
  }
}
```

`worldWidth/worldHeight` 必须大于 0；`pixelsPerUnit` 范围 `1..256`，输出任一维最多 4096。无效背景色回退透明，工具会渲染所有 Layer。

### 3D MultiAngle

```json
{
  "tool": "Unity.SceneView.CaptureMultiAngleSceneView",
  "arguments": {
    "focusObjectIds": [12345, 12346]
  }
}
```

输出 Isometric/Front/Top/Right 四视角 2×2 图，仅用于 3D 场景结构；禁止用于 2D、UGUI 或检查 Editor 窗口。InstanceID 必须来自当前查询结果；调用成本较高。

### 最终检查

最后读取 `Unity.ReadConsole` 的新增 Error/Warning。Console 无错误不等于画面正确，仍需资产信息、组件值或捕获图作为证据。

## 常见错误与边界

| 错误写法 | 正确处理 |
|---|---|
| 使用 `manage_material/manage_texture/manage_animation/manage_vfx` | 使用本文件矩阵中的官方工具 |
| 使用 `batch_execute` | 独立生成可并行；依赖操作顺序调用 |
| URP 材质用简单 `color:[...]` | 使用 `color.name=_BaseColor` + `color.value` |
| Renderer 使用 `material` | Editor 资产引用使用 `sharedMaterial` |
| `ManageAsset` 创建 Texture/Prefab | Texture 由导入/生成产生；Prefab 用 GameObject 保存或 RunCommand |
| `ManageShader` 省略 Contents | URP 项目传完整 URP Shader 源码 |
| 把 `.shadergraph` 当文本 Shader | 使用 Shader Graph 工作流或专门 Editor API |
| 硬编码 `modelId`、InstanceID、哈希工具名 | 从当前工具/查询返回值动态取得 |
| Pattern 路径直接传 `referenceImageInstanceId` | 先将资产路径解析为当前 InstanceID |
| `CreateAnimatorControllerFromClip` 构造复杂状态机 | 多状态/Transition/BlendTree 用 RunCommand |
| `forceGeneration=true` 跳过中断 | 先 List，并由用户决定 Resume/Discard |
| 2D/UI 使用 MultiAngle | 2D 用 Capture2D，UGUI 用目标 Camera/Prefab 数据验证 |
| 非 GOAL 使用 RunCommand | 停止执行，输出方案或进入 GOAL 流程 |

## 交叉引用

| 主题 | 文档 |
|---|---|
| 52 工具索引、脚本、场景、UI、Profiler | [mcp-tools.md](mcp-tools.md) |
