# BattleRuleService AI 上下文

> 供后续 AI 对话或开发接续使用。  
> 最后更新：2026-07-13  
> 详细策划文档见同目录 `README_BattleRuleSystem.md`  
> Buff 专项见 `BattleRule_BuffEffect_Context.md`  
> **链式学习来源参数（技能/装备/流派/轮语者，StackSourceKey → 动态数值）** 见 `BattleRule_SkillSourceBuff_Context.md`  
> **装备 LearnSkillRule / GloballyEffective / 月影祝福 / 词条参数** 见 `BattleRule_EquipLearnSkillRule_Context.md`  
> **流派卡英雄专属（IsAddHero / ScopeHeroCardId）** 见 `BattleRule_HeroScopedSchoolCard_Context.md`  
> **轮语者武器 ExtraAttributes / 战斗币·每秒 Computer** 见 `Assets/Scripts/Game/Player/WeaponExtraAttributes_Context.md`

**线路状态：ComputerGroup 计数系统已接入并收尾（2026-06-10）；BattleGoldNum / EverySecond 已接线（2026-07-13）。**  
奥数卡 / 流派卡原有 `LearnSkillRule` 规则不受影响；仅 `ComputerGroupId` 非空的规则走计数门槛。

---

## 1. 服务定位

`BattleRuleService` 是本局 **SkillRuleConfig** 的运行时调度中心，注册于 `BattleServiceManager`。

| 项 | 值 |
|----|-----|
| 文件 | `Assets/Scripts/Battle/Rules/BattleRuleService.cs` |
| 基类 | `GameServiceBase` |
| 规则来源 | `AttributeManager.BattleLearnedSkillRules` |
| 学习入口 | `AttributeType.LearnSkillRule(123)` → `AddBattleSkillRule(ruleId, stackSourceKey, sourceType, scopeHeroCardId)` |
| 英雄专属 | `scopeHeroCardId` = `SchoolCardConfig.IsAddHero`；见 `BattleRuleHeroScopeSupport.cs` |

---

## 2. 核心数据结构

| 字段 | 类型 | 说明 |
|------|------|------|
| `mOwnerRuleMap` | `Dictionary<int, List<int>>` | OwnerId → 规则 Id 列表 |
| `mOwnerBindings` | `Dictionary<int, OwnerRuleBinding>` | Owner 绑定的 ValueProvider + EffectExecutor |
| `mRuleRuntimeStates` | `Dict<RuleRuntimeKey, RuleRuntimeState>` | 每条规则的触发次数、上次触发时间 |
| `mComputerManager` | `BattleRuleComputerManager` | ComputerGroup 计数、窗口判定、触发后重置 |

全局战斗 OwnerId：`BattleRuleDamageSupport.GlobalBattleOwnerId`

---

## 3. 触发链路（单条规则）

```
TriggerXxx / TriggerLearnedBattleRules / TriggerForOwner
    ↓
BuildContext（补 Trigger / OwnerId / Time / Provider / Executor）
    ↓
CanTrigger（MaxTriggerNum + CoolDown）
    ↓
PassProbability（TriggerPro，兼容 0~1 与 0~100）
    ↓
ConditionManager.AreAllConditionsMet（条件组 AND）
    ↓
ComputerManager.AreComputerGroupsMet（ComputerGroup AND；空列表跳过）
    ↓
EffectManager.ExecuteEffects（效果组列表）
    ↓
ComputerManager.ResetAfterTrigger（ResetType.AfterTrigger）
    ↓
MarkTriggered（记录次数与时间）
```

私有方法 `TryExecuteRule` 封装上述完整流程；`TriggerForOwner` 内联相同步骤但遍历 Owner 规则列表。

### ComputerGroup 两阶段模型

`SkillRule.Trigger` 与 `ComputerGroup.TriggerType` **可不同**（如 108002）：

1. **累加阶段**：`BattleRuleComputerSupport.OnXxx` → `ComputerManager.OnCounterEvent`
2. **判定阶段**：`TryExecuteRule` → `AreComputerGroupsMet` → 达标才执行 `EffectGroupId`

计数键：`(ownerInstanceId, computerGroupId)`；窗口由 `CounterTime` + `TriggerTimes` 队列实现。

### SkillRuleConfig 三组 ID

| 字段 | 职责 |
|------|------|
| `ConditionGroupId` | 传统条件（血量、职业数量、属性比较等），AND |
| `ComputerGroupId` | 计数门槛（时间窗口 + 阈值），AND；**空列表跳过** |
| `EffectGroupId` | 达标后执行的效果组 |

### ComputerGroupConfig 字段

| 字段 | 说明 |
|------|------|
| `CounterType` | 计数槽位语义（Kill/Attack/BeHit 等，Luban `Config.CounterType`） |
| `CounterTime` | 时间窗口（秒）；`>0` 用窗口计数，否则用累计 `Count` |
| `OwnerType` | 计数归属（见下节） |
| `TriggerType` | **何时 +AddValue**（Luban `Config.CounterTriggerType`） |
| `AddValue` | 每次累加值 |
| `MaxValue` | 触发效果所需阈值 |
| `ResetType` | `Never` / `AfterTrigger` / `BattleEnd` / `TimeOut` / `BuffRemove` |
| `ResetParam` | `TimeOut`：无活动超时秒数；`BuffRemove`：BuffId（0=任意）；`Custom`：自定义事件 Id |

### OwnerType 归属语义（配表必读）

`OwnerType` **不随 Trigger 自动切换**，策划按「计在谁身上」显式配置。  
累加（`TriggerType`）与判定（`SkillRule.Trigger`）**共用同一 `OwnerType`**。

| OwnerType | 解析对象 | 典型场景 |
|-----------|----------|----------|
| `Self (0)` 自己 | `SoldierCard`（英灵/玩家方） | 受击次数、血量相关、技能释放 |
| `Self + KillCount`（全局规则） | `GlobalBattleOwnerId` | 怪物猎人等全队共享击杀数 |
| `Self + KillCount`（`ScopeHeroCardId>0`） | 击杀者 `SoldierCard.GetInstanceID()` | 英雄专属流派卡击杀计数 |
| `Target (1)` 目标 | `MonsterCard`（怪物） | 普攻命中、暴击、击杀 |
| `Pair (2)` 组合 | Source + Target 实例 Id 哈希 | 某对「英灵-怪物」独立计数 |

各事件上下文约定（`BattleRuleComputerSupport` / `BattleRuleHitTrigger`）：

| 事件 | Source | Target | Self → | Target → |
|------|--------|--------|--------|----------|
| 受击 `OnBeHit` | 攻击怪物 | 受击英灵 | 英灵 | 攻击怪物 |
| 伤害命中后 `OnDamageAfter` | 攻击英灵 | 被击怪物 | 英灵 | 被击怪物 |
| 生命值修改 `HpChange` | 受伤英灵 | 受伤英灵 | 英灵 | 攻击怪物（有 attacker 时） |
| 技能释放 `OnSkillCast` | 施法英灵 | 施法英灵 | 英灵 | 无怪物则解析失败 |

配表示例：

```
三重绝刃 12111 → 108002（2 秒内对同一敌人连续 3 次伤害，追加 10% 最大生命真实伤害）：
  SchoolCard.StarList.Attributes: LearnSkillRule(115) → 108002
  ComputerGroup.TriggerType = AttackCount（伤害管线前累加，普攻/技能均计）
  ComputerGroup.OwnerType   = Pair            ← 英灵-怪物对
  ComputerGroup.MaxValue    = 3, CounterTime = 2
  SkillRule.Trigger         = OnDamageBefore  ← AddDamage 须在伤害管线内执行

某怪物被普攻 5 次后触发：
  ComputerGroup.TriggerType = AttackCount
  ComputerGroup.OwnerType   = Target        ← 计在怪物
  SkillRule.Trigger         = OnDamageAfter
```

### 与旧规则兼容性

- 配表 46 条规则中仅 `108002` 含 `ComputerGroupId`；其余 `ComputerGroupId: []` → `AreComputerGroupsMet` 直接通过，行为与接入前一致。
- 奥数卡 / 流派卡仍走 `LearnSkillRule` → `BattleRuleService`，不经过已删除的 `SkillRuleSystem` 双轨。

---

## 4. 公开 API 速查

| 方法 | 场景 |
|------|------|
| `InitializeAsync` | 加载表、注册全局规则、补触发 NoCondition |
| `TryTriggerNoConditionOnAcquire(ruleId)` / `(entry)` | 学习规则时立即触发 Trigger=NoCondition（`entry` 含英雄专属） |
| `TriggerPendingNoConditionRulesOnInit` | 服务初始化后补触发已学 NoCondition |
| `ApplyLearnedRulesToNewSoldier(soldier)` | 新英灵入队，按 `NewSoldierSync` 补同步（跳过非本英灵的 `ScopeHeroCardId`） |
| `TriggerLearnedBattleRules(trigger, context)` | 遍历 `BattleLearnedSkillRules` 条目，按 Trigger + `ScopeHeroCardId` 过滤 |
| `RegisterOwnerRules(ownerId, ruleIds, ...)` | 注册单位规则 + 绑定 Provider/Executor |
| `UnregisterOwner(ownerId)` | 注销并清运行态 |
| `TriggerForOwner(ownerId, trigger, context)` | 触发指定 Owner 某时机全部规则 |
| `TriggerForAll(trigger, sharedContext)` | 遍历所有 Owner（每 Owner 克隆 context） |
| `TriggerRuleById(ownerId, ruleId, context)` | 定向触发（调试/强制） |
| `OnRuleEffectTriggered` | 效果执行事件（透传 EffectManager） |
| `RefreshComputerGroupIndex()` | 本局已学规则变化后刷新 ComputerGroup 索引 |
| `ClearComputerCounters()` | 清空全部运行时计数（新局/局内重开） |
| `TryTriggerNoConditionRulesForComputerTrigger` | Computer 累加后重试带对应门槛的 NoCondition（武器金/秒） |
| `TryTriggerNoConditionRulesForBuyEffects` | 局内购买强化种类变化后重试 BuyEffects 条件规则 |
| `ComputerManager` | 计数管理器（只读访问） |

---

## 4.1 ComputerGroup 接线（已实现）

| CounterTriggerType | 门面方法 | 接线文件 | 状态 |
|--------------------|----------|----------|------|
| BeHitCount | `OnBeHit` | `SoldierDamageReceiver` | 已接 |
| AttackCount（对目标造成伤害） | `AccumulateOutgoingDamageCount` | `BattleRuleDamageModifier`（OnDamageBefore 前） | 已接 |
| CriticalCount | `OnOutgoingHit` | `MonsterDamageReceiver`（命中后） | 已接 |
| KillCount | `OnKill` | `MonsterDamageReceiver`（hp<=0） | 已接 |
| SkillCastCount / ComboCount | `OnSkillCast` | `SoldierSkillExecutor` | 已接 |
| Custom | `OnCustomCounter(customEventId, context)` | 业务层按需调用 | 已接 |
| **BattleGoldNum** | `OnBattleGoldSpent(amount)` | `BattleRuleBattleEconomySupport` 监听 `GameGold` | **已接（2026-07-13）** |
| **EverySecond** | `OnEverySecond` | `BattleRuleBattleEconomySupport.Tick` ← `GamePlayManager.Update` | **已接（2026-07-13）** |
| round | `OnRoundStart` | `BattleRuleRoundStartTrigger` | 已接 |
| BuffRemove 重置 | `OnBuffRemoved` | `BuffController.RemoveBuff` | 已接 |
| 怪物离场清理 | `OnMonsterRemoved` | `MonsterCard.Die` | 已接 |
| 英灵复活清理 | `OnOwnerRevived` | `SoldierCard.ReviveSoldier` | 已接 |

**CounterType ↔ CounterTriggerType 语义配对（勿再比 int）：**

| CounterType | CounterTriggerType | 说明 |
|-------------|-------------------|------|
| BattleGoldCount(9) | BattleGoldNum(8) | 消耗战斗币 |
| TimeCount(10) | EverySecond(9) | 每秒计时 |
| Custom(8) | Custom(7) | 自定义事件 |
| round(7) | round(10) | 回合 |

`BattleGoldNum` / `EverySecond` / `round` 的 Self Owner → `GlobalBattleOwnerId`。

**NoCondition + Computer：** 累加后调用 `TryTriggerNoConditionRulesForComputerTrigger`（仅重试带对应 Computer 的 NoCondition）。  
`BattleGold`/`EverySecond` 的 `AfterTrigger`：**扣减 MaxValue、保留超额**（支持一次大额消耗多层）。

**ResetType 运行时：**

| ResetType | 行为 | 触发点 |
|-----------|------|--------|
| `Never` | 不主动清零 | — |
| `AfterTrigger` | 效果执行后清零（金/秒：扣阈值） | `TryExecuteRule` |
| `BattleEnd` | 仅清除该类型条目 | `BattleRuleService.Reset` / `ClearBattleLegacySkillState` |
| `TimeOut` | `ResetParam` 秒无新累加则清零 | 累加/判定时惰性检查 |
| `BuffRemove` | 指定 Buff 移除时清零 | `BuffController.RemoveBuff` |

**计数清理：**

| 场景 | 调用 |
|------|------|
| 新局 `InitCardTeams` | `ClearBattleSkillRules` → `ClearComputerCounters` + `BattleRuleBattleEconomySupport.ResetBattleState` |
| 技能挂载后 | `StartBattleWatchers` |
| 局内重开 `ClearBattleLegacySkillState` | `StopBattleWatchers` + `Reset` + `ClearComputerCounters` |
| 切场景销毁 | `BattleRuleService.OnDestroy` → `ClearAll` |
| 怪物死亡 | `OnMonsterRemoved`（不影响同帧 `OnKill` 累加） |

Receiver / 经济门面只调 `BattleRuleComputerSupport` 一行，不写业务判断。

---

## 5. 两类 EffectExecutor

| 执行器 | 用途 | 典型 Trigger |
|--------|------|--------------|
| `BattleRuleDamageEffectExecutor` | `AddDamage`；**白名单**即时改 `DamageContext`（穿透/AddMagic）；其余 `ModifyAttr` 写 `AttributeSet` | OnDamageBefore / OnAttackBefore / OnSkillBefore / OnCriticalBefore |
| `BattleRuleAcquireEffectExecutor` | Buff、英灵/模块 `AttributeSet` 持久属性（含 112012 叠层） | NoCondition / OnDamageAfter / HpChange / **RoundStart** |

接入文件：
- 伤害前：`BattleRuleDamageSupport.cs` → `BattleRuleDamageModifier`
- 命中后：`BattleRuleHitTrigger`
- HpChange：`BattleRuleHpChangeSupport.cs` → `SoldierCard.TakeDamage` 扣血后
- RoundStart：`BattleRuleRoundStartSupport.cs` → `OnInitComplete`（首日）、`EndDay`（换波）；详见 `BattleRule_RoundStart_2150102_Context.md`
- 获得时：`BattleRuleAcquireSupport.cs`

---

## 6. 与伤害系统关系

```
MonsterCard.TakeDamage
  → [前置] SkillDamage / SkillFireDamage 加成（非规则系统）
  → DamageContextBuilder.FillOutgoingContext
  → DamagePipeline.Execute
       1. BattleRuleDamageModifier  ← 规则：伤害前改上下文
       2. AttributesModifier        ← 旧 skillCarrying
       ...
  → MonsterDamageReceiver.Apply
  → BattleRuleComputerSupport      ← ComputerGroup 累加（Attack/Crit/Kill）
  → BattleRuleHitTrigger           ← 规则：命中后（含 OnDamageAfter + Computer 判定）
```

英灵受击：`SoldierDamageReceiver` → `BattleRuleComputerSupport.OnBeHit`（BeHitCount 累加）。

规则伤害加成写入 `DamageContext`，**不累加** `skillCarrying`，避免跨命中叠乘。

---

## 7. 新英灵同步（NewSoldierSync）

`ApplyLearnedRulesToNewSoldier` 在 `GamePlayManager.AddSoldierCard` 后调用，遍历 `BattleLearnedSkillRuleIds`，按配表 `SkillRuleConfig.NewSoldierSync` 分发（`BattleRuleNewSoldierSyncSupport`）：

| NewSoldierSync | 值 | 行为 | 典型规则 |
|----------------|---|------|----------|
| `None` | 0 | 不同步 | 108002 三重绝刃 |
| `Once` | 1 | 补 1 次效果 | 阵容职业数量、持久附加物伤 |
| `ByTriggerCount` | 2 | 按 `GetRuleTriggerCount` 重放 N 层，`ModifyAttr` 累加 | 112012 怪物猎人 |
| `Reevaluate` | 3 | 按新英灵当前状态重判条件后补 | NoCondition Buff、HpChange 满血 Buff |

- `ByTriggerCount` 使用 `AddModifier(..., isAdditive: true)`，sourceKey 为 `BattleRuleAcquire_{ruleId}`
- **跳过**模块类属性（LockAtkInterval 等，避免全场重复）
- 镜像英灵（`SoldierRoleType.Mirror`）和已死亡单位直接 return

详见 `BattleRule_RepeatableAcquire_Context.md`。

---

## 8. 配置表

| 表 | JSON 路径 |
|----|-----------|
| SkillRuleConfig | `Assets/Res/Config/Json/tbskillruleconfig.json`（含 `NewSoldierSync` 后上场同步策略） |
| ConditionGroupConfig | `Assets/Res/Config/Json/tbconditiongroupconfig.json` |
| ComputerGroupConfig | `Assets/Res/Config/Json/tbcomputergroupconfig.json` |
| EffectGroupConfig | `Assets/Res/Config/Json/tbeffectgroupconfig.json` |

---

## 9. 关联代码文件

| 文件 | 职责 |
|------|------|
| `BattleRuleService.cs` | 注册、触发、运行态 |
| `BattleRuleComputerManager.cs` | ComputerGroup 计数与窗口判定 |
| `BattleRuleComputerSupport.cs` | 计数事件门面（Receiver / 战斗币 / 每秒接线） |
| `BattleRuleBattleEconomySupport.cs` | 战斗币监听、每秒 tick、强化种类计数 |
| `BattleRuleCounterData.cs` | 单条计数运行时数据 |
| `BattleRuleConditionManager.cs` | 条件比较 |
| `BattleRuleEffectManager.cs` | 效果组分发 |
| `BattleRuleEffectApplier.cs` | 伤害上下文效果 |
| `BattleRuleAcquireSupport.cs` | 获得时执行器 |
| `BattleRuleHpChangeSupport.cs` | HpChange 触发 |
| `BattleRuleRoundStartSupport.cs` | RoundStart 触发（祈祷被动等） |
| `BattleRuleLearnBuffEffectApplier.cs` | AddBuff → `BuffApplier`（读 SkillBuffTemplateLibrary） |
| `BattleRuleRemoveBuffApplier.cs` | RemoveBuff（BaseValue = BuffId，不查 EffectsConfig） |
| `BattleRuleAcquireSoldierEffectApplier.cs` | NoCondition 英灵属性；`ApplyEffectToAttributeSet` |
| `BattleRuleEffectParamUtil.cs` | `BuildModifier`：`PreValue`/`SourceAttrType` → `AttributeModifier` |
| `BattleRuleCastDamageSession.cs` | 施法临时 AttributeSet 修饰（Register/Remove） |
| `BattleRuleNewSoldierSyncSupport.cs` | `NewSoldierSync` 后上场同步；`BuildSourceKey` / `BuildDamagePipelineSourceKey` |
| `CareerDamageModifier.cs` | 职业系伤害修正 52~56：SoldierDamage 管线按 `CarType` 增伤 |
| `BattleRuleSoldierHealthConditionValues.cs` | 生命比例条件 |
| `AttributeManager.cs` | 学习规则、同步服务 |

---

## 10. 开发约束（项目规范）

- 日志用 `LogTool`，禁止 `Debug.Log`
- 禁止 `?.` 空条件运算符
- 事件绑定用具名方法，解绑时 RemoveListener 同一方法
- 新旧规则不要双重真值源；新功能走 `LearnSkillRule` + SkillRuleConfig
- UILevel 若混用 QFramework / UIFramework，写全 `UIFramework.UILevel.xxx`
- **EffectGroup AddBuff/RemoveBuff**：`BaseValue` = `SkillBuffTemplateLibrary.BuffId`，**不走** `TbEffectsConfig`
- **EffectGroup ModifyAttr + PreValue**：持久英灵/技能属性写 `AttributeSet`（`PercentAdd`/`FlatAdd`），由 `GetFinal` 统一结算 `(Base × (1+ΣPercent)) + ΣFlat`；**勿**在伤害管线对同一属性反复叠乘 `DamageContext`
- **PreValue 配表**：纯百分比加成时 `SourceAttrType = 201 (Null)`；同属性误配时代码兜底走 `PercentAdd`（见 `EffectGroup_EffectGroupsType_Implementation.md` §3.2.1）
- **`PersistToAttributeSet`**：0=持久写 AttributeSet，1=临时仅当次伤害（见 `EffectGroup_EffectGroupsType_Implementation.md` §2.1）
- **职业系伤害修正 52~56**：持久 `ModifyAttr` 写入后，由 `CareerDamageModifier` 在 SoldierDamage 管线按攻击方 `CarType` 读取增伤
- **怪物猎人 112012**：`300002.MaxValue=10`，`212012.SourceAttrType=201`，详见 `BattleRule_RepeatableAcquire_Context.md` §10

---

## 11. Buff 规则速查（2026-06-08）

| 卡牌 | 规则 | BuffId | 说明 |
|------|------|--------|------|
| 2203001 全力出击 | 111003 + 111004 + 112010 | 20014 | 满血加 / 受伤删 / 获得时补加 |
| 2202006 黎明守护 | 111005 | 21004 | 首次 ≤50% 血，5 秒内每秒回 5% MaxHp |

详见 `BattleRule_BuffEffect_Context.md`。

---

## 12. 测试要点

### 回归（旧规则，ComputerGroupId 为空）

- 10301：技能前 50% 增伤，不跨命中累加
- 10701/10702：穿透%、吸血
- 11101/11102：满血学 Buff / 受伤删 Buff（20012）
- 111003/111004/112010：2203001 满血 Buff 20014 加/删/入队补加
- 111005：2202006 低血 HealBuff 21004
- 11103/11104：NoCondition 全队属性 + 新英灵补规则
- 108001（无 ComputerGroup）：与接入前一致
- 冷却、最大次数、镜像英灵不触发

### ComputerGroup 专项（12111 / 108002 / 300001）

| 步骤 | 预期 |
|------|------|
| 选取流派卡 12111 | `LearnSkillRule` → `BattleLearnedSkillRuleIds` 含 108002 |
| 2 秒内对同一怪物造成 3 次伤害 | `300001` Pair 计数达标 |
| 第 3 次伤害 OnDamageBefore | Computer 通过 → `208002` 追加目标 MaxHp×10% 真实伤害 |
| 效果触发后 | `ResetType.AfterTrigger` 清零该 Pair 计数 |
| 换一只怪或超 2 秒窗口 | 计数不足，不触发 |

### 怪物猎人（112012 / 300002 / 212012）

| 步骤 | 预期 |
|------|------|
| 累计击杀 9 只 | 不触发，面板无变化 |
| 第 10 杀 OnDamageAfter | Computer 达标 → 全队攻/法/攻速 `PercentAdd +0.05` |
| 第 20 杀 | 再 +0.05，累计约 ×1.10 |
| 新英灵入队 | `ByTriggerCount` 补满与全局相同的叠层数 |

---

## 13. PreValue / AttributeSet 变更说明（2026-06-17）

| 项 | 变更 |
|----|------|
| 英灵/技能 `ModifyAttr` | 伤害管线中（除穿透/AddMagic 等白名单）改写 `AttributeSet`，不再对 `DamageContext` 叠乘 `PreValue` |
| `BattleRuleCastDamageSession` | 施法临时修正写 AttributeSet，施法结束 `RemoveModifier` |
| `BuildModifier` | `SourceAttrType == AttributeType` 时 `PreValue` → `PercentAdd` |
| 配表修复 | 300002 `MaxValue: 10`；212012 `SourceAttrType: 201` |

详见 `EffectGroup_EffectGroupsType_Implementation.md`、`BattleRule_RepeatableAcquire_Context.md` §10。

---

## 14. PersistToAttributeSet / 职业系伤害修正（2026-06-17）

| 项 | 说明 |
|----|------|
| `EffectGroupConfig.PersistToAttributeSet` | **0** 持久 → AttributeSet；**1** 临时 → 仅当次 `DamageContext` / 施法会话 |
| 职业属性 52~56 | `SoldierDamageChange` / `ArcherDamageChange` / `MageDamageChange` / `AssassinDamageChange` / `AssistantDamageChange` |
| 伤害读取 | `CareerDamageModifier`（`AttributesModifier` 之后）：攻击方 `CarType` 匹配时 `FinalDamage`、`TrueDamage` × `(1 + GetFinal)` |
| 配表建议 | 职业增伤规则用 `PersistToAttributeSet=0` + `ModifyAttr` + `SourceAttrType=201` + `PreValue` |

详见 `EffectGroup_EffectGroupsType_Implementation.md` §2.1、§8。

---

## 15. 本条线路收尾说明（2026-06-10）

### 已完成

- `BattleRuleComputerManager`：累加 / 窗口判定 / `CounterType` 校验 / `TimeOut` / `BuffRemove` / `BattleEnd` / 实体离场清理
- `BattleRuleComputerSupport`：Receiver 门面 + Custom / Buff / 复活 / 怪物离场
- `BattleRuleService.TryExecuteRule` / `TriggerForOwner` 插入 Computer 步骤；`ClearComputerCounters`
- `AttributeManager.ClearBattleSkillRules` / `GamePlayManager.ClearBattleLegacySkillState` 计数清理
- 删除 `SkillRuleSystem` 双轨（`Assets/Scripts/Battle/SkillRule/` 已移除）

### 待策划配表 / 文档

- [x] 12111 三重绝刃链：`BattleRule_ComputerGroup_12111_Context.md`
- [ ] Luban `README_BattleRuleSystem.md` 同步补充 `ComputerGroupId` 章节
- [x] 英雄专属流派卡：`BattleRule_HeroScopedSchoolCard_Context.md`（IsAddHero + ScopeHeroCardId）
- [ ] 其他业务 ComputerGroup（如击杀 5 次加属性）配表与验收

---

## 16. 上下文文件索引（本模块）

| 文件 | 说明 |
|------|------|
| `README_BattleRuleSystem.md` | 策划/开发完整文档 |
| `BattleRuleService_Context.md` | 本文档（AI 速查） |
| `BattleRuleService_ExecutionLog_Context.md` | **规则执行成功日志（时刻/属性/规则/效果组）** |
| `BattleRule_RepeatableAcquire_Context.md` | **可叠层获得规则（112012 vs 108002）；怪物猎人错配 §10** |
| `BattleRule_ComputerGroup_12111_Context.md` | **12111/108002/300001/208002 配表链与常见错配** |
| `BattleRule_EquipLearnSkillRule_Context.md` | **装备/武器 LearnSkillRule、GloballyEffective、月影祝福** |
| `BattleRule_HeroScopedSchoolCard_Context.md` | **流派卡 IsAddHero、ScopeHeroCardId、专属击杀规则** |
| `BattleRule_BuffEffect_Context.md` | **Buff Add/Remove、2203001/2202006、BuffApplier** |
| `BattleRule_SkillSourceBuff_Context.md` | **链式学习来源参数：SkillContext / SourceParamSupport、*AttrName、SourceAttrType** |
| `BattleRuleSourceParamSupport.cs` | **装备/流派/轮语者配置型参数解析实现** |
| `BattleRule_RoundStart_2150102_Context.md` | **祈祷被动 RoundStart、113002/300007/20015 配表链** |
| `EffectGroup_EffectGroupsType_Implementation.md` | **EffectGroup 类型、PreValue/AttributeSet 语义** |
| `../Game/Player/WeaponExtraAttributes_Context.md` | **轮语者武器 ExtraAttributes、BattleGold/EverySecond、Live*/BuyEffects** |
| `Assets/Scripts/Game/Skill/Config/SkillBuffTemplateLibrary_README.md` | Buff 模板库说明 |

> `Assets/Scripts/Battle/SkillRule/` 已删除，计数统一由 `BattleRuleComputerManager` 承载。
