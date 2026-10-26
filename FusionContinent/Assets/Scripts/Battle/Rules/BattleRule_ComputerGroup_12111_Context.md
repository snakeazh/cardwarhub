# 三重绝刃（12111）ComputerGroup 配表与实现说明

> 更新：2026-06-10  
> 首条带 `ComputerGroupId` 的流派卡业务链，作为后续同类卡配表模板。

---

## 1. 配表链路

```
tbschoolcardconfig.json   Id=12111（三重绝刃）
  StarList[0].Attributes[]
    Type=115 (LearnSkillRule)
    Value=108002
        │
        ▼
tbskillruleconfig.json    Id=108002
  Trigger=6 (OnDamageBefore)
  ComputerGroupId=[300001]
  EffectGroupId=[208002]
        │                    │
        ▼                    ▼
tbcomputergroupconfig.json   tbeffectgroupconfig.json
  Id=300001                    Id=208002
```

### 1.1 学习入口（12111）

| 字段 | 值 | 说明 |
|------|-----|------|
| `Attributes[].Type` | 115 | `AttributeType.LearnSkillRule`，`Category=Module` |
| `Attributes[].Value` | 108002 | 写入 `AttributeManager.BattleLearnedSkillRuleIds` |
| `ConditionType` | 0 | 必须为 `none`，否则 `SchoolManager` 不生效属性 |

代码路径：`SchoolManager.AddSchoolCard` → `AttributeManager.SetAttributesLogic` → `AddBattleSkillRule(108002)` → `RefreshComputerGroupIndex`。

英雄专属卡：`IsAddHero>0` 时 `AddAttr(..., targetId=IsAddHero)`，规则带 `ScopeHeroCardId`；见 `BattleRule_HeroScopedSchoolCard_Context.md`。

### 1.2 规则（108002）

| 字段 | 推荐值 | 枚举 | 说明 |
|------|--------|------|------|
| `Trigger` | **6** | `OnDamageBefore` | `AddDamage` 必须走伤害管线；**不要用 7(OnDamageAfter)** |
| `ComputerGroupId` | [300001] | — | 空数组则跳过计数门槛 |
| `EffectGroupId` | [208002] | — | 达标后追加真实伤害 |
| `ConditionGroupId` | [] | — | 无额外条件 |
| `TriggerPro` | 1 | — | 100% 触发 |

### 1.3 计数（300001）

卡牌描述：**2 秒内对同一敌人连续造成 3 次伤害**。

| 字段 | 推荐值 | 枚举 | 说明 |
|------|--------|------|------|
| `CounterType` | **1** | `AttackCount` | 须与 `TriggerType` 一致，代码会校验 |
| `TriggerType` | **1** | `AttackCount` | 由 `AccumulateOutgoingDamageCount` 累加 |
| `OwnerType` | **2** | `Pair` | 英灵+怪物实例 Id 哈希，表示「同一敌人」 |
| `CounterTime` | **2** | 秒 | 时间窗口；窗口外事件从 `TriggerTimes` 剔除 |
| `MaxValue` | **3** | — | 达标阈值 |
| `AddValue` | 1 | — | 每次伤害 +1 |
| `ResetType` | **1** | `AfterTrigger` | 效果执行成功后清零 |
| `ResetParam` | 0 | — | `AfterTrigger` 不使用 |

**常见配错：**

| 错误配法 | 后果 |
|----------|------|
| `TriggerType=BeHitCount(4)` | 只在英灵受击时累加，与「造成伤害」无关 |
| `OwnerType=Target(1)` 且无 Pair | 换怪后计数可能串到错误实体 |
| `CounterTime=5` 而描述写 2 秒 | 窗口与文案不一致 |
| 累加放在命中后、判定放在 Before | 第 N 次命中判定时计数少 1 |

### 1.4 效果（208002）

卡牌描述：**附加目标最大生命值 10% 的真实伤害**。

| 字段 | 推荐值 | 枚举 | 说明 |
|------|--------|------|------|
| `EffectType` | **7** | `AddDamage` | 修改本次 `DamageContext` |
| `AttributeType` | **122** | `AddRealDamage` | 写入 `TrueDamage`；**不要用 201(Null)** |
| `SourceUnitType` | **1** | `Target` | 从**受击怪物**读来源属性 |
| `SourceAttrType` | **52** | `MonsterMaxHealth` | 目标最大生命 |
| `PreValue` | **0.1** | — | 10%；`sourceValue × PreValue` 追加真伤 |

**常见配错：**

| 错误配法 | 后果 |
|----------|------|
| `AttributeType=201(Null)` | 默认加到 `FinalDamage`，非真实伤害 |
| `SourceUnitType=0(Self)` | 从英灵读 `MonsterMaxHealth`，来源为 0 |
| `PreValue=0.2` | 实际 20%，与 10% 文案不符 |
| `Trigger=OnDamageAfter` + `AddDamage` | 命中后执行器不支持 `AddDamage` |

---

## 2. 运行时序（单帧）

```
MonsterCard.TakeDamage
  → FillOutgoingContext
  → DamagePipeline.Execute
       BattleRuleDamageModifier.Apply
         ① AccumulateOutgoingDamageCount     ← Counter +1（AttackCount / Pair）
         ② TriggerLearnedBattleRules(OnDamageBefore)
              AreComputerGroupsMet([300001])   ← 判定是否 ≥3
              ExecuteEffects([208002])         ← AddRealDamage
              ResetAfterTrigger                ← AfterTrigger 清零
         ③ OnAttackBefore / OnSkillBefore / OnCriticalBefore（其他规则）
  → MonsterDamageReceiver.Apply
       OnOutgoingHit（仅 CriticalCount，不再重复 AttackCount）
```

要点：**累加必须发生在 ①，判定在 ②**，否则第 3 次伤害时 Computer 仍为 2。

---

## 3. 代码接线清单

| 职责 | 文件 | 方法 |
|------|------|------|
| 学习规则 | `SchoolManager.cs` | `AddSchoolCard` |
| 规则注册 | `AttributeManager.cs` | `AddBattleSkillRule` |
| 计数累加 | `BattleRuleComputerSupport.cs` | `AccumulateOutgoingDamageCount` |
| 累加调用点 | `BattleRuleDamageSupport.cs` | `BattleRuleDamageModifier.Apply` |
| 计数存储/判定 | `BattleRuleComputerManager.cs` | `OnCounterEvent` / `AreComputerGroupsMet` |
| 规则触发 | `BattleRuleService.cs` | `TryExecuteRule` |
| 效果执行 | `BattleRuleDamageEffectExecutor` | `ApplyForDamagePipeline` |
| Pair 离场清理 | `MonsterCard.Die` | `OnMonsterRemoved` |

---

## 4. 测试检查清单

- [ ] 选取 12111 后日志：`[AttributeManager] 本局新增规则: RuleId=108002`
- [ ] 2 秒内对**同一只怪**造成 3 次伤害（普攻或技能均可）
- [ ] 第 3 次命中出现额外真实伤害（约 MaxHp×10%）
- [ ] 触发后对该 Pair 计数清零，需重新累计
- [ ] 换怪后计数独立，不继承上一只怪的进度
- [ ] 超过 2 秒窗口后旧次数不计入
- [ ] 旧规则（`ComputerGroupId: []`）行为不变

---

## 5. 后续同类卡配表模板

1. `SchoolCard` → `LearnSkillRule(115)` → `SkillRuleId`
2. `SkillRule.Trigger` 与效果类型匹配：
   - 改伤 / `AddDamage` → `OnDamageBefore`（6）及更早 Before 时机
   - 学 Buff / CD → `OnDamageAfter` / `NoCondition` 等 Acquire 管线
3. `ComputerGroup.TriggerType` 与代码累加入口一致
4. `OwnerType` 按「计在谁身上」显式配置（Self / Target / Pair）
5. `CounterType` 与 `TriggerType` 枚举值相同

---

## 6. 相关文档

| 文件 | 说明 |
|------|------|
| `BattleRuleService_Context.md` | 规则服务 AI 速查 |
| `EffectGroup_EffectGroupsType_Implementation.md` | `AddDamage` / `SourceUnitType` 细节 |
| `README_BattleRuleSystem.md` | 策划向完整文档 |
