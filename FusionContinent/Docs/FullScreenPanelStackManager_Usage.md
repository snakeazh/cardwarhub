# 全屏界面栈管理使用文档

## 概述

`FullScreenPanelStackManager` 专门管理全屏界面的入栈出栈操作。

**核心概念：**
- **全屏界面** (`IsFullScreen = true`): 如主界面、士兵界面、商店界面等
- **非全屏界面** (`IsFullScreen = false`): 如弹窗、提示等

**行为规则：**
1. 全屏界面打开另一个全屏界面时，当前界面自动隐藏并入栈
2. 关闭全屏界面时，自动恢复栈中的上一个全屏界面
3. 非全屏界面不影响全屏界面栈

## 自动设置 IsFullScreen

**重要：** 使用 `UIManager.OpenPanel` 打开界面时，会根据层级**自动设置** `IsFullScreen`：

- **Common 层级** → 自动设置 `IsFullScreen = true`
- **PopUI 层级** → 自动设置 `IsFullScreen = true`
- **Guide 层级** → 保持原值（通常是 `false`）
- **其他层级** → 保持原值

```csharp
// 示例：
UIManager.OpenPanel<SoldierPanelNew, SoldierPanelViewModel>(vm)
    .SetLevel(UILevel.Common)  // 自动设置 IsFullScreen = true
    .Open();

UIManager.OpenPanel<ItemDetailPanelNew, ItemDetailViewModel>(vm)
    .SetLevel(UILevel.PopUI)   // 自动设置 IsFullScreen = true
    .Open();

UIManager.OpenPanel<TutorialPanelNew, TutorialViewModel>(vm)
    .SetLevel(UILevel.Guide)    // 不自动设置，保持原值
    .Open();
```

**注意：** 只有 **Common** 层级的全屏界面会进入 `FullScreenPanelStackManager` 的栈管理。PopUI 层级的全屏界面虽然设置了 `IsFullScreen = true`，但只进入 `PanelStackManager` 的弹窗栈。

## 手动配置（可选）

如果需要覆盖自动设置，可以在 ViewModel 中手动设置：

```csharp
public class MainInterfaceViewModel : ViewModel
{
    // 手动设置为非全屏（覆盖自动设置）
    public override bool IsFullScreen { get; set; } = false;
}
```

**注意：** 手动设置会在 `OnCreate` 之后被覆盖，如果需要强制覆盖，可以在 `OnCreate` 中设置：

```csharp
public class MainInterfaceViewModel : ViewModel
{
    public override void OnCreate()
    {
        base.OnCreate();
        // 强制设置为非全屏（在 UIManager 自动设置之后）
        // 但这需要在打开界面后设置，不太实用
        // 建议通过层级来控制
    }
}
```

## 使用方式

### 1. 打开全屏界面

```csharp
// 方式1：使用 FullScreenPanelStackManager（推荐）
var vm = new SoldierPanelViewModel();
vm.OnCreate();
FullScreenPanelStackManager.OpenFullScreenPanel<SoldierPanelNew, SoldierPanelViewModel>(vm);

// 方式2：使用 UIManager.OpenPanel（需要配合自动检测）
// 如果 ViewModel.IsFullScreen = true，会自动进行入栈操作
UIManager.OpenPanel<SoldierPanelNew, SoldierPanelViewModel>(vm).Open();
```

**效果：**
```
操作前:
显示: [主界面]
栈: []

操作后:
显示: [士兵界面]
栈: [主界面] ← 自动隐藏并入栈
```

### 2. 关闭全屏界面

```csharp
// 方式1：使用 FullScreenPanelStackManager（推荐）
FullScreenPanelStackManager.CloseCurrentFullScreenPanel();

// 方式2：使用 panel.CloseSelf()
this.CloseSelf();
```

**效果：**
```
操作前:
显示: [士兵界面]
栈: [主界面]

操作后:
显示: [主界面] ← 自动恢复
栈: []
```

### 3. 返回到指定界面

```csharp
// 从士兵界面返回到主界面
// 会自动关闭士兵界面并恢复主界面
FullScreenPanelStackManager.BackTo<MainInterfacePanelNew>();
```

**效果：**
```
操作前:
显示: [士兵界面]
栈: [商店界面, 主界面]

操作后:
显示: [主界面] ← 直接返回到主界面
栈: [] ← 中间的商店界面被弹出
```

## 完整示例

### 场景：主界面 → 商店 → 士兵 → 返回

```csharp
// 1. 打开主界面（游戏启动时）
var mainVm = new MainInterfaceViewModel();
mainVm.OnCreate();
FullScreenPanelStackManager.OpenFullScreenPanel<MainInterfacePanelNew, MainInterfaceViewModel>(mainVm);
// 显示: [主界面]
// 栈: []

// 2. 点击商店按钮，打开商店界面
var shopVm = new ShopPanelViewModel();
shopVm.OnCreate();
FullScreenPanelStackManager.OpenFullScreenPanel<ShopPanelNew, ShopPanelViewModel>(shopVm);
// 显示: [商店界面]
// 栈: [主界面] ← 主界面自动隐藏

// 3. 点击士兵按钮，打开士兵界面
var soldierVm = new SoldierPanelViewModel();
soldierVm.OnCreate();
FullScreenPanelStackManager.OpenFullScreenPanel<SoldierPanelNew, SoldierPanelViewModel>(soldierVm);
// 显示: [士兵界面]
// 栈: [商店界面, 主界面] ← 商店界面自动入栈

// 4. 点击返回按钮，关闭士兵界面
FullScreenPanelStackManager.CloseCurrentFullScreenPanel();
// 显示: [商店界面] ← 自动恢复
// 栈: [主界面]

// 5. 再次点击返回，关闭商店界面
FullScreenPanelStackManager.CloseCurrentFullScreenPanel();
// 显示: [主界面] ← 自动恢复
// 栈: []
```

### 场景：弹窗不影响全屏界面栈

```csharp
// 当前显示士兵界面
// 栈: [商店界面, 主界面]

// 打开技能升级弹窗（非全屏界面）
var skillVm = new SkillUpGradeViewModel();
UIManager.OpenPanel<SkillUpGradePanelNew, SkillUpGradeViewModel>(skillVm)
    .SetLevel(UILevel.PopUI)
    .Open();

// 显示: [士兵界面] + [技能升级弹窗]
// 栈: [商店界面, 主界面] ← 不变！

// 关闭弹窗
skillUpGradePanel.CloseSelf();

// 显示: [士兵界面]
// 栈: [商店界面, 主界面] ← 仍然不变
```

## 手动控制

### 清空栈

```csharp
// 清空全屏界面栈，关闭所有界面
FullScreenPanelStackManager.ClearStack();
```

### 查询状态

```csharp
// 获取当前显示的全屏界面
var currentType = FullScreenPanelStackManager.GetCurrentFullScreenPanelType();
Debug.Log($"当前界面: {currentType?.Name}");

// 获取栈深度
int depth = FullScreenPanelStackManager.GetStackDepth();
Debug.Log($"栈深度: {depth}");

// 判断是否允许返回
bool canBack = FullScreenPanelStackManager.CanBack();

// 打印栈信息
Debug.Log(FullScreenPanelStackManager.GetStackInfo());
```

### 启用/禁用

```csharp
// 禁用全屏界面栈管理（调试用）
FullScreenPanelStackManager.EnableStackManagement = false;

// 启用（默认）
FullScreenPanelStackManager.EnableStackManagement = true;
```

## 事件监听

```csharp
// 订阅栈变化事件
FullScreenPanelStackManager.OnStackChanged += OnStackChanged;

private void OnStackChanged(FullScreenPanelNode node, FullScreenStackAction action)
{
    switch (action)
    {
        case FullScreenStackAction.Open:
            Debug.Log($"打开全屏界面: {node.PanelType.Name}");
            break;
        case FullScreenStackAction.Restore:
            Debug.Log($"恢复全屏界面: {node.PanelType.Name}");
            break;
        case FullScreenStackAction.BackTo:
            Debug.Log($"返回到界面: {node.PanelType.Name}");
            break;
        case FullScreenStackAction.Clear:
            Debug.Log($"清空栈: {node.PanelType.Name}");
            break;
    }
}
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
        // 优先处理弹窗层
        if (PanelStackManager.GetStackDepth(UILevel.PopUI) > 0)
        {
            PanelStackManager.Back();
            return;
        }
        
        // 处理全屏界面栈
        if (FullScreenPanelStackManager.CanBack())
        {
            FullScreenPanelStackManager.CloseCurrentFullScreenPanel();
        }
        else
        {
            // 已经是最后一个界面，提示退出游戏
            ShowExitGameDialog();
        }
    }
}
```

## 注意事项

1. **ViewModel 配置**: 必须在 ViewModel 中正确设置 `IsFullScreen` 属性
2. **生命周期**: 使用 `FullScreenPanelStackManager` 打开的全屏界面，关闭时会自动恢复上一个界面
3. **弹窗处理**: 弹窗使用 `UIManager.OpenPanel` 直接打开，不走全屏栈管理
4. **数据恢复**: 界面恢复时会自动恢复之前的 ViewModel 状态
5. **内存管理**: 隐藏的界面只是隐藏（Hide），并未销毁，注意内存占用

## 调试技巧

```csharp
// 在控制台打印当前栈状态
[ContextMenu("Print FullScreen Stack")]
private void PrintFullScreenStack()
{
    Debug.Log(FullScreenPanelStackManager.GetStackInfo());
}
```

输出示例：
```
=== FullScreen Panel Stack Info ===
当前显示: SoldierPanelNew
栈深度: 2
  [0] ShopPanelNew
  [1] MainInterfacePanelNew
```
