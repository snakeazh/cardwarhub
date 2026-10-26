# 属性系统 AI 上下文

> 供后续 AI 对话或开发接续使用。  
> 最后更新：2026-06-05

---

## 1. 核心类型

| 类型 | 路径 | 职责 |
|------|------|------|
| `AttributeValue` | `Assets/Scripts/Game/Attribute/AttributeValue.cs` | 单属性：BaseValue + Modifier 叠加 → FinalValue |
| `AttributeSet` | `Assets/Scripts/Game/Attribute/AttributeSet.cs` | 属性集合，懒初始化 `GetAttr(type)` |
| `AttributeSetExtensions` | `Assets/Scripts/Game/Attribute/AttributeSetExtensions.cs` | InitFromConfig、主属性、Buff/羁绊便捷接口 |
| `StaticAttributeCalculator` | `Assets/Scripts/Game/Attribute/StaticAttributeCalculator.cs` | 战斗外静态属性计算 |
| `AttributeManager` | `Assets/Scripts/Manager/AttributeManager.cs` | 全局模块属性、本局规则 Id 缓存 |

---

## 2. 配置依赖（强制）

### 2.1 枚举 vs 配表

- 枚举定义：`Assets/Scripts/Data/Client/Luban/AttributeType.cs`（Luban 生成，勿手改）
- 属性元数据：`Assets/Res/Config/Json/tbattributeconfig.json`
- 运行时查表：`DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(type)`

### 2.2 懒初始化与 fail-fast

```csharp
// AttributeSet.GetAttr → new AttributeValue(type)
// AttributeValue 构造函数：
var cfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(type);
if (cfg == null)
    throw new Exception($"Init AttributeValue Not Configured Type {type}");
```

**规则：凡是在代码中调用 `GetFinal` / `GetAttr` / `SetAttr` 的 `AttributeType`，必须在 `tbattributeconfig.json` 中有对应条目。**

新增枚举值后必须同步：
1. Excel 源表（Luban 导出）
2. `tbattributeconfig.json`
3. 若技能/特效配置已引用该 Type，确认 ValueLimit / DisplayType 合理

---

## 3. 已知问题：SkillFireDamage (194)

### 3.1 异常栈（2026-06-05 报告）

```
Exception: Init AttributeValue Not Configured Type SkillFireDamage
  AttributeValue..ctor(AttributeType type)
  AttributeSet.GetAttr → GetFinalSpecial → GetFinal
  MonsterCard.TakeDamage(...)  // line ~1268
  MonsterManager.OnAddHitEvent
  PenetrateHitModule.OnHit
```

### 3.2 根因

| 项 | 状态 |
|----|------|
| `AttributeType.SkillFireDamage = 194` | 枚举已存在 |
| `tbattributeconfig.json` AttributeType 194 | **曾缺失**（策划/导出漏配） |
| `tbspecialeffectsconfig.json` Type 194 | 已有多处引用（如 Value 0.2） |
| 代码引用 | `MonsterCard.TakeDamage` 读取灼烧增伤 |

### 3.3 语义重复属性

| 枚举 | Id | 配表 Name | 代码引用 |
|------|----|-----------|----------|
| `SkillDamageBonusBurnUnits` | 167 | 技能对灼烧单位额外伤害 | **无** |
| `SkillFireDamage` | 194 | 灼烧增伤 | `MonsterCard.TakeDamage` |

167 与 194 语义相近；当前业务以 **194** 为准。167 为历史遗留，特效/规则配置未使用。

### 3.4 业务逻辑（MonsterCard.TakeDamage 前置）

```csharp
// 1. SkillDamage(142)：无条件技能伤害加成
damage *= (1 + skillCard.Attributes.GetFinal(AttributeType.SkillDamage));

// 2. SkillFireDamage(194)：目标有灼烧层数时，每层额外增伤
float fireBonus = skillCard.Attributes.GetFinal(AttributeType.SkillFireDamage);
if (fireBonus > 0) {
    int burnStack = sum(BuffController.GetBuffs(BuffType.Burn).Stack);
    if (burnStack > 0)
        damage *= (1 + fireBonus * burnStack);
}
```

**注意：** 此逻辑在 `DamagePipeline` 之前执行，属于「进站伤害前置修正」，与 `DamageSystem_README.md` 中「后续可优化：迁入 DamageContextBuilder」一致。

### 3.5 修复方案

**推荐（配置修复）：** 在 `tbattributeconfig.json` 补全 194：

```json
{
  "AttributeType": 194,
  "Name": "灼烧增伤",
  "Icon": "",
  "ValueLimit": { "Minimum": 0, "Maximum": 2.1E+09 },
  "Algorithm": "加法",
  "Category": 3,
  "DisplayType": 0,
  "AdditionAttributes": []
}
```

同步 Excel 源表后重新 Luban 导出，避免 JSON 手改被覆盖。

**不推荐：** 改代码 fallback 到 167 或静默返回 0——会隐藏配置错误。

---

## 4. 属性读取路径

### 4.1 技能属性

- 技能实例：`ISkill.Attributes`（`AttributeSet`）
- 来源：技能配置 `SpecialEffectsConfig.Attributes` → 初始化时写入
- 战斗计算：`skillCard.Attributes.GetFinal(AttributeType.XXX)`

### 4.2 英灵属性

- `SoldierCard.BaseAttributes` / 运行时 `AttributeSet`
- 初始化：`AttributeSetExtensions.InitFromConfig(cardId, lv)`
- 特殊派生：`GetFinalSpecial` 处理攻速/暴击/闪避等（非简单 FinalValue）

### 4.3 模块属性

- `AttributeManager.SetAttributeLogic` / `GetAttributeValue`
- 与 `AttributeSet` 分离，用于 LockAtkInterval 等全场模块

---

## 5. Modifier 模式

```csharp
AttributeModifier {
    SourceType,   // Base / Buff / Talent / Equip ...
    SourceKey,    // 来源唯一键（Buff 用 BuffId.ToString()）
    FlatAdd,      // 固定加值
    PercentAdd,   // 百分比（0.1 = +10%）
    ProbabilityTrigger,
    EffectParam
}
// FinalValue = (BaseValue * (1 + Σ PercentAdd)) + Σ FlatAdd
// 受 ValueLimit.Minimum / Maximum 约束
```

---

## 6. 相关配置路径

| 配置 | 路径 |
|------|------|
| 属性元数据 | `Assets/Res/Config/Json/tbattributeconfig.json` |
| 技能特效属性 | `Assets/Res/Config/Json/tbspecialeffectsconfig.json` |
| 主属性转换 | `Assets/Res/Config/Json/tbmainattributeconfig.json` |
| 英灵等级属性 | `Assets/Res/Config/Json/tbsoldierbattleconfig.json` |

---

## 7. 上下文文件索引（本模块）

| 文件 | 说明 |
|------|------|
| `README_MainAttribute.md` | 主属性系统说明 |
| `AttributeSystem_Context.md` | 本文档（AI 上下文） |
| `../DamageCalculator/DamageSystem_README.md` | 伤害系统（含 SkillDamage/灼烧前置） |

---

## 8. 排查清单：AttributeValue Not Configured

1. 从异常栈定位 `AttributeType` 枚举名
2. `grep` 枚举值：`AttributeType.cs`
3. `grep` 配表：`tbattributeconfig.json` 搜 `"AttributeType": <id>`
4. `grep` 谁引用：`GetFinal(AttributeType.XXX)` / `"Type": <id>`
5. 补配表或改代码引用到已配置类型（单一真值源）
6. Unity 重载配置或重启验证
