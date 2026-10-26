# 职业增伤宝石（10000019~10000022）

> 最后更新：2026-07-17  
> 索引：`BattleRule_FusionGem_Index.md`

---

## 1. 功能概述

开局 NoCondition：若场上存在对应职业英灵，对其施加 `AllDamageBonuses`（参数 235，Lv1=+10%）。

| 宝石 Id | 名称 | RuleId | 条件 SpecialType |
|---------|------|--------|------------------|
| 10000019 | 射手增伤宝石 | 114074 | `IsArcher(27)` |
| 10000020 | 战士增伤宝石 | 114075 | `IsWarrior(28)` |
| 10000021 | 法师增伤宝石 | 114076 | `IsMage(29)` |
| 10000022 | 刺客增伤宝石 | 114077 | `IsAssassin(30)` |

共同：`LearnSkillRule` + `AddDamageValueTake(235)`；`GloballyEffective=0`（按英灵职业逐个判）。

---

## 2. 配表模式

```
规则 114074~77
  Trigger=11 NoCondition
  ConditionGroupId=[1051~1054]
  EffectGroupId=[214037~214040]
  GloballyEffective=0
效果: ModifyAttr AllDamageBonuses, SourceAttrType=235, PreValue=1
```

施加时机：融合石/装备延后 NoCondition → `ApplyEquipLearnedRulesToSoldier` / 逐英灵条件。

---

## 3. 测试

- [ ] 仅对应职业英灵获得增伤
- [ ] 参数随宝石等级 235 变化
- [ ] StackKey=`FusionGem_` 否则增幅为 0
