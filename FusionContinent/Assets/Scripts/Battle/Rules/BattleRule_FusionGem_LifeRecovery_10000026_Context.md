# 生命复苏宝石（10000026）

> 最后更新：2026-07-17  
> 索引：`BattleRule_FusionGem_Index.md`  
> 同类治疗 Buff：根深蒂固 `BattleRule_FusionGem_DeepRooted_10000023_Context.md`

---

## 1. 功能概述

每购买一次**强化**（三选一选卡），全队英灵回复固定生命（Lv1=100，参数 `LifeRecoveryEffect(243)`）。

| 项 | 值 |
|----|-----|
| 宝石 | 10000026 生命复苏宝石 |
| 规则 | **114078** |
| Trigger | **NoCondition(11)** |
| Computer | **300017** PurchaseEffect，MaxValue=1，AfterTrigger 重置 |
| GloballyEffective | 1 |
| 效果 | **214041 AddBuff → 30032** |
| 参数 | `LifeRecoveryEffect(243)`，Lv1=100 |

描述：每次购买强化，全体英灵恢复 100 血量。

---

## 2. 配表链路

```
宝石 10000026
  LearnSkillRule(126)=114078
  LifeRecoveryEffect(243)=100
        │
        ▼
规则 114078
  Trigger=11 NoCondition
  ComputerGroupId=[300017]
  EffectGroupId=[214041]
  GloballyEffective=1
        │
        ▼
Computer 300017
  CounterType=11 PurchaseEffectNum
  TriggerType=11 PurchaseEffect
  MaxValue=1, AddValue=1, ResetType=AfterTrigger
        │
        ▼
效果 214041
  EffectType=0 AddBuff
  BaseValue=30032
        │
        ▼
Buff 30032「生命复苏效果」
  BuffTypeId=9 Health / HealBuff
  EffectValueAttrName=LifeRecoveryEffect   ← 固定治疗量来自宝石参数
  Duration / TickInterval 见 §4
```

**旧链路（已弃）：** ModifyAttr `RestoreCurrentHealth` / InstantHeal。  
**现链路：** 与根深蒂固相同，走 Round/购买触发的 **AddBuff**。

---

## 3. 运行时

```
选卡购买强化
  → BattleRuleBattleEconomySupport
  → BattleRuleComputerSupport.OnPurchaseEffect
  → Computer 300017 达标
  → TryTriggerNoConditionRulesForComputer(PurchaseEffect)
  → 114078 → AddBuff 30032
  → AttachParamSkillToContext（FusionGem_ → LifeRecoveryEffect=100）
  → HealBuff.Value = 100
```

专属强化卡（`IsAddHero`）也会走 `OnPurchaseEffect` 的通用 PurchaseEffect 累加（专属额外 Computer 给 10000031 用）。

---

## 4. Buff 30032 时序注意

当前模板常见配置：

| 字段 | 值 | 风险 |
|------|-----|------|
| `Duration` | -1 | 永久 |
| `TickInterval` | 0 | `HealBuff` **每帧**回血 |

若保持该组合，购买一次后会持续每帧 +100，属于配表风险。建议改为短时单次（参考 30031：`Duration≈TickInterval` 小正数），或 Instant 语义（`OnAdd` 一次结算）。

以 `SkillBuffTemplateLibrary.asset` 现场值为准，改完需回归。

---

## 5. 测试清单

- [ ] 学到 114078 + FusionGem_
- [ ] 买一次强化：全队回血 ≈243 参数值
- [ ] 再买一次：再次回血（Computer AfterTrigger 可重复）
- [ ] 未装备宝石：购买不触发 30032
- [ ] 确认不会出现「每帧无限回血」

---

## 6. 关联

- `BattleRuleComputerSupport.OnPurchaseEffect`
- `BattleRuleBattleEconomySupport.cs`
- `BattleRuleLearnBuffEffectApplier.cs`
- `SkillBuffTemplateLibrary.asset` BuffId=30032
