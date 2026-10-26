# 可重复获得类规则 vs 单次伤害类规则（ComputerGroup 共用说明）

> 供后续 AI 对话或开发接续使用。  
> 最后更新：2026-07-03  
> 主文档见 `BattleRuleService_Context.md`  
> ComputerGroup 示例见 `BattleRule_ComputerGroup_12111_Context.md`  
> 英雄专属击杀计数见 `BattleRule_HeroScopedSchoolCard_Context.md`

---

## 1. 问题背景

`ComputerGroup` 的 `ResetType.AfterTrigger` **只表示「计数器在效果执行后清零」**，并不决定效果是否「叠层累加」。

两条规则都可能 `AfterTrigger`，但行为完全不同：

| 规则 | 卡牌 | ComputerGroup | ResetType | 玩家感知 |
|------|------|---------------|-----------|----------|
| **108002** 三重绝刃 | 12111 | 300001 | AfterTrigger | 每 3 次伤害**当次**多一段真伤，计数归零再攒 |
| **112012** 怪物猎人 | 12111 怪物猎人 | 300002 | AfterTrigger | 每 10 杀全队 **永久 +5%** 属性，可多次叠层 |

---

## 2. 核心区分：看「效果写到哪里」

```
                    ComputerGroup 达标
                            ↓
              ┌─────────────┴─────────────┐
              │                           │
     Trigger = OnDamageBefore      Trigger = OnDamageAfter
     伤害管线执行器                  获得/命中执行器
              │                           │
     AddADDamage / AddRealDamage    ModifyAttr（英灵持久）
              │                           │
     只改本次 DamageContext         写入 Soldier.BaseAttributes
     命中结束即失效                 整局保留，可叠层
```

### 三重绝刃 108002（不累加）

| 项 | 值 |
|----|-----|
| Trigger | `6` = `OnDamageBefore` |
| ComputerGroup | 300001（Pair + AttackCount，2 秒内 3 次） |
| EffectGroup | 208002 → `AddRealDamage`（EffectType 9） |
| 执行器 | `BattleRuleDamageEffectExecutor` |
| 效果落点 | `DamageContext.TrueDamage`（仅当次命中） |

**为何不累加：**

- 效果不是 `ModifyAttr`，不会往英灵 `BaseAttributes` 写 Modifier。
- 每次触发只是在**当前这一刀**追加「目标 MaxHp × 10%」真伤。
- `AfterTrigger` 只把 **Pair 计数** 清 0，下次再攒 3 次再触发**又一次**当次加伤，而不是把 +10% 真伤叠在英灵身上。

### 怪物猎人 112012（可累加）

| 项 | 值 |
|----|-----|
| Trigger | `7` = `OnDamageAfter` |
| ComputerGroup | 300002（Self/Global + KillCount，累计 10 杀） |
| EffectGroup | 212012 → `ModifyAttr` ×3（攻强/法强/攻速 +5%） |
| 执行器 | `BattleRuleAcquireEffectExecutor` |
| 效果落点 | 全场英灵 `BaseAttributes` + `AttributeModifier` |

**为何会累加：**

- 每次达标给全队加一层 **持久** `PercentAdd` 修饰（`PreValue=0.05` → +5%）。
- 代码对这类规则开启 `AddModifier(..., isAdditive: true)`，同一 `sourceKey` 下 **PercentAdd 累加**（0.05 + 0.05 + …）。
- 结算走 `AttributeSet`：`FinalValue = BaseValue × (1 + ΣPercentAdd) + ΣFlatAdd`，**不在每次 GetFinal 时再乘一遍**。
- `MarkTriggered` 记录规则已触发次数；后上场英灵通过 `ByTriggerCount` 按次数补满层数。

**212012 配表示例（攻强/法强/攻速，三条结构相同）：**

```json
{
  "EffectType": 5,
  "AttributeType": 2,
  "SourceUnitType": 0,
  "SourceAttrType": 201,
  "TargetUnitType": 0,
  "BaseValue": 0,
  "PreValue": 0.05
}
```

| 字段 | 要求 |
|------|------|
| `SourceAttrType` | **必须为 `201` (Null)**，表示纯百分比；勿填与 `AttributeType` 相同 Id |
| `PreValue` | `0.05` = +5%；2 次触发累计 `PercentAdd=0.10` → 基础 ×1.10 |
| `ComputerGroup 300002.MaxValue` | **必须为 `10`**（每 10 杀触发一次） |

---

## 3. 配表驱动：NewSoldierSync

后上场同步不再硬编码 4 条路径，改由 `SkillRuleConfig.NewSoldierSync` 声明：

| 枚举 | 值 | 含义 |
|------|---|------|
| `None` | 0 | 后召唤英灵不同步 |
| `Once` | 1 | 补 1 次（阵容规则、持久附加物伤） |
| `ByTriggerCount` | 2 | 按本局规则已触发次数重放并累加 Modifier |
| `Reevaluate` | 3 | 按新英灵当前状态重判条件后补 |

入口：`BattleRuleNewSoldierSyncSupport`（`ApplyLearnedRulesToNewSoldier` 单循环分发）

| 方法 | 作用 |
|------|------|
| `AllowsOnDamageAfterModifyAttr` | `NewSoldierSync == ByTriggerCount` 时允许 `OnDamageAfter` 走 `ModifyAttr` |
| `UsesAdditiveModifierStack` | `ApplyEffectToSoldier` 时 `AddModifier(..., isAdditive: true)` |
| `ApplyToSoldier` | 新英灵入队时按枚举分发（含 `ByTriggerCount` 按 `GetRuleTriggerCount` 重放） |
| `BuildSourceKey(ruleId)` | 统一 `BattleRuleAcquire_{ruleId}`，同规则多层共用 Key |
| `BuildDamagePipelineSourceKey(ruleId)` | 伤害管线 AttributeSet 修饰 Key：`BattleRuleDamage_{ruleId}` |

**典型配表：**

- `108002` → `NewSoldierSync: 0`（None，当次真伤不同步）
- `112012` → `NewSoldierSync: 2`（ByTriggerCount，击杀叠层同步）

**三重绝刃不走累加：**

- `NewSoldierSync = None` → 后上场不同步
- 效果类型为 `AddRealDamage` → 走伤害管线，与 `ByTriggerCount` **无关**

---

## 4. AfterTrigger 在两套规则里各自重置什么

| 规则 | AfterTrigger 清零对象 | 不清零 / 保留对象 |
|------|----------------------|-------------------|
| 108002 | Pair 上的 AttackCount（300001） | 英灵属性无变化 |
| 112012 | 全局 KillCount（300002） | 已写入英灵的 +5% Modifier 层数 |

因此：**「触发后重置」= 计数归零重新攒门槛**，≠「效果叠层归零」。

---

## 5. 运行时序差异

### 三重绝刃（108002）

```
伤害前 AccumulateOutgoingDamageCount (+1)
  → OnDamageBefore 判定 Computer(3/3)
  → AddRealDamage 写入本次 ctx
  → ResetAfterTrigger 清 Pair 计数
  → 伤害结算 → 当次多一段真伤
```

### 怪物猎人（112012）

```
怪物 hp<=0
  → OnKill 累加 KillCount (+1)
  → OnDamageAfter 判定 Computer(10/10)
  → ModifyAttr 全队 +5%（isAdditive 叠层）
  → ResetAfterTrigger 清 KillCount
  → MarkTriggered 规则次数 +1（供后上场同步）
```

---

## 6. 配表速查

### 300001 三重绝刃

```json
"OwnerType": 2,      // Pair：英灵-怪物对
"TriggerType": 1,    // AttackCount
"MaxValue": 3,
"CounterTime": 2,
"ResetType": 1       // AfterTrigger
```

### 300002 怪物猎人（全队击杀）

```json
"OwnerType": 0,      // Self → KillCount 且 ScopeHeroCardId=0 时映射 GlobalBattleOwnerId
"TriggerType": 0,    // KillCount
"MaxValue": 10,
"ResetType": 1       // AfterTrigger
```

### 英雄专属 KillCount（ScopeHeroCardId > 0）

当已学规则条目 `ScopeHeroCardId` 与 `ComputerGroupId` 关联时，**不使用** `GlobalBattleOwnerId`，改为击杀者 `SoldierCard.GetInstanceID()` 独立计数；其他英灵击杀不计入。

- 配表：建议为英雄专属卡**新建** ComputerGroup Id，避免与 300002 等全队规则共用
- 代码：`BattleRuleHeroScopeSupport` + `BattleRuleComputerManager.TryResolveOwnerInstanceId`
- 范例配表见 `BattleRule_HeroScopedSchoolCard_Context.md` §4.3

---

## 7. 新增类似规则时的判断清单

| 策划意图 | Trigger 建议 | EffectGroupsType | NewSoldierSync |
|----------|-------------|------------------|----------------|
| 当次命中额外伤害/穿透 | OnDamageBefore | AddADDamage / AddAPDamage / AddRealDamage | `None` |
| 击杀 N 次后全队永久加属性 | OnDamageAfter | ModifyAttr（英灵属性） | **`ByTriggerCount`** |
| 击杀 N 次后**指定英雄**永久加属性 | OnDamageAfter | ModifyAttr | **`ByTriggerCount`** + 流派卡 `IsAddHero` + 独立 ComputerGroup |
| N 次伤害后当次强化（丛刃类） | OnAttackBefore | ModifyAttr（NormalAttackDamage 即时） | `None` |
| 学 Buff / 满血加 Buff | NoCondition / HpChange | AddBuff | `Reevaluate` |
| 阵容职业数量加成 | NoCondition | ModifyAttr | `Once` |

**简单记法：**

- **改这一次伤害** → 伤害前/伤害管线 → `NewSoldierSync = None`  
- **改英灵面板且要越杀越强** → `OnDamageAfter` + `ModifyAttr` → `NewSoldierSync = ByTriggerCount`

---

## 8. 关联代码

| 文件 | 职责 |
|------|------|
| `NewSoldierSyncType.cs` / `SkillRuleConfig.NewSoldierSync` | 配表枚举 |
| `BattleRuleNewSoldierSyncSupport.cs` | 后上场同步分发 + 累加判定 |
| `BattleRuleEffectGroupsApplier.cs` | `allowModifyAttr` / `useAdditiveModifierStack` 分发 |
| `BattleRuleAcquireSoldierEffectApplier.cs` | `AddModifier(..., isAdditive)` |
| `BattleRuleDamageEffectApplier.cs` | 当次伤害类效果（三重绝刃等） |
| `BattleRuleComputerManager.cs` | 计数累加 / 达标 / AfterTrigger 清零；专属 KillCount Owner |
| `BattleRuleHeroScopeSupport.cs` | 英雄专属触发过滤、KillCount Owner 解析 |
| `BattleRuleEffectParamUtil.cs` | `BuildModifier`：`PreValue` → `PercentAdd` / 来源×系数 |
| `BattleRuleCastDamageSession.cs` | 施法临时 AttributeSet 修饰（203003 等） |
| `MonsterDamageReceiver.cs` | OnKill 先于 OnDamageAfter |

---

## 9. 测试要点

### 108002 不应叠层

- [ ] 2 秒内对同一怪第 3 次伤害：当次多真伤，英灵面板攻强不变
- [ ] 第 4～6 次再攒满：再次当次加伤，不是「真伤加成 % 越来越高」

### 112012 应叠层

- [ ] 第 9 杀：不触发；第 10 杀：全队攻/法/攻速约 +5%（`PercentAdd` 累计 0.05）
- [ ] 第 20 杀：再 +5%（累计约 +10%，即基础 ×1.10）

### 英雄专属击杀规则（IsAddHero）

- [ ] 日志：`ScopeHero=目标 cardId`
- [ ] 仅目标英雄击杀累加；其他英雄击杀不改变该规则计数
- [ ] 达标后属性加在击杀者（目标英雄）身上，而非全队

### 112012 后上场同步

- [ ] 第 15 杀后新召唤英灵：与场上单位同层数（`ByTriggerCount`）
- [ ] 英灵面板 Modifier 来源 Key 为 `BattleRuleAcquire_112012`，非 `BattleRuleDamage_*`

---

## 10. 常见错配与修复记录（2026-06-17）

### 症状：怪物猎人伤害/属性「离谱」偏高

| 错配 | 实际行为 | 正确配法 |
|------|----------|----------|
| `300002.MaxValue = 1` | **每杀 1 只**就叠一层 +5% | `MaxValue = 10` |
| `212012.SourceAttrType = AttributeType`（如攻强填 2） | 曾走 `FlatAdd += 当前攻击力×0.05`，叠层随当前值膨胀 | `SourceAttrType = 201` |
| 伤害管线对英灵 `PreValue` 叠乘 `DamageContext` | 与 `GetFinal` 双重加成 | 英灵/技能 `ModifyAttr` 写 `AttributeSet`（2026-06-17 代码已改） |

### 预期数值（仅 PercentAdd，无 FlatAdd）

| 触发次数 | 累计 PercentAdd | 攻击力倍率（约） |
|----------|-----------------|------------------|
| 1 | 0.05 | ×1.05 |
| 2 | 0.10 | ×1.10 |
| 10 | 0.50 | ×1.50 |

**Excel 导表时务必同步：** `tbcomputergroupconfig.json`（300002）、`tbeffectgroupconfig.json`（212012）。
