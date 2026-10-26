# 装备 LearnSkillRule 与 GloballyEffective AI 上下文

> 供后续 AI 对话或开发接续使用。  
> 最后更新：2026-07-17  
> 关联：`README_BattleRuleSystem.md`、`BattleRuleService_Context.md`、`PlayerEquipModel.cs`、`GamePlayManager.cs`、`BattleRule_SkillSourceBuff_Context.md`、`BattleRule_FusionGem_DeepRooted_10000023_Context.md`

---

## 1. 数据流总览

```
PlayerEquip.Entrys / Effects
  → Equip.GetModuleAttributeEntries()（StackSourceKey 实例级）
  → GamePlayManager.ApplyEquipModuleAttributes()
  → AttributeManager.AddBattleSkillRule（Equip 来源：仅登记，获得时不立即触发）
  → 技能/被动初始化完成
  → BattleRuleService.ApplyEquipLearnedRulesToSoldier（逐英灵 Reevaluate + 月影祝福补同步）
  → BattleRuleAcquireSoldierEffectApplier → SoldierCard.BaseAttributes ModifyAttr
```

**StackSourceKey 约定：**

| 来源 | StackSourceKey 格式 |
|------|---------------------|
| 洗练词条 LearnSkillRule | `Equip_{装备实例Id}_{词条Id}` |
| 月影祝福叠加层 | `Equip_{装备实例Id}_{词条Id}_EntryEffectBonus_{n}` |
| 装备特效 LearnSkillRule | `EquipEffect_{装备实例Id}_{特效Id}` |

同 RuleId + 不同 StackSourceKey → 修饰符可叠加（如两件装备各 +90，或月影祝福再 +90）。

---

## 2. GloballyEffective（SkillRuleConfig）

**唯一路由依据**，不再用 `ComparisonValue`、`Equip_` 前缀等启发式。

| GloballyEffective | 条件取值 | 施加范围 | 入口 |
|-------------------|----------|----------|------|
| **1** | `Global.SoldierTeams`（`BattleRuleLineupValueProvider`） | 满足条件后对**全队** | `TryApplyRosterCompositionRule` / `ApplyRosterCompositionRulesToSoldier` |
| **0** | 逐英灵（`BattleRuleAcquireValueProvider`） | 仅条件满足的**单个英灵** | `TryApplyNoConditionRuleToAllSoldiers` / `ApplyEquipLearnedRulesToSoldier` |

**代码判断：**

- `BattleRuleRosterCompositionSupport.IsGloballyEffectiveRule`
- `IsGloballyEffectiveNoConditionRule` = NoCondition + GloballyEffective≠0

**配表示例（需在 Excel/Luban 维护）：**

| RuleId | 语义 | GloballyEffective |
|--------|------|-------------------|
| 112005 / 112082+ | 入局战士≥2 → 全队加成 | **1** |
| 112076 | 四职业齐全 → 全队 | **1** |
| 112055 / 112077 / 112081 | 单职业自身加成 | **0** |
| 112012–112074（洗练词条） | 按英灵职业/最高级/主线 | **0** |

---

## 3. 装备 Module 属性（125 / 126）

### 3.1 战斗货币初始数量修正（125）

- 配置：`EquipEffectsConfig` 如 **4010003**，`AttributeType.BattleGoldInitialQuantity(125)`
- 行为：`ApplyEquipModuleAttributes` 后读 `ModuleAttributes.GetFinal(125)` 叠加到 `Global.GameGold`

### 3.2 装备词条属性效果修正 / 月影祝福（126）

- 配置：**4010004** 等，`AttributeType.EquipEntryEffectCorrection(126)`，`Value=1` → 洗练词条规则 **多学 1 次**
- **不**放大词条数值、**不**增加 UI 词条条数
- 实现：`PlayerEquipModel.CollectEquipModuleEntries`
  - 读同件装备 `Effects` 中所有 126 求和 → `entryRuleBonusStackCount`
  - 对每个洗练词条 `LearnSkillRule` 额外追加 `EntryEffectBonus` 条目
- 126 本身不写入 `ModuleAttributes`（`AttributeManager` 直接 return）

### 3.4 融合石 LearnSkillRule + 参数（2026-07-16 / 07-17）

佩戴在装备上的融合石，与装备词条同一开局入口 `GetModuleAttributeEntries` → `ApplyEquipModuleAttributes`。

**配表模式（`tbgemfusionconfig` LevelList.Attributes）：**

```
LearnSkillRule(126) = RuleId
Category=Add 参数行（如 235 / 242 / 243）= 规则动态数值
```

| 宝石 | RuleId | Trigger | 条件 / Computer | 效果组 | 详档 |
|------|--------|---------|-----------------|--------|------|
| 远程/飞行/高血怪增伤 | 107011/107012/107013 | OnDamageBefore | 条件组 | 207009~207011 → AllDamageBonuses 当次 DamageContext | [OnDamageBefore](BattleRule_FusionGem_DamageBonus_OnDamageBefore_Context.md) |
| 命悬一线 10000007 | 111007 | HpChange | 血量≤30% | 211007 持久 AllDamageBonuses | [命悬一线](BattleRule_FusionGem_LifeOnTheLine_10000007_Context.md) |
| 满血增伤 10000025 | 111008 | HpChange | 满血 | 211008 | [满血](BattleRule_FusionGem_FullHpDamage_10000025_Context.md) |
| **根深蒂固 10000023** | **113007** | **RoundStart(14)** | 无 | **213007 AddBuff 30031** | [根深蒂固](BattleRule_FusionGem_DeepRooted_10000023_Context.md) |
| 生命复苏 10000026 | 114078 | NoCondition | PurchaseEffect | 214041 AddBuff 30032 | [生命复苏](BattleRule_FusionGem_LifeRecovery_10000026_Context.md) |
| 强化增伤 10000030/31 | 114079/114080 | NoCondition | Purchase / 专属 | AllDamageBonuses 叠层 | [购买增伤](BattleRule_FusionGem_PurchaseDamage_10000030_31_Context.md) |
| 宝藏猎人 10000027 | — | — | Module `KillAnBossGoldCoin(130)` | 击杀精英/首领额外货币 | [宝藏猎人](BattleRule_FusionGem_TreasureHunter_10000027_Context.md) |
| 职业增伤 10000019~22 | 114074~77 | NoCondition | 职业 SpecialType | AllDamageBonuses | [职业](BattleRule_FusionGem_ClassDamage_10000019_22_Context.md) |

**总索引：** [`BattleRule_FusionGem_Index.md`](BattleRule_FusionGem_Index.md)

**根深蒂固（2026-07-17 链路变更）：**

- **旧：** `213007` ModifyAttr → `SoldierPerRestoreCurrentHealth(55)` + InstantHeal  
- **新：** `213007` AddBuff → 模板 **30031**，`PercentEffectValueAttrName=DeepRootedEffect`，`PercentValueSourceAttrName=SoldierMaxHealth`  
- 详见：**`BattleRule_FusionGem_DeepRooted_10000023_Context.md`**

**运行时约定：**

| 项 | 值 |
|----|-----|
| SourceType | `AttributeSourceType.FusionGem` |
| StackSourceKey | `FusionGem_{融合石实例Id}` |
| 条件取值 | `BattleRuleDamageValueProvider`：远程=`AtkType.Shooting`，飞行=`MoveType.Flying` |
| 伤害前 PersistToAttributeSet | **1**（写当次 DamageContext，避免持久叠在 AttributeSet） |
| 参数解析 | `BattleRuleSourceParamSupport.TryResolveFusionGemAttributes` |
| RoundStart AddBuff 参数 | `AttachParamSkillToContext` → `BattleRuleSourceParamSkillProxy` |

融合石规则与装备一样延后 NoCondition 施加；OnDamageBefore / RoundStart 仅登记，触发时由 `TriggerLearnedBattleRules` 带 StackKey 执行。

**HpChange + ModifyAttr（命悬一线）注意：**

- 每次生命变化都会重判：条件满足 → 写入 `AllDamageBonuses`；不满足 → **移除**同 SourceKey 修饰（`BattleRuleHpChangeModifyAttrSupport`）
- 学习/补同步必须带 `StackSourceKey=FusionGem_{实例Id}`，否则 `AddDamageValueTake(235)` 读不到，增幅为 0
- 生效时机：英灵已残血后的**下一次输出伤害**（扣血当帧是受击，不改当次打出的伤害）

### 3.3 词条规则参数（Skill 类属性，2026-07-09）

装备/特效词条除 `LearnSkillRule(126)` 外，可在**同一条目** `Attributes` 中追加 **Category=Skill** 的参数行，供规则效果动态读取（与技能 `LevelPropertyList` 参数模式一致）。

**配表示例（Excel 两行）：**

```
学习规则:112012:数值:0:0
BUFF基础伤害:30:数值:0:0
```

**效果组迁移（`212012` 射手 +30 攻）：**

| 字段 | 旧（写死） | 新（参数化） |
|------|-----------|-------------|
| `BaseValue` | `30` | `0` |
| `SourceAttrType` | `201 (Null)` | `213`（参数属性 Type） |
| `PreValue` | `0` | `1` |

**运行时：** `StackSourceKey=Equip_{实例Id}_{词条Id}` → `BattleRuleSourceParamSupport` 读 `TbEquipEntryConfig` → `FlatAdd = 0 + 30 × 1`。

**注意：**

- `LearnSkillRule` 的 `EffectParam` / `ProbabilityTrigger` **不传**效果数值
- 月影祝福叠加层（`EntryEffectBonus`）与词条**共用**同一套参数配置
- 存量词条若未加参数行，行为不变（仍读 `EffectGroup.BaseValue`）

详见：`BattleRule_SkillSourceBuff_Context.md` §4.3、§6.4

**补同步兜底：** `BattleRuleService.ApplyMissingEquipEntryEffectBonusRules`  
若登记阶段未生成 `_EntryEffectBonus_` 条目，施加阶段按 `FusionContinent.Model.Equip.GetEquipEntryEffectBonusStackCount` 补 Reevaluate（避免 `Game.Equip` 命名空间冲突，使用全限定类名）。

---

## 4. 施加时机（避免重复叠加）

| 阶段 | 行为 |
|------|------|
| `AddBattleSkillRule`（Equip） | **不** `TryTriggerNoConditionOnAcquire` |
| `TriggerPendingNoConditionRulesOnInit` | **跳过** Equip 来源 |
| 技能挂载后 | `ApplyPostSkillInitBattleRulesToVillageSoldiers` → `ApplyEquipLearnedRulesToSoldier` |
| 后召唤英灵 | `InitializeNewSoldierSkills` → 同上 + `ApplyLearnedRulesToNewSoldier`（**不含** Equip 条目） |

**原因：** 装备规则若在 `ApplyEquipModuleAttributes` 时立即触发，早于 `AddSkillCard`，技能向 `ModifyAttr` 无法写入；且会与补同步路径双重叠加。

---

## 5. 关键文件

| 文件 | 职责 |
|------|------|
| `PlayerEquipModel.cs` | `GetModuleAttributeEntries`、126 叠加 StackKey |
| `AttributeManager.cs` | `AddBattleSkillRule`、Equip 延后触发 |
| `GamePlayManager.cs` | `ApplyEquipModuleAttributes`、`ApplyPostSkillInitBattleRulesToVillageSoldiers` |
| `BattleRuleService.cs` | `ApplyEquipLearnedRulesToSoldier`、月影祝福补同步 |
| `BattleRuleRosterCompositionSupport.cs` | `GloballyEffective` 路由 |
| `BattleRuleNewSoldierSyncSupport.cs` | 装备来源强制 Reevaluate |
| `BattleRuleAcquireSoldierEffectApplier.cs` | ModifyAttr → `BaseAttributes` |
| `BattleRuleSourceParamSupport.cs` | 装备词条 / 特效 Skill 类参数解析 |
| `SkillBase.cs` | 普攻 `SumDamageTypes` 读 `SoldierAttackPower` |
| `HeroDetails.cs` | 长按英灵详情，读 `BaseAttributes.GetFinal` |

---

## 6. 日志排查

### 6.1 规则学习

```
[AttributeManager] 本局新增规则: RuleId=112055, StackKey=Equip_xxx_5030002, Source=Equip
[AttributeManager] 本局新增规则: RuleId=112055, StackKey=Equip_xxx_5030002_EntryEffectBonus_1, Source=Equip
```

有月影祝福时，同一 RuleId 应出现两条不同 StackKey。

### 6.2 规则施加

```
[BattleRuleNewSoldierSyncSupport] Reevaluate 同步: RuleId=..., StackKey=..., hero=...
[BattleRuleService] 月影祝福补同步: RuleId=..., StackKey=..._EntryEffectBonus_1, hero=...
[BattleRuleAcquireSoldierEffectApplier] 已施加属性 SoldierAttackPower, Flat=90, ...
```

### 6.3 攻强对比（普攻 vs HeroDetails）

| 日志前缀 | 触发时机 | 含义 |
|----------|----------|------|
| `[Attack]` | 普攻 `SumDamageTypes` | `base` / `final` 攻强、物理伤害 |
| `[HeroDetails]` | 长按英灵 | 面板打开时 `BaseAttributes` 攻强 |

**对照：**

- `[Attack] final` 升高、`[HeroDetails] final` 仍为 base → UI 刷新问题（见 §7）
- 两处 `final` 均等于 `base` → 装备规则未写入 `BaseAttributes`，查 §4 施加链路
- 两处 `final` 一致且已升高、界面数字不对 → ScrollView / RollingNumber 显示

---

## 7. HeroDetails 注意事项

- 路径：`SoldierCard` 长按 → `UIKit.OpenPanel<HeroDetails>().SetData(this)`
- 数值来源：`soldierCard.BaseAttributes.GetFinal(ShowAttributeTypes[i])`（**非** `Equip.GetAttributes` 聚合）
- 已修复：`SetData` 清空 `_attributeList`；`OnUpdateCardItem` 实时读 `GetFinal`；`OnClose` 清理引用

---

## 8. 测试检查清单

### 叠加与月影

- [ ] 两件装备同 RuleId（不同 StackKey）→ 效果数值叠加
- [ ] 4010004 + 洗练 LearnSkillRule → 两条 StackKey，效果约 2 倍
- [ ] 无月影祝福 → 每条规则只施加一次（无重复路径叠加）

### GloballyEffective

- [ ] `GloballyEffective=1` 且条件满足 → 全队获得（如 112005）
- [ ] `GloballyEffective=0` 战士词条 → 仅战士获得（112055 / 112077）
- [ ] 后召唤英灵 → 装备规则 + 阵容规则补同步

### 攻强与 UI

- [ ] 普攻 `[Attack] final` 含装备加成
- [ ] 长按 `[HeroDetails] final` 与 `[Attack] final` 一致
- [ ] 技能向属性规则（如 112071）在 `AddSkillCard` 之后生效

### 词条参数化（可选迁移）

- [ ] 词条 `Attributes` 含 `LearnSkillRule` + Skill 类参数行
- [ ] 效果组 `SourceAttrType` 指向参数，`BaseValue=0`
- [ ] 日志 `已施加属性 ..., Flat=<词条参数>`，非写死 EffectGroup 常量
- [ ] 同 RuleId 两件装备：各自 StackKey + 各自参数叠加

---

## 10. 配置路径

- `Assets/Res/Config/Json/tbequipeffectsconfig.json`（4010003/4010004…）
- `Assets/Res/Config/Json/tbequipentryconfig.json`（503000x LearnSkillRule）
- `Assets/Res/Config/Json/tbskillruleconfig.json`（`GloballyEffective` 字段）
- `Assets/Res/Config/Json/tbattributeconfig.json`（125/126 Category=Module；242/243 Category=Add）
- `Assets/Res/Config/Json/tbgemfusionconfig.json`（融合石 LearnSkillRule + 参数）
- `Assets/SkillGraph/SkillBuffTemplateLibrary.asset`（如根深蒂固 Buff 30031）

---

## 11. 变更历史（融合石节选）

| 日期 | 内容 |
|------|------|
| 2026-07-16 | 融合石开局学习 + `FusionGem_` StackKey + 参数解析；增伤/购买强化/击杀金币接入 |
| 2026-07-17 | 根深蒂固 213007 改为 AddBuff 30031；专用文档 `BattleRule_FusionGem_DeepRooted_10000023_Context.md` |
