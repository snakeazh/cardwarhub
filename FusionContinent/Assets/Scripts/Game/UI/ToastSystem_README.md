# Toast提示系统 使用说明

## 文件结构
```
Assets/
├── Scripts/
│   ├── Game/UI/
│   │   ├── ViewModels/
│   │   │   └── ToastViewModel.cs      # Toast数据模型
│   │   └── Services/
│   │       └── ToastService.cs        # Toast业务逻辑
│   └── UI/
│       └── ToastPanel.cs              # ToastUI面板
```

## 初始化配置

### 1. 注册ToastService
在 GameMainController 或游戏入口中：

```csharp
public class GameMainController : MonoBehaviour
{
    async void Start()
    {
        // 初始化UI框架
        UIManager.Initialize();
        
        // 初始化Service系统
        await GameServiceManager.InitializeAsync();
        
        // 注册ToastService
        GameServiceManager.Register(new ToastService());
        
        // 打开ToastPanel（常驻）
        OpenToastPanel();
    }
    
    void OpenToastPanel()
    {
        var vm = new ToastViewModel();
        vm.OnCreate();
        
        UIManager.OpenPanel<ToastPanel, ToastViewModel>(vm)
            .SetLevel(UILevel.Const)  // 使用Const层级，保持最上层
            .Open();
    }
}
```

### 2. 预制体配置
1. 创建 ToastPanel 预制体
2. 添加 ToastPanel 脚本
3. 设置 ToastPrefab（提示项预制体）
4. 设置 ToastContainer（容器，可为空，默认使用Panel本身）

## 使用方式

### 方式1：通过ToastService（推荐）

```csharp
// 获取ToastService
var toastService = GameServiceManager.Get<ToastService>();

// 显示普通提示
toastService.ShowToast("操作成功！");

// 显示指定时长的提示
toastService.ShowToast("保存成功", 2.0f);

// 立即显示（清空队列）
toastService.ShowToastImmediate("重要提示！", 3.0f);

// 关闭当前提示
toastService.CloseCurrentToast();

// 清空所有待显示提示
toastService.ClearAllToasts();
```

### 方式2：在ViewModel中使用

```csharp
public class MyViewModel : ViewModel
{
    private ToastService mToastService;
    
    public override void OnCreate()
    {
        mToastService = GameServiceManager.Get<ToastService>();
    }
    
    private void OnConfirm()
    {
        // 执行业务逻辑
        DoSomething();
        
        // 显示提示
        mToastService?.ShowToast("操作成功！");
    }
}
```

### 方式3：在Panel中使用

```csharp
public class MyPanelNew : UIPanel<MyViewModel>
{
    private void OnButtonClick()
    {
        // 直接获取Service显示提示
        GameServiceManager.Get<ToastService>()?.ShowToast("按钮被点击！");
    }
}
```

## 特性

### 1. 队列管理
- 自动管理多个Toast提示，按顺序显示
- 防止重复消息同时显示

### 2. 动画效果
- 淡入淡出效果
- 上移动画
- 使用 DOTween 实现

### 3. 生命周期
- 常驻面板（UILevel.Const）
- 不会被其他界面关闭
- 切换场景时保持

### 4. 事件系统
```csharp
var toastService = GameServiceManager.Get<ToastService>();

// 监听显示事件
toastService.OnShowToast += (message, duration) =>
{
    Debug.Log($"显示Toast: {message}, 时长: {duration}s");
};

// 监听关闭事件
toastService.OnToastClosed += () =>
{
    Debug.Log("Toast已关闭");
};
```

## 与旧ShowTipManager对比

| 特性 | ShowTipManager (旧) | ToastSystem (新) |
|------|---------------------|------------------|
| 架构 | 挂载在Panel上 | 独立的Service+Panel |
| 全局访问 | 需要查找Panel | 通过Service直接访问 |
| 队列管理 | 不支持 | 支持 |
| 重复消息 | 可能重复显示 | 自动过滤 |
| 生命周期 | 随Panel销毁 | 常驻，全局可用 |
| MVVM支持 | 不支持 | 完整支持 |

## 迁移建议

### 替换旧代码
**旧代码：**
```csharp
// 在Panel中获取ShowTipManager
GetComponent<ShowTipManager>()?.SetTiptxt("提示信息", 2f);

// 或
UIKit.GetPanel<GameMainPanel>()?.ShowTip("提示信息");
```

**新代码：**
```csharp
// 通过Service显示
GameServiceManager.Get<ToastService>()?.ShowToast("提示信息", 2f);

// 或在ViewModel中
mToastService?.ShowToast("提示信息");
```

### 批量替换正则
```csharp
// 查找
GetComponent<ShowTipManager>\(\)\?\.SetTiptxt\(([^,]+),\s*([^)]+)\)

// 替换
GameServiceManager.Get<ToastService>()?.ShowToast($1, $2)
```

## 注意事项

1. **必须先初始化**：在使用前确保 ToastService 已注册
2. **层级设置**：使用 UILevel.Const 保持Toast在最上层
3. **预制体设置**：确保 ToastPrefab 已正确配置
4. **内存管理**：Toast自动销毁，无需手动管理

## 扩展功能

### 自定义Toast样式
继承 ToastPanel，重写 CreateToast 方法：

```csharp
public class CustomToastPanel : ToastPanel
{
    protected override void CreateToast(string message, float duration)
    {
        // 自定义创建逻辑
        base.CreateToast(message, duration);
        
        // 添加自定义效果
        mCurrentToast?.GetComponent<Image>()?.DOColor(Color.red, 0.5f);
    }
}
```

### 添加Toast类型
```csharp
public enum ToastType
{
    Info,    // 普通信息
    Success, // 成功
    Warning, // 警告
    Error    // 错误
}

// 在ToastService中添加重载
public void ShowToast(string message, ToastType type, float duration = 0.5f)
{
    // 根据类型设置不同颜色/图标
}
```
