# UserTool 动态配置使用文档

## 概述

UserTool 现在支持根据当前激活的全屏界面动态切换显示内容和配置。

## 核心组件

### 1. UserToolData - 配置数据

```csharp
public class UserToolData
{
    public bool ShowUserInfo { get; set; }      // 显示用户信息
    public bool ShowGold { get; set; }          // 显示金币
    public bool ShowDiamond { get; set; }       // 显示钻石
    public bool ShowStamina { get; set; }       // 显示体力
    public bool ShowPointCoupon { get; set; }   // 显示点券
    public string CustomTitle { get; set; }     // 自定义标题
    public bool ShowBackButton { get; set; }    // 显示返回按钮
    public bool ShowCloseButton { get; set; }   // 显示关闭按钮
}
```

### 2. UserToolViewModel - 全屏界面基类

```csharp
public class MyPanelViewModel : UserToolViewModel
{
    // 重写配置
    public override UserToolData UserToolConfig => new UserToolData
    {
        ShowUserInfo = true,
        ShowGold = true,
        ShowDiamond = false,  // 隐藏钻石
        ShowStamina = false,  // 隐藏体力
        ShowPointCoupon = true
    };
    
    // 或返回静态配置
    public override UserToolData UserToolConfig => UserToolData.OnlyUserInfo;
}
```

### 3. 预设配置

```csharp
UserToolData.Default          // 显示所有
UserToolData.OnlyUserInfo     // 只显示用户信息
UserToolData.OnlyResources    // 只显示资源
UserToolData.Empty            // 不显示任何内容
```

## 使用示例

### 示例1：主界面（显示所有）

```csharp
public class MainInterfaceViewModel : UserToolViewModel
{
    public override UserToolData UserToolConfig => UserToolData.Default;
    
    public override void OnCreate()
    {
        base.OnCreate(); // 自动设置 UserTool 配置
        // ...
    }
}
```

### 示例2：士兵界面（只显示用户信息和金币）

```csharp
public class SoldierPanelViewModel : UserToolViewModel
{
    public override UserToolData UserToolConfig => new UserToolData
    {
        ShowUserInfo = true,
        ShowGold = true,
        ShowDiamond = false,
        ShowStamina = false,
        ShowPointCoupon = false
    };
}
```

### 示例3：商店界面（只显示资源，不显示用户信息）

```csharp
public class ShopPanelViewModel : UserToolViewModel
{
    public override UserToolData UserToolConfig => UserToolData.OnlyResources;
}
```

### 示例4：带返回按钮的界面

```csharp
public class DetailPanelViewModel : UserToolViewModel
{
    public override UserToolData UserToolConfig => new UserToolData
    {
        ShowUserInfo = true,
        ShowGold = true,
        ShowDiamond = true,
        ShowBackButton = true,  // 显示返回按钮
        OnBackButtonClick = () =>
        {
            // 自定义返回逻辑
            PanelStackManager.Back();
        }
    };
}
```

## 完整使用流程

### 1. 修改全屏界面 ViewModel

```csharp
// 修改前
public class MyPanelViewModel : ViewModel
{
    public override void OnCreate()
    {
        base.OnCreate();
        // ...
    }
}

// 修改后
public class MyPanelViewModel : UserToolViewModel  // 继承 UserToolViewModel
{
    public override UserToolData UserToolConfig => UserToolData.Default;
    
    public override void OnCreate()
    {
        base.OnCreate(); // 必须调用，会自动设置 UserTool 配置
        // ...
    }
}
```

### 2. 打开全屏界面

```csharp
var vm = new MyPanelViewModel();
vm.OnCreate(); // 自动配置 UserTool
UIManager.OpenPanel<MyPanelNew, MyPanelViewModel>(vm)
    .SetLevel(UILevel.Common)
    .Open();

// UserToolPanel 会自动显示 MyPanelViewModel.UserToolConfig 的配置
```

### 3. 切换界面自动更新

```csharp
// 当前显示: MainInterfaceViewModel (Default配置)
// UserTool 显示: 用户信息 + 所有资源

// 打开士兵界面
var soldierVm = new SoldierPanelViewModel();
soldierVm.OnCreate(); // 配置: 用户信息 + 金币
UIManager.OpenPanel<SoldierPanelNew, SoldierPanelViewModel>(soldierVm)
    .SetLevel(UILevel.Common)
    .Open();

// UserTool 自动更新: 隐藏钻石、体力、点券，只显示用户信息和金币

// 关闭士兵界面返回主界面
soldierPanel.CloseSelf();

// UserTool 自动恢复: 显示所有内容
```

## 手动控制 UserTool

### 全局设置

```csharp
// 设置当前界面的 UserTool 配置
UserToolManager.SetUserToolData("MyPanel", new UserToolData
{
    ShowUserInfo = true,
    ShowGold = true
});

// 重置为默认
UserToolManager.ResetToDefault();

// 隐藏 UserTool
UserToolManager.HideUserTool();
```

### 订阅配置变化

```csharp
UserToolManager.CurrentUserToolData.OnValueChanged += (oldVal, newVal) =>
{
    Debug.Log($"UserTool 配置变化: {newVal.ShowUserInfo}");
};
```

## 注意事项

1. **必须继承 UserToolViewModel** - 全屏界面需要显示 UserTool 的话
2. **调用 base.OnCreate()** - 确保自动配置生效
3. **ViewModel 生命周期** - OnCreate 时设置，OnDestroy 时可选重置
4. **动态修改** - 可以在运行时通过 UserToolManager 动态修改配置

## 进阶用法

### 根据状态动态配置

```csharp
public class BattlePanelViewModel : UserToolViewModel
{
    private bool mIsInBattle = false;
    
    public override UserToolData UserToolConfig => mIsInBattle 
        ? UserToolData.Empty  // 战斗中隐藏所有
        : UserToolData.OnlyResources;  // 非战斗显示资源
    
    public void EnterBattle()
    {
        mIsInBattle = true;
        // 触发配置更新
        UserToolManager.SetUserToolData(PanelName, UserToolConfig);
    }
}
```

### 带自定义标题

```csharp
public class ShopPanelViewModel : UserToolViewModel
{
    public override UserToolData UserToolConfig => new UserToolData
    {
        CustomTitle = "商店",
        ShowUserInfo = false,  // 隐藏默认用户信息
        ShowGold = true,
        ShowDiamond = true
    };
}
```
