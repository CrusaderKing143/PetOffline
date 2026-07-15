# Pet Offline Asset Provenance

更新日期：2026-07-15

## 状态定义

- Confirmed：来源、处理方式和许可均有可复核记录。
- Project-generated：由本项目代码或 Unity 原生资源生成，没有外部美术文件。
- Temporary：可用于内部原型，但交付前仍需替换或补齐授权。
- Unresolved：来源或许可证据不足，不能据此声称可公开分发。

## 已进入 Assets/PetOffline 的视觉资产

| Unity 资产 | 来源 | 处理方式 | 当前状态 |
| --- | --- | --- | --- |
| Assets/PetOffline/Art/UI/TitleBackground.png | Docs/Reference/images/4b9f737dc644643325fe3f4877875989.png | 字节级复制；作为 1672×941 Sprite 导入；在 1920×1080 UGUI 中缩放/裁切并覆盖真实按钮 | Temporary / Unresolved |
| Assets/PetOffline/Art/UI/Fonts/NotoSansCJKsc-Regular.otf | 仓库内未保存原始下载地址、版本或许可文件 | 作为 Noto Sans CJK SC 字体导入并包含 Font Data | Unresolved |
| Assets/PetOffline/Art/UI/Fonts/NotoSansCJKsc-Dynamic.asset | 上述 OTF | Unity TextMeshPro 生成的 Dynamic Font Asset | Derived / Unresolved |
| Assets/PetOffline/Art/VFX/GreyboxVision.mat | 项目内生成 | 用于世界 Camera Vision 灰盒表现 | Project-generated |
| Day 1 / Day 2 Scene 中的程序化房间、设备和 Latte 视觉 | WorldVisualAutomation.cs、LatteVisual2D.cs、Unity 内置 UI/Skin/UISprite.psd | Editor 自动化创建 SpriteRenderer、颜色、层级和世界标签 | Project-generated placeholder |

TitleBackground 源文件与 Unity 资产当前 SHA-256 相同：

    8557D679B11D9112BE7F91E42D0D860CB197843C124F7971894BC72F2C29B829

NotoSansCJKsc-Regular.otf 当前 SHA-256：

    2C76254F6FC379FDDFCE0A7E84FB5385BB135D3E399294F6EEB6680D0365B74B

字体家族通常存在开源发行版本，但仓库没有足以将该许可自动归属于当前二进制的下载记录与许可文本。在补齐确切来源、版本和对应许可文件前，本项目将其标记为 Unresolved。

## 程序化与占位表现

当前 World 美术没有导入外部角色 Sprite Sheet：

- Latte 由 SpriteRenderer 组合形状构成。
- 房间、地毯、光照条、家具细节、摄像头、机器人和道具细节由 Editor 自动化生成。
- 这些表现与 Collider、Sensor、Trigger、Path 和 Flow 位于不同节点，可在不改玩法引用的前提下替换。
- Bark 优先使用项目生成的 Audio_Bark Cue；LatteVisual2D 仍保留无 Cue 时的运行时程序化波形回退。

Project-generated 只说明没有外部文件来源，不代表视觉质量、动画或最终交付验收已经完成。

## 未导入的参考资料

- Docs/Reference/images 下的其余 PNG 保留为设计、色彩和布局参考，没有批量进入 Assets/PetOffline。
- 复合 Mockup、流程图、烘焙棋盘格人物稿和旧版关卡图不能直接作为生产 Sprite。
- 当前仓库缺少原计划中的 03_LatestDesignSource.pdf 与 05_ArtSource.mg，无法提取或核对其分层原始美术。
- 03_WebPlayableReference.html 内嵌图像只作行为/视觉参考；未作为世界玩法资产导入。

## 音频资产

| 项目 | 当前状态 |
| --- | --- |
| 外部下载/第三方音频文件 | 未发现 |
| 生成方式 | AudioAutomation.cs 以 22050 Hz、单声道、16-bit PCM 确定性写出 WAV |
| AudioCueDefinitionSO | 已创建 7 个 Cue 资产 |
| World Cue | Bark、Robot、CameraAlert、FeederOffline、Ambience |
| UI Cue | UIConfirm、UIReport |
| 生成入口 | Tools/Pet Offline/Setup Generated Audio |

当前 WAV 与 Cue 均为 Project-generated placeholder，没有外部音频许可依赖，但不代表最终音效质量已经验收。若引入正式音频，必须记录文件来源、作者、下载/生成日期、许可、编辑方式、原始哈希和导入设置。

## 截图与测试产物

Artifacts/Screenshots 下的 PNG 是运行状态证据，不是生产源美术。每张最终验收图应记录：

- Editor、Development 或 Release 来源
- 对应 Build/Scenario
- UTC 时间
- Scene/State/Ending
- 是否使用测试缩时配置

当前 Title.png 与 Day1_Opening.png 没有独立 manifest；其中 Day1_Opening.png 早于最近一次 World Scene 视觉重建，不能作为最终美术验收证据。

## 交付前必须关闭

1. 确认 TitleBackground 的作者、授权范围和可分发性；无法确认则替换为明确授权或项目生成的新图。
2. 记录字体的官方下载地址、版本与许可证，并把对应许可文本随仓库/Build 分发；否则替换字体。
3. 新增任何生成式美术时，记录所用工具、模型/版本、提示、日期、人工编辑和使用权结论。
4. 统一 Latte、主人、老板与 AI 的角色设计；禁止直接使用带烘焙棋盘格的参考图。
5. 回归验证现有生成 Cue 的 Scene/UI 引用，并在需要最终品质时以可追溯音频替换占位 WAV。
6. 更新最终截图 manifest，并确认 1920×1080 裁切、中文字符覆盖和无透明背景问题。

在上述来源和许可项关闭前，不得把 Temporary/Unresolved 资产描述为已获公开发行授权。
