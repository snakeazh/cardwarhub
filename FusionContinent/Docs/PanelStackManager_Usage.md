# 界面栈管理系统使用文档

## 概述

`PanelStackManager` 统一管理所有界面栈，包括：
- **全屏界面栈** - Common 层级的全屏界面，支持自动隐藏/恢复
- **弹窗栈** - PopUI 层级的弹窗
- **引导栈** - Guide 层级的引导界面

## 核心特性

1. **自动管理全屏界面** - 打开新全屏界面时自动隐藏上一个，关闭时自动恢复
2. **层级独立** - 弹窗、引导、全屏界面分别管理
3. **自动设置 IsFullScreen** - 根据层级自动标记全屏界面
4. **返回键支持** - 统一处理返回逻辑

## 自动设置 IsFullScreen

使用 `UIManager.OpenPanel` 打开界面时，根据层级自动设置：

```csharp
// Common 层级 → 自动设置 IsFullScreen = true，进入全屏栈管理
UIManager.OpenPanel<SoldierPanelNew, SoldierPanelViewModel>(vm)
    .SetLevel(UILevel.Common)
    .Open();

// PopUI 层级 → 自动设置 IsFullScreen = true，进入弹窗栈
UIManager.OpenPanel<ItemDetailPanelNew, ItemDetailViewModel>(vm)
    .SetLevel(UILevel.PopUI)
    .Open();

// Guide 层级 → 不自动设置，保持原值
UIManager.OpenPanel<TutorialPanelNew, TutorialViewModel>(vm)
    .SetLevel(UILevel.Guide)
    .Open();
```

## 全屏界面栈行为

### 打开全屏界面

```csharp
// 当前显示: [主界面]

UIManager.OpenPanel<SoldierPanelNew, SoldierPanelViewModel>(vm)
    .SetLevel(UILevel.Common)
    .Open();

// 结果:
// 显示: [士兵界面]
// 全屏栈: [主界面(Hidden)] ← 自动隐藏并入栈
```

### 关闭全屏界面

```csharp
// 当前显示: [士兵界面]
// 全屏栈: [主界面(Hidden)]

soldierPanel.CloseSelf();

// 结果:
// 显示: [主界面] ← 自动恢复
// 全屏栈: []
```

### 多层全屏界面

```csharp
// 1. 打开主界面
UIManager.OpenPanel<MainInterfacePanelNew>(mainVm).Open();
// 显示: [主界面]
// 全屏栈: []

// 2. 打开商店界面
UIManager.OpenPanel<ShopPanelNew>(shopVm).Open();
// 显示: [商店界面]
// 全屏栈: [主界面(Hidden)]

// 3. 打开士兵界面
UIManager.OpenPanel<SoldierPanelNew>(soldierVm).Open();
// 显示: [士兵界面]
// 全屏栈: [商店界面(Hidden), 主界面(Hidden)]

// 4. 关闭士兵界面
soldierPanel.CloseSelf();
// 显示: [商店界面] ← 恢复
// 全屏栈: [主界面(Hidden)]

// 5. 关闭商店界面
shopPanel.CloseSelf();
// 显示: [主界面] ← 恢复
// 全屏栈: []
```

## 弹窗栈行为

弹窗不影响全屏界面栈：

```csharp
// 当前显示: [士兵界面]
// 全屏栈: [商店界面(Hidden), 主界面(Hidden)]

// 打开弹窗
UIManager.OpenPanel<SkillUpGradePanelNew>(skillVm)
    .SetLevel(UILevel.PopUI)
    .Open();

// 显示: [士兵界面] + [技能升级弹窗]
// 全屏栈: [商店界面(Hidden), 主界面(Hidden)] ← 不变！
// 弹窗栈: [技能升级弹窗]

// 关闭弹窗
skillUpGradePanel.CloseSelf();

// 显示: [士兵界面]
// 全屏栈: [商店界面(Hidden), 主界面(Hidden)] ← 仍然不变
// 弹窗栈: []
```

## 手动控制

### 返回上一级

```csharp
// 优先关闭弹窗，再关闭全屏界面
bool success = PanelStackManager.Back();
```

**返回优先级：**
1. 弹窗栈 (PopUI)
2. 引导栈 (Guide)
3. 全屏界面栈 (Common)

### 返回到指定界面

```csharp
// 关闭当前界面和栈中其上方的所有界面
PanelStackManager.BackTo<MainInterfacePanelNew>();
```

### 清空栈

```csharp
// 清空指定层级的栈
PanelStackManager.ClearStack(UILevel.PopUI);

// 清空所有栈
PanelStackManager.ClearAllStacks();
```

### 查询状态

```csharp
// 获取当前显示的全屏界面
var currentType = PanelStackManager.GetCurrentFullScreenPanelType();

// 获取栈深度
int fullScreenDepth = PanelStackManager.GetStackDepth(UILevel.Common);
int popupDepth = PanelStackManager.GetStackDepth(UILevel.PopUI);

// 判断是否允许返回
bool canBack = PanelStackManager.CanBack();

// 判断界面是否在栈中
bool isInStack = PanelStackManager.Contains<ShopPanelNew>();

// 打印栈信息
Debug.Log(PanelStackManager.GetStackInfo());
```

## 返回键处理

```csharp
public class GameInputManager : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            HandleBackButton();
        }
    }
    
    private void HandleBackButton()
    {
        if (!PanelStackManager.CanBack())
        {
            ShowExitGameDialog();
            return;
        }
        
        bool success = PanelStackManager.Back();
        if (!success)
        {
            Debug.Log("返回操作失败");
        }
    }
}
```

## 事件监听

```csharp
// 订阅栈变化事件
PanelStackManager.OnStackChanged += OnStackChanged;

private void OnStackChanged(PanelStackNode node, StackAction action)
{
    switch (action)
    {
        case StackAction.Push:
            Debug.Log($"界面入栈: {node.PanelType.Name}");
            break;
        case StackAction.Pop:
            Debug.Log($"界面出栈: {node.PanelType.Name}");
            break;
        case StackAction.Back:
            Debug.Log($"返回操作: {node.PanelType.Name}");
            break;
        case StackAction.Restore:
            Debug.Log($"恢复界面: {node.PanelType.Name}");
            break;
        case StackAction.BackTo:
            Debug.Log($"返回到: {node.PanelType.Name}");
            break;
        case StackAction.Clear:
            Debug.Log($"清空栈: {node.PanelType.Name}");
            break;
    }
}
```

## 调试信息

```csharp
// 打印当前栈状态
[ContextMenu("Print Stack Info")]
private void PrintStackInfo()
{
    Debug.Log(PanelStackManager.GetStackInfo());
}
```

输出示例：
```
=== Panel Stack Info ===
FullScreen Stack (2):
  - ShopPanelNew [Hidden]
  - MainInterfacePanelNew [Hidden]
  Current: SoldierPanelNew
Popup Stack (1):
  - SkillUpGradePanelNew
Guide Stack (0):
```

## 注意事项

1. **层级选择**
   - Common：全屏界面，会自动入栈管理
   - PopUI：弹窗，不影响全屏界面
   - Guide：引导界面，独立管理

2. **关闭界面**
   - 使用 `panel.CloseSelf()` 或 `UIManager.ClosePanel<T>()`
   - 不要直接销毁 GameObject，否则栈状态会不一致

3. **内存管理**
   - 隐藏的界面只是 Hide，并未销毁
   - 注意控制栈深度，避免内存占用过大

4. **返回逻辑**
   - 弹窗优先于全屏界面关闭
   - 全屏界面关闭时自动恢复上一个

## 架构图

```
打开界面 (UIManager.OpenPanel)
    │
    ├── Level = Common + IsFullScreen = true
    │       │
    │       ├── 隐藏当前全屏界面
    │       ├── 压入全屏栈
    │       └── 显示新界面
    │
    ├── Level = PopUI
    │       │
    │       └── 压入弹窗栈
    │
    └── Level = Guide
            │
            └── 压入引导栈

关闭界面 (panel.Close)
    │
    ├── 全屏界面
    │       │
    │       ├── 关闭当前界面
    │       ├── 从全屏栈弹出
    │       └── 恢复上一个界面
    │
    ├── 弹窗
    │       │
    │       └── 从弹窗栈弹出
    │
    └── 引导界面
            │
            └── 从引导栈弹出
```
