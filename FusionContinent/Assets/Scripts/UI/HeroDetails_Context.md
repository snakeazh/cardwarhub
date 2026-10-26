# HeroDetails 战斗内英灵详情 AI 上下文

> 供后续 AI 对话或开发接续使用。  
> 最后更新：2026-06-29  
> 关联：`HeroDetails.cs`、`SoldierCard.cs`、`BattleRule_EquipLearnSkillRule_Context.md`

---

## 1. 打开方式

- 战斗内长按英灵 → `SoldierCard.OnHeroDetailsLongPress`
- `UIKit.OpenPanel<HeroDetails>(UILevel.PopUI).SetData(this)`
- 松开 → `UIKit.ClosePanel<HeroDetails>()`

---

## 2. 数据来源

| 展示区域 | 数据来源 |
|----------|----------|
| 主属性（力量/敏捷/智力等） | `BaseAttributes.GetAllMainAttributeValues()` |
| 滚动列表（攻强/攻速等） | `SoldierBattleConfig.ShowAttributeTypes` + `BaseAttributes.GetFinal(type)` |

**重要：** 面板读的是 **场上 `SoldierCard.BaseAttributes` 实时最终值**，包含战斗规则 `ModifyAttr`（装备 LearnSkillRule 等）。  
**不**走 `Equip.GetAttributes(cardId)`（该路径用于编队/养成界面聚合，且不含 Module 类 LearnSkillRule 的战斗施加结果）。

---

## 3. 与普攻伤害的关系

普攻物理伤害（`SkillBase.SumDamageTypes`）：

```text
physDmg ≈ skillPower + SoldierAttackPower(final) × inheritance + ...
```

因此 `[HeroDetails] final` 应与 `[Attack] final`（`SoldierAttackPower`）一致；若不一致，先查装备规则是否写入 `BaseAttributes`（见装备规则 Context §6）。

---

## 4. 已实现修复（2026-06-29）

1. `SetData` 开头 `_attributeList.Clear()`，避免面板复用显示旧条目/旧数值  
2. `OnUpdateCardItem` 使用 `_soldierCard.BaseAttributes.GetFinal(attr.Type)`，非缓存 `attr.Value`  
3. `OnClose` 清理 `_soldierCard` 与列表  
4. 调试日志：`[HeroDetails] hero=... SoldierAttackPower base=... final=...`

---

## 5. 排查步骤

1. 进局并确认装备规则已施加（见 `[BattleRuleAcquireSoldierEffectApplier]` 或 `[Attack]` 日志）  
2. 长按同一英灵，对比 `[HeroDetails]` 与 `[Attack]` 的 `final`  
3. 若日志正确但 UI 数字错误 → 查 `RollingNumberManager`、ScrollView 复用项  
4. 若日志均为 base → 查 `ApplyEquipLearnedRulesToSoldier` 时机与条件判定

---

## 6. 相关文件

- `Assets/Scripts/UI/HeroDetails.cs`
- `Assets/Scripts/UI/HeroDetails.Designer.cs`（自动生成，勿手改）
- `Assets/Res/Prefab/HeroDetails.prefab`
- `Assets/Scripts/Game/SoldierCard.cs`（长按入口）
- `Assets/Scripts/Game/Skill/SkillBase.cs`（`[Attack]` 日志）
