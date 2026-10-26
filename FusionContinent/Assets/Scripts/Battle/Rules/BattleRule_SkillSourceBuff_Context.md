# 战斗规则：链式学习来源参数传递（AddBuff / ModifyAttr）AI 上下文

> 供后续 AI 对话或开发接续使用。  
> 最后更新：2026-07-13  
> 关联：`BattleRule_BuffEffect_Context.md`、`BattleRule_EquipLearnSkillRule_Context.md`、`BattleRule_RoundStart_2150102_Context.md`、`BattleRuleService_Context.md`

---

## 1. 问题背景

规则效果的数值原本无法随「学习该规则的技能」等级变化，两条通道都存在断点：

**通道 A：AddBuff（`*AttrName`）**  
`EffectGroup AddBuff` 经 `BattleRuleLearnBuffEffectApplier` → `BuffApplier.ApplyBuff` 施加 Buff。Buff 模板支持 `*AttrName` 字段（如 `PercentEffectValueAttrName`），运行时从**施法者技能实例**的 `AttributeSet` 读动态数值。  
修复前：RoundStart / NoCondition 等触发时 `SkillId=0`，`GetSkillCard` 返回 null，只能用 `SkillBuffTemplateLibrary` 常量。范例：祈祷 2150102。

**通道 B：ModifyAttr（`SourceAttrType`）**  
`EffectGroup ModifyAttr` 支持 `FlatAdd = BaseValue + 来源属性 × PreValue`。  
修复前：来源属性只能读英灵 `BaseAttributes`（Category=Soldier）或 `ModuleAttributes`，**技能类属性（Category=Skill）读不到**，数值只能写死在 `BaseValue`。范例：2130102 击杀加魔法强度，各等级学同一规则 108003 → 效果组 208003 `BaseValue=1` 固定，升级无效。

**修复后（统一方案）：**

**A. 技能/被动（`SourceType=Skill` / `GeneralSkill`）** — `BattleRuleSkillSourceSupport`：从 `StackSourceKey` 反查技能实例与持有英灵：
- AddBuff：构造完整 `SkillContext`，`*AttrName` 读技能等级属性
- ModifyAttr：`SourceAttrType` 为 Skill 类属性时，从技能实例 `Attributes.GetFinal` 读按等级数值

**B. 链式学习其他来源（装备/流派/轮语者等）** — `BattleRuleSourceParamSupport`（2026-07-09）：从 `StackSourceKey` 反查**学习来源配置**中的 Skill 类参数属性：
- ModifyAttr：`BuildModifier(..., ruleContext)` → `ResolveParamValue`
- AddBuff：`AttachParamSkillToContext` → `BattleRuleSourceParamSkillProxy` → `BuffApplier.GetSkillCard` 回退读取 `*AttrName`

**配表约定：** 学习来源 `Attributes` 中除 `LearnSkillRule(123)` 外，再配置 **Category=Skill** 的属性行作为规则参数；效果组 `SourceAttrType` 引用该参数，`BaseValue=0, PreValue=1`（或所需系数）。

---

## 2. 核心原则

| 原则 | 说明 |
|------|------|
| **单一真值源** | 动态数值来自技能实例 `AttributeSet`，或学习来源配置中的 Skill 类参数行；不在 Buff / EffectGroup 上维护第二套数值 |
| **来源分流** | 有技能实例 → 绑真实 `SkillContext`；无技能实例但有来源配置参数 → `BattleRuleSourceParamSkillProxy`；否则回退模板 / `BaseValue` 常量 |
| **阵亡不释放** | **仅技能来源** AddBuff：施法英灵阵亡或不在场 → **整次 AddBuff 跳过**（装备/流派等无此限制） |
| **复用现有机制** | 不新增 Buff 参数字段；沿用 `BuffApplier.ResolveFloat`；ModifyAttr 沿用 `SourceAttrType × PreValue` |
| **逻辑与数值分离** | 一条 `SkillRuleConfig` 可复用；不同档位只改来源侧参数 Value，勿为每个数值拆整条规则 |

---

## 3. 来源类型与 StackSourceKey

规则学习时 `AttributeManager.AddBattleSkillRule(ruleId, stackSourceKey, sourceType, scopeHeroCardId)` 登记条目，触发时写入 `BattleRuleTriggerContext`。

| SourceType | StackSourceKey 示例 | 动态参数 | 数值来源 |
|------------|---------------------|----------|---------------|
| **Skill** | `"2150102"` / `"2130102"` | ✅ | 技能实例 `AttributeSet`（优先）或 `LevelPropertyList` 配置 |
| **GeneralSkill** | `"PassiveSkill_2150102"` | ✅ | 同上（英雄专属被动路径） |
| **SchoolCard** | `"12109"` | ✅ | `TbSchoolCardConfig.StarList[当前星级].Attributes` 中 Skill 类参数 |
| **Equip** | `"Equip_{实例Id}_{词条Id}"` | ✅ | `TbEquipEntryConfig.Attributes` 中 Skill 类参数 |
| **Equip**（月影祝福） | `Equip_..._EntryEffectBonus_{n}` | ✅ | 与对应词条配置共用参数 |
| **Equip**（特效） | `"EquipEffect_{实例Id}_{特效Id}"` | ✅ | `TbEquipEffectsConfig.Attributes` 中 Skill 类参数 |
| **SpeakerSkin** | `"SpeakerWeapon_{配置Id}_{星级}_{RuleId}"` | ✅ | `WeaponStarList[level]` 的 Base/ExtraAttributes |
| **SpeakerSkin** | `"SpeakerSkin_{配置Id}_{星级}_{RuleId}"` | ✅ | `ImageStarList[level]` 的 Base/ExtraAttributes |

**LearnSkillRule 行勿用 `EffectParam` / `ProbabilityTrigger` 传数值**：`Value` = RuleId；`EffectParam` 仅作 RuleId 备用（`Value≤0` 时）；参数数值写在**另一条 Skill 类属性行**。

---

## 4. 运行时数据流

### 4.1 技能来源（范例：祈祷 2150102 → 规则 113002）

```
tbspecialeffectsconfig (2150102)
  LevelPropertyList[].Attributes: LearnSkillRule=113002
        ↓ SkillBase.Initialize
AttributeManager.AddBattleSkillRule(113002, "2150102", Skill, scopeHero=0)
        ↓ 每波 RoundStart
BattleRuleService.TriggerLearnedBattleRules
  runtimeContext.StackSourceKey = "2150102"
  runtimeContext.ModifierSourceType = Skill
  runtimeContext.ScopeHeroCardId = entry.ScopeHeroCardId
        ↓
BattleRuleLearnBuffEffectApplier.TryCreateSkillContext
  ResolveSourceSkillId → 2150102
  ResolveSourceSoldier:
    ① ScopeHeroCardId > 0 → 找该 cardId 存活英灵
    ② SpecialEffects.ActsOnCard → 2050001（伊莱娅）
    ③ 遍历找第一个持有 skillId 的存活英灵
  英灵阵亡/不存在 → return false，跳过 AddBuff
        ↓
SkillContext { SkillId=2150102, Caster=伊莱娅, Level=技能等级 }
        ↓
BuffApplier.ApplyBuff(buffId, skillContext, target)
  GetSkillCard(context) → 2150102 的 ISkill
  ResolveFloat(skillCard, PercentEffectValueAttrName, 模板常量)
```

### 4.2 ModifyAttr 技能来源（范例：2130102 击杀加魔法强度 → 规则 108003）

```
tbspecialeffectsconfig (2130102, ActsOnCard=2030001)
  LevelPropertyList[各等级].Attributes: LearnSkillRule=108003
        ↓ SkillBase.Initialize
AttributeManager.AddBattleSkillRule(108003, "2130102", Skill)
        ↓ 击杀怪物（OnKill 计数 300005 → OnDamageAfter 触发）
BattleRuleService.TriggerLearnedBattleRules
  runtimeContext.StackSourceKey = "2130102", ModifierSourceType = Skill
        ↓
BattleRuleAcquireEffectExecutor.ApplyEffectGroup（EffectExecutor=Acquire，targetSoldier=击杀者）
        ↓
BattleRuleEffectGroupsApplier.ApplyModifyAttrForAcquire
  UsesSkillSourceAttribute(effect)?（SourceAttrType 为 Skill 类属性）
    → BattleRuleSkillSourceSupport.ResolveSourceSkill(ruleContext) → 2130102 实例
        ↓
BattleRuleEffectParamUtil.BuildModifier(..., sourceSkill)
  FlatAdd = BaseValue + sourceSkill.Attributes.GetFinal(SourceAttrType) × PreValue
        ↓
soldier.BaseAttributes.AddModifier（NewSoldierSync=2 → 每次击杀叠加一层）
```

**注意：** 208003 当前配置为 `BaseValue=1, SourceAttrType=219(Null)`，仍是固定 +1。要随等级变化须按 §6.3 改配置。

### 4.3 装备来源（范例：词条 5010001 → 规则 112012）

```
tbequipentryconfig (5010001)
  Attributes:
    LearnSkillRule=112012
    BUFF基础伤害(213)=30          ← Skill 类参数行
        ↓ ApplyEquipModuleAttributes
AttributeManager.AddBattleSkillRule(112012, "Equip_{实例Id}_5010001", Equip)
        ↓ ApplyEquipLearnedRulesToSoldier（Reevaluate）
BattleRuleEffectGroupsApplier.ApplyModifyAttrForAcquire
  ruleContext.StackSourceKey = Equip_{实例Id}_5010001
  ruleContext.ModifierSourceType = Equip
        ↓
BattleRuleSourceParamSupport.ResolveParamValue(ruleContext, SourceAttrType=213) → 30
        ↓
BuildModifier: FlatAdd = 0 + 30 × 1 = 30
```

效果组须改为 `BaseValue=0, SourceAttrType=213, PreValue=1`（勿再在 `BaseValue` 写死 30）。

### 4.4 流派卡来源（范例：卡 12109 → 规则 108002）

```
tbschoolcardconfig (12109)
  StarList[当前星级].Attributes:
    LearnSkillRule=108002
    <Skill类参数>=<按星级数值>
        ↓ SchoolManager.AddSchoolCard
AttributeManager.AddBattleSkillRule(108002, "12109", SchoolCard)
        ↓ 规则触发
BattleRuleSourceParamSupport.ResolveParamValue
  → SchoolManager.GetSchoolCard(12109).CardStar
  → StarList[star].Attributes
```

### 4.5 轮语者武器/皮肤来源

```
tbplayerweaponconfig ExtraAttributes:
  LearnSkillRule=112095 / 114051 / ...
  AddDamageValueTake(227)=...   ← Category=Add 参数
        ↓
StackSourceKey = SpeakerWeapon_{配置Id}_{星级}_{RuleId}
        ↓
BattleRuleSourceParamSupport → WeaponStarList[level] Base/ExtraAttributes
```

完整武器规则清单（战斗币/每秒 Computer、Live*、BuyEffects 等）见 `Assets/Scripts/Game/Player/WeaponExtraAttributes_Context.md`。

旧描述（已废弃）：

```
SchoolManager → ... → SkillContext { SkillId=0 } → 仅模板常量
```

---

## 5. 代码接线清单

| 职责 | 文件 | 方法 / 字段 |
|------|------|-------------|
| **技能实例来源解析** | `BattleRuleSkillSourceSupport.cs` | `ResolveSourceSkillId`、`ResolveSourceSoldier`、`ResolveSourceSkill` |
| **链式学习来源参数解析** | `BattleRuleSourceParamSupport.cs` | `ResolveParamValue`、`BuildParamAttributeSet`、`AttachParamSkillToContext`、`IsRuleSourceParamAttribute` |
| **Buff 参数代理技能** | `BattleRuleSourceParamSkillProxy.cs` | 仅 `Attributes` 有效，供 `BuffApplier` 读取 |
| **SkillContext 扩展键** | `SkillContextKeys.cs` | `RuleSourceParamSkill` |
| AddBuff 执行与 SkillContext 构造 | `BattleRuleLearnBuffEffectApplier.cs` | `TryCreateSkillContext`、`AttachParamSkillToContext` |
| ModifyAttr 规则参数判定 | `BattleRuleEffectParamUtil.cs` | `UsesSkillSourceAttribute`、`BuildModifier(..., ruleContext)`、`ResolveSourceAttributeValue` |
| ModifyAttr 获得管线透传 | `BattleRuleEffectGroupsApplier.cs` | `ApplyModifyAttrForAcquire(ruleContext, ...)` |
| ModifyAttr 落地 | `BattleRuleAcquireSoldierEffectApplier.cs` | `ApplyEffectToSoldier / ApplyEffectToAttributeSet`（`sourceSkill` + `ruleContext`） |
| 规则上下文 | `BattleRuleTriggerContext.cs` | `StackSourceKey`、`ModifierSourceType`、`ScopeHeroCardId` |
| 触发时透传 entry | `BattleRuleService.cs` | `TriggerLearnedBattleRules` 等设置 `StackSourceKey` / `ModifierSourceType` |
| 已学规则条目 | `BattleLearnedRuleEntry.cs` | `RuleId`、`StackSourceKey`、`SourceType`、`ScopeHeroCardId` |
| Buff 数值解析 | `BuffApplier.cs` | `GetSkillCard`（含参数代理回退）、`ResolveFloat`、`*AttrName` |
| 英雄 scope 存活判定 | `BattleRuleHeroScopeSupport.cs` | `IsScopedHeroAliveOnField`（GloballyEffective RoundStart） |

### 5.1 TryCreateSkillContext 决策树

```
DamageContext.SkillId > 0 ?
  └─ 是 → CreateSkillContextFromDamage（伤害管线，保持原逻辑）
  └─ 否 → ResolveSourceSkillId > 0 且 SourceType ∈ {Skill, GeneralSkill} ?
        ├─ 是 → ResolveSourceSoldier
        │     ├─ 英灵可用 → SkillContext(skillId, caster, level)
        │     └─ 英灵不可用 → return false（跳过整次 AddBuff）
        └─ 否 → CreateFallbackSkillContext
              → BattleRuleSourceParamSupport.AttachParamSkillToContext（装备/流派/轮语者等）
              → GetSkillCard 可回退到 BattleRuleSourceParamSkillProxy
```

**优先级：** 伤害管线 SkillId > 技能实例 > 来源配置参数代理 > 模板常量。

### 5.2 施法英灵解析优先级（`BattleRuleSkillSourceSupport.ResolveSourceSoldier`）

1. `ruleContext.Source` 英灵自身持有该技能（击杀等触发场景，最常见）
2. `ruleContext.ScopeHeroCardId`（英雄专属被动 / 流派 `IsAddHero` 路径）
3. `TbSpecialEffectsConfig.Get(skillId).ActsOnCard`
4. 场上第一个存活且 `GetSkillById(skillId) != null` 的英灵

---

## 6. 配表：如何让规则数值随来源变化

### 6.0 通用模式（技能 / 装备 / 流派 / 轮语者）

**两行配表：**

```
学习规则:{RuleId}:数值:0:0
{参数属性名}:{数值}:数值:0:0     ← Category=Skill 的 AttributeType
```

**效果组（ModifyAttr）：**

```
BaseValue=0, SourceAttrType={参数AttributeType}, PreValue=1
```

公式：`FlatAdd = BaseValue + 参数值 × PreValue`

**效果组（AddBuff）：** Buff 模板填写 `*AttrName` 指向参数属性枚举名；未配置则回退模板常量。

### 6.1 技能侧（SpecialEffects）

在 `tbspecialeffectsconfig.json` 对应技能的 `LevelPropertyList[等级-1].Attributes` 中配置可被引用的属性，例如：

```json
{
  "Type": 142,
  "Value": 0.35,
  "ValueType": 1
}
```

（具体 `AttributeType` 由策划与 `tbattributeconfig.json` 对齐。）

### 6.2 Buff 模板侧（SkillBuffTemplateLibrary）

在 `SkillBuffTemplateLibrary.asset` 对应 BuffId 的效果行填写 `*AttrName`，例如：

| 字段 | 示例 | 说明 |
|------|------|------|
| `PercentEffectValueAttrName` | `SoldierNumOfAttackSpeed` 或自定义属性名 | 从技能读百分比系数 |
| `EffectValueAttrName` | 某 `AttributeType` 枚举名 | 从技能读固定加成 |
| `DurationAttrName` | 同上 | 从技能读持续时间 |

未配置 `*AttrName` 时，使用同行常量（`PercentEffectValue`、`EffectValue`、`Duration`）。

### 6.3 ModifyAttr 通道配表（2130102 类：击杀叠属性随等级）

公式：`FlatAdd = BaseValue + 技能属性(SourceAttrType) × PreValue`

1. **属性表**：选用/新增一个 **Category=Skill(2)** 的 `AttributeType`（如复用 213「BUFF基础伤害」或在 Excel 新增专用属性）
2. **技能侧**：`tbspecialeffectsconfig` 2130102 各等级 `Attributes` 追加 `{Type: <该属性>, Value: <每级击杀增量>, ValueType: 1}`（`SkillBase.Initialize` 会写入技能自身 `AttributeSet`）
3. **效果组侧**：`tbeffectgroupconfig` 208003 改为 `SourceAttrType=<该属性>, PreValue=1, BaseValue=0`

技能类 `SourceAttrType` 解析不到来源时取值 0 并打 Warning，**不**静默回退英灵属性或 `EffectGroup.BaseValue`。

### 6.4 装备词条参数（5010001 类）

1. **词条侧** `tbequipentryconfig`：`LearnSkillRule` + Skill 类参数（如 `213=30` / `60` / `90` 分档）
2. **规则侧** 逻辑共用一条 `SkillRuleConfig`（如 `112012`）
3. **效果组侧** `SourceAttrType=213, BaseValue=0, PreValue=1`（替换原 `BaseValue=30` 写死）
4. **多件叠加**：同 RuleId、不同 `StackSourceKey`、各自词条参数独立叠加

### 6.5 流派卡 / 轮语者参数

- **流派卡**：`StarList[星级].Attributes` 内 `LearnSkillRule` + Skill 类参数；星级变化时读当前 `SchoolCard.CardStar`
- **武器/皮肤**：`WeaponStarList` / `ImageStarList` 对应等级行的 `BaseAttributes` + `ExtraAttributes` 均可含参数行

### 6.6 当前状态（2026-07-09）

| 项 | 状态 |
|----|------|
| 技能实例参数（AddBuff / ModifyAttr） | ✅ |
| 装备/流派/轮语者配置型参数（`BattleRuleSourceParamSupport`） | ✅ 代码已接入 |
| 存量装备词条（如 5010001 + 212012 `BaseValue=30`） | ⚠️ 仍为写死数值；迁移需改词条 + 效果组 |
| 祈祷 2150102 Buff 20015 动态系数 | ⚠️ 模板 `PercentEffectValueAttrName` 仍可能为空 |
| 2130102 效果组 208003 | ⚠️ 若仍为 `SourceAttrType=Null`，需按 §6.3 改配 |

---

## 7. 日志与排查

### 7.1 正常日志

```text
[AttributeManager] 本局新增规则: RuleId=113002, StackKey=2150102, Source=Skill, ScopeHero=0
[BattleRuleLearnBuffEffectApplier] AddBuff: buffId=20015, trigger=RoundStart, skillId=2150102, hero=..., hp=.../...
```

### 7.2 异常 / 跳过

| 日志 | 原因 | 处理 |
|------|------|------|
| `技能来源英灵不可用，跳过 AddBuff: skillId=...` | 施法英灵阵亡或不在场 | 预期行为（技能来源规则） |
| `AddBuff: ..., skillId=0, ...` | 装备/流派来源且无参数行或 `*AttrName` 未配 | 回退模板常量；若已配参数代理则仍可动态 |
| `规则参数未解析到来源，取值 0` | 效果组写了 Skill 类 `SourceAttrType` 但来源配置无对应参数行 | 补来源侧 Skill 类属性行 |
| `已施加属性 ..., Flat=固定值不变` | 效果组仍 `BaseValue` 写死且未改 `SourceAttrType` | 按 §6.0 迁移 |

---

## 8. 扩展新卡检查清单

### 装备/流派/轮语者学规则 + 动态数值（ModifyAttr / AddBuff）

- [ ] 来源 `Attributes`：`LearnSkillRule` + **Skill 类参数行**（勿用 EffectParam 传值）
- [ ] 效果组：`BaseValue=0, SourceAttrType=参数, PreValue=系数`
- [ ] AddBuff 时：Buff 模板 `*AttrName` 与参数属性枚举名一致
- [ ] 装备：验证 `StackKey=Equip_{实例}_{词条Id}` 日志 + `Flat=词条参数值`
- [ ] 流派：升星后参数随 `CardStar` 变化
- [ ] 同 RuleId 多来源：各 `StackSourceKey` 独立读各自参数并叠加

### 技能/被动学规则 + 动态 Buff（AddBuff）

- [ ] `LevelPropertyList` 写 `LearnSkillRule`，规则 `EffectGroup` 为 `AddBuff`
- [ ] 确认 `AddBattleSkillRule` 的 `SourceType` 为 `Skill` 或 `GeneralSkill`
- [ ] 技能各等级配置 Buff 相关属性（供 `*AttrName` 读取）
- [ ] Buff 模板填写对应 `*AttrName`（非常量依赖时）
- [ ] 确认 `ActsOnCard` 或 `ScopeHeroCardId` 能定位施法英灵
- [ ] 验证：施法英灵存活 → 全队/目标获得 Buff；阵亡 → 不释放
- [ ] 验证：升级被动后 Buff 数值变化

### 技能/被动学规则 + 动态叠属性（ModifyAttr，2130102 类）

- [ ] 效果组 `SourceAttrType` 填 Category=Skill 的属性，`PreValue=1`
- [ ] 技能各等级配置该属性的 Value（每次触发的增量）
- [ ] 验证：击杀后日志 `已施加属性 ..., Flat=<等级对应值>`
- [ ] 验证：升级被动后每次击杀增量变化；已叠加层数不回溯
- [ ] 验证：`NewSoldierSync=2` 时每次触发独立叠层（additive stack）

### 流派卡学规则 + 仅模板常量（未配参数行时）

- [ ] 未配 Skill 类参数行 → 数值仍来自 `EffectGroup.BaseValue` 或 Buff 模板常量
- [ ] 已配参数行 → 按 §6.5 走 `BattleRuleSourceParamSupport`

---

## 9. 相关文档

| 文件 | 说明 |
|------|------|
| `BattleRule_BuffEffect_Context.md` | AddBuff 总览、`SkillBuffTemplateLibrary` |
| `BattleRule_RoundStart_2150102_Context.md` | 祈祷被动 RoundStart 专项 |
| `BattleRule_EquipLearnSkillRule_Context.md` | 装备 LearnSkillRule、月影祝福、词条参数配表 |
| `BattleRule_HeroScopedSchoolCard_Context.md` | `ScopeHeroCardId` / `IsAddHero` |
| `WeaponExtraAttributes_Context.md` | 轮语者武器 ExtraAttributes、SpeakerWeapon StackKey、BattleGold/EverySecond |
| `BattleRuleService_Context.md` | 规则服务触发链路 |
| `Assets/Scripts/Game/Skill/Config/SkillBuffTemplateLibrary_README.md` | Buff 模板字段说明 |

---

## 10. 变更历史

| 日期 | 内容 |
|------|------|
| 2026-07-09 | 新增 `BattleRuleSourceParamSupport`：装备/流派/轮语者/技能配置型来源读 Skill 类参数；`BattleRuleSourceParamSkillProxy` + `BuffApplier.GetSkillCard` 回退 |
| 2026-07-09 | `BuildModifier` / `ResolveSourceAttributeValue` 增加 `ruleContext`；文档扩展为「链式学习来源参数」 |
| 2026-07-06 | 新增 `BattleRuleTriggerContext.ScopeHeroCardId`；`BattleRuleService` 触发时透传 |
| 2026-07-06 | `BattleRuleLearnBuffEffectApplier` 重写 `TryCreateSkillContext`：技能来源绑施法者；阵亡跳过 |
| 2026-07-06 | 抽出共享 `BattleRuleSkillSourceSupport`（AddBuff / ModifyAttr 共用技能来源解析） |
| 2026-07-06 | ModifyAttr 支持技能类 `SourceAttrType`：`BuildModifier(..., sourceSkill)` 从技能实例读按等级数值（2130102 类击杀叠属性） |
| 2026-07-06 | 本文档初版 |
