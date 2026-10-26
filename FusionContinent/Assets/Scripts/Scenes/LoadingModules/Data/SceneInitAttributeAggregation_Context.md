# SceneInitAttributeAggregation 属性聚合 AI 上下文

> 供后续 AI 对话或开发接续使用。  
> 最后更新：2026-07-01  
> 关联：`SceneInitAttributeAggregation.cs`、`GamePlayManager.cs`、`WeaponExtraAttributes_Context.md`

---

## 1. 模块职责

场景加载链路中的 **外部养成数值属性** 聚合模块：

- 执行时机：`GamePlayManager` 加载流程中（`SceneInitAttributeAggregation` 组件）
- 输出目标：
  - `GamePlayManager.ModuleAttributes`（`AttributeSet`）
  - `GamePlayManager.ModuleMainAttributes`（主属性字典）
- **不包含** Module 类规则属性（`LearnSkillRule` 等）—— 规则在战斗 `InitCardTeams` → `ApplyEquipModuleAttributes` 中单独处理

---

## 2. 属性来源清单

| 日志名称 | 获取方法 | 说明 |
|----------|----------|------|
| 轮语者皮肤 | `SpeakerService.GetAttributes()` | 含 `ImageStarList` Base/Extra（Extra 中 Module 仍可能被写入，待与武器对齐过滤） |
| 天赋 | `Talent.GetAttributes()` | 天赋树等级属性 |
| 装备 | `Equip.GetAttributes()` | 已过滤 Module 类（词条/特效 LearnSkillRule 不进聚合） |
| 轮语者武器 | `WeaponService.GetAttributes()` | BaseAttributes + ExtraAttributes（非 Module） |

> 注：奥数共鸣（`ArcResonanceService`）已随「去共鸣」改造移除，不再是属性来源。

聚合完成后调用：`gpm.RefreshCarBindingAttributes()`

---

## 3. 日志格式

```
[SceneInitAttributeAggregation] {来源名}: {AttributeType}={BaseValue}, ...
[SceneInitAttributeAggregation] {来源名}: Main[{MainAttributeType}]={Value}
[SceneInitAttributeAggregation] {来源名}: (无属性)
```

实现：`LogAttributeAggregationSource` — 遍历 `AttributeAggregation.Attributes.GetAll()` 与 `MainAttributes`。

### 3.1 解读注意

- **LearnSkillRule 不会出现在日志中**（武器/装备 ExtraAttributes 的 Module 部分被过滤或未进入 `GetAttributes`）
- `(无属性)` 表示该来源当前无有效数值（如未穿戴武器、无装备等）
- 日志仅反映 **BaseValue 非 0** 的条目

---

## 4. Module 规则 vs 数值聚合

```
                    ┌─────────────────────────────────────┐
  养成配置           │  BaseAttributes / 非 Module Extra   │──→ SceneInitAttributeAggregation
                    └─────────────────────────────────────┘
                    ┌─────────────────────────────────────┐
                    │  Module Extra（LearnSkillRule 123）  │──→ ApplyEquipModuleAttributes（战斗开局）
                    └─────────────────────────────────────┘
```

新增养成来源时：

1. 数值：在 `SetAttributeAggregation` 的 `attributeSources` 列表注册，并实现 `GetAttributes()`
2. 规则：实现 `GetModuleAttributeEntries()`（或等价），并在 `GamePlayManager.ApplyEquipModuleAttributes` 中施加

---

## 5. 扩展新来源步骤

1. Service/Model 提供 `AttributeAggregation GetAttributes()`，Module 属性用 `Equip.AddNonModuleBaseValues` 过滤
2. 若有 LearnSkillRule，提供 Module 条目获取并在 `ApplyEquipModuleAttributes` 注册
3. 在 `attributeSources` 增加 `( "显示名", xxx.GetAttributes() )`
4. 验证加载日志与战斗规则日志分离、无重复叠加

---

## 6. 相关文件

| 文件 | 职责 |
|------|------|
| `SceneInitAttributeAggregation.cs` | 本模块实现 |
| `GamePlayManager.cs` | `ModuleAttributes` / `ModuleMainAttributes` 持有者 |
| `DataModel.cs` | `AttributeAggregation` 定义 |
| `WeaponService.cs` | 武器数值 + Module 规则分离 |
| `PlayerEquipModel.cs` | 装备 Module 过滤工具 |
