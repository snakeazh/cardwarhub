# MonsterHealthBar 场景血条迁移差异报告

## 完成时间
2026-07-21

## 背景
精英/Boss 血条原先挂在 `MonsterCard1` 预制体的 World Space Canvas 下，靠父子节点跟随。现改为使用 GamePlay 场景中 `GameLandUI/MonsterHealth/Healtheffect`（Screen Space UI），通过世界坐标转 UI 坐标跟随怪物移动。

---

## 1. 已移除 / 停用的逻辑

### 1.1 预制体 World Space 血条激活与缩放补偿

**原代码：**
```csharp
Healtheffect.gameObject.SetActive(true);
HealthInit();
Healtheffect.transform.localScale = (Vector3.one / this.transform.localScale.x) / 100f;
```

以及 Init 中途对 Elite/Boss 的提前 `SetActive(true)`。

**状态：** ✅ 已改为绑定场景血条；预制体 `Healtheffect` 在 `Awake` 中关闭并保持关闭。

### 1.2 OnDisable 直接关预制体血条

**原代码：**
```csharp
Healtheffect.gameObject.SetActive(false);
```

**新代码：**
```csharp
ReleaseSceneHealthBar(); // 归还对象池 + 还原预制体引用并关闭
```

---

## 2. 新增逻辑

### 2.1 MonsterHealthBarManager（模板 + 对象池）

路径：`Assets/Scripts/Game/Monster/MonsterHealthBarManager.cs`

- `EnsureInitialized`：从 `GameLandUI` 查找 `MonsterHealth/Healtheffect`
- `Acquire` / `Release`：多精英同时存在时克隆模板，死亡回池
- `worldOffset`：血条相对怪物世界坐标偏移（默认 `(0, 1.5, 0)`）

### 2.2 MonsterHealthBar（跟随）

路径：`Assets/Scripts/Game/Monster/MonsterHealthBar.cs`

- `LateUpdate`：`Camera.main` WorldToScreen → Canvas UI 相机 → `MonsterHealth` 本地坐标
- `Bind` / `Unbind`：绑定目标怪物与释放

### 2.3 MonsterCard 绑定入口

```csharp
BindSceneHealthBar();   // Init Elite/Boss
ReleaseSceneHealthBar(); // OnDisable
```

`Healtheffect` 在存活期间指向场景克隆实例，外部（`DamageUIManager`、`MonsterDamageService` 引导）仍通过该字段访问子节点。

### 2.4 GameLandUI 初始化

```csharp
MonsterHealthBarManager.EnsureInitialized(transform);
```

---

## 3. 依赖与数据流变化

| 项目 | 变化 |
|------|------|
| 血条父节点 | 预制体 Canvas → `GameLandUI/MonsterHealth` |
| 跟随方式 | 父子 Transform → 每帧坐标转换 |
| 实例数量 | 一怪一预制体节点 → 模板 + 池 |
| HP/韧性刷新 | 仍在 `MonsterCard.HealthInit` / `SetHealth`（未拆 Controller） |
| 引导锚点 | 仍 `Healtheffect.transform.Find("health")`，路径兼容 |

---

## 4. 需要 TODO 的事项

- [ ] TODO 1: 实机微调 `worldOffset`（不同体型 Boss/精英）
- [ ] TODO 2: 验证多精英同屏、回池后再刷怪无残留
- [ ] TODO 3: 验证破韧/虚弱引导锚点
- [ ] TODO 4: 稳定后从 `MonsterCard1` 预制体删除旧 Canvas/Healtheffect
- [ ] TODO 5:（可选）将 `HealthInit`/`SetHealth` 抽到独立 Controller，对齐士兵血条

---

## 5. 测试检查清单

### 功能
- [ ] 精英/Boss 血条显示与跟随
- [ ] 受击百分比与双层血条动画
- [ ] 韧性条动画
- [ ] 死亡/回池隐藏

### 兼容
- [ ] 普通怪无血条
- [ ] Boss 出场特效/音效
- [ ] 引导节点绑定

### 稳定
- [ ] 无空引用 / 无模板找不到日志（配置正确时）
- [ ] 对象池反复进出无泄漏

---

## 6. 相关文档

- 架构上下文：`Assets/Scripts/Game/Monster/MonsterHealthBar_Context.md`

---

## 7. 历史问题规避说明

1. **关闭路径**：回池走 `Release`，避免场景血条残留在屏上。
2. **动态内容**：每怪独立克隆条，避免多精英共用一条导致列表/显示错乱。
3. **Fail Fast**：找不到 `MonsterHealth`/`Healtheffect` 打 `LogTool.LogError`，不回退到预制体 World Space 血条。
