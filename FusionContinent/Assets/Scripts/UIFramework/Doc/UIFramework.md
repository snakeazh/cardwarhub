# UIFramework 使用文档

## 概述

UIFramework 是一个基于 MVVM 架构的 Unity UI 框架，支持数据绑定、命令模式、Service 架构，使用 UniTask 进行异步操作。

## 核心特性

- **MVVM 架构**：分离 UI 逻辑和业务逻辑
- **数据绑定**：自动同步数据和 UI
- **命令模式**：处理用户交互
- **Service 层**：管理全局业务逻辑
- **UniTask 支持**：零 GC 异步操作
- **YooAssets 集成**：资源加载
- **Inspector 配置**：可视化层级管理

## 快速开始

### 1. 场景配置

在场景中创建 UIRoot：

```
1. 创建空物体命名为 "UIRoot"
2. 挂载 UIRoot 脚本（单例）
3. 挂载 UIRootConfig 脚本
4. 在 UIRootConfig 中配置层级：
   - Bg (SortingOrder: -100)
   - Common (SortingOrder: 0)
   - PopUI (SortingOrder: 100)
   - Guide (SortingOrder: 200)
   - Const (SortingOrder: 300)
   - Loading (SortingOrder: 1000)
```

### 2. 游戏入口初始化

```csharp
using UIFramework;
using UIFramework.Services;

public class GameMainController : MonoBehaviour
{
    async void Start()
    {
        // 1. 设置路径提供器
        UIManager.PathProvider = new ReflectionPrefabPathProvider("UI/Prefabs");
        
        // 2. 初始化 UI 管理器
        UIManager.Initialize();
        
        // 3. 初始化 Service 系统
        await GameServiceManager.InitializeAsync();
        
        // 4. 注册 Service
        RegisterServices();
        
        // 5. 初始化所有 Service
        await GameServiceManager.InitializeAllAsync();
        
        // 6. 打开首个界面
        OpenLoginPanel();
    }
    
    void RegisterServices()
    {
        GameServiceManager.Register(new LoginService());
        GameServiceManager.Register(new PlayerService());
        GameServiceManager.Register(new ToastService());
    }
    
    void OpenLoginPanel()
    {
        var vm = new LoginPanelViewModel();
        vm.OnCreate();
        
        UIManager.OpenPanel<LoginPanel, LoginPanelViewModel>(vm)
            .SetLevel(UILevel.Common)
            .Open();
    }
}
```

## 核心概念

### 1. UIPanel（视图层）

```csharp
using UIFramework;
using Game.Login;
using UnityEngine;

namespace FusionContinent
{
    [PrefabPath("LoginPanel")]  // 指定预制体路径
    public class LoginPanel : UIPanel<LoginPanelViewModel>
    {
        // 获取原 Panel 的组件（复用模式）
        private LoginPanelOld mOldPanel;
        
        void Awake()
        {
            mOldPanel = GetComponent<LoginPanelOld>();
        }
        
        protected override void OnBindViewModel(LoginPanelViewModel vm)
        {
            // 数据绑定
            vm.Account.OnValueChanged += (oldVal, newVal) =>
            {
                mOldPanel.AccountInput.text = newVal;
            };
            
            // 按钮绑定
            mOldPanel.LoginBtn.onClick.AddListener(() =>
            {
                vm.OnLoginCommand.Execute();
            });
        }
        
        protected override void OnUnbindViewModel(LoginPanelViewModel vm)
        {
            // 清理监听
            mOldPanel.LoginBtn.onClick.RemoveAllListeners();
        }
    }
}
```

### 2. ViewModel（视图模型层）

```csharp
using UIFramework;
using Game.Login;
using UnityEngine;

namespace Game.Login
{
    public class LoginPanelViewModel : ViewModel
    {
        // 属性（自动绑定）
        public BindableProperty<string> Account { get; } = new BindableProperty<string>("");
        public BindableProperty<string> Password { get; } = new BindableProperty<string>("");
        public BindableProperty<bool> IsLoading { get; } = new BindableProperty<bool>(false);
        
        // 命令
        public Command OnLoginCommand { get; private set; }
        
        // Service 依赖
        private LoginService mLoginService;
        
        public override void OnCreate()
        {
            // 获取 Service
            mLoginService = GameServiceManager.Get<LoginService>();
            
            // 初始化命令
            OnLoginCommand = new Command(OnLogin, () => !IsLoading.Value);
            
            // 加载数据
            LoadData();
        }
        
        private void LoadData()
        {
            Account.Value = PlayerPrefs.GetString("Account", "");
            Password.Value = PlayerPrefs.GetString("Password", "");
        }
        
        private async void OnLogin()
        {
            IsLoading.Value = true;
            
            var result = await mLoginService.LoginAsync(Account.Value, Password.Value);
            
            if (result.Success)
            {
                OnLoginSuccess?.Invoke(result.UserData);
            }
            else
            {
                // 显示错误提示
            }
            
            IsLoading.Value = false;
        }
        
        // 事件
        public System.Action<UserData> OnLoginSuccess;
    }
}
```

### 3. Service（服务层）

```csharp
using UIFramework.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Login
{
    public class LoginService : GameServiceBase
    {
        public UserData CurrentUser { get; private set; }
        
        public override async UniTask InitializeAsync()
        {
            // 初始化逻辑
            await base.InitializeAsync();
        }
        
        public async UniTask<LoginResult> LoginAsync(string account, string password)
        {
            // 网络请求
            var response = await WebDataFun.SendPostRequest(ApiKeys.ApiLogin, 
                new Dictionary<string, string>
                {
                    { "Account", account },
                    { "Password", password }
                });
            
            if (!string.IsNullOrEmpty(response))
            {
                CurrentUser = JsonConvert.DeserializeObject<UserData>(response);
                return new LoginResult { Success = true, UserData = CurrentUser };
            }
            
            return new LoginResult { Success = false, ErrorMessage = "登录失败" };
        }
    }
}
```

## 数据绑定

### BindableProperty<T>

```csharp
// 定义属性
public BindableProperty<string> UserName { get; } = new BindableProperty<string>("");
public BindableProperty<int> Level { get; } = new BindableProperty<int>(1);
public BindableProperty<bool> IsVip { get; } = new BindableProperty<bool>(false);

// 订阅变化（在 Panel 中）
protected override void OnBindViewModel(MyViewModel vm)
{
    // 简单赋值
    vm.UserName.OnValueChanged += (oldVal, newVal) =>
    {
        mTextComponent.text = newVal;
    };
    
    // 格式化显示
    vm.Level.OnValueChanged += (oldVal, newVal) =>
    {
        mLevelText.text = $"Lv.{newVal}";
    };
    
    // 条件显示
    vm.IsVip.OnValueChanged += (oldVal, newVal) =>
    {
        mVipIcon.SetActive(newVal);
    };
}
```

## 命令模式

### Command（无参数）

```csharp
// ViewModel 中定义
public Command OnConfirmCommand { get; private set; }

public override void OnCreate()
{
    // 创建命令（带 CanExecute 条件）
    OnConfirmCommand = new Command(OnConfirm, CanConfirm);
}

private void OnConfirm()
{
    // 执行逻辑
}

private bool CanConfirm()
{
    return !string.IsNullOrEmpty(UserName.Value);
}

// Panel 中绑定
mConfirmBtn.onClick.AddListener(() => vm.OnConfirmCommand.Execute());
```

### Command<T>（带参数）

```csharp
// ViewModel 中定义
public Command<int> OnSelectItemCommand { get; private set; }

public override void OnCreate()
{
    OnSelectItemCommand = new Command<int>(OnSelectItem);
}

private void OnSelectItem(int itemId)
{
    SelectedItemId.Value = itemId;
}

// Panel 中绑定
for (int i = 0; i < items.Count; i++)
{
    int index = i; // 重要：捕获变量
    itemButtons[i].onClick.AddListener(() => 
        vm.OnSelectItemCommand.Execute(index));
}
```

## 打开和关闭界面

### 打开界面

```csharp
// 方式1：简单打开
var vm = new MyViewModel();
vm.OnCreate();
UIManager.OpenPanel<MyPanel, MyViewModel>(vm)
    .SetLevel(UILevel.PopUI)
    .Open();

// 方式2：带完成回调
UIManager.OpenPanel<MyPanel, MyViewModel>(vm)
    .SetLevel(UILevel.PopUI)
    .SetData(v => v.UserId.Value = 123)
    .OnComplete(panel =>
    {
        Debug.Log($"面板打开完成: {panel.name}");
    })
    .Open();

// 方式3：异步打开
var panel = await UIManager.OpenPanel<MyPanel, MyViewModel>(vm)
    .SetLevel(UILevel.PopUI)
    .OpenAsync();
```

### 关闭界面

```csharp
// 方式1：通过类型关闭
UIManager.ClosePanel<MyPanel>();

// 方式2：通过实例关闭
UIManager.ClosePanel(panel);

// 方式3：在 Panel 内部关闭
CloseSelf();

// 方式4：关闭指定层级所有界面
UIManager.ClosePanelsByLevel(UILevel.PopUI);

// 方式5：关闭所有界面
UIManager.CloseAllPanels();
```

### 隐藏和显示界面

```csharp
// 隐藏界面（保留在内存中，不销毁）
UIManager.HidePanel<MyPanel>();

// 显示界面（从隐藏状态恢复）
UIManager.ShowPanel<MyPanel>();

// 切换显示/隐藏状态
UIManager.TogglePanel<MyPanel>();

// 通过实例操作
var panel = UIManager.GetPanel<MyPanel>();
UIManager.HidePanel(panel);
// ...
UIManager.ShowPanel(panel);
```

**Close 与 Hide 的区别：**

| 操作 | Hide | Close |
|------|------|-------|
| 游戏对象 | SetActive(false) | Destroy |
| 内存占用 | 保留 | 释放 |
| 恢复速度 | 快（直接显示） | 慢（重新加载） |
| 适用场景 | 频繁切换的界面 | 一次性界面 |

### 复用已隐藏的界面（GetOrOpenPanel）

对于需要频繁切换的界面（如商店、背包），使用 `GetOrOpenPanel` 可以复用已隐藏的界面，避免重复创建：

```csharp
// 方式1：如果界面已存在（即使被隐藏），则显示它；如果不存在，则创建新的
var shopVM = new ShopPanel01ViewModel();
shopVM.OnCreate();

UIManager.GetOrOpenPanel<ShopPanel01New, ShopPanel01ViewModel>(shopVM)
    .SetLevel(UILevel.Const)
    .OnComplete(panel => {
        // panel 可能是已存在的，也可能是新创建的
        if (panel.gameObject.activeSelf)
        {
            Debug.Log("显示了已存在的商店");
        }
        else
        {
            Debug.Log("创建了新的商店");
        }
    })
    .Open();

// 方式2：自动创建 ViewModel（简化版）
UIManager.GetOrOpenPanel<ShopPanel01New, ShopPanel01ViewModel>()
    .SetLevel(UILevel.Const)
    .Open();

// 方式3：先尝试显示已存在的，不存在则创建
if (!UIManager.TryShowExistingPanel<ShopPanel01New>())
{
    // 不存在，创建新的
    var vm = new ShopPanel01ViewModel();
    vm.OnCreate();
    UIManager.OpenPanel<ShopPanel01New, ShopPanel01ViewModel>(vm)
        .SetLevel(UILevel.Const)
        .Open();
}
```

**使用场景：**

- **使用 `GetOrOpenPanel`**：频繁切换的界面（商店、背包、角色面板）
- **使用 `OpenPanel`**：一次性界面（弹窗、提示、对话框）

### 获取已打开的界面

```csharp
// 获取已打开的界面
var panel = UIManager.GetPanel<MyPanel>();
if (panel != null)
{
    // 界面已打开
}

// 尝试获取
if (UIManager.TryGetPanel<MyPanel>(out var panel))
{
    // 使用 panel
}
```

## 界面层级

### UILevel 枚举

```csharp
public enum UILevel
{
    Bg = -100,          // 背景层
    Common = 0,         // 普通层
    PopUI = 100,        // 弹出层
    Guide = 200,        // 引导层
    Const = 300,        // 常驻层（不会被关闭）
    Loading = 1000,     // 加载层（最上层）
}
```

### 使用层级

```csharp
// 普通界面
UIManager.OpenPanel<MainPanel, MainViewModel>(vm)
    .SetLevel(UILevel.Common)
    .Open();

// 弹出界面（覆盖在普通界面上）
UIManager.OpenPanel<DialogPanel, DialogViewModel>(vm)
    .SetLevel(UILevel.PopUI)
    .Open();

// 引导界面（最高优先级）
UIManager.OpenPanel<GuidePanel, GuideViewModel>(vm)
    .SetLevel(UILevel.Guide)
    .Open();

// 加载界面（最上层，遮挡所有其他界面）
UIManager.OpenPanel<LoadingPanel, LoadingViewModel>(vm)
    .SetLevel(UILevel.Loading)
    .Open();
```

## 预制体路径配置

### 方式1：使用 PrefabPath 特性（推荐）

```csharp
[PrefabPath("UI/Login/LoginPanel")]
public class LoginPanel : UIPanel<LoginViewModel>
{
    // ...
}
```

### 方式2：使用 PathProvider

```csharp
// 在游戏启动时设置
UIManager.PathProvider = new ReflectionPrefabPathProvider("UI/Prefabs");

// 或使用默认提供器
UIManager.PathProvider = new DefaultPrefabPathProvider("UI/Prefabs");
```

## 生命周期

### Panel 生命周期

```csharp
public class MyPanel : UIPanel<MyViewModel>
{
    // 1. Awake - Unity 唤醒
    void Awake()
    {
        // 获取组件引用
    }
    
    // 2. OnBindViewModel - 绑定 ViewModel（重要！）
    protected override void OnBindViewModel(MyViewModel vm)
    {
        // 绑定数据和事件
    }
    
    // 3. OnOpen - 界面打开
    protected override void OnOpen(IUIData uiData = null)
    {
        // 每次打开时执行
    }
    
    // 4. OnShow - 界面显示
    protected override void OnShow()
    {
        // 从隐藏状态恢复时执行
    }
    
    // 5. OnHide - 界面隐藏
    protected override void OnHide()
    {
        // 界面隐藏时执行（未关闭）
    }
    
    // 6. OnUnbindViewModel - 解绑 ViewModel（重要！）
    protected override void OnUnbindViewModel(MyViewModel vm)
    {
        // 清理事件监听，防止内存泄漏
    }
    
    // 7. OnDestroy - Unity 销毁
    void OnDestroy()
    {
        // 清理资源
    }
}
```

### ViewModel 生命周期

```csharp
public class MyViewModel : ViewModel
{
    // 1. OnCreate - 创建时（重要！）
    public override void OnCreate()
    {
        // 初始化命令
        // 获取 Service
        // 加载数据
    }
    
    // 2. OnDestroy - 销毁时（重要！）
    public override void OnDestroy()
    {
        // 取消事件订阅
        // 清理资源
    }
}
```

## 最佳实践

### 1. 复用旧 Panel 组件

```csharp
public class MyPanelNew : UIPanel<MyViewModel>
{
    private MyPanelOld mOldPanel;
    
    void Awake()
    {
        // 获取旧 Panel 的组件
        mOldPanel = GetComponent<MyPanelOld>();
    }
    
    protected override void OnBindViewModel(MyViewModel vm)
    {
        // 复用旧组件
        vm.Property.OnValueChanged += (o, n) => mOldPanel.Text.text = n;
        mOldPanel.Button.onClick.AddListener(() => vm.Command.Execute());
    }
}
```

### 2. 事件订阅和取消订阅

```csharp
public class MyViewModel : ViewModel
{
    private LoginService mLoginService;
    
    public override void OnCreate()
    {
        mLoginService = GameServiceManager.Get<LoginService>();
        
        // 订阅事件
        mLoginService.OnLoginSuccess += OnLoginSuccess;
    }
    
    public override void OnDestroy()
    {
        // 必须取消订阅！防止内存泄漏
        mLoginService.OnLoginSuccess -= OnLoginSuccess;
    }
}
```

### 3. 异步操作

```csharp
public class MyViewModel : ViewModel
{
    public BindableProperty<bool> IsLoading { get; } = new BindableProperty<bool>(false);
    
    private async void OnConfirm()
    {
        IsLoading.Value = true;
        
        try
        {
            var result = await mService.DoSomethingAsync();
            // 处理结果
        }
        catch (Exception e)
        {
            Debug.LogError($"操作失败: {e}");
        }
        finally
        {
            IsLoading.Value = false;
        }
    }
}
```

### 4. 命令 CanExecute

```csharp
public class MyViewModel : ViewModel
{
    public Command OnSubmitCommand { get; private set; }
    
    public override void OnCreate()
    {
        OnSubmitCommand = new Command(OnSubmit, CanSubmit);
        
        // 属性变化时更新命令可用状态
        UserName.OnValueChanged += (o, n) => OnSubmitCommand.RaiseCanExecuteChanged();
        Password.OnValueChanged += (o, n) => OnSubmitCommand.RaiseCanExecuteChanged();
    }
    
    private bool CanSubmit()
    {
        return !string.IsNullOrEmpty(UserName.Value) 
            && !string.IsNullOrEmpty(Password.Value);
    }
}
```

## 常见问题

### Q: 如何传递数据给新打开的界面？

**A:**
```csharp
// 方式1：通过 ViewModel 构造函数
var vm = new MyViewModel();
vm.UserId.Value = 123;
vm.OnCreate();

// 方式2：使用 SetData
UIManager.OpenPanel<MyPanel, MyViewModel>(vm)
    .SetData(v => v.UserId.Value = 123)
    .Open();

// 方式3：使用 IUIData
public class MyData : IUIData
{
    public int UserId { get; set; }
}

UIManager.OpenPanel<MyPanel, MyViewModel>(vm)
    .SetLevel(UILevel.PopUI)
    .Open(new MyData { UserId = 123 });
```

### Q: 如何在新旧框架之间切换？

**A:**
```csharp
// 迁移期间可以共存
// 新界面使用 UIFramework
UIManager.OpenPanel<NewPanel, NewViewModel>(vm).Open();

// 旧界面继续使用 QFramework
UIKit.OpenPanel<OldPanel>();
```

### Q: 如何处理界面返回？

**A:**
```csharp
// 方式1：使用 Panel 堆栈
UIManager.PushToStack<MyPanel>();
// ... 打开其他界面
UIManager.Back(); // 返回上一个界面

// 方式2：手动管理
private Stack<Type> mPanelStack = new Stack<Type>();

void OpenPanel<T>() where T : UIPanel
{
    mPanelStack.Push(typeof(T));
    UIManager.OpenPanel<T, ...>(...).Open();
}

void GoBack()
{
    if (mPanelStack.Count > 0)
    {
        var type = mPanelStack.Pop();
        UIManager.ClosePanel(type);
    }
}
```

### Q: 如何优化性能？

**A:**
```csharp
// 1. 对象池
public class MyPanel : UIPanel<MyViewModel>
{
    protected override void OnHide()
    {
        // 隐藏时不销毁，只是隐藏
        gameObject.SetActive(false);
    }
}

// 2. 延迟加载
public override void OnCreate()
{
    // 延迟加载数据
    LoadDataAsync().Forget();
}

// 3. 批量更新
// 避免频繁更新 UI，可以批量处理
```

### Q: 如何复用已隐藏的界面？

**A:**
```csharp
// 对于频繁切换的界面（如商店），使用 GetOrOpenPanel
public void OpenShop()
{
    // 第一次：创建新界面
    // 第二次及以后：复用已隐藏的界面
    UIManager.GetOrOpenPanel<ShopPanel, ShopViewModel>()
        .SetLevel(UILevel.Const)
        .Open();
}

// 关闭时隐藏而不是销毁
public void CloseShop()
{
    UIManager.HidePanel<ShopPanel>(); // 隐藏，保留在内存中
    // 下次打开时会复用
}
```

## API 参考

### UIManager

#### 打开界面

```csharp
// 打开界面（创建新的）
public static PanelOpenBuilder<TView, TViewModel> OpenPanel<TView, TViewModel>(TViewModel viewModel)
    where TView : UIPanel<TViewModel>
    where TViewModel : ViewModel

// 打开界面（自动创建 ViewModel）
public static PanelOpenBuilder<TView, TViewModel> OpenPanel<TView, TViewModel>()
    where TView : UIPanel<TViewModel>
    where TViewModel : ViewModel, new()

// 获取或打开界面（复用已隐藏的）
public static PanelOpenBuilder<TView, TViewModel> GetOrOpenPanel<TView, TViewModel>(TViewModel viewModel)
    where TView : UIPanel<TViewModel>
    where TViewModel : ViewModel

// 获取或打开界面（自动创建 ViewModel）
public static PanelOpenBuilder<TView, TViewModel> GetOrOpenPanel<TView, TViewModel>()
    where TView : UIPanel<TViewModel>
    where TViewModel : ViewModel, new()
```

#### 关闭界面

```csharp
// 通过类型关闭
public static void ClosePanel<T>() where T : UIPanel

// 通过实例关闭
public static void ClosePanel(UIPanel panel)

// 关闭指定层级的所有界面
public static void ClosePanelsByLevel(UILevel level)

// 关闭所有界面
public static void CloseAllPanels()
```

#### 隐藏和显示界面

```csharp
// 隐藏界面（保留在内存中）
public static void HidePanel<T>() where T : UIPanel
public static void HidePanel(UIPanel panel)

// 显示界面（从隐藏状态恢复）
public static void ShowPanel<T>() where T : UIPanel
public static void ShowPanel(UIPanel panel)

// 切换显示/隐藏状态
public static void TogglePanel<T>() where T : UIPanel

// 尝试显示已存在的界面
public static bool TryShowExistingPanel<T>() where T : UIPanel
```

#### 获取界面

```csharp
// 获取已打开的界面
public static T GetPanel<T>() where T : UIPanel

// 尝试获取界面
public static bool TryGetPanel<T>(out T panel) where T : UIPanel

// 检查界面是否存在
public static bool ContainsPanel<T>() where T : UIPanel
```

#### 界面堆栈

```csharp
// 压入堆栈
public static void PushToStack<T>() where T : UIPanel

// 弹出堆栈（关闭当前界面）
public static void PopFromStack()

// 返回上一个界面
public static void Back()
```

### PanelOpenBuilder<TView, TViewModel>

```csharp
// 设置层级
public PanelOpenBuilder<TView, TViewModel> SetLevel(UILevel level)

// 设置数据
public PanelOpenBuilder<TView, TViewModel> SetData(Action<TViewModel> onSetData)

// 设置完成回调
public PanelOpenBuilder<TView, TViewModel> OnComplete(Action<TView> onComplete)

// 绑定节点（用于检查节点有效性）
public PanelOpenBuilder<TView, TViewModel> BindNode(UnityEngine.Object node)

// 打开界面（同步）
public void Open()

// 打开界面（异步）
public UniTask<TView> OpenAsync()
```

### BindableProperty<T>

```csharp
// 定义属性
public BindableProperty<T> Property { get; } = new BindableProperty<T>(defaultValue);

// 获取/设置值
public T Value { get; set; }

// 订阅值变化事件
public event Action<T, T> OnValueChanged
```

### Command

```csharp
// 无参数命令
public Command(Action execute, Func<bool> canExecute = null)

// 带参数命令
public Command<T>(Action<T> execute, Func<T, bool> canExecute = null)

// 执行命令
public void Execute()
public void Execute(T parameter)

// 检查是否可以执行
public bool CanExecute { get; }

// 触发 CanExecute 变化通知
public void RaiseCanExecuteChanged()
```

### ViewModel

```csharp
// 创建时调用
public virtual void OnCreate()

// 销毁时调用
public virtual void OnDestroy()
```

### UIPanel<TViewModel>

```csharp
// ViewModel 实例
public TViewModel VM { get; }

// 绑定 ViewModel（必须实现）
protected abstract void OnBindViewModel(TViewModel vm)

// 解绑 ViewModel（必须实现）
protected abstract void OnUnbindViewModel(TViewModel vm)

// 关闭自己
protected void CloseSelf()

// 生命周期方法
protected virtual void OnOpen(IUIData uiData = null)
protected virtual void OnShow()
protected virtual void OnHide()
```

## 完整示例

### LoginPanel 完整示例

```csharp
// LoginViewModel.cs
using UIFramework;
using Game.Login;
using UnityEngine;

namespace Game.Login
{
    public class LoginViewModel : ViewModel
    {
        public BindableProperty<string> Account { get; } = new BindableProperty<string>("");
        public BindableProperty<string> Password { get; } = new BindableProperty<string>("");
        public BindableProperty<bool> IsLoading { get; } = new BindableProperty<bool>(false);
        public BindableProperty<string> ErrorMessage { get; } = new BindableProperty<string>("");
        
        public Command OnLoginCommand { get; private set; }
        public Command OnCloseCommand { get; private set; }
        
        private LoginService mLoginService;
        
        public override void OnCreate()
        {
            mLoginService = GameServiceManager.Get<LoginService>();
            
            OnLoginCommand = new Command(OnLogin, () => !IsLoading.Value);
            OnCloseCommand = new Command(OnClose);
            
            LoadData();
        }
        
        private void LoadData()
        {
            Account.Value = PlayerPrefs.GetString("Account", "");
            Password.Value = PlayerPrefs.GetString("Password", "");
        }
        
        private async void OnLogin()
        {
            IsLoading.Value = true;
            ErrorMessage.Value = "";
            
            var result = await mLoginService.LoginAsync(Account.Value, Password.Value);
            
            if (result.Success)
            {
                OnLoginSuccess?.Invoke(result.UserData);
            }
            else
            {
                ErrorMessage.Value = result.ErrorMessage;
            }
            
            IsLoading.Value = false;
        }
        
        private void OnClose()
        {
            OnClosePanel?.Invoke();
        }
        
        public System.Action<UserData> OnLoginSuccess;
        public System.Action OnClosePanel;
    }
}

// LoginPanel.cs
using Game.Login;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    [PrefabPath("UI/Login/LoginPanel")]
    public class LoginPanel : UIPanel<LoginViewModel>
    {
        private LoginPanelOld mOldPanel;
        
        void Awake()
        {
            mOldPanel = GetComponent<LoginPanelOld>();
        }
        
        protected override void OnBindViewModel(LoginViewModel vm)
        {
            // 数据绑定
            vm.Account.OnValueChanged += (o, n) => mOldPanel.AccountInput.text = n;
            vm.Password.OnValueChanged += (o, n) => mOldPanel.PasswordInput.text = n;
            vm.IsLoading.OnValueChanged += (o, n) => mOldPanel.LoginBtn.interactable = !n;
            vm.ErrorMessage.OnValueChanged += (o, n) => ShowError(n);
            
            // 输入同步到 ViewModel
            mOldPanel.AccountInput.onValueChanged.AddListener(v => vm.Account.Value = v);
            mOldPanel.PasswordInput.onValueChanged.AddListener(v => vm.Password.Value = v);
            
            // 按钮绑定
            mOldPanel.LoginBtn.onClick.AddListener(() => vm.OnLoginCommand.Execute());
            mOldPanel.CloseBtn.onClick.AddListener(() => vm.OnCloseCommand.Execute());
            
            // 事件绑定
            vm.OnLoginSuccess += OnLoginSuccess;
            vm.OnClosePanel += OnClosePanel;
        }
        
        protected override void OnUnbindViewModel(LoginViewModel vm)
        {
            mOldPanel.AccountInput.onValueChanged.RemoveAllListeners();
            mOldPanel.PasswordInput.onValueChanged.RemoveAllListeners();
            mOldPanel.LoginBtn.onClick.RemoveAllListeners();
            mOldPanel.CloseBtn.onClick.RemoveAllListeners();
            
            vm.OnLoginSuccess -= OnLoginSuccess;
            vm.OnClosePanel -= OnClosePanel;
        }
        
        private void ShowError(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                // 显示错误提示
            }
        }
        
        private void OnLoginSuccess(UserData userData)
        {
            // 关闭登录界面
            CloseSelf();
            
            // 打开主界面
            var mainVm = new MainViewModel();
            mainVm.OnCreate();
            UIManager.OpenPanel<MainPanel, MainViewModel>(mainVm)
                .SetLevel(UILevel.Const)
                .Open();
        }
        
        private void OnClosePanel()
        {
            CloseSelf();
        }
    }
}
```

---

更多示例请参考 `/Assets/Scripts/Game/` 目录下的实际代码。
