# EffectManager 局内战斗特效规范

> **默认规则：战斗中（局内）所有视觉 VFX 统一走 `EffectManager`，禁止直接使用 `ObjectPoolManager` / `CommonItemPoolManager` 播放特效。**  
> 最后更新：2026-06-22

---

## 0. 适用范围与命名空间（必记）

### 适用范围

| 属于本系统 | 不属于本系统 |
|------------|--------------|
| 战斗中技能 / Buff / 投射物 / 命中 / AOE / 死亡等 **视觉特效** | UI 界面特效、主城、Loading 等非战斗场景 |
| `GamePlayManager` 局内流程中的 VFX | `BattleRuleEffectManager` 的 **规则效果**（数值/Buff/触发，非 VFX） |

### 命名空间对照

| 类 | 命名空间 | 职责 |
|----|----------|------|
| `EffectManager` | **全局（无命名空间）** | 战斗 VFX 播放 / 回收 / 预加载 |
| `EffectLifecycle` | **全局（无命名空间）** | 粒子 / 动画 / 定时自动回池 |
| `EffectLayer` | **全局（无命名空间）** | 特效分层：`World` / `Character` / `UI` |
| `BattleRuleEffectManager` | `Game.Battle` | ⚠️ **规则效果组调度，不是视觉特效，勿混用** |

### 调用方式

`EffectManager` 为全局类，**无需额外 `using`**，直接：

```csharp
EffectManager.Instance.PlayEffect("HeroDeathEffect01", position);
EffectManager.Instance.RecycleEffect(effect);
```

技能 Handler 等代码位于 `FusionContinent.Skill.Handler` 等命名空间下，同样直接调用全局 `EffectManager.Instance`。

### 相关文件路径

| 文件 | 路径 |
|------|------|
| 特效管理器 | `Assets/Scripts/Game/EffectManager.cs` |
| 生命周期组件 | `Assets/Scripts/Game/EffectLifecycle.cs` |
| 技能特效 Handler | `Assets/Scripts/Game/Skill/Handler/Action/EffectHandler.cs` |
| 动画特效 Handler | `Assets/Scripts/Game/Skill/Handler/Action/AnimatedEffectHandler.cs` |

---

## 1. 为什么统一 EffectManager

| 能力 | EffectManager | ObjectPoolManager |
|------|---------------|-------------------|
| 预加载 / 同步加载 | ✅ YooAsset + 对象池 | 仅对象池 |
| 分层（World / Character / UI） | ✅ | ❌ |
| 粒子 / 动画 / 定时自动回收 | ✅ `EffectLifecycle` | 仅 `DelayReturn` |
| 挂角色下延迟回池 | ✅ 避免 OnDisable 改 parent | ❌ |
| 投射物专用生命周期 | ✅ `PlayProjectileEffect` | ❌ |
| 战斗结束清池 | ✅ `ClearAllPools` | 分散管理 |

---

## 2. 核心 API

文件：`Assets/Scripts/Game/EffectManager.cs`

```csharp
// 普通特效（粒子/动画/定时自动回收，或 lifetime 指定秒数）
GameObject PlayEffect(
    string effectName,
    Vector3 position,
    float lifetime = -1f,          // -1=不启定时器；0=默认5s；>0=指定秒数
    Vector3? scale = null,
    Quaternion? rotation = null,
    Transform parent = null,       // 挂角色时用，回池会延迟一帧
    EffectLayer layer = EffectLayer.World);

// 投射物特效（按 maxTime 定时回收，配合 Projectile_new）
GameObject PlayProjectileEffect(
    string effectName,
    Vector3 position,
    float maxTime,
    Vector3? scale = null,
    ...);

// 手动回收（DOTween 动画结束、技能取消、Buff 移除等）
void RecycleEffect(GameObject effect);
```

---

## 3. 使用场景对照

| 场景 | 推荐 API | 回收方式 |
|------|----------|----------|
| 瞬时命中特效 | `PlayEffect(name, pos)` | 自动（粒子/动画/Timer） |
| 固定时长特效 | `PlayEffect(name, pos, lifetime: 1.9f)` | 自动 |
| 挂角色 Buff 特效 | `PlayEffect(name, pos, parent: character)` | `RecycleEffect` 或 Buff 结束 |
| 投射物 | `PlayProjectileEffect(name, pos, maxTime)` | Timer / `ProjectileManager` |
| DOTween 位移动画特效 | `PlayEffect` + 手动 `RecycleEffect` | `OnComplete` / `OnKill` |
| 技能图节点 `Effect` | `EffectHandler` | 追踪 `context.TrackedEffects` |
| 技能图节点 `AnimatedEffect` | `AnimatedEffectHandler` | 同上 |

---

## 4. 技能 Handler 参考实现

已统一使用 `EffectManager` 的 Handler：

- `EffectHandler.cs` — 静态特效
- `AnimatedEffectHandler.cs` — Fall / Rise / zhanji / RequiemChant
- `ProjectileHandler.cs` / `FanProjectileHandler.cs` / `WaveProjectileHandler.cs` — 投射物
- `AoeDamageHandler.cs` — 命中特效

**AnimatedEffect 手动回收模板（DOTween）：**

```csharp
var effect = EffectManager.Instance.PlayEffect(vfxPath, targetPos, scale: vfxScale);
context.TrackedEffects.Add(effect);

bool recycled = false;
void RecycleEffect()
{
    if (recycled || effect == null || !effect.activeInHierarchy) return;
    recycled = true;
    EffectManager.Instance.RecycleEffect(effect);
}

effect.transform.DOMove(endPos, duration)
    .OnComplete(RecycleEffect)
    .OnKill(RecycleEffect);   // 战斗切局 DOTween.KillAll 时防残留
```

---

## 5. 禁止事项

❌ 局内新代码使用 `ObjectPoolManager.Instance.PlayGameObjectPool(...)` 播放 VFX  
❌ 局内新代码使用 `CommonItemPoolManager` 播放战斗特效  
❌ 绕过 `EffectManager` 直接 `Instantiate` 战斗特效 prefab  
❌ 手动 `pool.Return(effect)` 而不走 `EffectManager.RecycleEffect`（挂角色场景会 parent 冲突）

✅ UI / 非战斗场景对象池仍可用各自 PoolManager（与战斗 VFX 分离）  
✅ `Game.Battle.BattleRuleEffectManager` 是规则效果调度（`Game.Battle` 命名空间），与全局 `EffectManager` 视觉特效无关，勿混淆

---

## 6. 存量迁移清单（待逐步替换）

以下仍使用 `ObjectPoolManager` 播放特效，后续重构时应迁移到 `EffectManager`：

| 文件 | 说明 |
|------|------|
| `Skill/Logic/RequiemChantSkill.cs` | 旧技能类 |
| `Skill/Logic/MoonlightShieldSkill.cs` | 旧技能类 |
| `Skill/Logic/FireBallSkill.cs` | 旧技能类 |
| `Skill/Logic/EnergyPouringSkill.cs` | 旧技能类 |
| `Skill/Logic/TheHolyLightFallsSkill.cs` | 旧技能类 |
| `Buff/Logic/MoonlightShieldBuff.cs` | Buff 护盾特效 |
| `GamePlayManager.cs` | 部分技能特效 |
| `SoldierCard.cs` / `MonsterCard.cs` | 部分技能/子弹特效 |

---

## 7. AI 开发检查清单

新增或修改局内特效时：

- [ ] 使用 `EffectManager.Instance.PlayEffect` / `PlayProjectileEffect`
- [ ] 动画驱动特效在 `OnComplete` + `OnKill` 双路径回收
- [ ] 技能上下文需要追踪时加入 `context.TrackedEffects`
- [ ] 挂角色 parent 的特效用 `RecycleEffect`，不要直接改 parent
- [ ] 日志使用 `LogTool`，禁止 `Debug.Log`
- [ ] 特效名与 YooAsset / VFX 预加载目录一致（默认 `VFX` 文件夹）
