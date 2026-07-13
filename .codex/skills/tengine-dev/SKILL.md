---
name: tengine-dev
description: TEngine 项目中的 Unity 官方 MCP 操作指导。用于通过 Unity MCP 处理场景、GameObject、UI Prefab、C# 脚本、Editor、Console、包管理、Profiler、材质、Shader、纹理、生成资产、动画、音频、ParticleSystem、VFX 与视觉验证。
---

# Unity 官方 MCP 开发指导

处理 Unity MCP 任务时遵循以下原则：

1. 以当前注册工具的运行时 schema 为准。
2. 优先使用 `Unity.ManageAsset`、`Unity.ManageGameObject`、`Unity.ManageShader`、`Unity.AssetGeneration.*` 等专用工具。
3. 仅在专用工具无法表达时使用 `Unity.RunCommand`；非 GOAL 模式不得调用。
4. 操作后使用查询、截图、Console 或 Profiler 验证结果。

## 文档路由

- 场景、GameObject、UI Prefab、脚本、Editor、Console、包管理、Profiler：读取 [mcp-tools.md](references/mcp-tools.md)。
- 材质、Shader、纹理、生成资产、动画、音频、VFX、视觉验证：读取 [mcp-visual.md](references/mcp-visual.md)。
