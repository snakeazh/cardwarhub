# UIRoot 配置指南

通过 Inspector 拖拽配置 UI 层级结构。

---

## 配置步骤

### 1. 创建 UIRoot 物体

在场景中创建空物体，命名为 **"UIRoot"**。

### 2. 挂载 UIRootConfig 脚本

在 UIRoot 物体上挂载 `UIRootConfig` 脚本。

### 3. 创建层级节点

在 UIRoot 下创建子物体作为各个层级：

```
UIRoot (挂载 UIRootConfig)
├── Bg (Canvas)
├── Common (Canvas)
├── PopUI (Canvas)
├── Guide (Canvas)
├── Const (Canvas)
└── Loading (Canvas)
```

### 4. 配置 UIRootConfig

选中 UIRoot，在 Inspector 中配置：

**LevelConfigs 列表：**
- 点击 "+" 添加配置项
- 设置 Level 枚举（会自动作为 Sorting Order）
- 拖拽对应的层级 Transform
- 勾选 IsActive

示例配置：

| Level | Root Transform | IsActive |
|-------|---------------|----------|
| Bg | Bg | ✓ |
| Common | Common | ✓ |
| PopUI | PopUI | ✓ |
| Guide | Guide | ✓ |
| Const | Const | ✓ |
| Loading | Loading | ✓ |

### 5. 层级排序值

每个层级的 Sorting Order 自动使用枚举值：

```csharp
public enum UILevel
{
    Bg = -100,          // Sorting Order: -100
    Common = 0,         // Sorting Order: 0
    PopUI = 100,        // Sorting Order: 100
    Guide = 200,        // Sorting Order: 200
    Const = 300,        // Sorting Order: 300
    Loading = 1000,     // Sorting Order: 1000
}
```

---

## 全局设置

在 UIRootConfig 中还可以设置：

- **Render Mode**: Canvas 渲染模式（默认 ScreenSpaceOverlay）
- **Adaptive Screen**: 是否自适应屏幕
- **Reference Resolution**: 参考分辨率（默认 1920x1080）

---

## 运行流程

```
游戏启动
    ↓
UIManager.Initialize()
    ↓
查找场景中的 "UIRoot"
    ↓
读取 UIRootConfig.LevelConfigs
    ↓
验证并自动设置 Sorting Order
    ↓
层级准备完成，可以打开界面
```

---

## 注意事项

1. **必须配置**: 如果没有 UIRoot 或 UIRootConfig，UIManager 会报错
2. **Sorting Order 自动同步**: 修改 UILevel 枚举后，运行时会自动同步到 Canvas
3. **动态层级**: 未配置的层级无法使用，不会自动创建
4. **Transform 必须设置**: 每个层级的 RootTransform 都必须拖拽赋值

---

## 示例场景结构

```
Scene: Main
├── Main Camera
├── Directional Light
└── UIRoot [UIRootConfig]
    ├── Bg [Canvas (Sorting Order: -100)]
    │   └── BackgroundImage
    ├── Common [Canvas (Sorting Order: 0)]
    │   └── MainPanel
    ├── PopUI [Canvas (Sorting Order: 100)]
    │   └── ShopPanel (初始隐藏)
    ├── Guide [Canvas (Sorting Order: 200)]
    │   └── TutorialMask (初始隐藏)
    ├── Const [Canvas (Sorting Order: 300)]
    │   └── HUDPanel
    └── Loading [Canvas (Sorting Order: 1000)]
        └── LoadingPanel (初始隐藏)
```

这样配置后，UIManager 会自动识别并使用这些层级打开界面！
