# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FusionContinent is a Unity-based tower defense mobile game with card collection and strategic battle mechanics. Built with Unity 2022.3.62f3 and C#, it uses hot-fix technology for runtime code updates.

## Build and Development

### Building the Project
- Use Unity Editor to build for target platforms
- The solution file is `FusionContinent.sln`
- HybridCLR is configured for hot-fix support - check `ProjectSettings/HybridCLRSettings.asset`
- Build configurations are managed through Unity's Build Settings

### Configuration Management
- **Luban** is used for configuration with Excel integration
- Configuration files are in `Assets/Scripts/Data/Client/Luban/`
- Game data is loaded through `DataConfigManager`
- Configs are compiled from Excel sources and loaded as runtime data

### Testing
- Unity Test Framework is available (`com.unity.test-framework`)
- Tests should be placed in appropriate test assemblies
- Use Unity Editor's Test Runner window to execute tests

## Architecture

### Core Framework
- **QFramework** - Primary framework providing MVC architecture, IOC container, and event system
- **HybridCLR** - Runtime code hot-fix system allowing updates without app store approval
- **YooAsset** - Asset bundle management system for dynamic resource loading
- **UniTask** - Async programming for Unity coroutines

### Global State Management
- `Global.cs` - Central singleton managing all game state, player data, and global variables
- Uses `BindableProperty<T>` for reactive state management
- Key global data:
  - `User` - User account data
  - `Player` - Player progression data
  - `PlayerAtlas` - Collection/card atlas data
  - `PlayerBattle` - Battle-related player data
  - `PlayerResource` - Resource tracking (gold, diamonds, stamina)
  - Game state properties (Difficulty, Date, StageId, etc.)

### Directory Structure

```
Assets/Scripts/
├── Game/           # Core gameplay logic
│   ├── Skill/      # Skill system implementations
│   ├── Monster/    # Enemy AI and behavior
│   └── Attribute/  # Character attribute system
├── Model/          # Data models and business logic
│   ├── Player*Model.cs    # Player data models
│   └── *Model.cs           # Feature-specific models
├── UI/             # UI controllers and views
│   ├── Panel/      # Individual UI panels
│   └── *.Designer.cs # UI designer generated code
├── Manager/        # System managers (Sound, Scene, Dialog, etc.)
├── Base/           # Base classes and utilities
│   └── Loader/     # Asset loading abstractions (YooAsset wrappers)
├── Data/           # Configuration data structures
├── Helper/         # Helper utilities
├── Utils/          # Utility functions
└── RedPoint/       # Red dot notification system
```

### Key Architectural Patterns

**MVC Pattern**
- Models in `Assets/Scripts/Model/`
- Views/UI in `Assets/Scripts/UI/`
- Controllers are typically in Panel classes or Manager classes

**IOC Container**
- QFramework's IOC system manages dependency injection
- Models are registered and resolved through the container

**Event System**
- `Global.Publisher` - MessagePublisher for cross-system communication
- Type-safe event messaging for loose coupling

**Panel System**
- UI screens inherit from QFramework's `UIPanel`
- Each panel has a corresponding `.Designer.cs` file for UI bindings
- Panel loading is managed through `YooPanelLoaderPool`

### Game Systems

**Card System**
- Soldier cards: Spearmen (2010001), Archer (2010002), Magician (2010003), Assassin (2010004)
- Card data managed through `PlayerCardModel`
- Cards have levels, skills, and attributes

**Battle System**
- Tower defense gameplay with strategic card placement
- Real-time combat with projectile and skill systems
- Building cards for city defense

**Progression Systems**
- City building with upgrades
- Gem enhancement/enhancement system
- Talent system
- Equipment system
- Card collection and atlas

**Resource Management**
- Gold, diamonds, stamina
- Fragment currencies
- Mana system for battles

### Hot-Fix Architecture

- Hot-fix code entry: `GameEntry.StartGame()` in `Assets/Scripts/GameEntry.cs`
- Hot-fix assembly defined in `Assets/Scripts/HotFix.asmdef`
- AOT (Ahead-Of-Time) code in `Assets/AOT/`
- Allows updating game logic without rebuilding the entire application

### Asset Management

- **YooAsset** handles all asset bundle operations
- Assets loaded through addressable system
- Sprite atlases used for texture optimization
- Preloading system for critical assets (UI panels, sounds)

### UI System

- Panel-based architecture where each screen is a separate panel
- UI pooling for performance optimization
- Custom ScrollRect implementations for lists
- Red dot notification system for UI badges
- Event-driven UI updates through reactive properties

### UIFramework MVVM Pattern (New UI Architecture)

项目正在将旧的 QFramework UIPanel 逐步迁移到 UIFramework MVVM 架构。新 UI 遵循以下模式：

**核心类：**
- `ViewModel` — 数据层，持有 `BindableProperty<T>` 和 `Command`
- `UIPanel<TViewModel>` — 视图层，通过 `[PrefabPath("XXX")]` 标注预制体路径
- 保留旧 Panel（`XXX.Designer.cs`）的引用，通过 `mOldPanel` 访问 UI 组件

**已迁移的界面：**
- `SoldierPanel` → `SoldierPanelNew` (ViewModel: `SoldierPanelViewModel`)
- `MainAttrDetail` → `MainAttrDetailNew` (ViewModel: `MainAttrDetailViewModel`)
- `BaseAttrDetail` → `BaseAttrDetailNew` (ViewModel: `BaseAttrDetailViewModel`)

**命令/事件模式：**
```csharp
// ViewModel 中定义命令和事件
public Command OnXXXCommand { get; private set; }
public System.Action<int> OnXXXRequested;

// 命令初始化
OnXXXCommand = new Command(OnXXX);

// 命令实现
private void OnXXX() { OnXXXRequested?.Invoke(args); }
```

```csharp
// New Panel 中订阅事件、绑定按钮
vm.OnXXXRequested += OnXXXRequested;
mOldPanel.XXXBtn.onClick.AddListener(() => vm.OnXXXCommand.Execute());
```

**动态列表模式：**
- ViewModel 用 `List<RowData>` 持有行数据
- New Panel 在 `OnBindViewModel` 中克隆模板 GameObject 填充数据
- 模板从 `mOldPanel.字段.transform.parent` 获取 Content 和 Template

**动画系统：**
- `UIAnimation` 基类，子类有 `UITweenScale`、`UITweenAlpha` 等
- 通过 `UIAnimationUtils.PlayAnimations(gameObject, onComplete)` 触发
- 打开/关闭动画分别在 `OnOpen` 和 `OnHide`/ExitBtn 回调中播放

### Attribute System (角色属性系统)

**核心类：**
- `AttributeSet` — 属性集合，使用 Modifier 模式叠加属性值
- `StaticAttributeCalculator` — 静态属性计算器
- `AttributeSetExtensions` — 扩展方法（InitFromConfig、ApplyMainAttribute 等）

**属性计算流程（SetSoldierData）：**
1. `InitFromConfig(cardId, lv)` → CalcBaseAttributes（基础配置 + 等级属性 + 主属性转换）
2. 天赋属性（ApplyMainAttribute + ApplyAttributeSource，来源 Talent）
3. 装备属性（ApplyMainAttribute + ApplyAttributeSource，来源 Equip）
4. 皮肤属性（ApplyAttributeSource，来源 Skin）

**获取最终值：** `attributeSet.GetFinal(AttributeType.XXX)` 返回 float

**属性名多语言：**
```csharp
var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(attrType);
var name = LocalizationManager.Instance.GetText(LocalizationModule.UITexts, attrCfg.Name);
```

**英灵核心属性类型（15个）：**
SoldierMaxHealth, SoldierAttackPower, SoldierSpellPower, SoldierAttackSpeed, SoldierPhysicalResistance, SoldierMagicResistance, SoldierCriticalRate, SoldierCriticalDamage, SoldierArmorPenetration, SoldierMagicPenetration, SoldierArmorPenetrationPer, SoldierMagicPenetrationPer, SoldierMissNum, SoldierAttackRange, SoldierAttackInterval

## Important Notes

### Code Generation
- Many `.Designer.cs` files are auto-generated - do not manually edit
- Luban generates config code from Excel files
- QFramework generates UI designer code

### Localization
- Multi-language support through `LocalizationManager`
- JSON-based language files in `Assets/Scripts/Localization/`
- Use localization keys for all user-facing text

### Performance Considerations
- Object pooling is heavily used (UI, game objects, effects)
- Asset bundles are loaded/unloaded dynamically
- Sprite atlases minimize draw calls
- Hot-fix allows quick iterations without full rebuilds

### Character Constants
Key soldier card IDs are defined in `Global.cs`:
- Spearmen: 2010001
- Archer: 2010002
- Magician: 2010003
- Assassin: 2010004


## Subagent 使用策略

### Explore 任务
- 使用 Haiku 模型 (model: "haiku")
- 将探索任务拆分成小块并行执行
- 适用于：查找文件、搜索关键词、理解代码结构、定位代码片段

### Plan 任务
- 使用 Sonnet 模型 (model: "sonnet")
- 适用于：架构设计、重构方案、复杂功能规划、风险评估
- 规划完成后需等待用户确认再执行

