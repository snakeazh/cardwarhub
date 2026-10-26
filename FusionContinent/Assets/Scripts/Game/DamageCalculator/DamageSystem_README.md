# 伤害系统重构说明

> 文档生成时间：2026-06-05  
> 适用版本：FusionContinent `cardwar1.9_dev`  
> 重构目标：计算与结算分离，保持原有战斗公式与 Modifier 顺序不变。

---

## 1. 目标架构

```
SkillRuntime / 怪物攻击
        ↓
DamageContextBuilder        ← 构建 DamageContext（只读属性，不改角色状态）
        ↓
DamageContext
        ↓
DamagePipeline.Execute()    ← 伤害公式计算（不改角色状态、不扣血）
        ↓
DamageResult
        ↓
DamageReceiver.ApplyDamage()← 分发入口
        ├── SoldierDamageReceiver.Apply()   英灵结算
        └── MonsterDamageReceiver.Apply()   怪物结算
        ↓
SoldierCard.TakeDamage(result) / MonsterCard.TakeDamage(result)
        （Card 仅作聚合入口，不含公式与结算细节）
```

---

## 2. 职责划分

| 模块 | 职责 | 禁止 |
|------|------|------|
| `DamageContextBuilder` | 填充 `DamageContext` 输入字段；提供 `CalculateIncoming` 快捷入口 | 扣血、护盾、事件、动画 |
| `DamagePipeline` | 按固定顺序执行 `IDamageModifier` 节点 | 修改 `SoldierCard` / `MonsterCard` 状态 |
| `DamageResult` | 从 `DamageContext` 构造结算快照 | 副作用 |
| `SoldierDamageReceiver` | 英灵：护盾 → 扣血 → 飘字 → 受击事件 | 伤害公式 |
| `MonsterDamageReceiver` | 怪物：护盾 → 扣血 → 死亡 → 飘字 → 受击事件 | 伤害公式 |
| `DamageReceiver` | 按 `Target` 类型分发到具体 Receiver | 具体结算逻辑 |
| `ShieldResolver` | 英灵普通护盾 / 月影盾吸收（结算后处理） | 伤害公式 |
| `SoldierCard` | 前置校验、`TakeDamage(result)` 委托、治疗飘字 | 伤害公式、受击结算 |
| `MonsterCard` | 前置校验、技能加成、构建上下文、执行管线、`TakeDamage(result)` 委托 | 伤害公式、受击结算 |

---

## 3. 核心类型

### 3.1 `DamageResult`

```csharp
public struct DamageResult
{
    public float FinalDamage;
    public bool IsCrit;
    public bool IsMiss;
    public object Attacker;   // MonsterCard 或 SoldierCard
    public object Target;     // SoldierCard 或 MonsterCard
    public int SkillId;
    public DamageContext Context;  // 扩展字段：TrueDamage、IsExecution 等
}
```

- 由 `DamageResult(DamageContext ctx)` 构造。
- `MonsterDamage` 管线：`Attacker = Monster`，`Target = Soldier`。
- 其他管线：`Attacker = Soldier`，`Target = Monster`。
- `IsMiss == true` 时 `FinalDamage = 0`。

### 3.2 `DamageApplyOptions`

结算阶段附加参数（不参与管线公式）：

| 字段 | 用途 |
|------|------|
| `HitPosition` | 怪物受击飘字/落雷位置 |
| `DamageType` | 伤害类型（飘字、吸血判定） |
| `Skill` | 技能来源 |
| `IsAgainDamage` | 连击/追加伤害标记 |
| `Recorder` | 英灵伤害调试记录器 |

### 3.3 `DamagePipelineType`

| 类型 | 场景 |
|------|------|
| `MonsterDamage` | 怪物攻击英灵 |
| `SoldierDamage` | 英灵技能/普攻攻击怪物 |
| `SceneSkillDamage` | 无攻击者的场景技能 |
| `BuffSkillDamage` | Buff / DOT 伤害 |

---

## 4. 管线 Modifier 顺序（未修改）

### MonsterDamage（怪物 → 英灵）

```
AttributesModifier → DodgeModifier → TakeDamageModifier → ResistanceModifier
→ CritFinalModifier → FragileModifier → ComboModifier
→ BrokenStateDamageModifier → ExecutionThresholdModifier
```

### SoldierDamage（英灵 → 怪物）

```
BattleRuleDamageModifier → AttributesModifier → TakeDamageModifier
→ StrengthModifier → ResistanceModifier → CritFinalModifier
→ FragileModifier → SoulFlawsBreakModifier
```

### BuffSkillDamage

```
ResistanceModifier → FragileModifier
```

### SceneSkillDamage

```
ResistanceModifier
```

---

## 5. 调用链示例

### 5.1 英灵受击（怪物攻击）

```
MainCity / GamePlayManager / BurnBuff
  → soldier.CanReceiveIncomingDamage(damage)
  → DamageContextBuilder.CalculateIncoming(soldier, monster, type, damage)
      → FillIncomingContext()
      → DamagePipeline(MonsterDamage).Execute(ctx)
  → soldier.TakeDamage(result)
  → DamageReceiver.ApplyDamage(result)
  → SoldierDamageReceiver.Apply()
      → ShieldResolver.Resolve()
      → 扣血 / 飘字 / HeroAfterHarmed 事件
```

**入口文件：**

- `Assets/Scripts/Game/MainCity.cs`
- `Assets/Scripts/Game/GamePlayManager.cs`（`VillageSoldierTaskDamage`）
- `Assets/Scripts/Game/Buff/Logic/BurnBuff.cs`

### 5.2 怪物受击（技能 / 普攻）

```
SkillRuntime 预掷暴击 → monster.damageContext.HasPreRolledCrit
  → MonsterDamageService.ReceiveDamage(request)
      → FillOutgoingContext → PreHitMonsterHp = target.hp
      → PrepareTemplateExecuteThreshold（SkillContext.HitExecuteBuffIds）
      → DamagePipeline(pipelineType).Execute(damageContext)
      → ApplyExecuteDamageOverride（PreHitHp ≤ 斩杀线 → FinalDamage=99999）
  → MonsterCard.TakeDamage(result, options)
  → MonsterDamageReceiver.Apply()
      → 护盾/HP/吸血/事件/韧性/死亡（IsExecution 飘字 99999）
```

**暴击预掷写入：**

- `Assets/Scripts/Game/Skill/SkillRuntime.cs`
- `Assets/Scripts/Game/Monster/MonsterManager.cs`
- `Assets/Scripts/Game/Projectile/BulletDamageDetector.cs`

### 5.3 模板斩杀（Execute Buff）

与 **5.2** 合并实现，详见：

`Assets/Scripts/Game/Skill/Doc/ExecuteBuff_配置说明.md`

| 步骤 | 说明 |
|------|------|
| 注册 | `BuffApplier.RegisterHitExecuteBuff`（投射物 AttachBuff / ApplyBuff 节点） |
| 斩杀线 | `PrepareTemplateExecuteThreshold` → `DamageContext.TemplateExecuteThreshold` |
| 判定血量 | `PreHitMonsterHp`（本段伤害前，非扣血后） |
| 升格 | `ApplyExecuteDamageOverride`：`FinalDamage = 99999`，`IsExecution = true` |
| 模块斩杀 | `MonsterExecutionThreshold` 比例斩杀，与模板斩杀 OR 合并 |

**注意**：`ExecutionThresholdModifier` 仅用于 `MonsterDamage` 管线（怪物打英灵）；英灵打怪物的斩杀升格在 `MonsterDamageService.ApplyExecuteDamageOverride`。

---

## 6. 文件清单

### 6.1 本次重构新增 / 核心文件

| 文件 | 说明 |
|------|------|
| `DamageContextBuilder.cs` | 构建受击/出站 `DamageContext` |
| `DamageResult.cs` | 管线结算结果 struct |
| `DamageApplyOptions.cs` | 结算阶段附加参数 |
| `DamageReceiver.cs` | 结算分发入口 |
| `SoldierDamageReceiver.cs` | 英灵受击结算 |
| `MonsterDamageReceiver.cs` | 怪物受击结算 |
| `DamageSystem_README.md` | 本文档 |

### 6.2 既有管线文件（公式未改）

| 文件 | 说明 |
|------|------|
| `DamageContext.cs` | 伤害计算上下文 |
| `DamagePipeline.cs` | 管线执行器，`Execute()` 返回 `DamageResult` |
| `DamagePipelineType.cs` | 管线类型枚举 |
| `IDamageModifier.cs` | Modifier 接口 |
| `ShieldResolver.cs` | 英灵护盾结算 |
| `DamageModifier/AttributesModifier.cs` | 全局增伤 |
| `DamageModifier/DodgeModifier.cs` | 闪避 |
| `DamageModifier/TakeDamageModifier.cs` | 受击加减成 |
| `DamageModifier/ResistanceModifier.cs` | 抗性 / 穿透 |
| `DamageModifier/CritModifier.cs` | 暴击判定 |
| `DamageModifier/CritFinalModifier.cs` | 暴击伤害 |
| `DamageModifier/FragileModifier.cs` | 易伤 |
| `DamageModifier/ComboModifier.cs` | 连击增伤 |
| `DamageModifier/StrengthModifier.cs` | 力量分摊 |
| `DamageModifier/BrokenStateDamageModifier.cs` | 虚弱/精英/Boss 修正 |
| `DamageModifier/ExecutionThresholdModifier.cs` | 斩杀阈值 |
| `DamageModifier/SoulFlawsBreakModifier.cs` | 破盾增伤 |
| `DamageModifier/BaseDamageModifier.cs` | Modifier 基类 |

### 6.3 兼容 / 遗留文件

| 文件 | 说明 |
|------|------|
| `SoldierDamageCalculator.cs` | 旧 API 兼容层，内部委托 `DamageContextBuilder` |
| `MonsterDamageCalculator.cs` | 旧怪物计算器（当前主流程未使用） |
| `DamageCalculationRecorder.cs` | 伤害调试记录器 |

### 6.4 聚合入口（Card 层）

| 文件 | 变更要点 |
|------|----------|
| `Assets/Scripts/Game/SoldierCard.cs` | `TakeDamage(DamageResult)` 仅委托 `DamageReceiver`；保留 `CanReceiveIncomingDamage`、`DamageEffect`（治疗飘字） |
| `Assets/Scripts/Game/MonsterCard.cs` | 主 `TakeDamage(...)` 负责前置校验 + 管线；`TakeDamage(DamageResult)` 仅委托；提供 `internal` 视觉钩子 |

### 6.5 关联系统文件（只读引用，未改公式）

| 文件 | 关联 |
|------|------|
| `Assets/Scripts/Battle/Rules/BattleRuleDamageSupport.cs` | `BattleRuleDamageModifier`、命中后规则 |
| `Assets/Scripts/Battle/Rules/BattleRuleEffectGroupsApplier.cs` | Buff 规则修正伤害上下文 |
| `Assets/Scripts/Game/GamePlayManager.cs` | `GetDamagePipeline()` 管线注册 |
| `Assets/Scripts/Game/Skill/SkillBase.cs` | `SumDamageTypes()` 技能基础攻击力 |
| `Assets/Scripts/Game/Skill/SkillRuntime.cs` | 暴击预掷 |
| `Assets/Scripts/Game/Monster/MonsterAttribute.cs` | 怪物抗性（旧路径残留） |

### 6.6 关联上下文文档（2026-06-08）

| 文档 | 内容 |
|------|------|
| `Assets/Scripts/Battle/Rules/BattleRule_BuffEffect_Context.md` | 规则 AddBuff/RemoveBuff、2203001/2202006、HealBuff |
| `Assets/Scripts/Battle/Rules/BattleRuleService_Context.md` | 规则服务 AI 速查 |
| `Assets/Scripts/Game/Skill/Config/SkillBuffTemplateLibrary_README.md` | Buff 模板库与 EffectGroup BaseValue |

---

## 7. 已删除文件

| 文件 | 原因 |
|------|------|
| `DamageCalculator.cs` | 职责已拆入 `DamageContextBuilder` + `DamagePipeline` + `DamageReceiver` |

---

## 8. 设计约束（重构时遵守）

1. **不修改任何伤害公式** — 所有数值逻辑仍在 `IDamageModifier` 内。
2. **不修改 Modifier 顺序** — `DamagePipeline.BuildPipeline()` 注册顺序保持原样。
3. **不修改 Buff 生效顺序** — `BattleRuleDamageModifier` 仍在 `SoldierDamage` 管线首位。
4. **计算与结算分离** — `DamagePipeline` / `DamageContextBuilder` 不产生副作用。
5. **Card 作聚合** — `TakeDamage(DamageResult)` 不包含公式与结算实现。

---

## 9. 上下文文件索引（AI / 开发参考）

以下为本次重构分析、实现时读取和修改的完整上下文文件列表：

### 新建

```
Assets/Scripts/Game/DamageCalculator/DamageContextBuilder.cs
Assets/Scripts/Game/DamageCalculator/DamageApplyOptions.cs
Assets/Scripts/Game/DamageCalculator/DamageReceiver.cs
Assets/Scripts/Game/DamageCalculator/SoldierDamageReceiver.cs
Assets/Scripts/Game/DamageCalculator/MonsterDamageReceiver.cs
Assets/Scripts/Game/DamageCalculator/MonsterDamageService.cs
Assets/Scripts/Game/DamageCalculator/MonsterDamageRequest.cs
Assets/Scripts/Game/DamageCalculator/DamageSystem_README.md
```

### 修改

```
Assets/Scripts/Game/DamageCalculator/DamageResult.cs
Assets/Scripts/Game/DamageCalculator/DamagePipeline.cs
Assets/Scripts/Game/DamageCalculator/SoldierDamageCalculator.cs
Assets/Scripts/Game/SoldierCard.cs
Assets/Scripts/Game/MonsterCard.cs
Assets/Scripts/Game/MainCity.cs
Assets/Scripts/Game/GamePlayManager.cs
Assets/Scripts/Game/Buff/Logic/BurnBuff.cs
```

### 删除

```
Assets/Scripts/Game/DamageCalculator/DamageCalculator.cs
```

### 只读分析（未改公式）

```
Assets/Scripts/Game/DamageCalculator/DamageContext.cs
Assets/Scripts/Game/DamageCalculator/DamagePipelineType.cs
Assets/Scripts/Game/DamageCalculator/ShieldResolver.cs
Assets/Scripts/Game/DamageCalculator/IDamageModifier.cs
Assets/Scripts/Game/DamageCalculator/DamageModifier/*.cs
Assets/Scripts/Game/DamageCalculator/MonsterDamageCalculator.cs
Assets/Scripts/Game/DamageCalculationRecorder.cs
Assets/Scripts/Battle/Rules/BattleRuleDamageSupport.cs
Assets/Scripts/Battle/Rules/BattleRuleEffectGroupsApplier.cs
Assets/Scripts/Game/Skill/SkillRuntime.cs
Assets/Scripts/Game/Skill/SkillBase.cs
Assets/Scripts/Game/Monster/MonsterManager.cs
Assets/Scripts/Game/Projectile/BulletDamageDetector.cs
Assets/Scripts/Game/Monster/MonsterAttribute.cs
Assets/Scripts/Game/Buff/Logic/AbnormalBuff.cs
Assets/Scripts/Game/Buff/Logic/HealBuff.cs
Assets/Scripts/Game/Buff/EffectsExecutor.cs
Assets/Scripts/Game/ArrowBullet.cs
```

---

## 10. 后续可优化项

- [x] 将 `MonsterCard.TakeDamage(...)` 中的 **SkillDamage / 灼烧加成** 前置逻辑迁入 `DamageContextBuilder.ApplyPrePipelineModifiers`
- [x] 怪物受击编排迁入 `MonsterDamageService`，`MonsterCard` 仅保留 `ReceiveDamage` + `TakeDamage(DamageResult)`
- [ ] 统一 `SoldierCard.DamageEffect` 与 `SoldierDamageReceiver.ShowFloatingText` 飘字实现，消除重复
- [ ] 废弃 `MonsterDamageCalculator`，统一走 `DamagePipeline`
- [ ] 为 `DamageReceiver` 补充单元测试（闪避、护盾穿透、斩杀、暴击）

---

## 11. 快速接入指南

### 新增「英灵受击」来源

```csharp
if (soldier.CanReceiveIncomingDamage(baseDamage))
{
    var result = DamageContextBuilder.CalculateIncoming(
        soldier, monsterAttacker, damageType, baseDamage);
    int applied = soldier.TakeDamage(result);
}
```

### 新增「怪物受击」来源

```csharp
// 技能多类型伤害
monster.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(
    monster, skillCard, hitPos, damageInheritance, soldier));

// 单段伤害（Buff/DOT/弹道等）
monster.ReceiveDamage(MonsterDamageRequest.Single(
    monster, damageType, baseDamage, skill, hitPos,
    soldier, pipelineType));
```
