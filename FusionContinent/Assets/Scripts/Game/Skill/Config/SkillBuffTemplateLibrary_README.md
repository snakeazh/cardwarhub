# SkillBuffTemplateLibrary 使用说明

> Buff 模板库：技能节点 TakeBuff 与 **战斗规则 EffectGroup AddBuff** 的共同数据源。  
> 最后更新：2026-07-03

---

## 1. 资源与代码

| 项 | 路径 |
|----|------|
| ScriptableObject | `Assets/SkillGraph/SkillBuffTemplateLibrary.asset` |
| 运行时 Provider | `SkillBuffTemplateProvider`（`SkillBuffTemplateLibrary.cs`） |
| 施加逻辑 | `BuffApplier.ApplyBuff(buffId, ...)` |
| 编辑器 | `SkillBuffEditorWindow.cs` |

每条模板：`BuffId` + `Effects[]`（`SkillBuffEffectData`）

---

## 2. 谁读这个库？

| 调用方 | buffId 来源 | 是否经 TbEffectsConfig |
|--------|-------------|------------------------|
| **战斗规则 AddBuff** | `EffectGroup.Effects[].BaseValue` | ❌ 直接是 BuffId |
| **战斗规则 RemoveBuff** | 同上（匹配 `buff.BuffId`） | ❌ |
| 技能 ApplyBuffHandler / AttachBuffHitModule | 节点参数 / 命中配置 | 可能经 EffectsConfig.EffectsId |
| EffectsExecutor TakeBuff | `EffectsConfig.EffectsId` | ✅ |

**策划配规则 Buff 时：只填 `SkillBuffTemplateLibrary.BuffId`，不要填 EffectsConfig 行 Id。**

---

## 3. 效果值字段语义

| 字段 | 说明 |
|------|------|
| `EffectValue` | 固定加值 |
| `PercentEffectValue` | 比例系数（≥1 为百分数，<1 为小数，如 0.05=5%） |
| `PercentValueSourceAttrName` | 比例乘数来源属性（如 `SoldierMaxHealth`） |
| `BuffValueSource` | `Caster` / `Target` |
| `TickInterval` | >0 时按间隔触发（HealBuff 等） |
| `Duration` | 秒；-1 永久 |

### Execute（BuffTypeId=15，斩杀）

完整说明见：**`Assets/Scripts/Game/Skill/Doc/ExecuteBuff_配置说明.md`**

| 字段 | 说明 |
|------|------|
| `PercentValueSourceAttrName` | 阈值比例来源（如 `MonsterMaxHealth`） |
| `PercentEffectValue` | 阈值比例（0.6=60% 最大生命；≥1 为百分数） |
| `EffectValue` | 可选：固定绝对斩杀线（一般留空，用比例） |
| 判定时机 | **本段伤害结算前**血量（`PreHitMonsterHp`）≤ 斩杀线 |
| 伤害结算 | 本段 `FinalDamage` 升格为 **99999**（`ApplyExecuteDamageOverride`） |

计算入口：`RegisterHitExecuteBuff` → `PrepareTemplateExecuteThreshold` → `ApplyExecuteDamageOverride`。

---

## 4. 已知 BuffId 示例

| BuffId | 名称 | BuffType | 典型规则 |
|--------|------|----------|----------|
| 20012 | （满血攻速等） | AttributeModifier | 111001 / 111002 |
| 20014 | 全力出击 | MultiEffectBuff | 111003 / 111004 / 112010 |
| 21004 | 黎明守护 | HealBuff | 111005 |
| 10003 | 万箭齐发斩杀 | Execute | 投射物 AttachBuff |
| 30006 | 拔刀斩斩杀 | Execute | 投射物 AttachBuff |

---

## 5. 多效果模板

同一 `BuffId` 下 `Effects.Count > 1` 时，`BuffApplier` 走 **MultiEffectBuff** 聚合模式，`buff.BuffId` 仍为模板 Id（如 20014）。

RemoveBuff 按 `buff.BuffId` 整实例移除，无需按子效果分别删。

---

## 6. 相关文档

- `Assets/Scripts/Game/Skill/Doc/ExecuteBuff_配置说明.md` — **斩杀 Buff 配置与伤害升格流程**
- `Assets/Scripts/Battle/Rules/BattleRule_BuffEffect_Context.md` — 规则 Add/Remove 完整链路
- `Assets/Scripts/Battle/Rules/EffectGroup_EffectGroupsType_Implementation.md` — EffectGroupsType 分发
