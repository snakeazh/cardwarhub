# SoldierAnimationController — 英灵 Spine 动画

> 供开发与 AI 接续使用的专项文档。  
> 最后更新：2026-06-24

**源码：** `Assets/Scripts/Game/Soldier/SoldierAnimationController.cs`

**关联文档：**

- 英灵总览：`SoldierCard_README.md`
- Hide/Return 技能图配置：`Assets/Scripts/Game/Skill/Doc/HideReturnAnimation_配置说明.md`

---

## 1. 职责

| 能力 | 说明 |
|------|------|
| Spine 生命周期 | `CreateSpineObject` / `Initialize` / `StopAllAnimations` |
| 技能动画 | `PlayAtkAnimation(skillId)`，动画名 = skillId 字符串 |
| 事件驱动 | Spine `atk` → 技能命中与节点图；Spine `hide` → 等待 Return |
| 表现动画 | `PlayReturnAnimation`（仅技能图 `ReturnAnimation` 节点调用） |
| 混合轨 | `PlayMixAnimation` / `StopMixAnimation`（技能图节点） |
| 抢占 | `playId` + `CurrentPlayId`，防止旧动画 Complete 覆盖新动画 |

---

## 2. 标准技能释放链路（未改动）

```
PlayAtkAnimation(skillId)
    ↓
Spine 播放 skillId 动画（轨道 0）
    ↓
Spine 事件 atk
    ↓
_onAttackHit → SoldierCard.OnAttackHit → UseSkill → 技能节点图
    ↓
（非 hide）动画 Complete → _onOverAtk → SwitchState(Idle)
```

**要点：**

- 技能逻辑入口仍是 **atk 事件**，不是动画结束。
- `PlayAtkAnimation` 每次调用 `++playId`，旧动画 Complete 若 `myId != playId` 则忽略。

---

## 3. Hide / Return 扩展（钻地、潜行等）

### 3.1 设计目标

- 技能动画本身仍是 **skillId 动画**，不在 atk/特效里写死 Return。
- 收到 **hide** 后角色保持隐藏姿态，等技能图跑完伤害/特效。
- 技能图末尾 **ReturnAnimation** 节点播放 **return** 动画，结束后回 Idle。

### 3.2 时序

```
PlayAtkAnimation(skillId)     playId++
    ↓
skillId 动画播放
    ↓
atk 事件 ──────────────────→ 技能节点图开始（SkillAnimationPlayId 快照）
    ↓
hide 事件 ──────────────────→ _waitReturn=true, _waitReturnPlayId=playId
    ↓
skillId 动画 Complete ─────→ 不切 Idle，不触发 _onOverAtk
    ↓
节点图：伤害 / 特效 / Delay / ...
    ↓
ReturnAnimation 节点
    ├─ CanPlayReturn(快照 playId) 失败 → 跳过（被新技能抢占或未 hide）
    └─ 成功 → PlayReturnAnimation("return")
              ↓
         return Complete → PlayIdle() + InvokeDeferredOverAtk()
```

### 3.3 核心字段

| 字段 / 属性 | 含义 |
|-------------|------|
| `playId` / `CurrentPlayId` | 每次 `PlayAtkAnimation` 递增 |
| `_waitReturn` | 是否处于 hide 后等待 Return |
| `_waitReturnPlayId` | 收到 hide 时的 playId |
| `SkillContext.SkillAnimationPlayId` | 技能图开始时快照的 playId |

### 3.4 公开 API

```csharp
public int CurrentPlayId { get; }

public bool CanPlayReturn(int cachedPlayId);
// _waitReturn && cachedPlayId == playId

public void PlayReturnAnimation(string animName = "return");
// 清除 _waitReturn → 播 return → Complete 后 Idle + 补发 _onOverAtk
```

### 3.5 动画抢占

```
SkillA: hide → 停住（_waitReturn=true, playId=5）
SkillB: PlayAtkAnimation → playId=6, _waitReturn=false

SkillA 节点图末尾 ReturnAnimation:
  CanPlayReturn(5) → false（5 != 6）
  → 不播放 Return，SkillB 动画不被覆盖
```

SkillA 的特效节点与 SkillA 节点图其余逻辑**不受影响**，仅 Return 被跳过。

---

## 4. Spine 资源要求（隐藏类技能）

| 项目 | 要求 |
|------|------|
| 技能动画 | 名称 = skillId，时间轴含 `atk` 事件 |
| hide 事件 | 在角色进入隐藏姿态的帧上打点，事件名 **`hide`** |
| return 动画 | 轨道 0 可播的非循环动画，默认名 **`return`** |
| Return 节点 | 技能图末尾连接 `ReturnAnimation`，`anim_name` 与 Spine 一致 |

未配置 `hide` 的技能：`_waitReturn` 恒为 false，`ReturnAnimation` 节点自动跳过。

未配置 `ReturnAnimation` 但配置了 `hide`：**不推荐** — 角色会一直保持 hide 末帧，且 `_onOverAtk` 不会触发，可能卡在施法态直至超时。

---

## 5. 相关代码索引

| 文件 | 说明 |
|------|------|
| `SoldierAnimationController.cs` | hide/return 状态机、`PlayReturnAnimation` |
| `ReturnAnimationHandler.cs` | 技能图节点 `ReturnAnimation` |
| `SkillRuntime.cs` | `ApplySkillAnimationPlayId` 快照 playId |
| `SkillContext.cs` | `SkillAnimationPlayId` 字段 |
| `SoldierCard.PlayAttackAnim` | 注册 atk / overAtk 回调（未改流程） |

---

## 6. 调试建议

1. 日志 `[ReturnAnimationHandler] 无法播放 Return`：未 hide 或 playId 已变化。
2. 日志 `缺少 Return 动画`：Spine 未导出 `return` 或 `anim_name` 配置错误。
3. 角色不回 Idle：检查是否缺少 Return 节点，或 `CanPlayReturn` 失败。
4. 对比 `SkillAnimationPlayId` 与 `CurrentPlayId` 可确认抢占是否发生。
