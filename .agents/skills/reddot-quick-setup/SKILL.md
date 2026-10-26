---
name: reddot-quick-setup
description: >-
  Implements FusionContinent red dot (红点) features end-to-end from a natural-language
  spec: RedNodeType, GlobalRedDotTreeSetup, RedDotRule, Bootstrap registration,
  UI binding, and MarkDirty/Publish hooks. Use when the user asks for 红点接入,
  红点快速接入, 新增红点, red dot setup, RegisterGlobal, RedNodeType, or describes
  a feature that needs a notification badge on UI.
disable-model-invocation: false
---

# 红点快速接入

用户用自然语言描述「做什么功能、在哪显示、什么时候亮」→ 搜索现有代码 → **直接改工程**，交付完整接入，而非只给示例。

详细路径、号码段、模板见 [reference.md](reference.md)。架构文档：`FusionContinent/Assets/Scripts/Game/RedDot/RedDot.md`。

## 触发后第一步：补全需求

用户未说清时，用最少问题补齐（能搜代码推断的不要问）：

| 项 | 默认推断 |
|----|----------|
| 全局 vs 局部 | 固定入口、跨面板 → 全局；依赖面板选中态/动态 path → 局部 |
| 父节点 | 主界面功能 → `NavBarBattle`；活动子项 → `MainPageActivityBtn`；轮语者 → `NavBarSpeaker`；见 reference 号码段表 |
| AggregateMode | 导航根 `Any`；按钮/叶子 `Sum` |
| 新建 vs 复用枚举 | 先 `grep RedNodeType`；主+子页同步常共用同一 Rule 注册两次 |
| Rule vs 手动 | 复杂条件 → Rule；UI 已算好 count → `LocalRedDot` / `SetGlobalCount` |

## 工作流（必须按序执行）

```
- [ ] 1. 搜索：类似 Rule、Model API、MessagePublisher 事件、UI 绑定位置
- [ ] 2. RedNodeType：新建或确认复用；号码段 + /// summary 中文注释
- [ ] 3. GlobalRedDotTreeSetup.Build：Add(...) 注册节点与父子层级
- [ ] 4. XxxRedDotRule.cs：OnBind/Unbind + Evaluate 完整逻辑
- [ ] 5. RedDotRuleBootstrap.RegisterAll 注册
- [ ] 6. UI：SetRedType / BindRedDot / BindLocalRedDot
- [ ] 7. 业务刷新：Publish(CHANGE_RED_TYPE+type) 或 MarkDirty / MarkLocalDirty
- [ ] 8. dotnet build HotFix.csproj 验证；HotFix.csproj 新 Rule 需加 Compile Include
- [ ] 9. 回复：改了哪些文件、Evaluate 条件摘要、如何联调
```

**必须读代码再写 Evaluate**：优先复用 `Global.*`、`ModuleFunction.CheckOpen`、现有 Model 方法；不要编造 API。

## Rule 写法约定

- 命名空间：`Game.RedDot.Business.Rules`
- 基类：`RedDotRuleBase`；`NodeType` 指向目标枚举
- 最常见订阅：`MessagePublisher.CHANGE_RED_TYPE + RedNodeType.Xxx` → `Context.MarkDirty()`
- 不需要道具/等级自动刷新时：`protected override bool CanWatchItemAllChange => false;`
- `Evaluate()`：模块未开返回 0；可领/可升级类返回 count 或 0/1
- 同一 NodeType 多 Rule：`Evaluate` 结果在 Service 层**相加**

## 三种模式（二选一，用户说了就听用户的）

| 模式 | 何时 | 交付 |
|------|------|------|
| **A 标准全局** | 固定入口 + Rule | 枚举 + GlobalRedDotTreeSetup + Rule + Bootstrap + UI + Publish |
| **B 全局手动** | 业务侧已有 count | GlobalRedDotTreeSetup + `SetGlobalCount` / `MarkDirty`，无 Rule 文件 |
| **C 局部** | 面板内上下文 | `LocalRedDotScopeBase` + `LocalRedDotRuleBase` + `BindLocalRedDot` |

## 输出格式（回复用户）

```markdown
## 接入摘要
- 功能 / 亮灭条件：…
- RedNodeType：…（父节点 / 聚合）

## 修改文件
- …

## 联调
1. …
2. …
```

## 禁止

- 只输出代码块不改文件（除非用户明确「只要示例」）
- 跳过 `GlobalRedDotTreeSetup`（节点未进树则导航不聚合）
- 引用已删除的 Config/asset/Editor 流程（`GlobalRedDotTreeConfig` 等已废弃）
- 用已删除/注释的枚举（如 `MainGuideFlowBtn`）
- 破坏 UTF-8：批量改 cs 时注意编码

## 参考 Rule

| 类型 | 文件 |
|------|------|
| 事件 + bool | `MainAfkRewardRedDotRule.cs` |
| 主+子页共用 | `MainAfkRewardRedDotRule`（注册 AFKPage + MainPage） |
| 活动可领数量 | `GiftPackRedDotRule.cs` |
| 材料/升级 | `TalentUpgradeRedDotRule.cs` |
| 轮语者装备锻造 | `SpeakerEquipForgeRedDotRule.cs` |
| 多 Rule 同节点 | `FirstRechargeRedDotRule` + `SoliderCanUpgrade` |
