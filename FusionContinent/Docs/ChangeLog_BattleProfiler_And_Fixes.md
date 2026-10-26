# 战斗 Profiler 埋点、Buff 修复与 BakeMesh 泄漏修复 — 变更说明

> 生成日期：2026-07-01  
> 范围：本对话中涉及的性能检测埋点、条件编译、Bug 修复相关文件。

---

## 1. 变更总览

| 类别 | 目的 | Release 包影响 |
|------|------|----------------|
| **BattleSkillProfilerMarkers** | 技能/Buff/对象池等链路 CPU、GC 拆分采样 | `Begin()` 为 no-op，不采样 |
| **BuffTrigger 子 Marker** | 定位 `ApplyBuff` 链路耗时与 GC 来源 | 同上 |
| **`#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`** | 正式包不创建真实 `ProfilerMarker` | 调用代码仍进包，开销极低 |
| **BakeMeshAnimationPlayer.Release** | 修复运行时 `Mesh` 原生泄漏 | 修复泄漏，应进正式包 |
| **MoonlightShieldBuff.OnRemove** | 修复 `OnDestroy` 时 NRE | 修复崩溃，应进正式包 |
| **AttributeManager.SetAttributeLogic** | `GamePlayManager.Default` 空防护 | 修复 teardown 崩溃 |

---

## 2. 文件清单

### 2.1 核心：Profiler 定义与条件编译

| 文件 | 变更类型 | 说明 |
|------|----------|------|
| `Assets/Scripts/Battle/Profiling/BattleSkillProfilerMarkers.cs` | 新增/重构 | 所有 Marker 定义；`Begin()` 统一入口；Release 下 `object` 占位 + `return null` |

### 2.2 BuffTrigger 子采样（业务逻辑未改，仅加 `using`）

| 文件 | 埋点 |
|------|------|
| `Assets/Scripts/Game/Skill/Handler/Buff/ApplyBuffHandler.cs` | `BuffTrigger`、`ResolveTargets`、`TriggerCondition`、`ApplyBuff`、`MoonlightBundle`、`TempList` |
| `Assets/Scripts/Game/Skill/Handler/Buff/BuffApplier.cs` | `CreateBuff`、`CreateContext`、`TriggerCondition`、`DamageModifier`、`TempList` |
| `Assets/Scripts/Game/Skill/Config/SkillBuffTemplateLibrary.cs` | `GetTemplate`、`TempList` |
| `Assets/Scripts/Game/Buff/BuffController.cs` | `IterateBuff` |
| `Assets/Scripts/Game/Buff/BuffBase.cs` | `EventDispatch`（`PlayBuffVfx`） |
| `Assets/Scripts/Game/Buff/Logic/AttributeModifierBuff.cs` | `TempList`、`AttributeModify`、`CounterUpdate` |
| `Assets/Scripts/Game/Buff/Logic/MultiEffectBuff.cs` | `TempList`、`RuleCheck`、`AttributeModify` |

### 2.3 其它战斗链路 Marker（`.Auto()` → `Begin()`）

| 文件 | Marker |
|------|--------|
| `Assets/Scripts/Battle/Scheduler/BattleScheduler.cs` | `UpdateSkill` |
| `Assets/Scripts/Battle/Scheduler/BattleCastDispatcher.cs` | `SkillRelease` |
| `Assets/Scripts/Battle/Rules/BattleRuleCastSupport.cs` | `RuleExecute` |
| `Assets/Scripts/Battle/Rules/BattleRuleEffectGroupsApplier.cs` | `RuleExecute` |
| `Assets/Scripts/Game/Skill/Handler/SkillNodeExecutor.cs` | `SkillRelease` |
| `Assets/Scripts/Game/SoldierCard.cs` | `SkillRelease`、`TargetSearch` |
| `Assets/Scripts/Game/Soldier/SoldierSkillExecutor.cs` | `SkillRelease`、`TargetSearch`、`CounterUpdate` |
| `Assets/Scripts/Game/Target/EnemyTargetFinder.cs` | `TargetSearch` |
| `Assets/Scripts/Game/Skill/Handler/Action/ProjectileHandler.cs` | `BulletSpawn` |
| `Assets/Scripts/Game/EffectManager.cs` | `EffectSpawn`、`ObjectPoolRelease` |
| `Assets/Scripts/Game/GameLand.SkillCards.cs` | `TweenPlay` |
| `Assets/Scripts/Game/DamageCalculator/MonsterDamageService.cs` | `DamagePipeline` |
| `Assets/Scripts/Game/Monster/MonsterManager.cs` | `DamagePipeline` |
| `Assets/Scripts/Manager/ConsecutiveSkillReleaseCounter.cs` | `CounterUpdate` |
| `Assets/Scripts/Pool/GameObjectPool.cs` | `ObjectPoolGet`、`ObjectPoolRelease` |
| `Assets/Scripts/Pool/GenericObjectPool.cs` | `ObjectPoolGet`、`ObjectPoolRelease` |

### 2.4 Bug 修复

| 文件 | 变更类型 | 说明 |
|------|----------|------|
| `Assets/Scripts/Game/Monster/Animation/BakeMeshAnimationPlayer.cs` | 修复 | `Release()` 中 `Destroy(_runtimeMesh)` 并清空引用（commit `121ebb64b`） |
| `Assets/Scripts/Game/Buff/Logic/MoonlightShieldBuff.cs` | 修复 | `RemoveSpellPowerModifier` 改为 `BaseAttributes.RemoveModifier`，不再走 `AttributeManager` |
| `Assets/Scripts/Manager/AttributeManager.cs` | 防护 | `Skill`/`Soldier`/`Monster` 分支访问 `GamePlayManager.Default` 前判空 |

---

## 3. Profiler 使用说明

### 3.1 调用方式（统一）

```csharp
using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTrigger))
{
    // 业务逻辑
}
```

**不要**再使用 `marker.Auto()`，统一走 `Begin()`。

### 3.2 生效环境

| 环境 | `Begin()` 行为 | Profiler 可见 |
|------|----------------|---------------|
| Unity Editor | `marker.Auto()` | ✅ |
| Development Build | `marker.Auto()` | ✅（需连接 Profiler） |
| 正式 Release | `return null` | ❌ |

条件编译符号：`UNITY_EDITOR` 或 `DEVELOPMENT_BUILD`。

### 3.3 Release 包说明

- `using (BattleSkillProfilerMarkers.Begin(...)) { ... }` **仍会编译进包体**。
- Release 下仅 `Begin()` 为空操作；`{ }` 内业务逻辑必须保留。
- 各调用文件中的 `using Unity.Profiling;` **多数可删除**（未直接使用 `ProfilerMarker` 类型）；仅 `BattleSkillProfilerMarkers.cs` 在 Dev 分支需要。

### 3.4 BuffTrigger 子 Marker 树

```
BuffTrigger
├── BuffTrigger.GetTemplate
├── BuffTrigger.ResolveTargets
├── BuffTrigger.TriggerCondition
├── BuffTrigger.ApplyBuff
│   ├── BuffTrigger.CreateBuff
│   ├── BuffTrigger.CreateContext
│   └── BuffTrigger.DamageModifier
├── BuffTrigger.IterateBuff
├── BuffTrigger.AttributeModify
├── BuffTrigger.RuleCheck
├── BuffTrigger.CounterUpdate
├── BuffTrigger.TempList
├── BuffTrigger.EventDispatch
└── BuffTrigger.MoonlightBundle
```

### 3.5 注意事项

- **禁止**用 Marker 包裹 `await` / UniTask 跨帧逻辑（会导致 `EndSample` 不匹配）。
- Profiler 录帧会占用大量内存；Editor 分析时控制帧数，避免系统页面文件耗尽。

---

## 4. Bug 修复详情

### 4.1 BakeMesh 运行时 Mesh 泄漏

**问题**：每次怪物禁用/重绑 `new Mesh()`，`Release()` 未 `Destroy`，Native 内存单调增长。

**修复**（`BakeMeshAnimationPlayer.Release`）：

- `Object.Destroy(_runtimeMesh)`
- 清空 `_frameData`、`_skeletonAnimation`、`_meshFilter`、`_tintedColors` 等引用

**验证**：Memory Profiler 对比多轮刷怪前后 `Mesh`（名称 `BakeMeshRuntime`）数量应不再线性增长。

### 4.2 MoonlightShield OnDestroy NRE

**堆栈**：

```
SoldierCard.OnDestroy → BuffController.ClearAll → MoonlightShieldBuff.OnRemove
→ RemoveSpellPowerModifier → AttributeManager.SetAttributeLogic
→ GamePlayManager.Default.TraverseVillageSoldier  // Default 已为 null
```

**修复**：

- 移除 modifier 改为直接 `soldier.BaseAttributes.GetAttr(...).RemoveModifier(...)`
- `AttributeManager` 在 teardown 时对 `GamePlayManager.Default` 判空早退

**验证**：带月影护盾的英灵死亡、切场景、结束战斗不再报 NRE。

---

## 5. 静态分析：BuffTrigger 热点（供 Profiler 对照）

| 优先级 | 子步骤 | 说明 |
|--------|--------|------|
| 1 | `GetTemplate` | `GetEffects()` 每次 `new List` + 深拷贝模板 |
| 2 | `ResolveTargets` | 在 `foreach (effectConfig)` 内重复解析；群攻遍历全场单位 |
| 3 | `ApplyBuff` | 每目标再次 `GetEffects` |
| 4 | `AttributeModify` | 每次 OnAdd `new Dictionary` + `AttributeModifier` |
| 5 | `IterateBuff` | `AddBuff` 线性扫描已有 Buff |

> 以上为定位用推断，精确排名以 Profiler 子 Marker 实测为准。

---

## 6. 测试检查清单

### Profiler / 构建

- [ ] Editor Play：CPU 树可见 `Skill.Release`、`BuffTrigger.*` 等
- [ ] Development Build 真机：连接 Profiler 可见同名 Marker
- [ ] Release 包：功能正常，无 Profiler 相关异常

### BakeMesh

- [ ] 多轮刷怪/回池后 Native `Mesh` 数量稳定
- [ ] BakeMesh 怪物动画正常

### Buff / 月影护盾

- [ ] 月影护盾施加、叠层、受击消耗、时间到期
- [ ] 英灵 `OnDestroy` / 切场景无 NRE
- [ ] 魔法强度 modifier 施加与移除正确

---

## 7. 后续可选优化（本次未做）

- [ ] 删除各文件多余 `using Unity.Profiling;`
- [ ] `GetEffects` 缓存，避免 Handler + Applier 重复分配
- [ ] `ResolveBuffTargets` 移出 `effectConfig` 循环
- [ ] Release 包调用点级 `#if` 完全去掉 `using (Begin...)` 壳（包体极致优化）
- [ ] `BakeMeshAnimationPlayer` 内 `Profiler.BeginSample` 同样加 `#if`

---

## 8. 相关 Git 提交

| Commit | 说明 |
|--------|------|
| `121ebb64b` | fix: destroy BakeMesh runtime mesh on Release to stop native leak |

（Profiler 条件编译、BuffTrigger 子 Marker、MoonlightShield / AttributeManager 修复如未单独提交，以工作区 diff 为准。）
