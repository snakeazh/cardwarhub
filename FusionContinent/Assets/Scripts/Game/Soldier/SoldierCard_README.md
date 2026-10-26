# SoldierCard 英灵战场卡牌

> 供开发接续与 AI 对话使用的总览文档。  
> 最后更新：2026-06-22

---

## 1. 概述

`SoldierCard` 是塔防战斗中**己方英灵单位**的核心 MonoBehaviour，聚合以下子系统：

| 子系统 | 实现 | 职责 |
|--------|------|------|
| 状态机 | `StateMachine` + `Soldier*State` | Idle / Attacking / CastingSkill / Dead |
| 属性 | `AttributeSet BaseAttributes` | 血量、攻速、攻击距离等战斗数值 |
| 技能数据 | `SoldierSkillComponent` | 学习、查询技能字典 |
| 技能释放 | `SoldierSkillExecutor` + `UseSkill` | 主动释放入口、技能图 Cast |
| 动画 | `SoldierAnimationController` | Spine 创建、攻击/待机动画、Hide/Return |
| Buff | `SoldierBuffController` | 增删 Buff、月影盾 |
| 血条 | `SoldierHealthBarController` | 血条显示与更新 |
| 受击 | `TakeDamage` → `SoldierDamageReceiver` | 前置校验 + 结算委托 |
| 索敌 | `SoldierAtkRange` + `EnemyTargetFinder` | 普攻锁定、施法落点刷新 |
| 拖拽 | `IPointerDown/Up/Drag` | 战场站位调整 |

**源码位置：** `Assets/Scripts/Game/SoldierCard.cs`（partial，UI 引用在 `SoldierCard.Designer.cs`）

**相关专项文档：**

- 伤害链路：`Assets/Scripts/Game/DamageCalculator/DamageSystem_README.md`
- 技能组件拆分：`Assets/Scripts/Game/Soldier/SoldierCard_Refactor_Context.md`
- 属性系统：`Assets/Scripts/Game/Attribute/AttributeSystem_Context.md`
- 动画控制器（含 Hide/Return）：`Assets/Scripts/Game/Soldier/SoldierAnimationController_README.md`

---

## 2. 组件架构

```
SoldierCard (ViewController, IBuffable)
├── StateMachine
│   ├── SoldierIdleState
│   ├── SoldierAttackingState
│   ├── SoldierCastingSkillState
│   └── SoldierDeadState
├── SoldierSkillComponent      ← 技能字典 / Learn / Query
├── SoldierSkillExecutor       ← StartReleaseSkill 流程
├── SoldierAnimationController ← Spine / 动画
├── SoldierBuffController      ← Buff 生命周期
├── SoldierHealthBarController ← 血条 UI
├── AttributeSet BaseAttributes
└── SoldierHitBox / SoldierAtkRange（Designer 绑定）
```

外部仍通过 `SoldierCard` 公开 API 访问，组件为 **纯 C# 类**，在 `Awake` 中 `new` 初始化。

---

## 3. 生命周期

### 3.1 Awake

1. 加载 Luban 配置表引用
2. `InitializeStateMachine()` — 创建四态及转换规则，初始 Idle
3. 创建 `AnimationController`、`SkillExecutor`、`SkillComponent`、`BuffController`、`HealthBarController`

### 3.2 Init(int id, int cardLevel, SoldierRoleType roleType)

战场实例化后的**数据与表现初始化**（`GamePlayManager.AddSoldierCard` 等调用）：

```
ClearRuntimeSubscriptions / ClearDotweenRuntime
  → SkillComponent.Reset()
  → BaseAttributes.InitFromConfig(id, cardLevel)
  → 模块属性 / 皮肤 Modifier 叠加
  → ResetSoldierHp()
  → AnimationController.CreateSpineObject
  → InitSpecialIds → LearnSkill
  → RegisterRuntimeSubscriptions()
  → RegisterVillagerAI()（AttackRange 初始化）
```

**角色类型 `SoldierRoleType`：**

| 值 | 行为差异 |
|----|----------|
| `Self` | 默认；可选中、受击、显示血条 |
| `Mirror` | 镜像：不显示血条、`CanReceiveIncomingDamage` 拒绝 |
| `CopyBody` | 复制体：半透明表现，主体死亡时联动 `Die()` |

### 3.3 Update

- 非暂停时：状态机 `Update`、普攻 CD 递减、Idle→Attacking 转换、`BuffController.Update`
- 生存时间到期：`DestroyOneself()`

### 3.4 Die / ReviveSoldier / OnDestroy

- **Die**：锁攻击、清订阅/Tween、Buff 清空、人口 -1、`SetActive(false)`、复制体联动死亡
- **ReviveSoldier**：人口 +1、重置血量、重新订阅、切回 Idle
- **OnDestroy**：兜底清理订阅、Tween、Buff、动画

---

## 4. 状态机

### 4.1 状态枚举 `SoldierStateType`

位标志枚举，通过 `SwitchState(SoldierStateType)` 写入 `_currentStateType`，Transition 条件读取该字段。

| 状态 | 典型进入条件 | 行为 |
|------|--------------|------|
| Idle | 初始 / 攻击或施法结束 | 播放 idle、解锁攻击 |
| Attacking | Idle 且 `nextAttackTime <= 0` 且有目标 | 驱动 `PlayAtkAnimation` |
| CastingSkill | `StartReleaseSkill` 成功 | 播放技能动画 |
| Dead | 血量 ≤ 0 | 死亡表现 |

### 4.2 转换图（简化）

```
        ┌──────────────────────────────────────┐
        │                                      │
        ▼                                      │
     [Idle] ──有目标+CD就绪──▶ [Attacking] ──动画结束──┘
        │         ▲
        │         │
        └──施法──▶ [CastingSkill] ──结束──┘
        │
        └──HP≤0──▶ [Dead] ──复活──▶ [Idle]
```

状态类路径：`Assets/Scripts/Game/StateMachine/`（`IdleState.cs`、`AttackingState.cs`、`CastingSkillState.cs`、`DeadState.cs`）。

---

## 5. 普攻与攻击流程

```
Update: Idle + nextAttackTime<=0 + currentTarget!=null
  → SwitchState(Attacking)
AttackingState → PlayAtkAnimation()
  → nowTargetInfo = EnemyTargetFinder.FromMonster(currentTarget)
  → nextAttackTime = 1 / SoldierAttackSpeed
  → PlayAttackAnim(NormalAttackSkillId, nowTargetInfo)
       ├─ 有 Spine 技能动画 → 命中回调 OnAttackHit → ExecuteAttack
       └─ 无动画 → ExecuteAttack 直接执行
```

隐藏类技能（钻地/潜行）：Spine 需 `hide` 事件 + 技能图末尾 `ReturnAnimation` 节点，详见 `SoldierAnimationController_README.md`。

```
ExecuteAttack
  → TryRefreshSkillCastDirection（施法前刷新存活目标）
  → UseSkill(skillId, skillTarget)
  → CheckNormalAttackHeavyBlowSkill（破军/重击）
```

**关键字段：**

- `currentTarget` — 由 `SoldierAtkRange` 经 `SetClosestTarget` 更新
- `nextAttackTime` — 普攻 CD；主动技能可能临时设为 3 或 99，动画结束兜底恢复
- `isAtkLock` — 放置后短暂禁止攻击，拖拽/死亡时锁定
- `CannotNormalAttack` — 属性或规则禁止普攻

---

## 6. 主动技能释放

### 6.1 StartReleaseSkill（委托 SkillExecutor）

```
条件检查 → BuildTarget → ApplyReleaseEffects(Comet)
  → SwitchState(CastingSkill) → ApplyReleaseEffects(Health)
  → TriggerReleaseEvents
```

详见 `SoldierCard_Refactor_Context.md` 第 5 节。

### 6.2 UseSkill（留在 SoldierCard）

1. 刷新施法方向 `TryRefreshSkillCastDirection`
2. `BattleRuleCastSupport` 前置触发
3. 无 SkillGraph → `CastLegacySkill`；有图 → `SkillManager.instance.CastSkill`
4. `finally` 中 `BattleRuleCastDamageSession.OnSkillCastCompleted`

---

## 7. 受击与伤害

`SoldierCard` **不负责伤害公式**，仅做门禁与委托：

```
调用方（怪物攻击 / MainCity / BurnBuff 等）
  1. CanReceiveIncomingDamage(damageVal)   ← 死亡/拖拽/镜像/引导保护
  2. DamageContextBuilder.CalculateIncoming(...)
  3. TakeDamage(DamageResult)              ← 委托 SoldierDamageReceiver
```

治疗与属性改血走 `SetSoldierHp` / `SetSoldierMaxHp`，飘字用 `DamageEffect`（非受击结算路径）。

完整链路见 `DamageSystem_README.md`。

---

## 8. 属性与运行时订阅

`Init` 后 `RegisterRuntimeSubscriptions` 监听：

| AttributeType | 回调行为 |
|---------------|----------|
| SoldierHealth | 血条更新、满血/低血条件、≤0 触发 Die |
| SoldierMaxHealth | 增量补血、血条上限 |
| SoldierNumOfAttackSpeed | 缩放剩余普攻 CD |
| SoldierAttackSpeed | 攻速转普攻伤害 Modifier |
| SoldierAttackRange | 更新 `attackRange` 与检测圈 |
| SoldierAttackPower | 攻击转额外法强、条件进度 |
| SoldierHealthRegenPerSecond | 定时回血 Tween |
| SoldierEveryScePerMaxHp | 按最大生命百分比定时回血 |

另有 `exp` 升级、`Global.ConsecutiveSkillReleaseDamageBonus`、流派星级特效、长按英雄详情等订阅。

`ClearRuntimeSubscriptions` 在 `Init` 重入、`Die`、`DestroyOneself`、`OnDestroy` 时对称解绑。

---

## 9. 拖拽与站位

实现 `IPointerDownHandler` / `IPointerUpHandler` / `IDragHandler`：

- **Down**：选中广播、显示攻击范围圈、clone 预览图
- **Drag**：clone 跟随鼠标、高亮最近 `friendlyPositions` 的 FloorShine；受 `SoldierCanDrag` / `CanDrag` 配置限制
- **Up**：吸附最近站位，与锁定位置上的其他英灵交换；延时 `DetectAdjacentSoldier`

`posIndex` 对应当前 `GamePlayManager.friendlyPositions` 下标。

---

## 10. 配置依赖

| 配置表 | 用途 |
|--------|------|
| `TbCardBaseConfig` | 图标、名称 |
| `TbSoldierBattleConfig` | Spine、普攻技能、攻击距离、InitSpecialIds |
| `TbSpecialEffectsConfig` | 技能 ActsOnCard、觉醒标记 |
| `TbSkinConfig` | 皮肤属性 Modifier |
| `TbAttributeConfig` | 条件属性（顽强意志、低血加成等） |

英灵 cardId 常量见 `Global.cs`（矛 2010001、弓 2010002、法 2010003、刺 2010004）。

---

## 11. 主要公开 API 速查

| 方法 / 属性 | 说明 |
|-------------|------|
| `Init(id, level, roleType)` | 战场初始化 |
| `PlayAtkAnimation()` | 普攻入口（Attacking 态调用） |
| `StartReleaseSkill(...)` | 主动技能释放入口 |
| `UseSkill(skillId, direction, ...)` | 技能图 / Legacy Cast |
| `LearnSkill` / `GetSkillById` / `GetReadySkillById` | 委托 SkillComponent |
| `SetSoldierHp` / `SetSoldierMaxHp` | 主动改血（含禁疗检查） |
| `CanReceiveIncomingDamage` / `TakeDamage` | 受击门禁与结算 |
| `Die` / `ReviveSoldier` | 死亡与复活 |
| `SetClosestTarget` / `SetFusionTarget` | 索敌与融合目标 |
| `AddBuff` / `AddMoonlightShield` | Buff 入口 |
| `SwitchState` | 状态机切换（状态类与 Executor 使用） |
| `BaseAttributes` | 战斗属性真值源 |
| `CurrentStateType` | 当前状态枚举 |

---

## 12. 外部主要调用方

| 调用方 | 典型 API |
|--------|----------|
| `GamePlayManager` | 创建、`LearnSkill`、`villageGameCards` |
| `GameLand` | `StartReleaseSkill`、技能卡 |
| `SoldierAtkRange` | `SetClosestTarget`、`PlayAtkAnimation` |
| `AttributeManager` / Buff | `LearnSkill`、`AddSkillAttributeModifier` |
| `Skill Handler` | `GetSkillById`、`GetComponent<SoldierCard>()` |
| `SoldierDamageReceiver` | `Shield`、`TakeDamage` 相关访问器 |
| `BattleRule*` | 各类规则触发与复活支持 |

---

## 13. 调试与日志

- 伤害调试：`EnableDamageRecording`、`GetLastDamageReport()`
- 技能释放日志前缀：`[SkillRelease-UseSkill]`、`[SkillRelease-Anim]`
- 必须使用 `LogTool`，禁止 `Debug.Log`

---

## 14. AI 接续提示词

```
FusionContinent 英灵战场核心类 SoldierCard（Assets/Scripts/Game/SoldierCard.cs）。

架构：状态机四态 + SkillComponent/Executor + Animation/Buff/HealthBar 控制器；
伤害公式不在 Card 内，见 DamageSystem_README.md；
技能拆分见 SoldierCard_Refactor_Context.md。

修改约束：不改外部公开 API；组件 Awake 初始化；RelearnSkill 覆盖写入；
事件绑定具名方法；日志用 LogTool。

总览文档：Assets/Scripts/Game/Soldier/SoldierCard_README.md
```
