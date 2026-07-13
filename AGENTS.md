# Codex Project Instructions

请使用中文写提案和回答。

本项目是 Unity/TEngine 项目，TEngine 基于 HybridCLR + YooAsset + UniTask + Luban 构建。

## 强制工作流

无论任务大小，先判断任务等级，再决定需要读取哪些项目知识库资料。

| 等级 | 判断标准 | 知识查询策略 |
| --- | --- | --- |
| L1 简单 | typo 修正、注释修改、日志输出、单行变量改名；前提是不涉及框架 API 名称、UI 节点前缀、事件定义或资源路径 | 可跳过查询，直接编码 |
| L2 调用 | 调用已知 API、单一模块的局部修改 | 先读取 `.codex/skills/tengine-dev` 中对应主题 |
| L3 功能 | 新功能开发、跨文件修改、新增 UI/资源/事件逻辑 | 先读取 `.codex/skills/tengine-dev` 中所有相关主题 |
| L4 架构 | 模块设计、系统重构、多模块协作、架构决策 | 先读取 `.codex/skills/tengine-dev` 中多个相关主题，并交叉验证 |

判断原则：宁可高估等级，不可低估；不确定时上调一级。

## 项目知识库

Codex 侧迁移后的知识库位于 `.codex/skills/`。在处理相关任务时，优先读取对应 `SKILL.md`，再按其中的 reference 路由读取必要文档。

常用 skill：

- `tengine-dev`：TEngine 框架开发、UI、资源、事件、模块、热更、YooAsset、HybridCLR。
- `luban-dev`：Luban 配置表、Excel/Schema、导表、配置代码生成。
- `html-to-ugui`：HTML/UI DSL 转 UGUI。
- `wiki-synchelper`：项目实现与 `repowiki/` 文档之间的同步、比对、报告。
- `openspec-*`：OpenSpec 提案、探索、应用、归档工作流。

## 核心编码红线

1. 异步优先：IO 操作用 `UniTask`，禁止同步加载和 Coroutine。
2. 模块访问：通过 `GameModule.XXX` 访问，而不是 `ModuleSystem.GetModule<T>()`。
3. 资源必须释放：`LoadAssetAsync` 对应 `UnloadAsset`，GameObject 使用 `LoadGameObjectAsync`。
4. 热更边界：`GameScripts/Main` 不热更，`GameScripts/HotFix/` 全部热更。
5. 事件解耦：模块间使用 `GameEvent`，UI 内部使用 `AddUIEvent`。
6. 当不在GOAL模式中完成功能开发后不进行C#编译，也不进行验证，在GOAL模式中可以进行编译和验证

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



# 开发原则

不要过度封装函数。除非逻辑会被复用、能显著提升可读性，或有明确业务边界，否则保持内联实现。不要创建只调用另一个函数的 wrapper，也不要为了未来扩展创建抽象层。优先最小改动、贴合现有代码风格。



````
## Code Search

Use `semble search` to find code by describing what it does or naming a symbol/identifier, instead of grep:

```bash
semble search "authentication flow" ./my-project
semble search "save_pretrained" ./my-project
semble search "save model to disk" ./my-project --top-k 10
```

Use `semble find-related` to discover code similar to a known location (pass `file_path` and `line` from a prior search result):

```bash
semble find-related src/auth.py 42 ./my-project
```

`path` defaults to the current directory when omitted; git URLs are accepted.

If `semble` is not on `$PATH`, use `uvx --from "semble[mcp]" semble` in its place.

### Workflow

1. Start with `semble search` to find relevant chunks.
2. Inspect full files only when the returned chunk is not enough context.
3. Optionally use `semble find-related` with a promising result's `file_path` and `line` to discover related implementations.
4. Use grep only when you need exhaustive literal matches or quick confirmation of an exact string.
````



当处于 goal mode，或用户 prompt 包含 `/goal` 时，必须进入本流程。

在当前涉及到的 project 下创建新的目录：

```txt
goal-[num]/
  input.md
  plan.md
  tasks.md
```

编号递增，不得覆盖已有目录。

如过目前没有 project，你需要新建。

- `input.md`：完整保存用户原始输入，逐字保留，不得改写。
- `plan.md`：分析需求、上下文、风险、执行方案、验证方式、回滚方案。
- `tasks.md`：把 plan 拆成小任务，每个 task 必须可独立验证。每三个task需要一次大型全面检查-debug循环，确保没有bug和问题。

在完成以上文件前，不得修改代码。

每次只执行一个 task。每次你想要结束task的时候，你必须思考：“你对当前实现100% 有信心吗？”如果没有，请找出所有可能的漏洞和提高的方案，提出合适的修复方案，然后不断重复这个循环，直到你对新实现在事实上达到100% 自信为止。
然后你需要提交代码（若有）
在tasks.md中把任务标记为完成并且写上你干的事情（你在tasks.md中需要留空用来干这些事）
然后你不需要向用汇报自动开始下一轮task

每次上下文压缩后，你必须全量读取这三个文件，以防止上下文模糊。

全部task完成后，你需要进行最后的最大的review，全面从c端，代码，安全性等角度分析项目，并且进行修缮和测试，直到完美为止。然后把goal标记为完成
