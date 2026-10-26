# Migration / 改动差异报告：轮语者武器属性聚合与 ExtraAttributes 规则

> 日期：2026-07-01  
> 范围：武器 Module 属性接入、SceneInitAttributeAggregation 日志、1007 ExtraAttributes 规则修复

---

## 1. 已移除 / 变更的逻辑

### 1.1 武器属性获取

- **原：** `WeaponService.GetAttributes()` 仅合并 `BaseAttributes`，忽略 `ExtraAttributes`
- **新：** 合并 `BaseAttributes` + `ExtraAttributes`，Module 类经 `AddNonModuleBaseValues` 过滤

### 1.2 规则施加路径

- **原：** 无武器 Module 规则入口；即使配置 `LearnSkillRule` 也不注册战斗规则
- **新：** `GetModuleAttributeEntries()` + `ApplyEquipModuleAttributes` 与装备同链路

### 1.3 命名空间

- `Game.Player.WeaponService` 中 `Equip.xxx` 改为 `FusionContinent.Model.Equip.xxx`（避免 `Game.Equip` 歧义）

---

## 2. 架构变化

| 层级 | 变化 |
|------|------|
| **WeaponService** | 拆分数值聚合 vs Module 规则；`TryGetEquippedLevelConfig` 共用 |
| **SceneInitAttributeAggregation** | 具名来源列表 + 分来源 `LogTool` 输出 |
| **GamePlayManager** | `ApplyModuleAttributeEntries` 抽象；武器规则 `SourceType=SpeakerSkin` |
| **AttributeManager** | `SpeakerWeapon_` 规则延后触发（同 Equip） |
| **BattleRuleService** | `ApplyEquipLearnedRulesToSoldier` 含 SpeakerWeapon 条目 |
| **BattleRuleNewSoldierSyncSupport** | `SpeakerWeapon_` 参与 Reevaluate；编队条件用 Lineup Provider |
| **PlayerEquipModel** | `AddNonModuleBaseValues` / `AddNonModuleSubAttributes` 改为 public |

---

## 3. 代码改动清单（文件级）

| 文件 | 改动类型 |
|------|----------|
| `Assets/Scripts/Game/Player/Services/WeaponService.cs` | 修改：GetAttributes、新增 GetModuleAttributeEntries |
| `Assets/Scripts/Scenes/LoadingModules/Data/SceneInitAttributeAggregation.cs` | 修改：来源日志、武器来源 |
| `Assets/Scripts/Game/GamePlayManager.cs` | 修改：ApplyEquipModuleAttributes、ApplyModuleAttributeEntries |
| `Assets/Scripts/Model/PlayerEquipModel.cs` | 修改：过滤方法 public |
| `Assets/Scripts/Manager/AttributeManager.cs` | 修改：SpeakerWeapon 延后 |
| `Assets/Scripts/Battle/Rules/BattleRuleService.cs` | 修改：补同步含武器规则 |
| `Assets/Scripts/Battle/Rules/BattleRuleNewSoldierSyncSupport.cs` | 修改：StackKey + ValueProvider |
| `Assets/Scripts/Battle/Rules/BattleRuleRosterCompositionSupport.cs` | 新增：UsesLineupScopedConditions |

---

## 4. 需要 TODO / 验证的事项

- [ ] 武器 1007：激活 + 穿戴后进战斗，确认 4 条规则登记与 Reevaluate
- [ ] 不同职业数 1~4 档条件与伤害加成是否与设计一致
- [ ] 卸下武器后下一局无 `SpeakerWeapon_` 规则
- [ ] `SceneInitAttributeAggregation` 日志中武器仅显示 Base 数值
- [ ] 轮语者皮肤 ExtraAttributes 中 LearnSkillRule 是否也需对齐武器过滤（当前 SpeakerService 未过滤 Module）

---

## 5. 测试检查清单

### 功能

- [ ] 穿戴武器 → 加载阶段有 Base 属性日志
- [ ] 穿戴武器 → 战斗开局 Module count 与 ExtraAttributes 中 LearnSkillRule 条数一致
- [ ] 编队满足 DifferentProNum → Reevaluate 日志出现
- [ ] 未穿戴 → 无武器属性、无武器规则

### 兼容性

- [ ] 装备 LearnSkillRule 链路无回归
- [ ] 奥数共鸣 / 天赋 / 皮肤数值聚合正常
- [ ] 后召唤英灵仍触发武器规则 Reevaluate

### 稳定性

- [ ] 无 `Game.Equip` / `FusionContinent.Model.Equip` 编译冲突
- [ ] 重复进战斗规则不异常叠加（StackKey 按 RuleId 唯一）

---

## 6. 历史问题规避说明

1. **UILevel / 命名空间：** 武器侧显式使用 `FusionContinent.Model.Equip`，避免与 `Game.Equip` 模块冲突（同类于迁移文档中的 UILevel 歧义检查）
2. **Module 规则重复叠加：** 武器 LearnSkillRule 不进 `SceneInitAttributeAggregation`，仅走战斗开局 AttributeManager 单一路径
3. **动态规则未生效：** 编队类条件（DifferentProNum）必须使用 Lineup ValueProvider + 延后 Reevaluate，避免获得时 Acquire Provider 判 0 失败

---

## 7. 后续优化建议

1. **SpeakerService 对齐：** 皮肤 `ExtraAttributes` 建议同样拆分 Module / 非 Module，避免 LearnSkillRule 误入数值聚合
2. **配置一致性：** 112095~112098 等编队语义规则可考虑 Luban 侧 `GloballyEffective=1`
3. **AttributeSourceType：** 若后续武器规则增多，可评估新增 `SpeakerWeapon` 来源类型，替代复用 `SpeakerSkin`

---

## 8. 文档索引

- 武器 ExtraAttributes 详解：`Assets/Scripts/Game/Player/WeaponExtraAttributes_Context.md`
- 属性聚合模块：`Assets/Scripts/Scenes/LoadingModules/Data/SceneInitAttributeAggregation_Context.md`
- 装备规则（对照）：`Assets/Scripts/Battle/Rules/BattleRule_EquipLearnSkillRule_Context.md`
