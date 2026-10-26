# 流派卡英雄专属（IsAddHero）与 ScopeHeroCardId AI 上下文

> 更新：2026-07-03  
> 关联：`SchoolCardConfig.cs`、`SchoolManager.cs`、`AttributeManager.cs`、`BattleRuleHeroScopeSupport.cs`  
> 主文档：`README_BattleRuleSystem.md`  
> 击杀叠层范例：`BattleRule_RepeatableAcquire_Context.md`

---

## 1. 语义约定

| 配置字段 | 含义 |
|----------|------|
| `SchoolCardConfig.IsAddHero` | **>0** 时表示该流派卡为**英雄专属**：三选一池仅在该英灵（`cardId`）在场时出现 |
| `BattleLearnedRuleEntry.ScopeHeroCardId` | 与 `IsAddHero` 对齐；**0**=全局规则，**>0**=仅该英灵触发/计数/获得效果 |

**配表约定：**

- 英雄专属卡的三选一 **LearnSkillRule** 写在 `tbschoolcardconfig.json` → `StarList[].Attributes`，**Type=123**
- **不要**用 Type=122（`ExclusiveLearningEffects`）绕 `tbspecialeffectsconfig` 被动再挂 LearnSkillRule——专属被动路径原先会跳过 Module 属性；代码已在 `LearnPassiveSkillDirectToSoldier` 补登记，但**推荐**流派卡直接 Type=123

---

## 2. 代码链路

### 2.1 三选一选卡（推荐）

```
SchoolChooseCardService.ChooseCard
  → SchoolManager.AddSchoolCard
  → AddAttr(isAdditiveAttr, redouble=1, targetId=IsAddHero)   // IsAddHero>0 时传入
  → AttributeManager.SetAttributesLogic(..., targetId)
  → LearnSkillRule: AddBattleSkillRule(ruleId, stackKey, SchoolCard, scopeHeroCardId=targetId)
  → BattleRuleService.TriggerLearnedBattleRules / Computer 计数 / NoCondition 均按 Scope 过滤
```

### 2.2 专属被动（Type 122，可选）

```
SchoolManager.HandleExclusiveLearningEffects
  → GamePlayManager.LearnPassiveSkillCard(passiveId, 1, IsAddHero)
  → LearnPassiveSkillDirectToSoldier
  → ApplyPassiveLearnSkillRules（LearnSkillRule + scopeHeroCardId）
  → ApplyPassiveModifiersToSoldierSkills（仅 Skill 类属性）
```

---

## 3. 运行时行为（`BattleRuleHeroScopeSupport`）

| 场景 | 全局规则（Scope=0） | 英雄专属（Scope=英雄 cardId） |
|------|---------------------|-------------------------------|
| `TriggerLearnedBattleRules` | 任意英灵上下文可触发 | 仅 `context.Source` / `DamageContext.Soldier` 为该英雄时触发 |
| KillCount 累加（`ComputerGroup`） | `OwnerType.Self` + KillCount → `GlobalBattleOwnerId`（全队共享击杀数） | 按**该英灵实例** `GetInstanceID()` 单独计数 |
| 其他英灵击杀 | 计入全局池 / 可触发全局规则 | **不计入**该英雄专属计数；**不触发**其规则 |
| `NoCondition` 获得时 | 全场或默认逻辑 | 仅对场上目标英灵执行（`GetVillageGameCard(ScopeHeroCardId)`） |
| `ApplyLearnedRulesToNewSoldier` | 按 `NewSoldierSync` 同步 | 仅 `soldier.cardId == ScopeHeroCardId` 时补同步 |

**效果落点说明（击杀加属性）：**

- `OnDamageAfter` + `ModifyAttr` 时，执行器从 `ruleContext.Source` 取施法/击杀英灵，专属规则触发后属性加在**该英灵**身上（与怪物猎人全队加成不同，除非效果组刻意写全场逻辑）。

---

## 4. 配表示例：英雄专属「击杀叠属性」

### 4.1 流派卡（`tbschoolcardconfig.json`）

```json
{
  "Id": 14xxx,
  "IsAddHero": 2030001,
  "IsAdditiveAttr": 1,
  "ConditionType": 0,
  "StarList": [{
    "Attributes": [{
      "Type": 123,
      "Value": 你的RuleId,
      "ValueType": 1,
      "EffectParam": 0
    }]
  }]
}
```

| 字段 | 要求 |
|------|------|
| `IsAddHero` | 目标英灵 `cardId` |
| `ConditionType` | **必须为 0**（`none`），否则 `SchoolManager` 不执行 `AddAttr` |
| `Attributes[].Type` | **123**（LearnSkillRule），非 122 |

### 4.2 规则（`tbskillruleconfig.json`）

参考怪物猎人 **108002** / 装备 **112012**：

| 字段 | 击杀叠属性建议 |
|------|----------------|
| `Trigger` | **7**（`OnDamageAfter`） |
| `ComputerGroupId` | KillCount 组（可新建，勿与全队规则共用同一组除非明确意图） |
| `EffectGroupId` | `ModifyAttr` 英灵属性 |
| `NewSoldierSync` | **2**（`ByTriggerCount`，可叠层时） |

### 4.3 ComputerGroup（英雄专属 KillCount）

专属规则学习后，代码对 **ScopeHeroCardId>0** 且引用该 `ComputerGroupId` 的规则：

- 击杀累加使用 **击杀者英灵实例 Id**（非 `GlobalBattleOwnerId`）
- 建议为英雄专属卡**单独建** ComputerGroup Id，避免与「怪物猎人」等全队击杀规则混用同一计数池

```json
{
  "Id": 3000xx,
  "CounterType": 0,
  "CounterTime": 0,
  "OwnerType": 0,
  "TriggerType": 0,
  "AddValue": 1,
  "MaxValue": 5,
  "ResetType": 1,
  "ResetParam": 0
}
```

---

## 5. API 变更

```csharp
// AttributeManager
AddBattleSkillRule(int ruleId, string stackSourceKey, AttributeSourceType sourceType, int scopeHeroCardId = 0)

// BattleLearnedRuleEntry
public int ScopeHeroCardId { get; }  // 0=全局

// BattleRuleService
TryTriggerNoConditionOnAcquire(BattleLearnedRuleEntry entry)  // 含英雄专属分支
```

---

## 6. 日志与排查

### 学习成功

```
[AttributeManager] 本局新增规则: RuleId=xxx, ..., ScopeHero=2030001
```

### 常见问题

| 现象 | 原因 |
|------|------|
| 选卡后无规则日志 | `ConditionType != 0`；或 LearnSkillRule 写在被动上且未走 `ApplyPassiveLearnSkillRules` |
| 其他英雄击杀也叠层 | 规则 `ScopeHeroCardId=0`（未设 `IsAddHero` 或未传 targetId）；或 ComputerGroup 与全局规则共用 |
| 专属英雄击杀不触发 | `Trigger` 不是击杀后时机；`ComputerGroup.MaxValue` 未达标；RuleId 配置不存在 |
| 三选一看不到卡 | `IsAddHero` 对应英雄未上场（`CanCardAppearInChoosePool`） |

---

## 7. 关联文件

| 文件 | 职责 |
|------|------|
| `BattleLearnedRuleEntry.cs` | `ScopeHeroCardId` |
| `BattleRuleHeroScopeSupport.cs` | 触发过滤、专属 KillCount |
| `SchoolManager.cs` | `AddAttr(..., IsAddHero)` |
| `AttributeManager.cs` | `AddBattleSkillRule(..., scopeHeroCardId)` |
| `BattleRuleService.cs` | 按 entry 遍历触发；专属 NoCondition |
| `BattleRuleComputerManager.cs` | 专属 KillCount → 英灵实例 Owner |
| `GamePlayManager.cs` | `ApplyPassiveLearnSkillRules` |

---

## 8. 测试清单

- [ ] `IsAddHero>0` 的流派卡：仅目标英雄在场时进三选一池
- [ ] 选卡后日志含 `ScopeHero=目标 cardId`
- [ ] 目标英雄击杀：KillCount 累加、达标后 `ModifyAttr` 生效
- [ ] 其他英雄击杀：不推进该专属规则的计数、不触发其效果
- [ ] 中场召唤同 cardId 新英灵：`NewSoldierSync=ByTriggerCount` 时按触发次数补层
- [ ] Type 122 专属被动上的 LearnSkillRule（若使用）：`LearnPassiveSkillDirectToSoldier` 同样带 Scope
