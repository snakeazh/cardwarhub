# 战斗规则 Buff 效果 AI 上下文

> 供后续 AI 对话或开发接续使用。  
> 最后更新：2026-07-17  
> 关联：`BattleRuleService_Context.md`、`BattleRule_SkillSourceBuff_Context.md`、`BattleRule_FusionGem_DeepRooted_10000023_Context.md`、`EffectGroup_EffectGroupsType_Implementation.md`、`Assets/Scripts/Game/Skill/Config/SkillBuffTemplateLibrary_README.md`

---

## 1. 核心原则（2026-06-08 起）

**EffectGroup 的 `AddBuff` / `RemoveBuff` 不走 `TbEffectsConfig`。**

| 项 | 说明 |
|----|------|
| 配表字段 | `EffectGroupConfig.Effects[].BaseValue` |
| 语义 | **`SkillBuffTemplateLibrary.BuffId`**（与运行时 `buff.BuffId` 一致） |
| 模板资源 | `Assets/SkillGraph/SkillBuffTemplateLibrary.asset` |
| 运行时读取 | `SkillBuffTemplateProvider.GetEffects(buffId)` |

**技能节点 TakeBuff** 仍走 `EffectsExecutor` → `TbEffectsConfig`（`EffectsType.TakeBuff`），与战斗规则路径分离。

---

## 2. 代码链路

### 2.1 AddBuff（EffectGroupsType = 0）

```
BattleRuleEffectGroupsApplier
  → BattleRuleLearnBuffEffectApplier.TryApply
  → BattleRuleEffectTargets.ResolveBuffId(BaseValue)   // 校验模板存在
  → BuffApplier.ApplyBuff(buffId, skillContext, target)
  → SkillBuffTemplateProvider.GetEffects(buffId)
  → BuffFactory / MultiEffectBuff / HealBuff ...
  → SoldierBuffController.AddBuff
```

**文件：** `BattleRuleLearnBuffEffectApplier.cs`、`BuffApplier.cs`

### 2.1.1 SkillContext 与动态数值（2026-07-06）

规则 AddBuff 时 `BattleRuleLearnBuffEffectApplier.TryCreateSkillContext` 构造 `SkillContext`：

| 规则来源 | SkillId | Buff 数值 |
|----------|---------|-----------|
| 伤害管线（有 `DamageContext.SkillId`） | 来自伤害上下文 | 技能 `*AttrName` |
| `SourceType=Skill` / `GeneralSkill` | 从 `StackSourceKey` 解析 | 技能实例 `*AttrName`；施法英灵阵亡则**跳过** |
| 装备 / 流派 / 轮语者 / **融合石** | `0` 或参数代理 | 来源配置 Add/Skill 类参数（`*AttrName`）或模板 / `BaseValue` 常量 |

详见：**`BattleRule_SkillSourceBuff_Context.md`**

`BuffApplier` 解析规则（`ResolveFloat`）：`GetSkillCard(context)` 依次为真实技能 → `BattleRuleSourceParamSkillProxy`；配置了 `*AttrName` 时读对应属性，否则用模板常量。

**融合石 RoundStart 治疗（10000023）：** AddBuff **30031**，`PercentEffectValueAttrName=DeepRootedEffect` + `PercentValueSourceAttrName=SoldierMaxHealth`。详见 **`BattleRule_FusionGem_DeepRooted_10000023_Context.md`**。

### 2.2 RemoveBuff（EffectGroupsType = 1）

```
BattleRuleEffectGroupsApplier
  → BattleRuleRemoveBuffApplier.TryApply
  → BattleRuleEffectTargets.ResolveRemoveBuffId(BaseValue)  // 直接用 BaseValue，不查 EffectsConfig
  → BuffController.RemoveBuff(匹配 buff.BuffId == buffId)
```

**文件：** `BattleRuleRemoveBuffApplier.cs`、`BattleRuleEffectTargets.cs`

移除失败时日志会输出 `activeBuffIds=[...]`，便于核对 Id 是否一致。

### 2.3 Id 解析差异

| 操作 | 方法 | 是否校验 SkillBuffTemplateLibrary | 失败行为 |
|------|------|-----------------------------------|----------|
| AddBuff | `ResolveBuffId` | ✅ 是 | 打 Error，返回 0，不施加 |
| RemoveBuff | `ResolveRemoveBuffId` | ❌ 否 | 仅校验 `rawValue > 0` |

---

## 3. 典型卡牌与规则

### 3.1 奥数卡 2203001「全力出击」

**配表：** `tbspecialeffectsconfig.json` Id=2203001  
**学习规则（三条缺一不可）：**

```
学习规则:111003 | 学习规则:111004 | 学习规则:112010
```

| 规则 Id | Trigger | 条件组 | 效果组 | 行为 |
|---------|---------|--------|--------|------|
| **111003** | HpChange(12) | 1017 满血 hp≥100% | 211003 AddBuff | 加 Buff **20014** |
| **111004** | HpChange(12) | 1018 非满血 hp<100% | 211004 RemoveBuff | 删 Buff **20014** |
| **112010** | NoCondition(10) | 1017 满血 | 211003 AddBuff | 获得规则/入队时补加 **20014** |

**Buff 20014**（`SkillBuffTemplateLibrary`）：
- 类型：`MultiEffectBuff`（模板内 2 条属性效果）
- 效果：满血时攻速值 +50% 来源、暴击 +30
- `Duration: -1`（永久，靠规则移除）

**常见故障：**
- 只学了 111003/112010，未学 **111004** → 掉血后 Buff 不消失（已在 2203001 配置补全 111004）
- RemoveBuff BaseValue 与 AddBuff 不一致 → 移除日志 `未找到可移除 Buff`

### 3.2 被动卡 2202006「黎明守护」

**学习规则：** 仅 **111005**（`MaxTriggerNum: 1`）

| 规则 Id | Trigger | 条件组 | 效果组 | 行为 |
|---------|---------|--------|--------|------|
| **111005** | HpChange(12) | 1001 hp≤50% | 211005 AddBuff | 加 Buff **21004** |

**Buff 21004**（HealBuff）：
- `PercentEffectValue: 0.05` × `SoldierMaxHealth` → 每秒回复 5% 最大生命
- `Duration: 5`，`TickInterval: 1`
- 与 111003/111004/20014 **无关**

---

## 4. 条件组速查（生命比例）

实现：`BattleRuleSoldierHealthConditionValues` + `BattleRuleConditionManager`

| 条件 Id | SpecialType | Attribute | ComparisonType | ComparisonValue | 含义 |
|---------|-------------|-----------|----------------|-----------------|------|
| **1017** | GoalAttribute(0) | SoldierMaxHealth(0) | GreaterThanOrEqualTo(0) | 1 | 满血 |
| **1018** | GoalAttribute(0) | SoldierMaxHealth(0) | LessThan(2) | 1 | 非满血（已受伤） |
| **1001** | GoalAttribute(0) | SoldierMaxHealth(0) | LessThanOrEqualTo(3) | 0.5 | 血量 ≤ 50% |

左值均为 `CurrentHp / MaxHp`（0~1）。

---

## 5. BuffApplier 治疗量计算（21004 / 30031 等）

**文件：** `Assets/Scripts/Game/Skill/Handler/Buff/BuffApplier.cs`

配置了 `PercentValueSourceAttrName`（如 `SoldierMaxHealth`）时：

```
finalValue = PercentEffectValue × 来源属性值
```

- `|PercentEffectValue| >= 1` → 按百分数（÷100），如 `5` = 5%
- `|PercentEffectValue| < 1` → 按小数比例，如 `0.05` = 5%、`0.1` = 10%
- `PercentEffectValue` 也可来自 `PercentEffectValueAttrName`（如融合石 `DeepRootedEffect`）
- **不再**二次乘以 `GetPercentValueSource`（已移除重复乘算）
- `BuffValueSource=Target` 且未配 `PercentValueSourceAttrName` 时，默认按伤害类型落到 **攻强**（易配错）→ 百分比治疗**必须**显式填 `SoldierMaxHealth`

**HealBuff：** `TickInterval > 0` 时仅在 `OnIntervalTick` 治疗；`BuffFactory` 会写入 `TickInterval`。不在 `OnAdd` 立即跳血。

### 5.1 根深蒂固 Buff 30031

| 字段 | 值 |
|------|-----|
| 规则 | 113007 RoundStart → 效果组 213007 AddBuff |
| 比例 | `DeepRootedEffect`（宝石参数代理） |
| 基数 | `SoldierMaxHealth`（目标） |
| 详档 | `BattleRule_FusionGem_DeepRooted_10000023_Context.md` |

---

## 6. 与 TbEffectsConfig 的关系（勿混用）

| 配置 Id | 用途 | 是否用于 EffectGroup AddBuff/RemoveBuff |
|---------|------|----------------------------------------|
| `TbEffectsConfig.Id`（如 21004） | 技能效果表行 Id | ❌ |
| `TbEffectsConfig.EffectsId` | 技能 TakeBuff 指向的模板 Id | ❌（仅 EffectsExecutor） |
| `SkillBuffTemplateLibrary.BuffId`（如 20014、21004） | 规则 BaseValue | ✅ |

历史坑：`tbeffectsconfig.json` 曾把 Id=21004 的 `EffectsId` 配成 21104，与模板 BuffId=21004 不一致；**规则路径已不读该表**。

---

## 7. 测试检查清单

### 2203001 全力出击

- [ ] 进局日志：`本局新增规则: RuleId=111003`、`111004`、`112010`
- [ ] 满血：身上有 Buff 20014，攻速/暴击提升
- [ ] 受伤：日志 `已移除 Buff 20014`，属性回退
- [ ] 回满血：再次加上 Buff 20014

### 2202006 黎明守护

- [ ] 首次 hp≤50%：加上 Buff 21004
- [ ] 每秒 `[HealBuff] Soldier healed +N HP`，N ≈ MaxHp×5%
- [ ] 规则 111005 整局最多触发 1 次（`MaxTriggerNum: 1`）

### 根深蒂固宝石 10000023 / Buff 30031

- [ ] 换波后全队按 MaxHp×宝石比例回血
- [ ] 日志含 `AddBuff: buffId=30031` 与 `[HealBuff] Soldier healed`
- [ ] 模板仍配置 `PercentValueSourceAttrName=SoldierMaxHealth`

### 通用

- [ ] AddBuff BaseValue 在 `SkillBuffTemplateLibrary` 中存在
- [ ] RemoveBuff BaseValue 与 AddBuff 施加时的 `buff.BuffId` 相同
- [ ] 移除失败时检查日志 `activeBuffIds`

---

## 8. 关联文件

| 文件 | 职责 |
|------|------|
| `BattleRuleEffectTargets.cs` | `ResolveBuffId` / `ResolveRemoveBuffId` |
| `BattleRuleLearnBuffEffectApplier.cs` | 规则 AddBuff、`TryCreateSkillContext` |
| `BattleRule_SkillSourceBuff_Context.md` | 技能来源 → Buff 动态数值传递 |
| `BattleRule_FusionGem_DeepRooted_10000023_Context.md` | 融合石根深蒂固 RoundStart 治疗 Buff |
| `BattleRuleRemoveBuffApplier.cs` | 规则 RemoveBuff |
| `BuffApplier.cs` | 模板读取、效果值计算、创建 Buff |
| `HealBuff.cs` | 治疗 Tick |
| `MultiEffectBuff.cs` | 多属性聚合 Buff（20014） |
| `SkillBuffTemplateLibrary.asset` | Buff 模板数据源 |
| `tbeffectgroupconfig.json` | 效果组 BaseValue |
| `tbspecialeffectsconfig.json` | 卡牌学习的规则 Id 列表 |
| `tbgemfusionconfig.json` | 融合石 LearnSkillRule + 参数 |

---

## 9. 变更历史

| 日期 | 内容 |
|------|------|
| 2026-06-08 | EffectGroup AddBuff/RemoveBuff 改为直接读 `SkillBuffTemplateLibrary`，废弃 EffectsConfig 间接解析 |
| 2026-06-08 | 修复 BuffApplier 百分比治疗计算、HealBuff 间隔触发 |
| 2026-06-08 | 2203001 补学规则 111004；文档化 111003/111004/112010 与 20014 配对 |
| 2026-07-06 | AddBuff 支持技能来源 `StackSourceKey` → `SkillContext`；见 `BattleRule_SkillSourceBuff_Context.md` |
| 2026-07-09 | AddBuff 支持链式学习来源参数代理（`BattleRuleSourceParamSkillProxy`）；见 `BattleRule_SkillSourceBuff_Context.md` §4.3~4.5 |
| 2026-07-17 | 根深蒂固改为 RoundStart AddBuff 30031；文档化融合石参数 + `SoldierMaxHealth` 必填；见 `BattleRule_FusionGem_DeepRooted_10000023_Context.md` |
