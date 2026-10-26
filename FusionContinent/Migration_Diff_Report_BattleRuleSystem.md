# 战斗规则系统 迁移/实现差异报告

## 完成时间

2026-05-22

## 概述

新增基于 `SkillRuleConfig` 的本局战斗规则运行时，与旧 `TriggerBuff` / `skillCarrying` 并行。涵盖伤害前穿透吸血、命中后学 Buff、生命值变化、直接触发属性、新英灵补同步等。

---

## 1. 已移除 / 替代的逻辑

### 1.1 规则伤害写入 skillCarrying（已修复）

**原问题：** 规则增伤写入 `skillCarrying.attributeStructs`，跨命中无限叠加（1.5×1.5…）。

**现逻辑：** `BattleRuleDamageEffectExecutor` 仅修改 `DamageContext`；`BattleRuleEffectApplier.ApplyDamageAttributes` 单次结算。

### 1.2 学 Buff 走 TriggerBuff 全图监听（规则路径已替代）

**原逻辑：** `AttributeManager.LearnBuffEffect` → 全队 `TriggerBuff` + `ConditionsTypeEvent`。

**规则路径：** `BattleRuleLearnBuffEffectApplier` → `EffectsExecutor.ExecuteTakeBuff` → `BuffApplier`。

**说明：** `AttributeManager.LearnBuffEffect` 仍保留给非规则入口；规则 EffectGroup 不再调用。

### 1.3 Buff 实例 BuffId 用模板内 Id（已修复）

**原逻辑：** `buff.BuffId = effectConfig.BuffId`（模板内 effect 行 Id）。

**现逻辑：** 优先外部 `buffId`（如 `20012`），与 `RemoveBuff`、属性 `SourceKey` 一致。

### 1.4 穿透按 ValueType 分支错误（已修复）

**原问题：** `ValueType.Percentage` 走 `prePenre *= (1+value)`，配置 `1` 并非 100% 穿透。

**现逻辑：** `*PenetrationPer` 统一 `NormalizePenetrationRatio`，`Value=1` → `resistanceIgnorePreValue += 1`。

---

## 2. 新增能力

| 能力 | 实现 |
|------|------|
| OnDamageBefore | `BattleRuleDamageModifier` 触发 10701/10702 |
| HpChange | `SoldierCard.TakeDamage` → `BattleRuleHpChangeTrigger` |
| NoCondition 直接属性 | `BattleRuleAcquireSoldierEffectApplier` |
| 生命比例条件 | `BattleRuleSoldierHealthConditionValues` |
| 单英灵 RemoveBuff | `BattleRuleRemoveBuffApplier` |
| 新英灵补规则 | `ApplyLearnedRulesToNewSoldier` @ `AddSoldierCard` |
| 属性移除对齐 | `AttributeModifierBuff.AttributeSourceKey` = `BuffId` |

---

## 3. 架构变化

### 职责分离

| 层级 | 职责 |
|------|------|
| 配置 | `SkillRuleConfig` / `ConditionGroupConfig` / `EffectGroupConfig` |
| `BattleRuleService` | 触发流程、概率、冷却、次数 |
| `IBattleRuleEffectExecutor` | 伤害场 vs 获得/命中后场 |
| `DamageContext` | 单次伤害真值源（穿透、吸血、增伤） |
| `BuffApplier` | Buff 模板施加 |

### 数据流变化

```text
旧：LearnBuffEffect → TriggerBuff → EventBus → EffectsExecutor
新：SkillRule → Condition → EffectGroup → ExecuteTakeBuff / DamageContext
```

---

## 4. 代码文件清单

### 新增

- `Assets/Scripts/Battle/Rules/BattleRuleService.cs`
- `Assets/Scripts/Battle/Rules/BattleRuleConditionManager.cs`
- `Assets/Scripts/Battle/Rules/BattleRuleEffectManager.cs`
- `Assets/Scripts/Battle/Rules/BattleRuleEffectApplier.cs`
- `Assets/Scripts/Battle/Rules/BattleRuleDamageSupport.cs`
- `Assets/Scripts/Battle/Rules/BattleRuleAcquireSupport.cs`
- `Assets/Scripts/Battle/Rules/BattleRuleHpChangeSupport.cs`
- `Assets/Scripts/Battle/Rules/BattleRuleLearnBuffEffectApplier.cs`
- `Assets/Scripts/Battle/Rules/BattleRuleRemoveBuffApplier.cs`
- `Assets/Scripts/Battle/Rules/BattleRuleAcquireSoldierEffectApplier.cs`
- `Assets/Scripts/Battle/Rules/BattleRuleSoldierAttributeApplier.cs`
- `Assets/Scripts/Battle/Rules/BattleRuleSoldierHealthConditionValues.cs`
- `Assets/Scripts/Battle/Rules/BattleRuleEffectTargets.cs`
- `Assets/Scripts/Battle/Rules/BattleRuleTriggerContext.cs`
- `Assets/Scripts/Battle/Rules/README_BattleRuleSystem.md`

### 修改

- `Assets/Scripts/Manager/AttributeManager.cs` — `BattleLearnedSkillRuleIds`、`AddBattleSkillRule`
- `Assets/Scripts/Game/MonsterCard.cs` — 管线前规则、吸血/穿透初值、命中后触发
- `Assets/Scripts/Game/GamePlayManager.cs` — 新英灵 `ApplyLearnedRulesToNewSoldier`
- `Assets/Scripts/Game/SoldierCard.cs` — `BattleRuleHpChangeTrigger`
- `Assets/Scripts/Game/Skill/Handler/Buff/BuffApplier.cs` — 外部 BuffId
- `Assets/Scripts/Game/Buff/Logic/AttributeModifierBuff.cs` — SourceKey 统一
- `Assets/Scripts/Game/DamageCalculator/DamagePipeline.cs` — `BattleRuleDamageModifier` 首位
- `Assets/Scripts/Game/DamageCalculator/DamageModifier/AttributesModifier.cs` — 穿透比例

---

## 5. 调用点清单

| 调用点 | 规则时机 |
|--------|----------|
| `AttributeManager.AddBattleSkillRule` | 学习时 + `NoCondition` 立即执行 |
| `BattleRuleService.TriggerPendingNoConditionRulesOnInit` | 战斗服务初始化补触发 |
| `BattleRuleDamageModifier.Apply` | OnDamageBefore / OnAttackBefore / OnSkillBefore / OnCriticalBefore |
| `BattleRuleHitTrigger.*` | OnAttackAfter / OnSkillAfter / OnCriticalAfter / … |
| `BattleRuleHpChangeTrigger.Trigger` | HpChange |
| `GamePlayManager.AddSoldierCard` | 新英灵同步 NoCondition + HpChange |

---

## 6. 测试检查清单

### 功能

- [ ] 学习 10701/10702 后伤害穿透/吸血符合概率与数值
- [ ] 学习 11103 后全队攻强；新召唤英灵同样有加成
- [ ] 11101/11102/11104 满血 Buff / 受伤删 Buff
- [ ] 10201 普攻后 Buff；移除后属性恢复
- [ ] 10301 技能增伤不跨命中叠乘

### 兼容性

- [ ] 旧流派卡 `atkCarryings` 仍可用
- [ ] 旧 `TriggerBuff` 未学规则卡时行为不变

### 稳定性

- [ ] 冷却、`MaxTriggerNum` 限制
- [ ] 镜像英灵不异常挂 Buff/属性

---

## 7. 后续优化建议

1. **一次性迁移**：`SchoolManager` / `GeneralSkillManager` 的 `atkCarryings` → `LearnSkillRule`，统一触发类型映射。
2. **OnDamageAfter / HeroAfterHarmed**：在 `SkillRule` 中接满 `TriggerType`，逐步下线 `ConditionsTypeEvent`。
3. **条件 1017/1018**：策划表可改为 `HeroNum` 或显式文档化「生命比例」避免误解。
4. **单元测试**：对 `NormalizePenetrationRatio`、`BattleRuleSoldierHealthConditionValues` 做纯逻辑测试。

---

## 8. 历史问题规避说明

1. **UILevel / 双框架**：规则代码在 `Game.Battle`，未引入 QFramework `UILevel`。
2. **关闭路径**：规则不持有 Panel 生命周期；Buff 移除走 `BuffController.RemoveBuff`。
3. **动态列表**：效果组由配置驱动，非 UI 列表；新英灵同步避免「界面打开但新单位无加成」。
4. **伤害累加**：明确禁止规则写入 `skillCarrying`，仅 `DamageContext`。

---

## 相关文档

- 开发说明：[Assets/Scripts/Battle/Rules/README_BattleRuleSystem.md](Assets/Scripts/Battle/Rules/README_BattleRuleSystem.md)
