# 根深蒂固宝石（10000023）AI 上下文

> 供后续 AI 对话或开发接续使用。  
> 最后更新：2026-07-17  
> 关联：`BattleRule_EquipLearnSkillRule_Context.md` §3.4、`BattleRule_BuffEffect_Context.md`、`BattleRule_RoundStart_2150102_Context.md`、`BattleRule_SkillSourceBuff_Context.md`、**`BattleRule_FusionGem_Index.md`**

---

## 1. 功能概述

融合石 **10000023「根深蒂固宝石」**：每波（天）开始时，全队存活英灵按宝石参数比例恢复最大生命。

| 项 | 说明 |
|----|------|
| 宝石 Id | `10000023`（`tbgemfusionconfig`） |
| 学习规则 | `LearnSkillRule(126)` → **113007** |
| 回血比例参数 | `DeepRootedEffect(242)`（Lv1=0.1，Lv2=0.2…） |
| 触发时机 | `TriggerType.RoundStart`（**14**） |
| 生效范围 | `GloballyEffective=1` → 全队存活英灵 |
| 效果形态 | **AddBuff** → 模板 **30031**（`HealBuff` / `BuffType.Health=9`） |

**旧链路（已废弃，勿再按此实现）：**

```
213007 ModifyAttr → SoldierPerRestoreCurrentHealth(55)
  SourceAttrType=DeepRootedEffect(242), PreValue=1
  → BattleRuleInstantHealSupport.SetSoldierHp
```

**新链路（2026-07-17 起）：** RoundStart 挂治疗 Buff，数值由 Buff 模板 `*AttrName` + 融合石参数代理解析。

---

## 2. 配表链路

```
tbgemfusionconfig.json          Id=10000023
  LevelList[].Attributes[]
    Type=126 LearnSkillRule     Value=113007
    Type=242 DeepRootedEffect   Value=0.1 / 0.2 / …
        │
        ▼
tbskillruleconfig.json          Id=113007
  Trigger=14 (RoundStart)
  GloballyEffective=1
  ConditionGroupId=[]
  ComputerGroupId=[]
  EffectGroupId=[213007]
  NewSoldierSync=0
        │
        ▼
tbeffectgroupconfig.json        Id=213007
  PersistToAttributeSet=0
  Effects[0]:
    EffectType=0 (AddBuff)
    AttributeType/SourceAttrType=230 (Null 占位)
    BaseValue=30031          ← SkillBuffTemplateLibrary.BuffId
    PreValue=0
        │
        ▼
SkillBuffTemplateLibrary.asset  BuffId=30031「根深蒂固宝石效果」
  BuffTypeId=9 (Health / HealBuff)
  PercentEffectValueAttrName=DeepRootedEffect
  PercentValueSourceAttrName=SoldierMaxHealth   ← 必须配置，否则默认攻强
  BuffValueSource=Target
  Duration / TickInterval     ← 见 §4
```

### 2.1 宝石等级参数（示例）

| Level | DeepRootedEffect(242) | 描述语义 |
|-------|----------------------|----------|
| 1 | 0.1 | 每回合开始恢复 10% 生命 |
| 2 | 0.2 | 20% |
| … | 随等级递增 | 以 `tbgemfusionconfig` 为准 |

`ValueType=1`（数值）；运行时按小数比例使用（`|v|≤1` 不 ÷100）。

### 2.2 规则（113007）

| 字段 | 值 | 说明 |
|------|-----|------|
| `Trigger` | **14** | `RoundStart`（首日 `OnInitComplete`、换波 `EndDay`） |
| `GloballyEffective` | **1** | 全队 |
| `ComputerGroupId` | [] | 无 Computer 门槛 |
| `ConditionGroupId` | [] | 无条件 |
| `NewSoldierSync` | **0** | 波中新召唤英灵**不**补本波回血 |

### 2.3 效果组（213007）

| 字段 | 值 | 说明 |
|------|-----|------|
| `EffectType` | **0** AddBuff | 不是 ModifyAttr(5) |
| `BaseValue` | **30031** | 必须是模板 BuffId，禁止填 EffectsConfig 行 Id |
| `PersistToAttributeSet` | 0 | AddBuff 不写 AttributeSet |

### 2.4 Buff 模板（30031）

| 字段 | 值 | 说明 |
|------|-----|------|
| `BuffTypeId` | 9 | `BuffType.Health` → `HealBuff` |
| `PercentEffectValueAttrName` | `DeepRootedEffect` | 从融合石参数代理读比例 |
| `PercentValueSourceAttrName` | `SoldierMaxHealth` | 基数=目标最大生命 |
| `BuffValueSource` | `Target` | 读受 Buff 英灵自身属性 |
| `EffectValue` / `EffectValueAttrName` | 0 / 空 | 无固定加值 |

**治疗量公式（`BuffApplier.CalculatePercentValue`）：**

```
heal = SoldierMaxHealth × DeepRootedEffect
     = MaxHp × 0.1（Lv1）
```

---

## 3. 运行时数据流

```
开战 Equip.CollectFusionGemModuleEntries
  → StackSourceKey=FusionGem_{实例Id}
  → SourceType=FusionGem
  → AttributeManager.AddBattleSkillRule(113007, …)  仅登记（延后，同装备）

波次开始 BattleRuleRoundStartTrigger.Trigger
  → TriggerLearnedBattleRules(RoundStart)
  → runtimeContext.StackSourceKey / ModifierSourceType 来自学习条目
  → TryExecuteRule → ExecuteEffects(213007)
  → BattleRuleAcquireEffectExecutor
  → BattleRuleLearnBuffEffectApplier.TryApply
       ├─ ResolveTargetSoldiers：Source 非英灵 → 全场存活英灵
       ├─ TryCreateSkillContext → CreateFallbackSkillContext
       └─ AttachParamSkillToContext
            → TryResolveFusionGemAttributes(FusionGem_{id})
            → BattleRuleSourceParamSkillProxy(DeepRootedEffect=0.1)
  → BuffApplier.ApplyBuff(30031)
       → ResolveFloat(…, DeepRootedEffect) = 0.1
       → CalculatePercentValue(…, SoldierMaxHealth) = MaxHp×0.1
  → HealBuff：按 TickInterval 在 OnIntervalTick 回血
```

### 3.1 关键代码

| 文件 | 职责 |
|------|------|
| `PlayerEquipModel.cs` | `CollectFusionGemModuleEntries`、`FusionGem_{id}` |
| `BattleRuleSourceParamSupport.cs` | `TryResolveFusionGemAttributes`、`AttachParamSkillToContext` |
| `BattleRuleRoundStartSupport.cs` | RoundStart 入口 |
| `BattleRuleLearnBuffEffectApplier.cs` | AddBuff + 参数代理 |
| `BuffApplier.cs` | `*AttrName` → 最终治疗量 |
| `HealBuff.cs` | `SetSoldierHp` |
| `BattleRuleEffectGroupsApplier.cs` | RoundStart 仍允许 ModifyAttr（其它规则）；本宝石走 AddBuff 不受 `allowModifyAttr` 限制 |

### 3.2 参数解析约定

| 项 | 值 |
|----|-----|
| SourceType | `AttributeSourceType.FusionGem` |
| StackSourceKey | `FusionGem_{融合石实例Id}` |
| 参数 Category | `Add(8)`（`DeepRootedEffect` 在 `tbattributeconfig`） |
| 代理技能 | `BattleRuleSourceParamSkillProxy`（`SkillContextKeys.RuleSourceParamSkill`） |

学习时**只登记** `LearnSkillRule(126)`（Module）；`DeepRootedEffect(242)` 不写 ModuleAttributes，触发时从宝石 `GetLevelCfg().Attributes` 现读。

---

## 4. 时序与配表注意

`HealBuff`：`TickInterval > 0` 时仅在 `OnIntervalTick` 治疗（不在 `OnAdd` 立即跳血）。

| 配法 | 行为 |
|------|------|
| `Duration=0.1` + `TickInterval=0.1` | 约 0.1s 后回血一次后结束（偏「立刻」） |
| `Duration=1` + `TickInterval=1` | 约 1s 后才回血 |
| `TickInterval=0` + `Duration>0` | **每帧**回血直到结束（易过量，禁止用于本宝石） |

以 `SkillBuffTemplateLibrary.asset` 当前值为准；改时长后需回归「换波是否及时回血」。

---

## 5. 历史问题与规避

| 问题 | 原因 | 处理 |
|------|------|------|
| 旧 ModifyAttr 不回血 | RoundStart 未进 `allowModifyAttr` | 已放行 RoundStart ModifyAttr；**本宝石已改 AddBuff，不再依赖** |
| 回血量像「攻强×比例」 | `PercentValueSourceAttrName` 为空 → 默认 `SoldierAttackPower` | **必须**填 `SoldierMaxHealth` |
| 增伤为 0 / 参数为 0 | StackKey 丢失或 SourceType≠FusionGem | 学习日志须含 `StackKey=FusionGem_`、`Source=FusionGem` |
| AddBuff 失败 | BaseValue 填成 EffectsConfig Id | BaseValue=**30031** 模板 Id |

---

## 6. 日志排查

```
[AttributeManager] 本局新增规则: RuleId=113007, StackKey=FusionGem_xxx, Source=FusionGem
[BattleRuleRoundStartTrigger] 回合开始规则触发完成: day=N, triggered≥1
[BattleRuleLearnBuffEffectApplier] AddBuff: buffId=30031, trigger=RoundStart, ...
[HealBuff] Soldier healed +N HP (Value=...)
```

若只有 RoundStart 完成、无 AddBuff / HealBuff：查效果组是否仍为旧 ModifyAttr、或 `ResolveBuffId(30031)` 模板缺失。

---

## 7. 测试检查清单

- [ ] 装备 10000023 开战，日志学习 `113007` + `FusionGem_`
- [ ] 英灵掉血后换波 → 约按 MaxHp×宝石等级比例回血
- [ ] 全队存活英灵均回血（非仅佩戴者）
- [ ] Lv1≈10%、升级后比例随 242 变化
- [ ] 未装备宝石时换波无 30031 / 无对应治疗
- [ ] `PercentValueSourceAttrName` 仍为 `SoldierMaxHealth`（防回退攻强）

---

## 8. 配置路径

- `Assets/Res/Config/Json/tbgemfusionconfig.json`（10000023）
- `Assets/Res/Config/Json/tbskillruleconfig.json`（113007）
- `Assets/Res/Config/Json/tbeffectgroupconfig.json`（213007）
- `Assets/Res/Config/Json/tbattributeconfig.json`（242 DeepRootedEffect, Category=Add）
- `Assets/SkillGraph/SkillBuffTemplateLibrary.asset`（30031）

---

## 9. 变更历史

| 日期 | 内容 |
|------|------|
| 2026-07-16 | 初版：ModifyAttr + InstantHeal；RoundStart 放行 allowModifyAttr |
| 2026-07-17 | 效果组改为 AddBuff 30031；Buff 补 `SoldierMaxHealth`；本文档新建 |
