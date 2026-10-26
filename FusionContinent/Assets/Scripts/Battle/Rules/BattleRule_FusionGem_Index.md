# 融合石战斗规则 — 文档索引

> 供后续 AI / 开发接续。  
> 最后更新：2026-07-17  
> 通用机制：`BattleRule_EquipLearnSkillRule_Context.md` §3.4

---

## 1. 通用约定（所有 LearnSkillRule 融合石）

| 项 | 值 |
|----|-----|
| 开局入口 | `Equip.CollectFusionGemModuleEntries` → `ApplyEquipModuleAttributes` |
| SourceType | `AttributeSourceType.FusionGem` |
| StackSourceKey | `FusionGem_{融合石实例Id}` |
| 学习属性 | `LearnSkillRule(126)`，Value=RuleId |
| 参数属性 | 同 Level 的 Category=Add 行（235/242/243…），触发时 `TryResolveFusionGemAttributes` 现读 |
| 延后策略 | 与装备相同：开局只登记；NoCondition 延后；伤害/回合/购买时再触发 |

**非 LearnSkillRule：** 如宝藏猎人仅写 Module `KillAnBossGoldCoin(130)`，不进规则列表。

---

## 2. 已接入宝石一览

| 宝石 Id | 名称 | RuleId | Trigger | 详档 |
|---------|------|--------|--------|------|
| 10000008 | 远程怪物宝石 | 107011 | OnDamageBefore(7) | [条件增伤 OnDamageBefore](BattleRule_FusionGem_DamageBonus_OnDamageBefore_Context.md) |
| 10000009 | 飞行怪物宝石 | 107012 | OnDamageBefore(7) | 同上 |
| 10000014 | 增伤宝石（高血怪） | 107013 | OnDamageBefore(7) | 同上 |
| 10000007 | 命悬一线宝石 | 111007 | HpChange(13) | [命悬一线](BattleRule_FusionGem_LifeOnTheLine_10000007_Context.md) |
| 10000025 | 满血增伤宝石 | 111008 | HpChange(13) | [满血增伤](BattleRule_FusionGem_FullHpDamage_10000025_Context.md) |
| 10000023 | 根深蒂固宝石 | 113007 | RoundStart(14) | [根深蒂固](BattleRule_FusionGem_DeepRooted_10000023_Context.md) |
| 10000026 | 生命复苏宝石 | 114078 | NoCondition+Purchase | [生命复苏](BattleRule_FusionGem_LifeRecovery_10000026_Context.md) |
| 10000027 | 宝藏猎人宝石 | —（Module 130） | 击杀精英/首领 | [宝藏猎人](BattleRule_FusionGem_TreasureHunter_10000027_Context.md) |
| 10000030 | 强化增伤宝石 | 114079 | NoCondition+Purchase | [购买强化增伤](BattleRule_FusionGem_PurchaseDamage_10000030_31_Context.md) |
| 10000031 | 专属强化宝石 | 114080 | NoCondition+专属购买 | 同上 |
| 10000019~22 | 职业增伤宝石 | 114074~77 | NoCondition+职业条件 | [职业增伤](BattleRule_FusionGem_ClassDamage_10000019_22_Context.md) |

---

## 3. 按机制分类

### 3.1 伤害前条件增伤（写当次 DamageContext）

- 远程 / 飞行 / 高血怪：`AllDamageBonuses(140)` + `AddDamageValueTake(235)`
- 关键：`IsImmediateDamageAttribute`、`BattleRuleDamageValueProvider`

### 3.2 HpChange 持久 / 即时 ModifyAttr

- 命悬一线：残血时持久写入 `AllDamageBonuses`，回血移除
- 满血增伤：满血时增伤（效果组 Persist 以配表为准）

### 3.3 RoundStart / 购买 → AddBuff 治疗

- 根深蒂固：换波 AddBuff 30031
- 生命复苏：购买强化 AddBuff 30032

### 3.4 购买强化 Computer → ModifyAttr 叠层

- 强化增伤 / 专属强化：`PurchaseEffect` / `PurchaseExclusiveEffect`

### 3.5 纯 Module 经济

- 宝藏猎人：`KillAnBossGoldCoin` → `OnGoldGained`（货币数值，非 CreateCoin 个数）

---

## 4. 关键代码速查

| 文件 | 职责 |
|------|------|
| `PlayerEquipModel.cs` | 融合石 Module 条目收集 |
| `BattleRuleSourceParamSupport.cs` | 宝石参数解析 |
| `BattleRuleDamageSupport.cs` | 远程/飞行 SpecialType、伤害前触发 |
| `BattleRuleMonsterHealthConditionValues.cs` | 怪血比例条件 |
| `BattleRuleHpChangeModifyAttrSupport.cs` | 命悬一线写/撤 |
| `BattleRuleEffectApplier.cs` | `AllDamageBonuses` 即时写 DamageContext |
| `BattleRuleInstantHealSupport.cs` | ModifyAttr 路径即时治疗（旧链路备用） |
| `BattleRuleComputerSupport.cs` | 购买强化 Computer |
| `BattleRuleBattleEconomySupport.cs` | 选卡 → OnPurchaseEffect |
| `MonsterCard.cs` | 精英/首领额外金币 |
| `BattleRuleRoundStartSupport.cs` | 换波 RoundStart |
| `BattleRuleLearnBuffEffectApplier.cs` | AddBuff + 参数代理 |

---

## 5. 配置路径

- `Assets/Res/Config/Json/tbgemfusionconfig.json`
- `Assets/Res/Config/Json/tbskillruleconfig.json`
- `Assets/Res/Config/Json/tbeffectgroupconfig.json`
- `Assets/Res/Config/Json/tbconditiongroupconfig.json`
- `Assets/Res/Config/Json/tbcomputergroupconfig.json`
- `Assets/SkillGraph/SkillBuffTemplateLibrary.asset`
