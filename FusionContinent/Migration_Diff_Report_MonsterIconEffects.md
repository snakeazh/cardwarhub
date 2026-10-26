# MonsterIconEffects 场景状态图标迁移差异报告

## 完成时间
2026-07-24

## 背景
怪物状态图标（缴械 / 斩杀 / 破盾）原先挂在 `MonsterCard1` 预制体的 World Space Canvas（`Canvas/IconEffects`）下，靠父子节点跟随。现改为与 `Healtheffect` 相同的方案：使用 GamePlay 场景中 `GameLandUI/MonsterIconEffects/IconEffects`（Screen Space UI），通过世界坐标转 UI 坐标跟随怪物移动。

---

## 1. 已移除 / 停用的逻辑

### 1.1 预制体 World Space IconEffects 作为显示源

**原行为：**
- 预制体 `Canvas/IconEffects` 随怪激活，子节点 `DisarmIcon` / `ExecutionIcon` / `ShieldBreakIcon` 直接挂在怪身上。

**状态：** ✅ 预制体 `IconEffects` 在 `Awake` 中关闭并保持关闭；运行时改绑场景克隆实例。

### 1.2 OnDisable 仅关本地节点

**新代码：**
```csharp
ReleaseSceneIconEffects(); // 归还对象池 + 还原预制体字段引用并关闭
```

与血条的 `ReleaseSceneHealthBar()` 并列，在 `MonsterCard.OnDisable` 中调用。

---

## 2. 新增逻辑

### 2.1 MonsterIconEffectsManager（模板 + 对象池）

路径：`Assets/Scripts/Game/Monster/MonsterIconEffectsManager.cs`

- `EnsureInitialized`：从 `GameLandUI` 查找 `MonsterIconEffects/IconEffects`
- `Acquire` / `Release`：多怪同屏时克隆模板，死亡回池
- `worldOffset`：相对怪物世界坐标偏移（默认 `(0, 1.6, 0)`）

### 2.2 MonsterIconEffects（跟随）

路径：`Assets/Scripts/Game/Monster/MonsterIconEffects.cs`

- `LateUpdate`：`Camera.main` WorldToScreen → Canvas UI 相机 → `MonsterIconEffects` 本地坐标
- `Bind` / `Unbind`：绑定目标怪物；入池时重置三图标为关闭
- 暴露 `DisarmIcon` / `ExecutionIcon` / `ShieldBreakIcon` 供 `MonsterCard` 字段重定向

### 2.3 MonsterCard 绑定入口

```csharp
BindSceneIconEffects();    // Init（所有怪物）
ReleaseSceneIconEffects(); // OnDisable
```

存活期间：
```csharp
DisarmIcon = _sceneIconEffects.DisarmIcon;
ExecutionIcon = _sceneIconEffects.ExecutionIcon;
ShieldBreakIcon = _sceneIconEffects.ShieldBreakIcon;
```

外部调用方（`MonsterAbnormal`、`MonsterDamageReceiver` 等）仍通过原字段名访问，无需改业务 API。

### 2.4 GameLandUI 初始化

```csharp
MonsterHealthBarManager.EnsureInitialized(transform);
MonsterIconEffectsManager.EnsureInitialized(transform);
```

---

## 3. 依赖与数据流变化

| 项目 | 变化 |
|------|------|
| 图标父节点 | 预制体 `Canvas/IconEffects` → `GameLandUI/MonsterIconEffects` |
| 跟随方式 | 父子 Transform → 每帧坐标转换 |
| 实例数量 | 一怪一预制体节点 → 模板 + 池 |
| 绑定范围 | 全怪（与血条仅 Elite/Boss 不同） |
| 业务开关 | 仍 `xxxIcon.SetActive(...)`，字段指向场景实例 |
| 预制体节点 | 保留但禁用，稳定后可删 |

---

## 4. 需要 TODO 的事项

- [ ] TODO 1: 实机微调 `worldOffset`（与血条相对位置）
- [ ] TODO 2: 验证多怪同屏、回池后再刷怪无残留图标
- [ ] TODO 3: 验证缴械 / 斩杀 / 破盾三种图标显隐与延时关闭
- [ ] TODO 4: 稳定后从 `MonsterCard1` 预制体删除旧 `Canvas/IconEffects`
- [ ] TODO 5:（可选）破盾 `DOVirtual.DelayedCall` 与回池竞态：怪提前回收时避免操作已还原的预制体引用

---

## 5. 测试检查清单

### 功能
- [ ] 全品质怪物图标根节点跟随
- [ ] 缴械图标显示/清除
- [ ] 斩杀图标
- [ ] 破盾图标延时关闭
- [ ] 死亡/回池隐藏

### 兼容
- [ ] 业务代码仍写 `monster.DisarmIcon` 等，无需改调用点
- [ ] 与场景血条互不干扰（独立 Manager / 池）

### 稳定
- [ ] 无空引用 / 无模板找不到日志（配置正确时）
- [ ] 对象池反复进出无泄漏、无屏上残留

---

## 6. 相关文档

- 架构上下文：`Assets/Scripts/Game/Monster/MonsterIconEffects_Context.md`
- 对齐参考（血条）：`Assets/Scripts/Game/Monster/MonsterHealthBar_Context.md`
- 血条差异报告：`Migration_Diff_Report_MonsterHealthBar.md`

---

## 7. 历史问题规避说明

1. **关闭路径**：回池走 `Release` + `Unbind`，避免场景图标残留在屏上。
2. **动态内容**：每怪独立克隆一组 IconEffects，避免多怪共用导致显隐错乱。
3. **Fail Fast**：找不到 `MonsterIconEffects`/`IconEffects` 打 `LogTool.LogError`，不回退到预制体 World Space 图标。
