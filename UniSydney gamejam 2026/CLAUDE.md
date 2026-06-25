# FairyTale 童话卡牌叙事游戏 — 项目宪法（CLAUDE.md）

Claude Code 每次启动都会读这份文件。这里只写「从代码里推断不出来、但会改变你判断」的东西。

## 项目概览
- 2D 童话风、**故事驱动**的卡牌叙事游戏。Unity 6.x + C#。
- 每章三阶段循环：① 翻牌合成（获取道具）→ ② 2D 演出（角色登场 / 剧情推进）→ ③ 道具卡放置（在预设卡槽放卡，推动或改变剧情分支）。
- 三人团队项目，数据驱动（ScriptableObject），模块用 Assembly Definition（asmdef）解耦。

## 技术栈
- Unity 6.x（6000.x），URP，2D。
- C#，所有命名空间统一前缀 `FairyTale.*`。
- 编辑器操作通过 Unity MCP（可直接读 Console、改场景、跑测试）。

## 模块与归属（改任何代码前先看这里）
- `Assets/Scripts/Data/` → `FairyTale.Data`：共享数据层。**所有模块都依赖它，它不依赖任何人。本人维护。**
- `Assets/Scripts/Flip/` → `FairyTale.Flip`：翻牌 · 合成 · 手牌。**组员 A 负责，非必要不要动。**
- `Assets/Scripts/Story/` → `FairyTale.Story`：演出 · 放置点触发。**组员 B 负责，非必要不要动。**
- `Assets/Scripts/Core/` → `FairyTale.Core`：GameFlowController 总控状态机 + 集成。**本人主战场。**
- `Assets/Scripts/Save/` → `FairyTale.Save`：存档（待建）。

## 核心设计规则（分支与玩法的判断依据，别违背）
- **合成 = 固定配方表**：两张普通卡 → 一张道具卡，配方是 `RecipeSO`，匹配顺序无关。
- **放置 = 预设卡槽**：道具只能拖到场景中预先放好的卡槽，不是任意自由放置。
- **效果分三档（`EffectTier`）**：`Cosmetic` 纯演出（只播动画、不写状态）/ `ChapterEnding` 影响本章结局 / `FinalEnding` 影响最终结局（跨章累积）。
- **放牌顺序有意义**：分支判定依据是「放了哪些 + 哪个槽 + 什么顺序」。必须用 `StoryState` 里**有序的** `placements` 历史，**绝不要**换成 set / 无序集合。
- **`StoryState` 是全局唯一的剧情状态**，跨章节存活，驱动章结局与最终结局。新游戏开始时**必须先调 `ResetState()`**——ScriptableObject 数据在编辑器里运行后会残留。

## 集成接缝（当前主要工作）
- 手牌(A) → 放置(B)：玩家把选中的手牌道具交给卡槽。
- 放置(B) → StoryState(本人)：放置完成后回传 `{道具, 卡槽, 章节}`，由 Core 调用 `StoryState.RecordPlacement(...)` 写状态并判定分支。
- 规则：A 与 B 之间**只依赖共享接口 / 事件**，谁都不引用对方内部实现。任何新的跨模块交互，接口都放进 `FairyTale.Data`。
- ⚠️ A、B 的代码是 merge 进来的，**实际类型名以仓库为准**。对接前先读对应模块，确认真实的「道具卡类型」和「放置触发签名」，再写接口——不要假设。

## 约定
- 代码注释一律**中文**。
- 剧情、卡牌、配方等内容用 ScriptableObject 资产承载，**不要硬编码进 C#**。
- 公共数据类型只在 `FairyTale.Data` 定义一份，三个模块共用，不要各造一套。

## 团队 / Git 红线
- **绝不**两人同时改同一个 `.unity` 场景或同一个 prefab——拆成各自的 prefab 或 additive 多场景。
- 改动 `FairyTale.Data` 里的公共类型前，先确认不会破坏 A / B 的编译。
- commit 前用 Unity MCP 读一次 Console，确认无编译错误再提交。

## 命令 / 验证
- 这是 Unity 工程，没有传统 build/test CLI，以编辑器内验证为准。
- 编译：保存脚本后 Unity 自动编译；通过 Unity MCP「读取 Console」检查 warning / error。
- 测试：Unity Test Runner（EditMode / PlayMode），如果模块带测试就跑对应的。
