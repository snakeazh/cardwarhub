# Hide / Return 技能动画 — 配置说明

> **适用对象**：策划、动画、程序  
> **最后更新**：2026-06-24  
> **程序文档**：`Assets/Scripts/Game/Soldier/SoldierAnimationController_README.md`

---

## 一、适用技能类型

需要「先隐藏再出手、结束后现身」的表现，例如：

- 钻地突击
- 潜行刺杀
- 遁地位移后伤害

**普通技能无需配置**，保持原 skillId 动画 + atk 即可。

---

## 二、Spine 动画配置

### 2.1 技能施法动画（名称 = 技能 ID）

与现有规则相同，动画名必须为 **skillId 数字字符串**。

时间轴上至少需要：

| 事件名 | 时机 | 作用 |
|--------|------|------|
| `atk` | 出手/命中帧 | 触发技能节点图（伤害、弹道、特效等） |
| `hide` | 角色进入隐藏姿态的帧 | 标记进入等待 Return；动画结束后**不回 Idle** |

**注意：** `atk` 通常早于 `hide`（先触发技能逻辑，再进入隐藏姿态），具体以美术节奏为准。

### 2.2 Return 动画

- 动画名默认：**`return`**
- 非循环，播放在轨道 0
- 表示角色从隐藏状态回到站立
- 若美术使用其他名称，在技能图节点里修改 `anim_name`

---

## 三、技能节点图配置

在技能图 **末尾**（伤害、特效等逻辑之后）添加节点：

| 节点类型 | NodeType | 说明 |
|----------|----------|------|
| Return动画 | `ReturnAnimation` | 播放 return 并回 Idle |

### 3.1 节点参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `anim_name` | `return` | Spine return 动画名 |

### 3.2 推荐连线示例

```
Cast / 入口
    ↓
… 伤害 / AoeDamage / Projectile …
    ↓
Effect（可选）
    ↓
ReturnAnimation    anim_name = return
```

**只有配置了 `ReturnAnimation` 的技能才会播放 Return。**

---

## 四、运行时行为说明

### 4.1 正常流程

1. 播放 skillId 技能动画  
2. `atk` → 技能节点图执行  
3. `hide` → 角色保持隐藏末帧  
4. 节点图继续（伤害、特效不受影响）  
5. `ReturnAnimation` → 播放 return → 回 Idle  

### 4.2 未配置 hide

- 技能动画结束后按**原逻辑**回 Idle  
- `ReturnAnimation` 节点：`CanPlayReturn` 为 false，**自动跳过**  

### 4.3 配置了 hide 但未配置 ReturnAnimation

- 角色可能**一直停在 hide 末帧**  
- 施法状态可能直到超时才恢复  
- **务必**在隐藏类技能图末尾添加 `ReturnAnimation`  

### 4.4 技能被打断（动画抢占）

若 SkillA 隐藏期间 SkillB 开始新的 `PlayAtkAnimation`：

- SkillA 的特效等节点图逻辑可继续  
- SkillA 的 `ReturnAnimation` **不会播放**（避免覆盖 SkillB 动画）  

---

## 五、编辑器操作

1. 打开技能图编辑器（Skill Graph）  
2. 在动作类节点中选择 **「Return动画 / ReturnAnimation」**  
3. 填写 `anim_name`（默认 `return`）  
4. 将节点接在特效/伤害链末尾  

---

## 六、检查清单

- [ ] 技能 Spine 含 `atk` 事件  
- [ ] 隐藏类技能含 `hide` 事件  
- [ ] Spine 含 `return` 动画（或节点 `anim_name` 与资源一致）  
- [ ] 技能图末尾有 `ReturnAnimation` 节点  
- [ ] 实机验证：hide 后不回 Idle，Return 后正常站立  
- [ ] 实机验证：连续放两个技能时，前一个 Return 不覆盖后一个  
