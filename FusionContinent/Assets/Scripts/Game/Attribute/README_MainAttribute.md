# 属性系统与主属性系统说明

## 概述

当前项目的属性兼容层目标是：

- 不修改现有 `AttributeSet`
- 通过扩展方法补齐初始化、查询和主属性支持
- 统一战斗内与战斗外属性读取方式
- 对接项目真实配表，而不是通用示例字段

当前主属性系统的核心数据来源为：

- `TbSoldierBattleConfig.MainAttrType`
- `SoldierLevelPropertyList.MainAttributes`
- `TbMainAttributeConfig.ConvertToBaseAttributes`

含义是：

1. 每个英灵有一个主属性类型
2. 每个等级会提供一组主属性值
3. 主属性再按 `TbMainAttributeConfig` 转换为基础属性加成

## 关键文件

主属性兼容能力集中在以下文件：

| 文件 | 说明 |
|------|------|
| `IAttributeProvider.cs` | 统一属性提供接口 |
| `StaticAttributeCalculator.cs` | 战斗外静态属性计算 |
| `AttributeSetExtensions.cs` | `AttributeSet` 扩展能力 |
| `MainAttributeConfig.cs` | 主属性配置/转换辅助 |

## 初始化方式

### 初始化 AttributeSet

```csharp
var attrSet = new AttributeSet();
attrSet.InitFromConfig(configId: 1001, level: 50, star: 5, awaken: 2);
```

### 手动指定主属性

```csharp
attrSet.InitFromConfig(
    configId: 1001,
    level: 50,
    star: 5,
    awaken: 2,
    mainType: MainAttributeType.Strength
);
```

## 常用调用

### 战斗内

```csharp
float atk = attrSet.GetFinal(AttributeType.SoldierAttackPower);
int configId = attrSet.GetConfigId();
int level = attrSet.GetLevel();
var mainType = attrSet.GetMainAttributeType();
var mainValue = attrSet.GetMainAttributeValue();
var allMainValues = attrSet.GetAllMainAttributeValues();
```

### 主属性变化后刷新

如果战斗内某个主属性被 Buff、被动或其他逻辑修改，可以手动刷新：

```csharp
attrSet.UpdateMainAttributeValue(MainAttributeType.Strength, 120f);
```

如果只是刷新当前主属性：

```csharp
attrSet.UpdateMainAttributeValue(120f);
```

### 战斗外

```csharp
var panel = StaticAttributeCalculator.CalcBaseAttributes(1001, 50);
var mainPanel = StaticAttributeCalculator.CalcMainAttributes(1001, 50);
```

## Buff / 羁绊便捷方法

```csharp
attrSet.ApplyBuff("buff_001", new Dictionary<AttributeType, (float, float)>
{
    { AttributeType.SoldierAttackPower, (100f, 0.2f) },
    { AttributeType.SoldierMaxHealth, (500f, 0f) }
});

attrSet.RemoveBuff("buff_001");

attrSet.ApplyBond("bond_001", new Dictionary<AttributeType, (float, float)>
{
    { AttributeType.SoldierAttackPower, (0f, 0.1f) }
});
```

## 计算链路

`InitFromConfig(...)` 当前会做这些事：

1. 记录 `configId / level / star / awaken`
2. 从 `TbSoldierBattleConfig` 读取角色基础属性
3. 从 `LevelPropertyList` 读取等级属性
4. 从 `MainAttributes` 读取该等级主属性值
5. 按 `TbMainAttributeConfig.ConvertToBaseAttributes` 把主属性转成基础属性加成

## 主属性兼容说明

当前兼容层不是示例版通用实现，而是对接项目真实配表。

## 注意事项

1. 这里使用的是项目真实配表，不是通用示例字段
2. 属性枚举请使用项目里的真实值，例如 `AttributeType.SoldierAttackPower`
3. 主属性枚举使用 `Config.MainAttributeType`
4. 主属性加成来源统一使用 `AttributeSourceType.MainAttribute`
5. 不再维护额外的 V2 扩展文件，统一通过 `AttributeSetExtensions.cs` 提供能力

## 兼容性结论

✅ 现有 `AttributeSet` 可继续使用  
✅ 不需要改 `AttributeSet.cs`  
✅ 初始化、静态查询、主属性转换统一走扩展层  
✅ 不再保留额外扩展文件  
