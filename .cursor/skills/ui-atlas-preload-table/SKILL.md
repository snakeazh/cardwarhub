---
name: ui-atlas-preload-table
description: >-
  Analyzes a FusionContinent UI panel's related SpriteAtlas dependencies and
  writes them into UIAtlasPreloadTable.sPanelAtlasKeys after user
  confirm/cancel/append. Use when the user gives a panel name for 图集预加载、
  分析界面依赖图集、写入相关依赖图集、UIAtlasPreloadTable、补面板图集表.
disable-model-invocation: false
---

# UI 图集预加载表接入

**本 Skill 的交付物：把该界面的相关依赖图集写入 `sPanelAtlasKeys`。**

用户给一个**界面名字** → 分析相关依赖图集短名 → **等用户确认 / 取消 / 追加** → 写入
[`UIAtlasPreloadTable.cs`](FusionContinent/Assets/Scripts/UIFramework/Core/UIAtlasPreloadTable.cs) 的面板字典。

路径与字段映射见 [reference.md](reference.md)。

## 触发后第一步

1. 解析界面名：接受 `ShopPanel01New` / `ShopPanel01` /「商店面板」等；优先定位到 `*New` 类型名作为 `PanelKey`（`Type.Name`）。
2. 若同名新旧并存：默认同时分析 `XxxNew` + 旧 `Xxx`（及 `.Designer.cs`），但**写入 Key 默认用用户指定的那个**；未指定时用 `*New`。
3. 未找到面板类：先 `Glob`/`Grep`，仍找不到再问用户要路径或正确类名。**不要臆造 PanelKey。**

## 工作流（必须按序）

```
- [ ] 1. 定位面板：cs / Designer / ViewModel / PrefabPath
- [ ] 2. 分析相关依赖图集 → 去重、短名化（见下方）
- [ ] 3. 读 UIAtlasPreloadTable 现有条目；标注哪些也在 Common（仅信息）
- [ ] 4. 输出「分析报告」并停止等待用户
- [ ] 5. 按用户指令：确认写入 / 取消 / 追加 / 删减
- [ ] 6. 仅在确认后把相关依赖写入 sPanelAtlasKeys
- [ ] 7. 回复：PanelKey、AtlasKeys、diff
```

**硬性规则：步骤 4 之后、用户明确确认前，禁止改 `UIAtlasPreloadTable.cs`。**

## 什么算「相关依赖图集」

必须写入候选的来源（去重后即为拟写入列表）：

| 来源 | 如何提取 AtlasKey（短名） |
|------|---------------------------|
| 面板自身 `Global.XxxAtlas` / `GetSprite` / `GetSpriteExt` | 查 `[SpriteAtlasTarget("Key")]` |
| 同功能旧 Panel / Designer / 本面板 ViewModel | 同上 |
| 壳层面板直接打开的子面板、本面板调用的 Helper（如 `ShopConsumeRenderer`） | 同上，算作该界面相关依赖 |
| `"SpriteAtlas_Foo"` | 短名 `Foo` |
| Prefab 明确可解析的 atlas 引用 | 可加入；不确定标「待确认」勿瞎加 |

排除：注释代码、无关面板、其它模块偶然同名引用。

## 与 Common 的关系（重要）

- **本 Skill 默认只写 `sPanelAtlasKeys`**，不改 `CommonAtlasKeys`（除非用户明确要求升为通用 / 写入 Common）。
- **已在 `CommonAtlasKeys` 的短名：报告中标注「通用 / 不写入面板表」，不得进入拟写入列表。**
- 用户说「X 是通用图集 / 放到通用」：确保 X 在 `CommonAtlasKeys`，并从该面板（及拟写入）中去掉 X。
- 过滤后若无剩余相关依赖：说明原因；不删已有条目，除非用户确认删除。

## 用户确认协议

| 用户意图 | 行为 |
|----------|------|
| **确认** / 写入 / OK | 将拟写入的相关依赖写入 `sPanelAtlasKeys` |
| **取消** | 不改文件 |
| **追加** `A,B,C` | 并入候选，再展示，再次确认后写入 |
| **去掉** `A,B` / **只要** … | 调整后再次确认 |
| **改 Key** 为 `Xxx` | 改 PanelKey 后再确认 |

## 写入规则

目标：`UIAtlasPreloadTable.cs` → `sPanelAtlasKeys`。

1. **已有同 PanelKey**：默认与拟写入**并集**更新；用户要求覆盖则覆盖。
2. **无条目**：新增  
   `{ "PanelKey", new[] { "Key1", "Key2" } },`
3. 只用短名（`ItemIcon`），禁止 `SpriteAtlas_ItemIcon`。
4. **默认不修改 `CommonAtlasKeys`。**
5. 保持 UTF-8；不重排无关条目。
6. 若分析后相关依赖为空：说明原因，**不删**已有条目，除非用户明确要求删除。

## 分析报告格式（步骤 4 必须用）

```markdown
## 图集依赖分析：{PanelKey}

### 定位
- 面板文件：…
- 关联：Designer / ViewModel / PrefabPath / 子面板 …

### 相关依赖图集（拟写入，已排除 Common）
| AtlasKey | 来源 | 表中已有 |
|----------|------|---------|
| FooIcon | … | 否 |

### 已在 Common（不写入面板表）
| AtlasKey | 来源 |
|----------|------|
| UICommonBtn | … |

### 相对表内现状
- 当前条目：无 / `a,b,c`
- 确认后将变为：`…`

### 请选择
- **确认** — 写入相关依赖到 UIAtlasPreloadTable
- **取消** — 不修改
- **追加** / **去掉** / **只要** …
- **放到通用** `X` — X 进 Common，不进面板表
```

## 禁止

- 未确认就改表
- 编造 AtlasKey / PanelKey
- 把「亦在 Common」当成默认不写入相关依赖的理由（除非用户明确要求）
- 默认去改 `CommonAtlasKeys`
- 用户已确认后不写文件

## 完成后回复

```markdown
## 已写入
- PanelKey：…
- AtlasKeys：…（相关依赖）
- 操作：新建 / 并集更新 / 覆盖
```
