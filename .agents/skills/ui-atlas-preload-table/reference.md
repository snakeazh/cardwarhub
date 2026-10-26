# UI 图集预加载表 — 参考

## 关键文件

| 用途 | 路径 |
|------|------|
| **唯一写入目标** | `FusionContinent/Assets/Scripts/UIFramework/Core/UIAtlasPreloadTable.cs` |
| 运行时消费 | `FusionContinent/Assets/Scripts/UIFramework/Core/AtlasManager.cs` |
| Global 手写图集字段 | `FusionContinent/Assets/Scripts/Gloabl.SpriteAltas.cs` |
| Global 自动生成字段 | `FusionContinent/Assets/Scripts/Global.SpriteAtlas.Auto.cs` |
| 特性定义 | `FusionContinent/Assets/Scripts/Attributes/SpriteAtlasTargetAttribute.cs` |
| UI 面板目录 | `FusionContinent/Assets/Scripts/UI/` |
| ViewModel | `FusionContinent/Assets/Scripts/Game/{Module}/ViewModels/` |

运行时地址：`SpriteAtlas_{AtlasKey}`。表内与分析结果一律用**短名**。

## PanelKey 约定

- 使用 C# 类型的 `Type.Name`（如 `ShopPanel01New`）。
- UIKit 旧面板与 UIFramework `*New` 可各写一条。
- 用户给中文名时：先搜类名再确认 Key。

## 定位面板文件

1. `Glob`：`**/{PanelName}.cs`、`**/{PanelName}New.cs`
2. 读 `[PrefabPath("...")]`（若有）
3. 同功能旧 Panel：`Xxx.cs` + `Xxx.Designer.cs`
4. ViewModel：`grep` 面板打开处或 `*ViewModel` 同模块

## Global 字段 → AtlasKey 短名

`[SpriteAtlasTarget("Key")]` 的 `Key` 即短名。常用映射（字段名 → 短名）：

| Global 字段 | AtlasKey |
|-------------|----------|
| `ItemIconSpriteAtlas` | `ItemIcon` |
| `CardIconSpriteAtlas` | `CardIcon` |
| `ShopIconSpriteAtlas` | `ShopIcon` |
| `ShopGoodsIconAtlas` | `ShopGoodsIcon` |
| `EquipIconSpriteAtlas` | `WeaponIcon` |
| `FusionGemIconSpriteAtlas` | `FusionGemIcon` |
| `HeroIconSpriteAtlas` | `HeroPaintHead` |
| `HeroHeadIconAtlas` | `HeroHead` |
| `TreasureChestIconSpriteAtlas` | `TreasureChestIcon` |
| `UICommonBtnAtlas` | `UICommonBtn` |
| `BigWorldIconAtlas` | `BigWorldIcon` |
| `CommonAtlas` | `Common` |
| `LoginAtlas` | `Login` |
| `EquipBgStyleAtlas` | `EquipBgStyle` |
| `AttributeIconAtlas` | `AttributeIcon` |
| `SpecialSkillIconAtlas` | `SpecialSkillIcon` |
| `PlayerHeadAtlas` | `PlayerHead` |
| `PlayerHeadFrameAtlas` | `PlayerHeadFrame` |

不确定时：**读源文件上的 `[SpriteAtlasTarget("...")]`**，勿猜。

## Grep 建议

在面板目录 / 相关 ViewModel 上搜：

```
Global\.\w+(Atlas|SpriteAtlas)
GetSpriteExt
GetSprite\(
SpriteAtlas_
SpriteAtlasTarget
```

## 与 Common 的关系

- `CommonAtlasKeys`：启动常驻 Pin。
- `sPanelAtlasKeys`：面板相关依赖；**已在 Common 的短名不写入**。
- 用户「放到通用 / X 是通用图集」：写入/确认 Common，并从面板条目去掉。