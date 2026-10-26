# 战斗规则系统（BattleRule）开发文档

## 概述

本局战斗规则系统基于 Luban 配置驱动，将 **触发时机**、**条件组**、**效果组** 解耦，替代部分旧的 `TriggerBuff` / `skillCarrying` / `atkCarryings` 逻辑。

- 配置表：`SkillRuleConfig`、`ConditionGroupConfig`、`EffectGroupConfig`
- 运行时缓存：`AttributeManager.BattleLearnedSkillRules`（允许同 RuleId 多次叠加）
- 核心服务：`BattleRuleService`（`Assets/Scripts/Battle/Rules/`）

---

## 配置结构

### SkillRuleConfig

| 字段 | 说明 |
|------|------|
| `Id` | 规则 Id |
| `Trigger` | `TriggerType` 触发时机 |
| `ConditionGroupId` | 条件组 Id 列表（AND） |
| `EffectGroupId` | 效果组 Id 列表 |
| `TriggerPro` | 触发概率（0~1；>1 时按百分比 /100）。0/100% 为确定性；其余走**伪随机**：未触发按当前档位多掷叠加（失败次数归零于触发后）；连续触发衰减 `base → base/2 → base*7/30 → 5%`（基准 30% 时为 30→15→7→5） |
| `CoolDown` | 冷却（秒） |
| `MaxTriggerNum` | 最大触发次数（0=不限） |
| `GloballyEffective` | **0=按英灵逐个判条件并施加；1=按入局编队判条件，满足后对全队生效** |
| `NewSoldierSync` | 后召唤英灵同步策略（`SyncType`） |

学习入口：`AttributeType.LearnSkillRule(123)`（`Category=Module`），`Value` 为规则 Id → `AttributeManager.AddBattleSkillRule(ruleId, stackSourceKey, sourceType, scopeHeroCardId)`。

- `scopeHeroCardId = 0`：全局规则（全队/全局 Owner）
- `scopeHeroCardId > 0`：英雄专属，与流派卡 `SchoolCardConfig.IsAddHero` 对齐

详见：`BattleRule_HeroScopedSchoolCard_Context.md`

### 流派卡英雄专属（IsAddHero）

| 配置 | 说明 |
|------|------|
| `SchoolCardConfig.IsAddHero` | 英灵 `cardId`；>0 时三选一仅在该英雄在场时出现，且所学规则带 `ScopeHeroCardId` |
| `StarList.Attributes` | **Type=123** 直接写 `LearnSkillRule`；`ConditionType` 必须为 **0** |
| 击杀叠属性 | `SkillRule`：`Trigger=OnDamageAfter` + `ComputerGroup(KillCount)` + `ModifyAttr`；专属规则击杀计数按**该英灵实例**累加 |
| 反模式 | Type=122 → 被动 `tbspecialeffectsconfig` 再挂 LearnSkillRule（易漏登记；已补 `ApplyPassiveLearnSkillRules`，仍推荐 Type=123） |

范例：全队击杀叠层 → 流派卡 **12113** 怪物猎人 + Rule **108002**；英雄专属 → `IsAddHero` + 独立 Rule/ComputerGroup。

### 装备词条 LearnSkillRule

| 配置 | 说明 |
|------|------|
| `PlayerEquip.Entrys` | 词条 Id 列表（`TbEquipEntryConfig`） |
| `EquipEntryConfig.Attributes` | 通常为 `Type=123`，`Value=SkillRuleConfig.Id`（如 112012–112074） |
| 战斗开局 | `GamePlayManager.ApplyEquipModuleAttributes` → `Equip.GetModuleAttributeEntries` |
| StackSourceKey | `Equip_{装备实例Id}_{词条Id}`，同 RuleId 多件装备可叠加；月影祝福见下文 |
| 条件路由 | **`GloballyEffective=1`** → `TryApplyRosterCompositionRule`（`BattleRuleLineupValueProvider` + 全队）；**`GloballyEffective=0`** 且含职业/最高级/主线条件 → `TryApplyNoConditionRuleToAllSoldiers`（`BattleRuleAcquireValueProvider` + 逐英灵） |
| 施加时机 | Equip 来源 **仅登记**；技能挂载后 `ApplyEquipLearnedRulesToSoldier` 统一 Reevaluate |
| NewSoldierSync | 装备规则 112012–112074 配置为 `Reevaluate(3)`，后召唤英灵补判条件 |

### 装备特效 Module 属性（125 / 126）

| AttributeType | 配置示例 | 行为 |
|---------------|----------|------|
| **125** `BattleGoldInitialQuantity` | 4010003 | 开局 `Global.GameGold` +Value |
| **126** `EquipEntryEffectCorrection` | **4010004** 月影祝福 | 同件装备洗练词条 `LearnSkillRule` **多学 1 次**（`_EntryEffectBonus_` StackKey），不放大词条数值 |

详见：`BattleRule_EquipLearnSkillRule_Context.md`（含融合石 §3.4）；**融合石总索引** `BattleRule_FusionGem_Index.md`；根深蒂固 `BattleRule_FusionGem_DeepRooted_10000023_Context.md`

### TriggerType 一览

| 值 | 枚举 | 代码接入点 |
|----|------|------------|
| 0 | OnAttackBefore | `BattleRuleDamageModifier`（普攻） |
| 1 | OnAttackAfter | `BattleRuleHitTrigger` |
| 2 | OnSkillBefore | `BattleRuleDamageModifier`（技能） |
| 3 | OnSkillAfter | `BattleRuleHitTrigger`（命中后） |
| 4 | OnSkillUse | `BattleRuleCastSupport.TriggerSkillAfterUse`（`UseSkill` 成功后一次） |
| 5 | OnCriticalBefore | `BattleRuleDamageModifier`（暴击） |
| 6 | OnCriticalAfter | `BattleRuleHitTrigger` |
| 16 | OnSkillCriticalAfter | `BattleRuleHitTrigger.TriggerSkillCriticalAfter`（技能暴击命中后，跳过普攻） |
| 7 | OnDamageBefore | `BattleRuleDamageModifier`（任意伤害前） |
| 8 | OnDamageAfter | `BattleRuleHitTrigger` |
| 9~10 | 普攻暴击前/后 | `BattleRuleHitTrigger` / DamageModifier |
| 11 | NoCondition | `TryTriggerNoConditionOnAcquire` / 新英灵同步 |
| 12 | HeroAfterHarmed | 仍走 `ConditionsTypeEvent`（旧 Buff） |
| 13 | HpChange | `BattleRuleHpChangeTrigger`（受击扣血后） |
| 14 | RoundStart | `BattleRuleRoundStartTrigger`（首日 `OnInitComplete`、换波 `EndDay`）；详见 `BattleRule_RoundStart_2150102_Context.md`；融合石回血见 `BattleRule_FusionGem_DeepRooted_10000023_Context.md` |
| 15 | AfterTreatmentWasApplied | `BattleRuleTreatmentTrigger`（施加治疗/护盾后；仅 `DamageType=Treatment`）；奥数卡 115001/115002 |

### EffectGroupConfig

| 字段 | 执行器 | 说明 |
|------|--------|------|
| `Effects` | `BattleRuleEffectApplier` | 本次伤害即时效果（穿透、吸血、增伤等） |
| `Attributes` | 分类型路由 | 见下文「属性路由」 |

---

## 架构与数据流

```
LearnSkillRule → BattleLearnedSkillRules（含 StackSourceKey、ScopeHeroCardId，可重复 RuleId）
       ↓
BattleRuleService.TriggerLearnedBattleRules(trigger, context)  // 按 ScopeHeroCardId 过滤
       ↓
BattleRuleConditionManager（条件 AND）
       ↓
BattleRuleEffectManager → IBattleRuleEffectExecutor
       ├── BattleRuleDamageEffectExecutor   （伤害前/暴击前等，改 DamageContext）
       └── BattleRuleAcquireEffectExecutor  （获得时/命中后/HpChange，Buff/模块属性）
```

### 伤害管线顺序（英灵 → 怪物）

```
MonsterCard.TakeDamage
  → 填充 DamageContext（含吸血/穿透初值）
  → DamagePipeline [SoldierDamage]
       1. BattleRuleDamageModifier   ← 规则：OnDamageBefore / OnAttackBefore / OnSkillBefore / OnCriticalBefore
       2. AttributesModifier         ← 旧 skillCarrying（并行保留）
       3. ResistanceModifier         ← 穿透在此生效
       ...
  → ApplyLifesteal / BattleRuleHitTrigger（命中后规则）
```

**重要：** 规则伤害加成写入 `DamageContext`，**不再**累加到 `skillCarrying`，避免跨命中 1.5×1.5 叠加。

---

## 属性路由（EffectGroup.Attributes）

| 属性类型 | 执行路径 | 作用域 |
|----------|----------|--------|
| 穿透/吸血/增伤等伤害类 | `BattleRuleEffectApplier.ApplyDamageAttributes` | 仅本次伤害 |
| `LearnBuffEffect(107)` | `EffectsExecutor.ExecuteTakeBuff` | 受击英灵 / 新英灵 |
| `RemoveBuff(116)` | `BattleRuleRemoveBuffApplier` | 同上，按 BuffId 移除 |
| 模块类（如 `LockAtkInterval`） | `AttributeManager`（NoCondition 全场一次） | 模块/全场 |
| 英灵属性（Soldier 类） | `SetAttributeLogic` 或 `BattleRuleAcquireSoldierEffectApplier` | 英灵 BaseAttributes |

### 穿透比例语义（*PenetrationPer）

配置 `SoldierArmorPenetrationPer(12)` / `SoldierMagicPenetrationPer(13)`：

- **`Value` 表示穿透比例（0~1）**，与 `ValueType` 无关
- **`Value = 1` → 100% 穿透**（写入 `resistanceIgnorePreValue`）
- `Value > 1` 时按百分数解析（如 `100` → 100%）

实现：`BattleRuleEffectApplier.NormalizePenetrationRatio`、`AttributesModifier` 同步逻辑。

公式（`ResistanceModifier`）：

```csharp
res *= (1 - (penetra + preValue) * (1 + prePenre));
```

### 生命值条件（GoalAttribute + SoldierMaxHealth/Health）

配置 `Attribute=0/1` 时，**不按最大生命属性值比较**，而按：

```text
CurrentHp / MaxHp  （0~1）
```

实现：`BattleRuleSoldierHealthConditionValues`（HpChange / 获得规则条件共用）。

### Buff Id（2026-06-08 更新）

**EffectGroup `AddBuff(0)` / `RemoveBuff(1)`：**

- `BaseValue` = **`SkillBuffTemplateLibrary.BuffId`**（如 `20014`、`21004`）
- **不经过** `TbEffectsConfig`（技能 TakeBuff 才用 EffectsConfig）
- Add：`BattleRuleEffectTargets.ResolveBuffId` 校验模板存在 → `BuffApplier.ApplyBuff`
- Remove：`BattleRuleEffectTargets.ResolveRemoveBuffId` 直接用 BaseValue → 匹配 `buff.BuffId`

**旧 AttributeStruct 路径（兼容）：**

- `LearnBuffEffect(107)` / `RemoveBuff(116)` 的 `Value` 同样填模板 BuffId

**运行时：**

- 实例 `buff.BuffId` 与模板 Id 一致（MultiEffectBuff / HealBuff 等同理）
- 属性 SourceKey：`BuffId.ToString()`（AttributeModifierBuff）

详见：`BattleRule_BuffEffect_Context.md`、`SkillBuffTemplateLibrary_README.md`

---

## 典型配置示例

| 规则 Id | Trigger | 概率 | 效果 | 说明 |
|---------|---------|------|------|------|
| 10301 | OnSkillBefore | 0.5 | 20301 增伤 | 技能命中前 1.5 倍伤害 |
| 10201 | OnAttackAfter | 1 | 20201 学 Buff | 普攻后 `ExecuteTakeBuff` |
| 10701 | OnDamageBefore | 0.5 | 20701 穿透% +1 | 50% 概率 100% 穿透 |
| 10702 | OnDamageBefore | 0.3 | 20702 吸血 +0.3 | 30% 概率 30% 吸血 |
| 11101 | HpChange | 1 | 21101 AddBuff | 满血条件 1017，Buff 20012 |
| 11102 | HpChange | 1 | 21102 RemoveBuff | 受伤条件 1018，Buff 20012 |
| **111003** | HpChange | 1 | 211003 AddBuff | 满血 1017，Buff **20014**（2203001） |
| **111004** | HpChange | 1 | 211004 RemoveBuff | 非满血 1018，Buff **20014** |
| **112010** | NoCondition | 1 | 211003 AddBuff | 获得时满血补 Buff 20014 |
| **111005** | HpChange | 1 | 211005 AddBuff | hp≤50%（1001），Buff **21004**，MaxTriggerNum=1 |
| 11103 | NoCondition | 1 | 21103 攻强/法强×2 | 直接触发，全队持久属性 |
| 11104 | NoCondition | 1017 | 21101 AddBuff | 满血时新英灵也学 Buff |

### 条件组示例

| Id | 含义（生命比例） |
|----|------------------|
| 1017 | `GoalAttribute` + `>= 1` → 满血 |
| 1018 | `GoalAttribute` + `< 1` → 已受伤 |

---

## 代码文件索引

| 文件 | 职责 |
|------|------|
| `BattleRuleService.cs` | 注册、触发、NoCondition、**新英灵同步** |
| `BattleRuleConditionManager.cs` | 条件比较 |
| `BattleRuleEffectManager.cs` | 效果组分发 |
| `BattleRuleEffectApplier.cs` | 伤害上下文效果 + 穿透比例 |
| `BattleRuleDamageSupport.cs` | 伤害 ValueProvider、`BattleRuleDamageModifier`、`BattleRuleHitTrigger` |
| `BattleRuleAcquireSupport.cs` | 获得时 ValueProvider、`BattleRuleAcquireEffectExecutor` |
| `BattleRuleHpChangeSupport.cs` | HpChange 触发与 ValueProvider |
| `BattleRuleTreatmentSupport.cs` | AfterTreatmentWasApplied 触发（`DamageType=Treatment`） |
| `BattleRuleMustCriticalSupport.cs` | ModifyAttr 必暴；六律·技：施法计数达标 → 下一招技能必暴 |
| `BattleRuleObtainDamageForLossHpSupport.cs` | HpChange：每损失 5% 最大生命叠 AllDamageBonuses（增幅读 BaseValue） |
| `BattleRuleRoundStartSupport.cs` | RoundStart 触发（祈祷被动 2150102 等） |
| `BattleRuleLearnBuffEffectApplier.cs` | 学 Buff |
| `BattleRuleRemoveBuffApplier.cs` | 删 Buff |
| `BattleRuleAcquireSoldierEffectApplier.cs` | NoCondition 的 Effects → 英灵属性 |
| `BattleRuleSoldierAttributeApplier.cs` | 单英灵 Attributes |
| `BattleRuleSoldierHealthConditionValues.cs` | 生命比例条件 |
| `BattleRuleHeroScopeSupport.cs` | 英雄专属规则触发过滤、专属 KillCount |
| `BattleRuleEffectTargets.cs` | BuffId 解析、目标英灵 |

### 外部接入点

| 位置 | 调用 |
|------|------|
| `AttributeManager.AddBattleSkillRule` | 学习规则（可选 `scopeHeroCardId`）+ NoCondition 立即触发 |
| `GamePlayManager.AddSoldierCard` | `ApplyLearnedRulesToNewSoldier` |
| `SoldierCard.TakeDamage` | `BattleRuleHpChangeTrigger`（扣血后） |
| `MonsterCard.TakeDamage` | 伤害管线 + 命中后触发 |

---

## 新英灵同步

`BattleRuleService.ApplyLearnedRulesToNewSoldier(soldier)` 在 `AddSoldierCard` 后调用：

- 遍历已学规则中 **`NoCondition(10)`**、**`HpChange(12)`**
- 按当前英灵重新判条件
- 仅对**该英灵**补 Effects / Buff / 英灵 Attributes
- **跳过**模块类属性（避免 `LockAtkInterval` 等重复施加全场）

### GloballyEffective 路由（NoCondition）

| GloballyEffective | 条件取值 | 施加范围 | 入口 |
|-------------------|----------|----------|------|
| **1** | `Global.SoldierTeams` 入局编队（`BattleRuleLineupValueProvider`） | 满足条件后对**全队**持久 ModifyAttr | `TryApplyRosterCompositionRule` / `ApplyRosterCompositionRulesToSoldier` |
| **0** | 逐英灵（`BattleRuleAcquireValueProvider`） | 仅条件满足的**单个英灵** | `TryApplyNoConditionRuleToAllSoldiers` / `ApplyEquipLearnedRulesToSoldier` |

**不再**用 `ComparisonValue`、`Equip_` StackKey 等启发式区分小队/个人；以配置 `GloballyEffective` 为唯一路由依据。

示例：`112005`（WarriorNum≥2 全队加成）→ `GloballyEffective=1`；`112077`（战士自身 HP）→ `GloballyEffective=0`。

---

## 与旧系统并行说明

以下仍保留，尚未全量迁移：

- `SoldierCard.atkCarryings` → `SkillCarrying` → `AttributesModifier`
- `SchoolManager` / `GeneralSkillManager` 写入 `atkCarryings`
- `TriggerBuff` + `ConditionsTypeEvent`（部分时机已由规则替代）
- `HeroAfterHarmed(11)` 规则表项仍为空，受击 Buff 监听仍可能走旧事件

新规则与旧链路**不要**对同一效果双重真值源；新功能优先走 `LearnSkillRule` + `SkillRuleConfig`。

---

## 测试检查清单

### 伤害类

- [ ] 10301：技能伤害前 50% 概率增伤，且不跨命中累加
- [ ] 10701：50% 概率攻击几乎无视抗性
- [ ] 10702：30% 概率攻击后回血（物/法分开）

### Buff / 生命

- [ ] 10201：普攻后挂 Buff 20012，BuffId 与移除一致
- [ ] 11101 满血学 Buff / 11102 受伤删 Buff
- [ ] 移除 Buff 后攻强等属性回退（`AttributeSourceKey` 一致）

### 直接触发 / 新英灵

- [ ] 11103：全队攻强/法强提升
- [ ] 战后召唤新英灵：自动补 11103 属性、满血补 Buff（11104）

### 流派卡 / 英雄专属

- [ ] `IsAddHero>0`：仅目标英雄在场时进三选一；日志 `ScopeHero=cardId`
- [ ] 专属击杀规则：仅该英雄击杀累加计数；其他英雄击杀不触发
- [ ] 怪物猎人 12113（全队）：`ScopeHero=0`，KillCount 走 `GlobalBattleOwnerId`

### 装备词条（LearnSkillRule 123）

- [ ] 单件 5010001（Rule 112012）：仅射手 +30 攻
- [ ] 两件装备同 RuleId：射手 +60（StackSourceKey 独立叠加）
- [ ] 5010016（HighLevel 射手）：仅队内最高 level 射手获得技能伤害加成
- [ ] 5030021（MainStage）：仅主线关卡生效
- [ ] **4010004 月影祝福**：同 RuleId 两条 StackKey，效果约 2 倍
- [ ] **GloballyEffective=0** 战士词条（112055）：仅战士；**=1** 阵容规则（112005）：全队
- [ ] 战斗开局：`ClearBattleSkillRules` → 装备登记 → 技能初始化 → `ApplyEquipLearnedRulesToSoldier`
- [ ] 城市升级后加英雄：Reevaluate 补判条件
- [ ] `[Attack]` 与 `[HeroDetails]` 的 `SoldierAttackPower final` 一致

### 稳定性

- [ ] 规则冷却、最大次数生效
- [ ] 镜像英灵不触发 HpChange / 新英灵同步
- [ ] 无 `Debug.Log` 残留（应使用 `LogTool`）

---

## 策划配置注意

1. **穿透%**：`SoldierArmorPenetrationPer` 的 `Value` 填 `0~1`，`1` = 100%。
2. **吸血**：`PhysicalBloodSucking` / `MagicBloodSucking` 的 `Value` 为比例（如 `0.3` = 30%）。
3. **学 Buff / 删 Buff（EffectGroup）**：`AddBuff`/`RemoveBuff` 的 `BaseValue` 填 **`SkillBuffTemplateLibrary.BuffId`**（如 `20014`），**不是** `TbEffectsConfig.Id` 或 `EffectsId`。
3.1 **链式学习来源动态数值**：`LearnSkillRule` 来源侧配置 **Skill 类参数行** + 效果组 `SourceAttrType` 引用；支持技能实例、装备词条、流派星级、轮语者武器/皮肤。见 `BattleRule_SkillSourceBuff_Context.md` §6.0。
3.2 **轮语者武器 ExtraAttributes**：`LearnSkillRule` + 参数行（如 227）；含 BattleGoldNum / EverySecond Computer、LiveMage/Warrior、BuyEffects。见 `Assets/Scripts/Game/Player/WeaponExtraAttributes_Context.md`。
4. **生命条件**：用 `GoalAttribute` + 属性 0/1，比较值为比例（`1` = 100% 血量）。
5. **NoCondition**：模块效果全场只触发一次；新英灵只补英灵向效果。
6. **GloballyEffective**：全队规则填 `1`，单英灵/职业规则填 `0`（勿依赖 ComparisonValue 推断）。
7. **月影祝福 126**：只叠加洗练词条 LearnSkillRule 次数，不翻倍词条面板数值。
8. **成对规则**：加 Buff 与删 Buff 必须学齐（例：2203001 需 111003 + **111004** + 112010）。
9. **英雄专属流派卡**：`IsAddHero` + `StarList` 内 Type=123；规则作用域见 `BattleRule_HeroScopedSchoolCard_Context.md`。

---

## 相关配置路径

- `Assets/Res/Config/Json/tbschoolcardconfig.json`
- `Assets/Res/Config/Json/tbequipentryconfig.json`
- `Assets/Res/Config/Json/tbequipeffectsconfig.json`
- `Assets/Res/Config/Json/tbskillruleconfig.json`
- `Assets/Res/Config/Json/tbconditiongroupconfig.json`
- `Assets/Res/Config/Json/tbeffectgroupconfig.json`
- `Assets/SkillGraph/SkillBuffTemplateLibrary.asset`
- `Assets/Scripts/Data/Client/Luban/SkillRuleConfig.cs`
- `Assets/Scripts/Battle/Rules/BattleRule_FusionGem_Index.md`
- `Assets/Scripts/Battle/Rules/BattleRule_FusionGem_DeepRooted_10000023_Context.md`
- `Assets/Scripts/Battle/Rules/BattleRule_FusionGem_DamageBonus_OnDamageBefore_Context.md`
- `Assets/Scripts/Battle/Rules/BattleRule_FusionGem_LifeOnTheLine_10000007_Context.md`
- `Assets/Scripts/Battle/Rules/BattleRule_FusionGem_LifeRecovery_10000026_Context.md`
- `Assets/Scripts/Battle/Rules/BattleRule_FusionGem_TreasureHunter_10000027_Context.md`
- `Assets/Scripts/Battle/Rules/BattleRule_FusionGem_PurchaseDamage_10000030_31_Context.md`
- `Assets/Scripts/Battle/Rules/BattleRule_BuffEffect_Context.md`
- `Assets/Scripts/Battle/Rules/BattleRule_SkillSourceBuff_Context.md`
- `Assets/Scripts/Battle/Rules/BattleRule_EquipLearnSkillRule_Context.md`
- `Assets/Scripts/Battle/Rules/BattleRuleSourceParamSupport.cs`
- `Assets/Scripts/Battle/Rules/BattleRule_HeroScopedSchoolCard_Context.md`
- `Assets/Scripts/Game/Player/WeaponExtraAttributes_Context.md`
- `Assets/Res/Config/Json/tbplayerweaponconfig.json`
- `Assets/Res/Config/Json/tbcomputergroupconfig.json`
- `Assets/Res/Config/Json/tbgemfusionconfig.json`
