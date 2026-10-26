# 命悬一线宝石（10000007）

> 最后更新：2026-07-17  
> 索引：`BattleRule_FusionGem_Index.md`

---

## 1. 功能概述

英灵**自身血量 ≤30%** 时，输出伤害 +（宝石参数，Lv1=30%）；血量回升超过阈值后**移除**该加成。

| 项 | 值 |
|----|-----|
| 宝石 | 10000007 命悬一线宝石 |
| 规则 | **111007** |
| Trigger | **HpChange(13)** |
| 参数 | `AddDamageValueTake(235)`，Lv1=0.3 |
| GloballyEffective | 0（按触发英灵） |
| 效果 | `AllDamageBonuses` 持久写入英灵 AttributeSet |

描述：英灵血量低于 30% 时，伤害 +30%。

---

## 2. 配表链路

```
宝石 10000007
  LearnSkillRule(126)=111007
  AddDamageValueTake(235)=0.3
        │
        ▼
规则 111007
  Trigger=13 HpChange
  ConditionGroupId=[1047]
  EffectGroupId=[211007]
  GloballyEffective=0
        │
        ▼
条件 1047
  GoalAttribute，Attribute=SoldierMaxHealth(0)
  → 左值 CurrentHp/MaxHp
  ComparisonType=3 ≤，ComparisonValue=0.3
        │
        ▼
效果 211007
  PersistToAttributeSet=1
  ModifyAttr AllDamageBonuses(140)
  SourceAttrType=235, PreValue=1
```

---

## 3. 运行时

```
SoldierCard 扣血/回血
  → BattleRuleHpChangeTrigger
  → TriggerLearnedBattleRules(HpChange)
  → 条件满足：ApplyModifyAttr → BaseAttributes 写 AllDamageBonuses
  → 条件不满足：BattleRuleHpChangeModifyAttrSupport.RemoveModifyAttrEffects
       （同 SourceKey 移除）
```

**生效时机：** 已残血后的**下一次输出伤害**（扣血当帧是受击，不改当次打出的伤害）。

**StackSourceKey：** 必须为 `FusionGem_{实例Id}`，否则参数 235 解析为 0。

---

## 4. 与 OnDamageBefore 增伤宝石的区别

| | 命悬一线 | 远程/飞行/高血怪 |
|--|----------|------------------|
| Trigger | HpChange | OnDamageBefore |
| 条件对象 | 己方英灵血量 | 受击怪物类型/血量 |
| 写入目标 | 英灵 AttributeSet（持久到条件失效） | 当次 DamageContext |
| 移除 | 条件不满足时主动 Remove | 每刀重判，不持久叠在人身上 |

---

## 5. 测试清单

- [ ] 学到 `111007` + `FusionGem_`
- [ ] 血量压到 ≤30%：后续输出带增伤
- [ ] 治疗回升 >30%：增伤消失
- [ ] 多件宝石各自 StackKey 可叠加

---

## 6. 关联

- `BattleRuleHpChangeModifyAttrSupport.cs`
- `BattleRuleHpChangeSupport.cs`
- `BattleRuleSoldierHealthConditionValues.cs`
