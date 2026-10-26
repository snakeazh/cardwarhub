# 强化增伤 / 专属强化宝石（10000030 / 10000031）

> 最后更新：2026-07-17  
> 索引：`BattleRule_FusionGem_Index.md`

---

## 1. 功能概述

| 宝石 Id | 名称 | 规则 | 语义（Lv1） |
|---------|------|------|-------------|
| 10000030 | 强化增伤宝石 | **114079** | 每次购买强化，全队伤害 +5% |
| 10000031 | 专属强化宝石 | **114080** | 每次选中**专属**强化（`IsAddHero`），该英灵伤害 +40% |

参数均为 `AddDamageValueTake(235)`；效果均为 `AllDamageBonuses(140)`。

---

## 2. 配表对照

### 2.1 10000030 → 114079（全队购买强化）

```
LearnSkillRule=114079, AddDamageValueTake=0.05
规则:
  Trigger=11 NoCondition
  ComputerGroupId=[300018]  PurchaseEffect
  EffectGroupId=[214042]
  GloballyEffective=1
  NewSoldierSync=0（配表现状；叠层见 §4）
Computer 300018:
  CounterType/TriggerType=PurchaseEffect(11)
  MaxValue=1, ResetType=AfterTrigger
效果 214042:
  ModifyAttr AllDamageBonuses, SourceAttrType=235, PreValue=1
```

### 2.2 10000031 → 114080（专属强化）

```
LearnSkillRule=114080, AddDamageValueTake=0.4
规则:
  Trigger=11 NoCondition
  ComputerGroupId=[300019]  PurchaseExclusiveEffect
  EffectGroupId=[214043]
  GloballyEffective=0（作用在触发上下文英灵 / 专属英雄）
Computer 300019:
  CounterType/TriggerType=PurchaseExclusiveEffect(12)
  MaxValue=1, ResetType=AfterTrigger
效果 214043: 同 AllDamageBonuses × 235
```

---

## 3. 运行时

```
三选一选卡 RegisterBoughtEffectCard / 经济结算
  → BattleRuleBattleEconomySupport
  → OnPurchaseEffect(exclusiveHeroCardId)
       ├─ 始终：CounterTrigger PurchaseEffect → 可触发 114079
       └─ exclusiveHeroCardId>0：再累加 PurchaseExclusiveEffect → 可触发 114080
  → TryTriggerNoConditionRulesForComputer
  → ModifyAttr AllDamageBonuses（参数来自 FusionGem_ 上的 235）
```

| Computer | CounterType | 枚举 |
|----------|-------------|------|
| 300018 | 11 | `PurchaseEffectNum` / `PurchaseEffect` |
| 300019 | 12 | `PurchaseExclusiveEffectNum` / `PurchaseExclusiveEffect` |

---

## 4. 叠层说明（重要）

期望：每买一次强化叠加一层 +5% / +40%。

代码里 `UsesAdditiveModifierStack` 仅当 `NewSoldierSync == SyncType.ByTriggerCount(2)` 时为 true。  
**当前 JSON 中 114079/114080 的 `NewSoldierSync=0`**。若实机发现「多次购买不叠加、只保留一层」，应将规则改为 `NewSoldierSync=2`，并回归新召唤英灵补层。

Computer `AfterTrigger` 重置保证每次购买都能再次触发规则；叠加是否成立取决于 ModifyAttr 的 additive SourceKey 策略。

---

## 5. 测试清单

- [ ] 买普通强化：全队 +5%（114079），可多次累加（若配 ByTriggerCount）
- [ ] 买专属强化：对应英灵 +40%（114080），且仍触发全队 114079（若同时装备）
- [ ] 日志 Computer 达标 + ModifyAttr Flat/Percent
- [ ] 无宝石时购买不涨 `AllDamageBonuses`

---

## 6. 关联

- `BattleRuleComputerSupport.OnPurchaseEffect`
- `BattleRuleBattleEconomySupport.cs`
- `BattleRuleNewSoldierSyncSupport.UsesAdditiveModifierStack`
- `CounterType.PurchaseEffectNum` / `PurchaseExclusiveEffectNum`
