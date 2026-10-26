# 祈祷被动（2150102）RoundStart 规则 AI 上下文

> 供后续 AI 对话或开发接续使用。  
> 最后更新：2026-07-17  
> 关联：`BattleRule_BuffEffect_Context.md`、`BattleRuleService_Context.md`、`README_BattleRuleSystem.md`、`BattleRule_FusionGem_DeepRooted_10000023_Context.md`

---

## 1. 功能概述

伊莱娅被动 **2150102「祈祷」** 通过 `LearnSkillRule` 学习规则 **113002**，在**每波（天）开始**时给**全体存活友方英灵**施加 Buff **20015**（暴击律动）。

| 项 | 说明 |
|----|------|
| 触发时机 | `TriggerType.RoundStart`（**14**；旧笔记曾误写 13，13 为 HpChange） |
| 生效范围 | `GloballyEffective=1` → 全队 |
| 新英雄补同步 | `NewSoldierSync=0`（不同步；波中加入的英雄不补当前波 Buff） |
| 旧实现 | `PraySkill.DoRandomSoldierFortify()` 已废弃，不再走随机单人强化 |
| 同类融合石 | 根深蒂固 10000023 → 规则 113007 → AddBuff 30031，见 `BattleRule_FusionGem_DeepRooted_10000023_Context.md` |

---

## 2. 配表链路

```
tbspecialeffectsconfig.json   Id=2150102（祈祷被动）
  ActsOnCard=2050001（伊莱娅）
  PassiveSkill=1
  LevelPropertyList[0].Attributes[]
    Type=123 (LearnSkillRule)
    Value=113002
        │
        ▼
tbskillruleconfig.json        Id=113002
  Trigger=14 (RoundStart)
  GloballyEffective=1
  ComputerGroupId=[300007]
  EffectGroupId=[210003]
  ConditionGroupId=[]
  TriggerPro=1
  NewSoldierSync=0
        │                    │
        ▼                    ▼
tbcomputergroupconfig.json     tbeffectgroupconfig.json
  Id=300007                      Id=210003
  CounterType=6 (Round)            EffectType=0 (AddBuff)
  TriggerType=6 (round)            BaseValue=20015
  MaxValue=1
  ResetType=1 (AfterTrigger)
        │
        ▼
SkillBuffTemplateLibrary.asset
  BuffId=20015「暴击律动」
  AttrTypeName=SoldierNumOfAttackSpeed
  PercentEffectValue=0.3（攻速 +30%）
  Duration=5
```

### 2.1 学习入口（2150102）

| 字段 | 值 | 说明 |
|------|-----|------|
| `Attributes[].Type` | **123** | `AttributeType.LearnSkillRule`，`Category=Module`（`tbattributeconfig.json`） |
| `Attributes[].Value` | **113002** | 规则 Id |
| 代码路径 | `AddSkillCard` → `SoldierSkillComponent.LearnSkill` → `SkillBase.Initialize` → `AttributeManager.AddBattleSkillRule` |
| StackSourceKey | `2150102` | `SourceType=Skill` |
| ScopeHeroCardId | **0** | 普通被动解锁路径不带英雄专属 scope |

**英雄专属被动路径**（流派卡 `IsAddHero`）：`LearnPassiveSkillDirectToSoldier` → `ApplyPassiveLearnSkillRules`，带 `ScopeHeroCardId=2050001`。  
`GloballyEffective=1` 的 RoundStart 规则在 scope 判定时改为检查**场上是否有该英雄存活**，不要求 `context.Source` 为该英雄。

### 2.2 规则（113002）

| 字段 | 值 | 枚举 | 说明 |
|------|-----|------|------|
| `Trigger` | **14** | `RoundStart` | 波次/回合开始；**不是** `NoCondition`；**不是** HpChange(13) |
| `GloballyEffective` | **1** | — | 满足后对全队施加 Buff |
| `ComputerGroupId` | [300007] | — | 回合计数门槛；空数组则跳过计数 |
| `EffectGroupId` | [210003] | — | AddBuff 20015 |
| `ConditionGroupId` | [] | — | 无额外条件 |
| `TriggerPro` | 1 | — | 100% 触发 |
| `MaxTriggerNum` | 0 | — | 不限次数（每波一次） |
| `NewSoldierSync` | 0 | `None` | 波中新增英雄**不**补当前波 Buff |

### 2.3 计数（300007）

| 字段 | 值 | 枚举 | 说明 |
|------|-----|------|------|
| `CounterType` | **6** | `Round` | 须与 `TriggerType` 一致 |
| `TriggerType` | **6** | `round` | 由 `BattleRuleComputerSupport.OnRoundStart` 累加 |
| `OwnerType` | **0** | `Self` | Round 类型映射为 `GlobalBattleOwnerId`（全局波次计数） |
| `MaxValue` | **1** | — | 每波 +1 后达标；**禁止配 100** |
| `AddValue` | 1 | — | 每波开始 +1 |
| `ResetType` | **1** | `AfterTrigger` | 效果成功后清零，下一波重新累计 |

**常见配错：**

| 错误配法 | 后果 |
|----------|------|
| `MaxValue=100` | 每波只 +1，永远 `Computer 未达标`，规则不生效 |
| `Trigger≠RoundStart` 却走 `EndDay` 触发 | 时机与配表不一致 |
| `EffectGroup BaseValue` 写成 `TbEffectsConfig.Id` | AddBuff 失败；须为 `SkillBuffTemplateLibrary.BuffId` |
| 仍依赖 `PraySkill.OnEnable` | 旧逻辑已注释，随机单人强化不会执行 |

### 2.4 Buff（20015）

| 字段 | 值 | 说明 |
|------|-----|------|
| 名称 | 暴击律动 | `SkillBuffTemplateLibrary.asset` |
| 属性 | `SoldierNumOfAttackSpeed` | 攻速值 |
| `PercentEffectValue` | 0.3 | +30% 攻速（模板 fallback 常量） |
| `PercentEffectValueAttrName` | （空） | 若需随被动等级变化，须配技能属性并填此处 |
| `Duration` | 5 | 持续 5 秒 |
| `Stack` | 5 | 最大叠层（模板配置） |

**动态数值（2026-07-06）：** 规则触发时 `StackSourceKey=2150102` → 绑定伊莱娅 `SkillContext`；`*AttrName` 可读技能等级属性。伊莱娅阵亡则**不释放**。

**链式学习参数（2026-07-09）：** 装备/流派/轮语者等同理，见 `BattleRuleSourceParamSupport`（`BattleRule_SkillSourceBuff_Context.md` §6.0）。

---

## 3. 运行时序

### 3.1 首日（第 1 波）

```
SceneInitCardTeams
  → InitCardTeams
  → AddSkillCard(2150102) → LearnSkillRule 113002 登记

SceneInitStartSpawning
  → InitFirstDay → EndDay（isDayInProgress=false，不触发 RoundStart）

BattleSceneLifecycle
  → BattleRuleService.InitializeAsync（规则已登记）

OnInitComplete
  → StartGame()（isDayInProgress=true）
  → BattleRuleRoundStartTrigger.Trigger()   ← 首日固定入口
       ① BattleRuleComputerSupport.OnRoundStart（round 计数 +1）
       ② BattleRuleService.TriggerLearnedBattleRules(RoundStart)
            AreComputerGroupsMet([300007])   ← current >= 1
            ExecuteEffects([210003])         ← AddBuff 20015 × 全队
            ResetAfterTrigger                ← 计数清零
```

### 3.2 战中换波（第 2 波起）

```
怪物刷完 → 倒计时结束 → EndDay()
  → Global.Date++
  → isDayInProgress==true
  → BattleRuleRoundStartTrigger.Trigger()   ← 换波入口
       （同上 ①②）
  → StartGame()（下一波继续）
```

要点：

1. **累加在 ①，判定在 ②**（与 12111 ComputerGroup 相同原则）。
2. 首日**不能**只靠 `EndDay`：`InitFirstDay` 时 `isDayInProgress=false`。
3. 首日**不能**只靠 `BattleRuleService` 初始化补触发：与 `InitCardTeams` 存在竞态，规则可能尚未登记。

---

## 4. 代码接线清单

| 职责 | 文件 | 方法 / 类型 |
|------|------|-------------|
| 规则学习 | `SkillBase.cs` | `Initialize` → `AddBattleSkillRule` |
| 被动专属学习 | `GamePlayManager.cs` | `ApplyPassiveLearnSkillRules` |
| RoundStart 触发入口 | `BattleRuleRoundStartSupport.cs` | `BattleRuleRoundStartTrigger.Trigger` |
| 回合计数累加 | `BattleRuleComputerSupport.cs` | `OnRoundStart` |
| Round Owner 解析 | `BattleRuleComputerManager.cs` | `TryResolveOwnerInstanceId`（`round` → 全局 Owner） |
| 规则判定与执行 | `BattleRuleService.cs` | `TriggerLearnedBattleRules` / `TryExecuteRule` |
| AddBuff 执行 | `BattleRuleLearnBuffEffectApplier.cs` | `TryApply` → `TryCreateSkillContext` → `BuffApplier` |
| 目标解析（全队） | `BattleRuleEffectTargets.cs` | `ResolveTargetSoldiers`（无 Source 时遍历存活英灵） |
| 英雄专属 scope | `BattleRuleHeroScopeSupport.cs` | `MatchesTriggerContext`（GloballyEffective + RoundStart） |
| 首日触发 | `GamePlayManager.cs` | `OnInitComplete` → `StartGame` 后 |
| 换波触发 | `GamePlayManager.cs` | `EndDay`（`isDayInProgress` 为 true） |
| 旧逻辑（废弃） | `PraySkill.cs` | `DoRandomSoldierFortify`；`OnEnable` 已注释 |

---

## 5. 日志与排查

### 5.1 正常日志

```text
[AttributeManager] 本局新增规则: RuleId=113002, StackKey=2150102, Source=Skill, ScopeHero=0
[BattleRuleRoundStartTrigger] 回合开始规则触发完成: day=1, triggered=1
[BattleRuleLearnBuffEffectApplier] AddBuff: buffId=20015, trigger=RoundStart, skillId=2150102, hero=..., hp=.../...
```

### 5.2 异常日志

| 日志 | 原因 | 处理 |
|------|------|------|
| `RoundStart 规则跳过: ... reason=Computer 未达标` | `300007.MaxValue` 过大或计数未累加 | 确认 `MaxValue=1`；确认 `OnRoundStart` 在判定前调用 |
| `triggered=0` 且无跳过日志 | 规则未学习 / Trigger 不匹配 / scope 过滤 | 查 `BattleLearnedSkillRules`；英雄专属时确认伊莱娅在场 |
| `未找到 Buff 模板: buffId=20015` | 模板库缺失 | 检查 `SkillBuffTemplateLibrary.asset` |
| `技能来源英灵不可用，跳过 AddBuff` | 伊莱娅阵亡或不在场 | 预期；技能来源规则不释放 |
| `AddBuff: skillId=0` | 非技能来源规则 | 流派卡等；数值为模板常量 |
| 有 `RuleId=113002` 但无 `RoundStartTrigger` | 未走到触发入口 | 查 `OnInitComplete` / `EndDay` 调用链 |
| `BattleRuleService 不可用`（无此日志但无触发） | 服务未初始化 | 确认战斗场景 `BattleSceneLifecycle` 已执行 |

---

## 6. 测试检查清单

### 功能

- [ ] 携带被动 2150102 进局：日志 `本局新增规则: RuleId=113002`
- [ ] 第 1 天开始：全体存活英灵获得 Buff 20015，攻速提升
- [ ] 进入第 2 天：再次全体加 Buff（`triggered=1`）
- [ ] 波中新增英雄：**不**自动获得当前波 Buff（`NewSoldierSync=0`）

- [ ] 伊莱娅阵亡时：日志 `技能来源英灵不可用，跳过 AddBuff`，全队不加 Buff
- [ ] 升级被动后：若已配 `*AttrName`，Buff 数值随等级变化

### 配表

- [ ] `300007.MaxValue=1`（非 100）
- [ ] `210003.BaseValue=20015` 且模板库存在该 BuffId
- [ ] `113002.Trigger=13`（RoundStart）
- [ ] `113002.GloballyEffective=1`

### 回归

- [ ] 旧 `PraySkill` 路径不再随机强化单人
- [ ] 其他 ComputerGroup / 伤害管线规则不受影响
- [ ] 无 `RoundStart` 规则的对局无多余日志

---

## 7. 后续同类卡扩展模板

1. 被动 / 技能 `LevelPropertyList` 写 `Type=123, Value={RuleId}`
2. `SkillRule.Trigger=13`（RoundStart）
3. 全队 Buff：`GloballyEffective=1` + `EffectGroup AddBuff`
4. 每波一次门槛：`ComputerGroup` 使用 `CounterType/TriggerType=round`，`MaxValue=1`，`ResetType=AfterTrigger`
5. 触发入口统一走 `BattleRuleRoundStartTrigger.Trigger()`，**不要**在业务里直接 `BuffApplier`
6. 若需波中新增英雄也获得 Buff：改 `NewSoldierSync` 并扩展 `BattleRuleNewSoldierSyncSupport`（当前 RoundStart **未**支持 Reevaluate）

---

## 8. 相关文档

| 文件 | 说明 |
|------|------|
| `BattleRule_SkillSourceBuff_Context.md` | 技能来源 → Buff 动态数值、`StackSourceKey` 传递 |
| `BattleRule_BuffEffect_Context.md` | EffectGroup AddBuff / `SkillBuffTemplateLibrary` |
| `BattleRule_FusionGem_DeepRooted_10000023_Context.md` | 融合石根深蒂固：RoundStart AddBuff 30031 |
| `BattleRule_ComputerGroup_12111_Context.md` | ComputerGroup 累加与判定时序模板 |
| `BattleRule_HeroScopedSchoolCard_Context.md` | 英雄专属 `ScopeHeroCardId` |
| `BattleRuleService_Context.md` | 规则服务 AI 速查 |
| `README_BattleRuleSystem.md` | 战斗规则系统总览 |

---

## 9. 变更历史

| 日期 | 内容 |
|------|------|
| 2026-07-04 | 新增 `TriggerType.RoundStart` 运行时：`BattleRuleRoundStartTrigger`、`OnRoundStart` 计数 |
| 2026-07-04 | 首日触发改到 `OnInitComplete`；换波在 `EndDay`；废弃 `PraySkill` 调用 |
| 2026-07-04 | 修复 `300007.MaxValue` 100→1；GloballyEffective RoundStart 的 hero scope 判定 |
| 2026-07-04 | 增加 `RoundStart 规则跳过` 日志便于排查 |
| 2026-07-06 | AddBuff 绑定 `StackSourceKey=2150102` → 伊莱娅 SkillContext；阵亡跳过；见 `BattleRule_SkillSourceBuff_Context.md` |
| 2026-07-17 | 校正 RoundStart 枚举值为 **14**；交叉引用根深蒂固融合石文档 |
