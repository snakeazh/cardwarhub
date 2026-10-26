# 轮语者武器 ExtraAttributes 与 Module 规则 AI 上下文

> 供后续 AI 对话或开发接续使用。  
> 最后更新：2026-07-13  
> 关联：`WeaponService.cs`、`BattleRuleBattleEconomySupport.cs`、`GamePlayManager.cs`、`BattleRuleService_Context.md`、`BattleRule_SkillSourceBuff_Context.md`

---

## 1. 配置结构

### 1.1 Luban 类型

| 类型 | 路径 | 字段 |
|------|------|------|
| `PlayerWeaponConfig` | `Assets/Scripts/Data/Client/Luban/PlayerWeaponConfig.cs` | `WeaponStarList` |
| `WeaponStarListStruct` | `Assets/Scripts/Data/Client/Luban/WeaponStarListStruct.cs` | `BaseAttributes`、`ExtraAttributes` |

JSON 源表：`Assets/Res/Config/Json/tbplayerweaponconfig.json`

### 1.2 两类属性分工

| 字段 | Category | 聚合阶段 | 战斗开局 |
|------|----------|----------|----------|
| `BaseAttributes` | 非 Module | `WeaponService.GetAttributes()` → `SceneInitAttributeAggregation` → `ModuleAttributes` | 数值直接参与局外/局内模块属性 |
| `ExtraAttributes` | 可能含 Module（如 `LearnSkillRule=123`） | **非 Module** 部分同上；**Module** 部分不进数值聚合 | `GetModuleAttributeEntries()` → `ApplyEquipModuleAttributes` → `AttributeManager.AddBattleSkillRule` |

**禁止**在 `GetAttributes()` 中直接把 `LearnSkillRule` 写入 `AttributeSet`（会与装备规则一样重复或无效）。

过滤工具（与装备共用）：

- `FusionContinent.Model.Equip.IsModuleCategoryAttribute`
- `FusionContinent.Model.Equip.AddNonModuleBaseValues`
- `FusionContinent.Model.Equip.AddNonModuleSubAttributes`

> 在 `Game.Player` 命名空间下调用 Equip 工具类时，须写全限定名 `FusionContinent.Model.Equip`，避免与 `Game.Equip` 命名空间冲突。

### 1.3 ExtraAttributes 常见非 Module 参数 Type

| AttributeType | 枚举名 | 用途 |
|---------------|--------|------|
| 227 | `AddDamageValueTake` | 规则效果 `SourceAttrType` 读增伤/叠层数值 |
| 232 | `AddDurationTime` | Buff 时长参数（`DurationAttrName`） |
| 233 | `AddValue` | Buff 百分比效果参数（`PercentEffectValueAttrName`） |
| 143 | `SkillCdTime` | ReduceCoolDown 来源秒数（可与 PreValue=-1 清空 CD） |
| 9 / 131 / 132 / 153 | 各类数值加成 | 直接进非 Module 聚合 |

---

## 2. 数据流总览

```
穿戴武器 (EquippedWeaponId) + 已激活 (Global.PlayerWeapons)
  │
  ├─【数值】WeaponService.GetAttributes()
  │     → BaseAttributes + ExtraAttributes(非 Module)
  │     → SceneInitAttributeAggregation 合并到 gpm.ModuleAttributes
  │
  └─【规则】WeaponService.GetModuleAttributeEntries()
        → ExtraAttributes 中 Category=Module（LearnSkillRule 等）
        → GamePlayManager.ApplyEquipModuleAttributes()
        → AttributeManager.AddBattleSkillRule（SpeakerSkin + SpeakerWeapon_ StackKey，延后触发）
        → 技能/被动初始化完成
        → BattleRuleService.ApplyEquipLearnedRulesToSoldier（含 SpeakerWeapon_ 条目）
        → BattleRuleNewSoldierSyncSupport.ReevaluateRuleOnSoldier
        → BattleRuleBattleEconomySupport.StartBattleWatchers()  // 战斗币/每秒 Computer
```

---

## 3. StackSourceKey 约定

| 来源 | StackSourceKey 格式 |
|------|---------------------|
| 轮语者武器 LearnSkillRule | `SpeakerWeapon_{配置Id}_{星级}_{RuleId}` |

示例：武器 1007、1 星、规则 112095 → `SpeakerWeapon_1007_1_112095`

同 RuleId 的多条规则必须使用 **不同 StackKey**（按 RuleId 区分），避免修饰符栈冲突。

### 3.1 规则参数行

`WeaponStarList[level].BaseAttributes` / `ExtraAttributes` 中，除 `LearnSkillRule` 外可配置 **Category=Add / Skill** 参数属性；运行时经 `SpeakerWeapon_{配置Id}_{星级}_{RuleId}` → `BattleRuleSourceParamSupport` 读取。配表与装备词条相同（见 `BattleRule_SkillSourceBuff_Context.md` §6.0）。

---

## 4. 前置条件（常见「不生效」原因）

`TryGetEquippedLevelConfig` 返回 false 时，武器不产生任何属性/规则：

1. `Global.UserPlayerSpeaker.EquippedWeaponId <= 0`（未穿戴）
2. 武器未激活（`Global.PlayerWeapons` 中无对应实例）
3. 配置表无该 `PlayerWeaponConfig`

**仅拥有、未穿戴 → ExtraAttributes 与 BaseAttributes 均不生效。**

---

## 5. 武器规则清单（2026-07-13）

### 5.1 总表

| 武器 Id | 规则 Id | Trigger | 门槛 | 效果要点 |
|---------|---------|--------|------|----------|
| 1001 | 112095~112114 | NoCondition | DifferentProNum | 不同职业数 → 小队伤害（Attr 135） |
| 1004 | 114051~114053 | NoCondition + Computer | BattleGoldNum | 每消耗 N 货币叠伤害，最多 5 层 |
| 1005 | 114054~114056 | NoCondition + Computer | EverySecond | 每隔 N 秒叠伤害，最多 6 层 |
| 1006 | 114057 | **OnDamageBefore** | MonsterMaxHealth≤30% | 对残血怪当次增伤（Persist=1） |
| 1008 | 114058~114059 | NoCondition | LiveMageNum≥2/4 | 存活法师数门槛 |
| 1009 | 114060~114061 | NoCondition | LiveWarriorNum≥2/4 | 存活战士数门槛 |
| 1101 | 103006~103010 | **OnSkillUse** | TriggerPro | 技能使用后概率清空刚释放技能 CD（非命中） |
| 1102 | 114062 | NoCondition | MainStage | 主线挑战全队增伤 |
| 1103 | 114063 | NoCondition + Computer | EverySecond=30 + IsAssistant=No | 随机 1 名非辅助加 Buff 30027 |
| 1105 | 114064~114066 | NoCondition | BuyEffectsNumInBattle | 局内购买强化种类数 |
| 1106 | 114067 | OnDamageAfter | TriggerPro | 概率冻结（Buff 2070051） |

数值多来自 ExtraAttributes 中 **227**（`SourceAttrType=227, PreValue=1`）。

### 5.2 ComputerGroup：战斗币 / 每秒

| ComputerGroup | CounterType | TriggerType | 语义 |
|---------------|-------------|-------------|------|
| 300008~300010 | BattleGoldCount(9) | BattleGoldNum(8) | 累计消耗货币达 MaxValue |
| 300011~300014 | TimeCount(10) | EverySecond(9) | 累计秒数达 MaxValue |

**枚举值不对齐：** `CounterType` 与 `CounterTriggerType` 在 Custom 之后数值不一致；运行时用 `AreCounterTypesCompatible` **语义配对**，禁止再依赖 `(int)CounterType == (int)TriggerType`。

**累加与触发：**

```
Global.GameGold 减少
  → BattleRuleBattleEconomySupport 监听
  → BattleRuleComputerSupport.OnBattleGoldSpent(amount)
  → ComputerManager.OnCounterEvent(BattleGoldNum, addValueOverride=amount)
  → BattleRuleService.TryTriggerNoConditionRulesForComputerTrigger
  → ResetAfterTrigger：扣减 MaxValue，保留超额（支持一次大额消耗多层）

GamePlayManager.Update（未暂停）
  → BattleRuleBattleEconomySupport.Tick(delta)
  → 每满 1 秒 OnEverySecond → 同上（EverySecond）
```

Owner 解析：`BattleGoldNum` / `EverySecond` / `round` → `GlobalBattleOwnerId`。

### 5.3 新增 SpecialsType

| SpecialsType | 值 | 取值 |
|--------------|----|------|
| LiveMageNum | 19 | 场上存活法师数（`villageGameCards`） |
| LiveWarriorNum | 20 | 场上存活战士数 |
| IsAssistant | 21 | Source 英灵是否辅助（1/0） |
| BuyEffectsNumInBattle | 22 | 本局三选一已选卡牌去重数 |

`ComparisonsType.Yes` / `No`：按左值非零 / 为零判定（1103 非辅助用 `No`）。

购买强化：`SchoolChooseCardService` 选卡 → `RegisterBoughtEffectCard` → `TryTriggerNoConditionRulesForBuyEffects`。

### 5.4 关键规则实现要点

| 规则 | 实现要点 |
|------|----------|
| 1004/1005 | `NoCondition` + Computer；达标后重试 NoCondition（非获得时一次性） |
| 1006 | Trigger=`OnDamageBefore`；`PersistToAttributeSet=1`；伤害管线用来源参数写当次 `AllDamageBonuses` |
| 1101 | Trigger=`OnSkillUse`；`UseSkill` 成功后触发一次；`ReduceCoolDown` 清空本次 `SkillId` CD |
| 1103 | Computer 达标后随机 1 名非辅助，`Source=该英灵` 再 `AddBuff(30027)`；参数 232/233 |
| 1106 | Trigger=`OnDamageAfter`；AddBuff TargetUnitType=`Target` → 受击怪物；模板 `Vertigo` |

### 5.5 案例：武器 1001 / 旧 1007 职业数

| RuleId | Condition | 语义 |
|--------|-----------|------|
| 112095~112114 | DifferentProNum ≥ 1~4（分星级档位） | 入局不同职业数 → Attr 135 |

历史问题（DifferentProNum Provider / 延后施加）见下文 §6 历史修复；链路与装备对齐。

---

## 6. 历史问题与修复

| 日期 | 问题 | 修复 |
|------|------|------|
| 2026-07-01 | ExtraAttributes 未注册 / DifferentProNum 立即失败 | `GetModuleAttributeEntries` + 延后施加 + LineupValueProvider |
| 2026-07-13 | BattleGold/EverySecond 未接线；CounterType 校验误杀 | `BattleRuleBattleEconomySupport` + 语义配对 |
| 2026-07-13 | Live*/BuyEffects/IsAssistant/YesNo 未实现 | ValueProvider + ConditionManager |
| 2026-07-13 | 114057/103006 Trigger 配错 | 改为 OnDamageBefore / OnSkillAfter；214025 Persist=1 |
| 2026-07-13 | 1101 不进 CD 若挂 OnSkillAfter 会每命中一次 | Trigger=`OnSkillUse` + `BattleRuleCastSupport.TriggerSkillAfterUse` |
| 2026-07-13 | 1106 冻结打到英灵上无效 | AddBuff 按 `TargetUnitType` 解析目标；效果组 Target=`Target` |
| 2026-07-13 | ReduceCoolDown 忽略 SourceAttr / 上下文技能 | `ResolveReduceCoolDownSeconds` + 清空当前技能 CD |

---

## 7. 施加时机（与装备对齐）

| 阶段 | 武器 Module 规则行为 |
|------|----------------------|
| `AddBattleSkillRule` | **不** `TryTriggerNoConditionOnAcquire`（`SpeakerWeapon_` 延后） |
| `TriggerPendingNoConditionRulesOnInit` | **跳过** `SpeakerWeapon_` 条目 |
| 技能挂载后 | `ApplyPostSkillInitBattleRulesToVillageSoldiers` → `ApplyEquipLearnedRulesToSoldier` |
| 同上之后 | `BattleRuleBattleEconomySupport.StartBattleWatchers()` |
| 后召唤英灵 | `InitializeNewSoldierSkills` → 同上 Reevaluate |
| 清局 | `ClearBattleLegacySkillState` / `ClearBattleSkillRules` → `Stop` + `ResetBattleState` |

---

## 8. 关键文件

| 文件 | 职责 |
|------|------|
| `WeaponService.cs` | `GetAttributes`、`GetModuleAttributeEntries`、`BuildWeaponStackSourceKey` |
| `BattleRuleBattleEconomySupport.cs` | 战斗币监听、每秒 tick、强化种类计数 |
| `BattleRuleComputerSupport.cs` | `OnBattleGoldSpent` / `OnEverySecond` |
| `BattleRuleComputerManager.cs` | 语义 Counter 配对、超额扣减、addValueOverride |
| `BattleRuleService.cs` | `TryTriggerNoConditionRulesForComputerTrigger` / `ForBuyEffects` |
| `BattleRuleEffectGroupsApplier.cs` | ReduceCoolDown 来源秒数 |
| `BattleRuleEffectApplier.cs` | 伤害管线 ModifyAttr 读规则来源参数 |
| `GamePlayManager.cs` | StartWatchers / Tick / Stop |
| `SchoolChooseCardService.cs` | `RegisterBoughtEffectCard` |
| `BattleRuleAcquireSupport.cs` / `RosterCompositionSupport.cs` | Live*/BuyEffects/IsAssistant |

---

## 9. 日志排查

### 9.1 加载阶段（仅数值）

```
[SceneInitAttributeAggregation] 轮语者武器: SoldierAttackPower=50, SoldierSpellPower=50
[SceneInitAttributeAggregation] 轮语者武器: (无属性)   ← 未穿戴或未激活
```

### 9.2 规则登记 / 施加

```
[GamePlayManager] 轮语者武器 Module 属性生效: count=...
[AttributeManager] 本局新增规则: RuleId=..., StackKey=SpeakerWeapon_..., Source=SpeakerSkin
[BattleRuleNewSoldierSyncSupport] Reevaluate 同步: RuleId=..., StackKey=SpeakerWeapon_...
```

### 9.3 Computer 叠层

- 消耗货币后应出现规则执行日志（`BattleRuleExecutionLogger`）
- Counter 未达标时 NoCondition 重试应跳过（Computer 未达标）

---

## 10. 测试检查清单

### 穿戴与 1001 职业数

- [ ] 激活并穿戴 → BaseAttributes 有日志
- [ ] 编队不同职业数达标 → 112095+ Reevaluate

### 新增规则

- [ ] 1004：累计消耗达阈值叠层；一次大额消耗可多层
- [ ] 1005：挂机满秒数叠层，受 MaxTriggerNum 限制
- [ ] 1006：打 ≤30% 血怪物有当次增伤
- [ ] 1008/1009：存活法师/战士数达标
- [ ] 1101：放技能后概率 CD 被清
- [ ] 1102：主线关卡全队增伤
- [ ] 1103：每 30 秒随机非辅助获 Buff 30027
- [ ] 1105：购买强化达种类数后增伤
- [ ] 1106：攻击概率上冻结 Buff

### 清理

- [ ] 重开/清局后货币监听与强化计数重置

---

## 11. 配置路径

- `Assets/Res/Config/Json/tbplayerweaponconfig.json`
- `Assets/Res/Config/Json/tbskillruleconfig.json`
- `Assets/Res/Config/Json/tbconditiongroupconfig.json`（1031~1044、Live*/BuyEffects/MainStage）
- `Assets/Res/Config/Json/tbcomputergroupconfig.json`（300008~300014）
- `Assets/Res/Config/Json/tbeffectgroupconfig.json`（212034~、214023~、203006、2070051）
- `Assets/SkillGraph/SkillBuffTemplateLibrary.asset`（Buff 30027、2070051）
