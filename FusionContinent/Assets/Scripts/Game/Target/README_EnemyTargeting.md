# 英灵索敌方案（Enemy Targeting）

本文说明英灵 → 怪物的普攻 / 主动技能索敌规则、核心类型与调用链。  
怪物 → 英灵索敌见 `MonsterCard.EngageSoldierCard`（按 `BattleSide` 分侧选最近英灵），不在本文范围。

## 当前规则一览

| 场景 | 规则 | 起点 | 距离限制 | 说明 |
|------|------|------|----------|------|
| **普攻锁定** | `NearestInRange` | `AttackRange` 节点 | 攻击距离（`attackRange/250`） | 范围内距离最近 |
| **主动技能** | `TrySelectFanCrowdTarget` | 人物自身（优先 Spine） | 攻击距离（`attackRange/250`） | 朝敌人最密集方向释放，窗口内选最近（前排）目标 |
| 多目标弹道填充 | `Nearest` | 弹道/施法点 | 无上限 | 主目标优先，其余按近到远 |
| 指定落点释放 | — | — | — | `FromPosition`，不索敌 |

**禁止**：主动技能失败后回退到普攻 `currentTarget`（会退化成「选最近」）。

## 目录与核心类型

```
Scripts/Game/Target/
  EnemyTargetFinder.cs     # 选敌算法（唯一入口）
  EnemySelectionRule.cs    # 规则枚举
  FanSectorParams.cs       # 扇形参数
  BattleTargetInfo.cs      # 攻击/技能目标快照
  README_EnemyTargeting.md # 本文档
```

| 类型 | 职责 |
|------|------|
| `EnemyTargetFinder` | 遍历 `MonsterManager.enemies` 选敌；距离换算、合法性、扇形、落点解析 |
| `EnemySelectionRule` | 选敌规则枚举（普攻 / 技能 / 扇形 / 兼容旧规则） |
| `FanSectorParams` | `Origin` + `CenterPoint` 定朝向，`AngleDegrees` + `MaxDistance` |
| `BattleTargetInfo` | 快照：`target` / `targetPosition` / `targetUniqueId`（防对象池复用跟错） |

敌人列表唯一数据源：`MonsterManager.Instance.enemies`（线性遍历，无空间分区）。

## 距离与常量

```csharp
// 配置攻击距离 → 世界半径（与 SoldierAtkRange 一致）
EnemyTargetFinder.ToWorldRange(configAttackRange) // = configAttackRange / 250f

EnemyTargetFinder.DefaultFanAngleDegrees = 30f   // 通用扇形默认张角
EnemyTargetFinder.SkillFanAngleDegrees   = 15f   // 主动技能扇形张角
EnemyTargetFinder.DefaultCrowdRadius     = 1.5f  // MostCrowded 邻域（已不用于主动技能）
```

候选过滤：`IsValidEnemy` — 存活、激活、非 `die`；可按 `uniqueId` 排除。

## 普攻索敌

```
SoldierAtkRange.Update（约 0.2s）
  → EnemyTargetFinder.TrySelectEnemy(..., NearestInRange)
  → SoldierCard.SetClosestTarget → currentTarget
  → 有目标且 CD 就绪 → 普攻动画 / 弹道
```

- 组件：`SoldierAtkRange`（只负责轮询，算法在 `EnemyTargetFinder`）
- 范围变化：`SoldierAttackRange` 属性变更 → `AttackRange.SetDetectionRange`

施法前刷新（普攻）：快照仍活 → 沿用；否则 `currentTarget`；再否则 `NearestInRange`。

## 主动技能索敌

### 扇形参数（`SoldierCard.TrySelectSkillFanTarget`）

| 参数 | 取值 |
|------|------|
| Origin | `PlayerspineObj.position`，否则 `transform.position` |
| 朝向 | 车道来敌方向：`centerPoint = origin + (0, -Lane.MoveDirectionY, 0)` |
| 扫描角 | `scanAngleDegrees`（默认 180°，前方半圆） |
| 密度窗口 | `windowAngleDegrees` = `SkillFanAngleDegrees`（15°） |
| 半径 | 与普攻一致：复用 `SoldierAtkRange.CurrentAttackRange`（`range/250`），跟随普攻检测半径 |
| 规则 | 方向密度：窗口内敌人最多 → 该窗口内距顶点最近的目标 |

**不要**把扇形中轴对准普攻 `currentTarget`，否则最密集方向 ≈ 全范围选最近。

### 方向密度选敌（`TrySelectFanCrowdTarget`）

1. 在 `scanAngleDegrees`（默认 180° 前方半圆）内、普攻同款攻击距离内收集存活敌人候选（含角度 / 距离），并保留主相机 y 方向中央 85% 过滤（`IsTargetInScreen`，防御性，避免自动战斗选中过高的怪）。
2. 以每个候选的角度为窗口中心，统计 `windowAngleDegrees`（15°）窗口内的敌人数量，取数量最多的方向为释放方向；平手时优先更接近来敌中轴（角度更小）的方向。
3. 在该最密集方向窗口内选距顶点最近的敌人作为目标——方向仍指向密集区，落点贴近前排怪群，避免天流斩等范围技能落点偏向后方单个怪。

### 释放入口（`SoldierSkillExecutor.BuildTarget`）

```
StartReleaseSkill
  → 有 targetPosition → FromPosition
  → 否则 TrySelectSkillFanTarget
       · 成功 → nowTargetInfo = FromMonster
       · 失败且（自动释放 / useNearestEnemy）→ return false
       · 失败且手动不强制索敌 → 允许无目标施法
```

点击技能、PVP AI 使用 `useNearestEnemy: true`，扇形失败则不放技能。

### 施法前刷新（`TryResolveLiveSkillTarget`）

| 类型 | 顺序 |
|------|------|
| 普攻 | 快照存活 → `currentTarget` → `NearestInRange` |
| 主动技能 | 快照存活 → 重新 `TrySelectSkillFanTarget`（不回退 `currentTarget`） |

## 规则枚举说明

| 规则 | 状态 | 用途 |
|------|------|------|
| `NearestInRange` | **在用** | 普攻持续锁定 |
| `FanNearest` | 保留 | 扇内选最近；主动技能已改为方向密度 |
| `Nearest` | 在用 | 多目标弹道等全场最近 |
| `LowestYInRange` | 保留 | 旧「范围内 Y 最靠前」 |
| `MostCrowded` | 保留 | 邻域密集；主动技能已改为方向密度 |
| `FanSmallestAngle` / `FanAngleDistanceWeighted` | 已实现 | 暂无业务接线 |

## 调用关系

```
EnemyTargetFinder
├── SoldierAtkRange.DetectTargets          [NearestInRange]
├── SoldierCard.TrySelectSkillFanTarget    [TrySelectFanCrowdTarget 最密集方向 + 全场无距离限制]
├── SoldierSkillExecutor.BuildTarget       → TrySelectSkillFanTarget
├── SoldierCard.TryResolveLiveSkillTarget  [普攻 NearestInRange / 技能扇形]
└── MultiTargetEnemySelector               [SelectEnemies / Nearest 填充]

技能图二次选目标（SelectTarget / AllyGroup / RandomEnemy 等）
  → SkillTargetResolver（与上述预选独立，读 SkillContext）
```

## 与 PVP 的关系

| 方向 | 是否按 `BattleSide` 过滤 |
|------|--------------------------|
| 英灵 → 怪 | **否**（全局 `enemies`；靠横向分车道 + 距离/扇形自然偏向同侧） |
| 怪 → 英灵 | **是**（`villageGameCards` / `opponentGameCards`） |

PVP AI 放主动技能：`StartReleaseSkill(..., useNearestEnemy: true)`，与玩家同一套扇形索敌。

## 相关文件

```
Scripts/Game/Target/*                         # 本模块
Scripts/Game/SoldierAtkRange.cs               # 普攻轮询
Scripts/Game/SoldierCard.cs                   # TrySelectSkillFanTarget / 施法刷新
Scripts/Game/Soldier/SoldierSkillExecutor.cs  # BuildTarget
Scripts/Game/GameLand.SkillCards.cs           # 点击/自动释放（useNearestEnemy）
Scripts/Game/GamePlayManager.cs               # PVP AI 放技能
Scripts/Battle/Arena/README_PvePvpArena.md    # 双车道与怪物分侧索敌
```

## 调参备忘

- 技能扇形太窄打不到侧向怪 → 调大 `windowAngleDegrees` / `SkillFanAngleDegrees`
- 最密集方向偏侧太极端 → 减小 `scanAngleDegrees`（如 90°）或调大密度窗口
- 技能索敌不再受 `attackRange` 限制（`maxDistance=0` 全场）；普攻仍受攻击距离限制
- 日志：扇形失败时 `[SkillRelease-Start] ... 扇形索敌失败`（原有日志；方向密度选敌本身不打印日志）

## 变更摘要（相对旧逻辑）

1. 普攻：`LowestYInRange`（最靠前）→ `NearestInRange`（最近）
2. 技能：全场 `Nearest` / 密集区 → **人物起点 + 来敌方向扫描 + 攻击距离 + 朝敌人最密集方向（`TrySelectFanCrowdTarget`，15° 密度窗口，窗口内选最近前排目标）**
3. 去掉技能对普攻 `currentTarget` 的回退，避免「名义扇形、实际最近」
4. 主动技能索敌：扇形内最近（`FanNearest`）→ **朝敌人最密集方向（`TrySelectFanCrowdTarget`）**，平手优先来敌中轴、窗口内选距顶点最近目标（落点贴近前排怪群）；索敌限制在攻击距离内
5. 移除方向密度选敌的临时调试日志（`[SkillFan-Crowd]`）与 Scene Gizmos 绘制
6. 候选范围：从「无限距离」回到「普攻检测半径」——技能索敌复用 `SoldierAtkRange.CurrentAttackRange`（`range/250`），与普攻完全一致；检测仍是前方 180° 半圆 + 方向密度；保留主相机 y 方向中央 85% 过滤（`IsTargetInScreen`，防御性）
