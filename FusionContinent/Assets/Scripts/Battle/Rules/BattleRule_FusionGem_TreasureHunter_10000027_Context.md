# 宝藏猎人宝石（10000027）

> 最后更新：2026-07-17  
> 索引：`BattleRule_FusionGem_Index.md`

---

## 1. 功能概述

击杀**精英或首领**时，额外获得战斗货币（Lv1=50）。  
**不走 LearnSkillRule**，直接 Module 属性写入 `ModuleAttributes`。

| 项 | 值 |
|----|-----|
| 宝石 | 10000027 宝藏猎人宝石 |
| 属性 | `KillAnBossGoldCoin(130)`，Category=Module |
| Lv1 Value | 50（**货币数值**，不是金币实体个数） |
| 触发点 | `MonsterCard` 精英/首领死亡掉落逻辑 |

描述：击杀精英/首领额外获得 50 金币。

---

## 2. 配表

```
tbgemfusionconfig 10000027 LevelList.Attributes
  Type=130 KillAnBossGoldCoin
  Value=50
  （无 126 LearnSkillRule）
```

开局：`CollectFusionGemModuleEntries` → 仅 Module 类属性 → `AttributeManager` 写入 `GamePlayManager.ModuleAttributes`。

---

## 3. 运行时

```
精英/Boss 死亡
  → MonsterCard 掉落分支
  → bossExtraGold = ModuleAttributes.GetFinal(KillAnBossGoldCoin)
  → 若 >0：GeneralSkillManager.OnGoldGained(bossExtraGold)
```

### 3.1 严禁误用 CreateCoin

| API | 语义 |
|-----|------|
| `GoldCoinManager.CreateCoin(n, pos)` | 生成 **n 个**金币实体，每个再按 `DorpGoldBaseNumber` 结算 → 会放大成数百 |
| `OnGoldGained(n)` | 直接增加 **n 点**战斗货币 ← 本宝石正确路径 |

历史 bug：把 130 的值传给 `CreateCoin`，额外货币暴涨。

---

## 4. 测试清单

- [ ] 装备宝石后 ModuleAttributes 含 KillAnBossGoldCoin
- [ ] 击杀精英/首领：货币 +50（或当前等级 Value）
- [ ] 击杀普通怪：无该额外奖励
- [ ] 日志/数值不是「50×基础掉落」级别的暴涨

---

## 5. 关联

- `PlayerEquipModel.CollectFusionGemModuleEntries`
- `MonsterCard.cs`（精英/Boss 额外金币）
- `AttributeType.KillAnBossGoldCoin = 130`
