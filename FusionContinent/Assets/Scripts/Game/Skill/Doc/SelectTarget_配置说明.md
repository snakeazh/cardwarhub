# SelectTarget 目标选取 — 配置说明

> **适用对象**：策划、程序  
> **最后更新**：2026-07-01  
> **相关代码**：`SkillTargetResolver.cs`、`SelectTargetHandler.cs`、`SkillContext.SelectedTargets`

---

## 一、解决的问题

技能图中 **特效落点** 与 **Buff 施加对象** 往往需要一致，但原先各节点各自解析目标，会出现：

- `ApplyBuff` 选了「随机友方」，Buff 套在 A 身上  
- `Effect` / `AnimatedEffect` 仍按释放点 / 施法者 / 全体友军播放，特效不在 A 身上  

**SelectTarget** 在链路前置统一解析目标，写入 `SelectedTargets`，后续节点选 **「已选取目标 / Selected」** 即可共用同一结果。

---

## 二、节点一览

| 节点 | NodeType | 角色 |
|------|----------|------|
| 目标选取 | `SelectTarget` | **写入** `SelectedTargets` |
| 应用 Buff | `ApplyBuff` | 可读 `target=Selected` |
| 特效 | `Effect` | 可读 `position=Selected` |
| 动画特效 | `AnimatedEffect` | 可读 `position=Selected`（含慰灵圣咏） |
| 投掷物 | `Projectile` | 可读 `aim_target=Selected` |
| 扇形投射 | `FanProjectile` | 可读 `aim_target=Selected` |

> **注意**：以上节点通过 `Selected` 读取 `SelectedTargets`。`AoeSelect` 使用 `HitMonsters`，机制不同，不可混用。

---

## 三、SelectTarget（目标选取节点）

### 3.1 参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `target` | string | `Target` | 选取规则（见下表） |
| `clear_selected_list` | bool | `true` | 执行前是否清空 `SelectedTargets`；循环内请保持开启 |
| `exclude_previous_selected` | bool | `true` | 随机规则时避开本轮技能已选过的目标（循环不重复） |

### 3.2 选取规则（`target`）

| 配置值 | 编辑器显示 | 说明 |
|--------|------------|------|
| `Target` | 目标 | 当前技能锁定目标 `context.Target` |
| `Self` | 自身 | 施法者 |
| `EnemyGroup` | 敌方群体 | 施法者为友方时取场上怪物；为敌方时取存活英灵 |
| `AllyGroup` | 友方群体 | 施法者为友方时取存活英灵；为敌方时取场上怪物（含施法者） |
| `RandomAlly` | 随机友方(包含自己) | 从友方群体中随机 1 个（含施法者） |
| `RandomAllyExcludeSelf` | 随机友方(不包含自己) | 从友方群体中随机 1 个（排除施法者） |
| `RandomEnemy` | 随机敌方 | 从敌方群体中随机 1 个 |
| `Hit` | 命中目标 | 复用 `AoeSelect` / `AoeDamage` 写入的 `HitMonsters` |

**不支持** `Selected`（不能选取「已选取目标」）。

### 3.3 运行时行为

1. 按 `target` 调用 `SkillTargetResolver.Resolve` 解析目标列表  
2. 将结果写入 `SkillContext.SelectedTargets`（`Component` 列表）  
3. 若解析结果为空，列表为空；下游 `Selected` 节点会跳过或无效  

---

## 四、下游节点：读取已选目标

### 4.1 ApplyBuff

| 参数 | 新增可选值 |
|------|------------|
| `target` | `Selected`（编辑器：**已选取目标**） |

- 对 `SelectedTargets` 中每个可 Buff 单位施加 Buff  
- **必须**在链路中先于本节点执行 `SelectTarget`（或通过 `Hit` 等由 SelectTarget 解析的规则间接写入）  
- 无已选目标时：不施加 Buff  
- **斩杀 Buff（Execute）**：若与 `AoeDamage` 等同次命中，本节点须排在伤害节点**之前**；详见 `ExecuteBuff_配置说明.md`

### 4.2 Effect

| 参数 | 新增可选值 |
|------|------------|
| `position` | `Selected`（编辑器：**已选取目标**） |

- 在 **第一个** 已选目标的 `transform.position` 播放特效（叠加 `random_offset_radius` 偏移）  
- 无已选目标时：跳过特效，打日志 `[EffectHandler] position=Selected 但无已选取目标`  

### 4.3 AnimatedEffect

| 参数 | 新增可选值 |
|------|------------|
| `position` | `Selected`（编辑器：**已选取目标**） |

| `animation_type` | 与 `position` 的关系 |
|------------------|----------------------|
| `Fall` / `Rise` / `zhanji` | 动画落点 = `position` 解析位置 |
| `RequiemChant` | **仅向 `position` 对应的一个目标位置** 发射珠子（不再遍历全体友军） |

无已选目标且 `position=Selected` 时：整节点跳过。

### 4.4 position 完整枚举（Effect / AnimatedEffect）

| 配置值 | 编辑器显示 | 位置来源 |
|--------|------------|----------|
| `Target` | 目标位置 | `context.TargetPosition` |
| `Caster` | 施法者位置 | 施法者 `transform.position` |
| `Enemy` | 敌人位置 | `context.Target.transform.position` |
| `Selected` | 已选取目标 | `SelectedTargets[0]` 的位置 |

### 4.5 Projectile / FanProjectile

| 参数 | 新增可选值 |
|------|------------|
| `aim_target` | `Selected`（编辑器：**已选取目标**） |

- 瞄准点 = `SelectedTargets[0]` 的位置；扇形中心方向为「施法者 → 已选目标」
- `move_type=Homing` 时，追踪 Transform 为已选目标
- 无已选目标时：跳过发射，打日志 `aim_target=Selected 但无已选取目标`

| 配置值 | 编辑器显示 | 瞄准来源 |
|--------|------------|----------|
| `Target` | 技能目标 | `context.Target` 位置，否则 `TargetPosition` |
| `Selected` | 已选取目标 | `SelectedTargets[0]` |

---

## 五、推荐技能图链路

### 5.1 祈祷：随机强化一名英灵（特效 + Buff 同目标）

```
Cast
  ↓
SelectTarget          target = RandomAlly（或 RandomAllyExcludeSelf）
  ↓
Fork（可选，并行）
  ├─ AnimatedEffect     animation_type = RequiemChant, position = Selected
  └─ ApplyBuff        target = Selected, buff_id = …
```

### 5.2 顺序执行（无 Fork）

```
Cast → SelectTarget → Effect(position=Selected) → ApplyBuff(target=Selected)
```

### 5.3 随机敌方 + 弹道（赫式多箭等）

```
Cast
  ↓
Loop
  body → SelectTarget          target = RandomEnemy, exclude_previous_selected = true
       → Projectile            aim_target = Selected
```

### 5.4 范围选取后再 Buff（沿用 Hit 机制）

若目标是 **范围内多个怪物**，仍用 `AoeSelect` + `ApplyBuff(target=Hit)`，**不需要** SelectTarget：

```
AoeSelect → ApplyBuff(target=Hit)
```

`SelectTarget` 与 `AoeSelect` 适用场景不同：

| 机制 | 存储 | 典型场景 |
|------|------|----------|
| `AoeSelect` | `HitMonsters` | 范围多目标（怪物） |
| `SelectTarget` | `SelectedTargets` | 单体/群体/随机友方，且要与特效共用 |

---

## 六、ApplyBuff 施加目标完整枚举

在 **不经过 SelectTarget** 时，`ApplyBuff` 仍可单独使用以下 `target`：

| 配置值 | 说明 |
|--------|------|
| `Target` | 当前锁定目标 |
| `Self` | 施法者 |
| `EnemyGroup` | 敌方群体 |
| `AllyGroup` | 友方群体 |
| `RandomAlly` | 随机友方（含自己） |
| `RandomAllyExcludeSelf` | 随机友方（不含自己） |
| `RandomEnemy` | 随机敌方 |
| `Hit` | `HitMonsters` 列表 |
| `Selected` | `SelectedTargets`（需前置 SelectTarget） |

> 若特效、弹道与 Buff 都要随机敌方且必须一致，应使用 **SelectTarget(RandomEnemy) + Selected**，不要在各节点分别填 `RandomEnemy`。

---

## 七、程序说明

### 7.1 核心类

| 文件 | 说明 |
|------|------|
| `Handler/SkillContext.cs` | `SelectedTargets` 列表 |
| `Handler/SkillTargetResolver.cs` | 统一目标解析、`TryResolveEffectPosition` |
| `Handler/Action/SelectTargetHandler.cs` | SelectTarget 节点 |
| `Handler/Buff/ApplyBuffHandler.cs` | 经 `SkillTargetResolver.Resolve` 解析 target |
| `Handler/Action/EffectHandler.cs` | `position=Selected` |
| `Handler/Action/ProjectileHandler.cs` | `aim_target=Selected` |
| `Handler/Action/FanProjectileHandler.cs` | `aim_target=Selected` |

### 7.2 注册与编辑器

- `SkillHandlerRegistry` 已注册 `SelectTargetHandler`  
- `NodeTypeRegistry` 分类：`Utility`  
- `SkillNodeEditorWindow`：选取「已选取目标」时显示黄色前置提示  

### 7.3 日志关键字

| 日志 | 含义 |
|------|------|
| `[SelectTargetHandler] 选取目标: N 个` | 选取成功，N 为写入数量 |
| `[EffectHandler] position=Selected 但无已选取目标` | 缺少前置 SelectTarget 或选取为空 |
| `[AnimatedEffectHandler] position=Selected 但无已选取目标` | 同上 |
| `[ProjectileHandler] aim_target=Selected 但无已选取目标` | 同上 |
| `[FanProjectileHandler] aim_target=Selected 但无已选取目标` | 同上 |

---

## 八、常见问题

### Q1: 选了「已选取目标」但 Buff / 特效没生效？

1. 检查技能图中 **SelectTarget 是否在本节点之前**（同一执行分支内）  
2. 检查 SelectTarget 的 `target` 规则是否解析到单位（如场上无其他存活英灵时 `RandomAllyExcludeSelf` 为空）  
3. 查看 Console 是否有 `无已选取目标` 警告  

### Q2: Loop 循环内每次还是同一个目标？

1. **SelectTarget 必须接在 Loop 的 `body` 端口内**（不能放在 Loop 之前，否则只选一次）
2. 选取规则用 `RandomAlly` / `RandomEnemy`，不要用 `Target`（锁定目标不会变）
3. 开启 **随机不重复已选目标**（`exclude_previous_selected=true`，默认开启）
4. 开启 **选取前清空已选列表**（`clear_selected_list=true`，默认开启）
5. 下游节点（Projectile / Effect / ApplyBuff）使用 **已选取目标 / Selected**

### Q3: Buff 和特效还是不在同一个人身上？

- 确认 ApplyBuff / Effect / AnimatedEffect 的 target 或 position 均为 **Selected**  
- 确认 **没有** 在 ApplyBuff 上单独使用 `RandomAlly`（会二次随机）  

### Q4: 慰灵圣咏为什么只飞一颗珠子？

- 当前设计：`RequiemChant` 仅飞向 `position` 解析的 **一个** 目标位置  
- 需多目标时：使用多个技能图分支，或改用其他动画类型 + 多次 ApplyBuff  

### Q5: 与 AoeSelect 的 Hit 有什么区别？

- `Hit` → `HitMonsters`，仅 `MonsterCard`，由范围检测写入  
- `Selected` → `SelectedTargets`，可为英灵或怪物，由 `SelectTarget` 或规则解析写入  

---

## 九、变更记录

| 日期 | 内容 |
|------|------|
| 2026-07-01 | 新增 `RandomEnemy`；`Projectile` / `FanProjectile` 增加 `aim_target=Selected` |
| 2026-07-01 | 新增 `SelectTarget` 节点；`ApplyBuff` 增加 `RandomAlly` / `RandomAllyExcludeSelf` / `Selected`；`Effect` / `AnimatedEffect` 增加 `position=Selected`；`RequiemChant` 改为仅对 `position` 单目标生效 |
