# 满血增伤宝石（10000025）

> 最后更新：2026-07-17  
> 索引：`BattleRule_FusionGem_Index.md`  
> 同类：命悬一线 `BattleRule_FusionGem_LifeOnTheLine_10000007_Context.md`

---

## 1. 功能概述

英灵**满血**时伤害提高（Lv1 +10%）。

| 项 | 值 |
|----|-----|
| 宝石 | 10000025 满血增伤宝石 |
| 规则 | **111008** |
| Trigger | **HpChange(13)** |
| 参数 | `AddDamageValueTake(235)`，Lv1=0.1 |
| GloballyEffective | 1 |
| 条件 | 1055：血量比例与 1 比较（满血） |
| 效果组 | 211008 → `AllDamageBonuses`，`SourceAttrType=235` |

---

## 2. 配表要点

```
LearnSkillRule=111008 + AddDamageValueTake=0.1
规则 Trigger=HpChange, GloballyEffective=1
条件 1055：GoalAttribute 生命比例，ComparisonValue=1（满血）
效果 211008：ModifyAttr AllDamageBonuses，PersistToAttributeSet=0（以配表为准）
```

运行时同样依赖 `FusionGem_` StackKey 解析 235。掉血后应失去/不再满足满血增伤；回满血后再次满足。

具体写 AttributeSet 还是 DamageContext，以 `PersistToAttributeSet` + `IsImmediateDamageAttribute` 分支为准；排查时对照命悬一线文档的「写/撤」模式。

---

## 3. 测试

- [ ] 满血输出有加成
- [ ] 受伤后加成消失或不再触发
- [ ] 回满血恢复加成
