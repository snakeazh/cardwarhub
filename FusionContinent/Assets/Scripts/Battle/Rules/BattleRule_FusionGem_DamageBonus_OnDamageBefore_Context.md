# 融合石：远程 / 飞行 / 高血怪增伤（OnDamageBefore）

> 最后更新：2026-07-17  
> 索引：`BattleRule_FusionGem_Index.md`

---

## 1. 功能概述

对满足条件的受击怪物，在**伤害前**将 `AllDamageBonuses` 写入**当次** `DamageContext`（不依赖效果组 Persist 配表也能即时生效）。

| 宝石 Id | 名称 | RuleId | 条件 | Lv1 增伤参数 235 |
|---------|------|--------|------|------------------|
| 10000008 | 远程怪物宝石 | 107011 | 远程怪 `IsRemoteMonster(25)` | 0.1 |
| 10000009 | 飞行怪物宝石 | 107012 | 飞行怪 `IsFlyingMonster(26)` | 0.1 |
| 10000014 | 增伤宝石 | 107013 | 怪血比例 ≥70%（条件 1050） | 0.3 |

描述示例（10000014 Lv1）：对血量大于 70% 的怪物造成的伤害 +30%。

---

## 2. 配表链路（以 10000014 为例）

```
tbgemfusionconfig 10000014
  LearnSkillRule(126)=107013
  AddDamageValueTake(235)=0.3
        │
        ▼
tbskillruleconfig 107013
  Trigger=7 OnDamageBefore
  GloballyEffective=1
  ConditionGroupId=[1050]
  EffectGroupId=[207011]
        │
        ▼
tbconditiongroupconfig 1050
  SpecialType=0 GoalAttribute
  Attribute=62 MonsterMaxHealth   ← 返回 currentHp/maxHp
  ComparisonType=0 ≥
  ComparisonValue=0.7
        │
        ▼
tbeffectgroupconfig 207011
  Effects: ModifyAttr
    AttributeType=140 AllDamageBonuses
    SourceAttrType=235 AddDamageValueTake
    BaseValue=0, PreValue=1
  PersistToAttributeSet=1（远程/飞行为 0；均走即时 DamageContext）
```

### 2.1 远程 / 飞行条件

| 条件 Id | SpecialType | 实现 |
|---------|-------------|------|
| 1048 | `IsRemoteMonster(25)` | `monster._monsterConfig.AtkType == AtkType.Shooting` |
| 1049 | `IsFlyingMonster(26)` | `monster._monsterConfig.MoveType == MoveType.Flying` |

代码：`BattleRuleDamageValueProvider`（`BattleRuleDamageSupport.cs`）。

### 2.2 高血怪条件（1050）

`BattleRuleMonsterHealthConditionValues`：`Attribute=MonsterMaxHealth` → `currentHp/maxHp`，与 `0.7` 做 ≥ 比较。

---

## 3. 运行时

```
伤害管线 OnDamageBefore
  → TriggerLearnedBattleRules(OnDamageBefore)
  → StackSourceKey=FusionGem_{id}, SourceType=FusionGem
  → 条件通过
  → ModifyAttr AllDamageBonuses
  → IsImmediateDamageAttribute(AllDamageBonuses)=true
  → 写当次 DamageContext（FlatAdd = 235参数 × PreValue）
```

**关键修复（2026-07）：** 不可把 `AllDamageBonuses` 仅当持久 AttributeSet；必须走 `IsImmediateDamageAttribute` 即时分支，否则「看起来学了规则但当刀不涨伤」。

参数：`BattleRuleSourceParamSupport.ResolveParamValue` → 宝石 Level 上的 `235`。

---

## 4. 效果组对照

| 效果组 | 宝石 | Persist 配表 | 实际行为 |
|--------|------|--------------|----------|
| 207009 | 远程 | 0 | 即时 DamageContext |
| 207010 | 飞行 | 0 | 即时 DamageContext |
| 207011 | 高血 | 1 | 仍走即时（`IsImmediateDamageAttribute`） |

---

## 5. 日志与测试

```
[AttributeManager] 本局新增规则: RuleId=107013, StackKey=FusionGem_xxx, Source=FusionGem
```

- [ ] 打远程怪：伤害升高；近战怪不高
- [ ] 打飞行怪：同上
- [ ] 怪血 ≥70% 增伤，压到 70% 以下后当刀不再吃该加成
- [ ] StackKey 丢失 → 增幅为 0

---

## 6. 关联文件

- `BattleRuleEffectApplier.cs` — `IsImmediateDamageAttribute`
- `BattleRuleDamageSupport.cs` — 远程/飞行判断
- `BattleRuleMonsterHealthConditionValues.cs`
- `BattleRuleSourceParamSupport.cs`
