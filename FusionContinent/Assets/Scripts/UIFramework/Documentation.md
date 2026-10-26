# UIFramework 使用文档

完全独立的 MVVM UI 框架，支持可插拔的资源加载系统。

## 目录

1. [快速开始](#快速开始)
2. [核心概念](#核心概念)
3. [API 参考](#api-参考)
4. [配置说明](#配置说明)
5. [最佳实践](#最佳实践)
6. [常见问题](#常见问题)

---

## 快速开始

### 1. 初始化

在场景启动时调用一次：

```csharp
public class GameManager : MonoBehaviour
{
    void Start()
    {
        // 初始化 UIFramework
        UIManager.Initialize();
        
        // 可选：配置资源加载器
        // UIManager.AssetLoader = new YooAssetsLoader("DefaultPackage");
    }
}
```

### 2. 创建 ViewModel

ViewModel 是纯 C# 类，不依赖 Unity：

```csharp
using UIFramework;

public class MainViewModel : ViewModel
{
    // 可绑定属性
    public BindableProperty<int> Gold { get; } = new BindableProperty<int>(0);
    public BindableProperty<string> PlayerName { get; } = new BindableProperty<string>("玩家");
    
    // 命令
    public Command StartGameCommand { get; private set; }
    
    public override void OnCreate()
    {
        // 初始化命令
        StartGameCommand = new Command(OnStartGame);
    }
    
    private void OnStartGame()
    {
        Debug.Log("开始游戏！");
    }
}
```

### 3. 创建 View

继承 `UIPanel<TViewModel>`：

```csharp
using UIFramework;
using UnityEngine.UI;

public class MainPanel : UIPanel<MainViewModel>
{
    public Text GoldText;
    public Text NameText;
    public Button StartBtn;
    
    protected override void OnBindViewModel(MainViewModel vm)
    {
        // 绑定属性到 UI
        UIBind.Text(GoldText, vm.Gold, "金币: {0}");
        UIBind.Text(NameText, vm.PlayerName);
        
        // 绑定命令到按钮
        UIBind.Click(StartBtn, vm.StartGameCommand);
    }
    
    protected override void OnUnbindViewModel(MainViewModel vm)
    {
        // 清理工作（通常不需要手动处理）
    }
}
```

### 4. 放置 Prefab

1. 创建 GameObject，挂载 `MainPanel` 脚本
2. 拖拽绑定 UI 组件
3. 保存为 Prefab：`Assets/Resources/UI/Prefabs/MainPanel.prefab`

### 5. 打开界面

```csharp
// 方式1：使用构建器
var vm = new MainViewModel();
vm.Gold.Value = 1000;
vm.PlayerName.Value = "勇者";

UIManager.OpenPanel<MainPanel, MainViewModel>(vm)
    .SetLevel(UILevel.Common)
    .OnComplete(panel => Debug.Log("界面已打开"))
    .Open();

// 方式2：await
var panel = await UIManager.OpenPanel<MainPanel, MainViewModel>(vm)
    .SetLevel(UILevel.Common);

// 方式3：绑定关键节点（如果节点销毁，界面不打开）
UIManager.OpenPanel<MainPanel, MainViewModel>(vm)
    .BindNode(playerGameObject)  // 如果玩家被销毁，不打开界面
    .Open();
```

---

## 核心概念

### MVVM 架构

```
View (MonoBehaviour) <-> ViewModel (纯C#) <-> Model (数据)
     |                         |                    |
     | 绑定属性更新UI          | 业务逻辑           | 原始数据
     |                         |                    |
    Text                     Command              服务器/本地
    Button                   Property
```

### 可绑定属性 (BindableProperty)

数据变更自动通知 UI 更新：

```csharp
public BindableProperty<int> Gold { get; } = new BindableProperty<int>(0);

// 设置值，UI 自动更新
Gold.Value = 100;

// 监听变化（通常不需要手动处理）
Gold.OnValueChanged += (oldVal, newVal) => {
    Debug.Log($"金币从 {oldVal} 变为 {newVal}");
};
```

### 命令 (Command)

封装可绑定的操作：

```csharp
// 无参命令
public Command SaveCommand { get; private set; }
SaveCommand = new Command(OnSave, CanSave);  // Action, CanExecute

// 带参命令
public Command<ItemData> BuyCommand { get; private set; }
BuyCommand = new Command<ItemData>(OnBuy, CanBuy);

// 刷新命令可用状态
SaveCommand.RaiseCanExecuteChanged();
```

### UI 层级

```csharp
public enum UILevel
{
    Bg = -2,        // 背景层（最底层）
    Common = 0,     // 普通层（默认）
    PopUI = 2,      // 弹出层（弹窗）
    Guide = 3,      // 引导层（新手引导）
    Const = 4,      // 持续层（常驻 UI）
    Loading = 999,  // 加载层（最上层）
}
```

---

## API 参考

### UIManager

#### 初始化
```csharp
// 初始化 UIFramework
UIManager.Initialize();

// 配置资源加载器
UIManager.AssetLoader = new YooAssetsLoader("DefaultPackage");

// 配置路径规则
UIManager.PathProvider = new DefaultPrefabPathProvider("Assets/MyUI");
```

#### 打开界面
```csharp
// 返回构建器，支持链式调用
public static PanelOpenBuilder<TView, TViewModel> OpenPanel<TView, TViewModel>(TViewModel viewModel)
    where TView : UIPanel<TViewModel>
    where TViewModel : ViewModel;

// 自动创建 ViewModel（需要无参构造函数）
public static PanelOpenBuilder<TView, TViewModel> OpenPanel<TView, TViewModel>()
    where TView : UIPanel<TViewModel>
    where TViewModel : ViewModel, new();
```

#### 关闭界面
```csharp
// 关闭指定界面
public static void ClosePanel<T>() where T : UIPanel;

// 关闭指定层级的所有界面
public static void ClosePanelsByLevel(UILevel level);

// 关闭所有界面
public static void CloseAllPanels(bool excludeLoading = true);
```

#### 查询界面
```csharp
// 检查界面是否已打开
public static bool ContainsPanel<T>() where T : UIPanel;

// 获取已打开的 ViewModel
public static TViewModel GetViewModel<TViewModel>() where TViewModel : ViewModel;

// 获取指定层级的所有界面
public static IEnumerable<Type> GetPanelsByLevel(UILevel level);

// 获取界面信息
public static PanelInfo GetPanelInfo<T>() where T : UIPanel;
```

#### 栈管理
```csharp
// 压入栈（支持返回功能）
public static void PushToStack<T>() where T : UIPanel;

// 弹出栈顶并关闭
public static void PopFromStack();

// 返回上一个界面（等同于 PopFromStack）
public static void Back();
```

### PanelOpenBuilder

```csharp
// 绑定关键节点
public PanelOpenBuilder<TView, TViewModel> BindNode(UnityEngine.Object node);

// 设置层级
public PanelOpenBuilder<TView, TViewModel> SetLevel(UILevel level);

// 设置数据
public PanelOpenBuilder<TView, TViewModel> SetData(Action<TViewModel> onSetData);

// 打开完成回调
public PanelOpenBuilder<TView, TViewModel> OnComplete(Action<TView> onComplete);

// 执行打开（回调方式）
public void Open();

// 执行打开（Task 方式）
public Task<TView> OpenAsync();
```

### UIBind

```csharp
// 文本绑定
UIBind.Text(Text text, BindableProperty<string> property);
UIBind.Text(Text text, BindableProperty<int> property, string format = "{0}");

// Image 绑定
UIBind.Sprite(Image image, BindableProperty<Sprite> property);
UIBind.FillAmount(Image image, BindableProperty<float> property);

// 按钮绑定
UIBind.Click(Button button, Command command);
UIBind.Click(Button button, Action action);
UIBind.Click<T>(Button button, Command<T> command, Func<T> paramProvider);

// GameObject 激活状态绑定
UIBind.Active(GameObject obj, BindableProperty<bool> property);

// Slider 绑定
UIBind.Slider(Slider slider, BindableProperty<float> property, bool twoWay = false);
```

---

## 配置说明

### 资源加载器

#### 1. ResourcesAssetLoader（默认）

```csharp
UIManager.AssetLoader = new ResourcesAssetLoader();
```

Prefab 路径：`Resources/UI/Prefabs/{PanelName}.prefab`

#### 2. YooAssetsLoader

```csharp
// 需要安装 YooAssets 包，并添加 YOO_ASSETS 宏
UIManager.AssetLoader = new YooAssetsLoader("DefaultPackage");
```

Prefab 路径：根据 YooAssets 配置的路径

#### 3. 自定义加载器

```csharp
public class AddressablesLoader : IAssetLoader
{
    public void Initialize() { }
    
    public void LoadAssetAsync<T>(string path, Action<T> onComplete) where T : Object
    {
        Addressables.LoadAssetAsync<T>(path).Completed += handle =>
        {
            onComplete?.Invoke(handle.Result);
        };
    }
    
    public IEnumerator LoadAssetCoroutine<T>(string path, Action<T> onComplete) where T : Object
    {
        var handle = Addressables.LoadAssetAsync<T>(path);
        yield return handle;
        onComplete?.Invoke(handle.Result);
    }
    
    public void ReleaseAsset(string path)
    {
        Addressables.Release(path);
    }
    
    public void LoadSceneAsync(string sceneName, Action onComplete = null)
    {
        Addressables.LoadSceneAsync(sceneName).Completed += handle =>
        {
            onComplete?.Invoke();
        };
    }
}

// 使用
UIManager.AssetLoader = new AddressablesLoader();
```

### 路径提供器

```csharp
// 自定义路径规则
public class MyPathProvider : IPrefabPathProvider
{
    public string GetPrefabPath(Type panelType)
    {
        // 根据类型返回不同路径
        if (panelType.Name.EndsWith("Panel"))
        {
            return $"UI/Panels/{panelType.Name}";
        }
        else if (panelType.Name.EndsWith("Popup"))
        {
            return $"UI/Popups/{panelType.Name}";
        }
        return $"UI/{panelType.Name}";
    }
}

UIManager.PathProvider = new MyPathProvider();
```

---

## 最佳实践

### 1. 界面打开/关闭流程

```csharp
public class GameController : MonoBehaviour
{
    async void Start()
    {
        // 显示加载界面
        await UIManager.OpenPanel<LoadingPanel, LoadingViewModel>(new LoadingViewModel())
            .SetLevel(UILevel.Loading);
        
        // 加载数据
        await LoadGameData();
        
        // 关闭加载界面
        UIManager.ClosePanel<LoadingPanel>();
        
        // 打开主界面
        var mainVm = new MainViewModel();
        await UIManager.OpenPanel<MainPanel, MainViewModel>(mainVm)
            .SetLevel(UILevel.Common);
    }
}
```

### 2. 处理异步打开

```csharp
// 方式1：await（推荐）
async void OpenShop()
{
    var vm = new ShopViewModel();
    var panel = await UIManager.OpenPanel<ShopPanel, ShopViewModel>(vm)
        .SetLevel(UILevel.PopUI);
    
    if (panel != null)
    {
        // 界面成功打开
        panel.DoSomething();
    }
}

// 方式2：回调
void OpenShop()
{
    var vm = new ShopViewModel();
    UIManager.OpenPanel<ShopPanel, ShopViewModel>(vm)
        .SetLevel(UILevel.PopUI)
        .OnComplete(panel =>
        {
            if (panel != null)
            {
                // 界面成功打开
            }
        })
        .Open();
}
```

### 3. 节点绑定检查

```csharp
// 如果玩家被销毁，不打开商店
public void OpenShop()
{
    UIManager.OpenPanel<ShopPanel, ShopViewModel>(new ShopViewModel())
        .BindNode(playerGameObject)      // 关键节点1
        .BindNode(shopNPCObject)         // 关键节点2
        .OnComplete(panel =>
        {
            // 只有在所有节点都有效时才会执行
        })
        .Open();
}
```

### 4. 数据驱动更新

```csharp
public class PlayerPanel : UIPanel<PlayerViewModel>
{
    public Text HpText;
    public Slider HpSlider;
    public Image AvatarImage;
    
    protected override void OnBindViewModel(PlayerViewModel vm)
    {
        // 绑定属性，数据变更自动更新 UI
        UIBind.Text(HpText, vm.Hp, "{0}/{1}");
        UIBind.Slider(HpSlider, vm.HpPercent);
        UIBind.Sprite(AvatarImage, vm.AvatarSprite);
        
        // 监听特定事件（非必需，仅演示）
        vm.Hp.OnValueChanged += (oldVal, newVal) =>
        {
            if (newVal < oldVal)
            {
                // 播放受伤动画
                PlayDamageAnimation();
            }
        };
    }
}
```

### 5. 返回功能实现

```csharp
public class MainPanel : UIPanel<MainViewModel>
{
    protected override void OnBindViewModel(MainViewModel vm)
    {
        // 打开子界面时压入栈
        UIBind.Click(OpenShopBtn, () =>
        {
            UIManager.PushToStack<ShopPanel>();
            UIManager.OpenPanel<ShopPanel, ShopViewModel>(new ShopViewModel())
                .SetLevel(UILevel.PopUI)
                .Open();
        });
    }
}

// 在任意地方返回
public void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        UIManager.Back();  // 关闭栈顶的界面
    }
}
```

---

## 常见问题

### Q: 界面 Prefab 应该放在哪里？

**A:** 默认路径：`Assets/Resources/UI/Prefabs/{PanelName}.prefab`

可以通过 `UIManager.PathProvider` 自定义路径规则。

### Q: 如何支持 TextMeshPro？

**A:** 在项目设置中添加 `UIFramework_TMPRO` 宏：

```
Edit -> Project Settings -> Player -> Scripting Define Symbols
添加：UIFramework_TMPRO
```

### Q: 界面打开时提示 "找不到 Prefab"？

**A:** 检查以下几点：
1. Prefab 是否放在正确的目录（Resources/UI/Prefabs/）
2. Prefab 名称是否与 Panel 类名一致（区分大小写）
3. 是否配置了自定义的 `PathProvider`

### Q: 如何更换资源加载方式？

**A:** 在 `Initialize` 之前设置加载器：

```csharp
void Start()
{
    UIManager.AssetLoader = new YooAssetsLoader("DefaultPackage");
    UIManager.Initialize();
}
```

### Q: 支持 Addressables 吗？

**A:** 支持，实现 `IAssetLoader` 接口即可，参考上面的自定义加载器示例。

### Q: 如何在打开界面时传递数据？

**A:** 通过 ViewModel 传递：

```csharp
var vm = new ShopViewModel();
vm.PlayerId = 12345;
vm.ShopType = ShopType.Weapon;

UIManager.OpenPanel<ShopPanel, ShopViewModel>(vm)
    .Open();
```

### Q: 界面加载过程中如何取消？

**A:** 使用 `BindNode` 绑定关键节点：

```csharp
UIManager.OpenPanel<ShopPanel, ShopViewModel>(vm)
    .BindNode(playerObject)  // 如果 playerObject 被销毁，界面不会打开
    .Open();
```

---

## 示例项目结构

```
Assets/
├── Scripts/
│   ├── UI/
│   │   ├── Panels/           # Panel 脚本
│   │   │   ├── MainPanel.cs
│   │   │   └── ShopPanel.cs
│   │   └── ViewModels/       # ViewModel 脚本
│   │       ├── MainViewModel.cs
│   │       └── ShopViewModel.cs
│   └── GameManager.cs
├── Resources/
│   └── UI/
│       └── Prefabs/          # UI Prefab
│           ├── MainPanel.prefab
│           └── ShopPanel.prefab
└── UIFramework/              # 框架代码
    └── ...
```

---

## 更新日志

### v1.0.0
- 初始版本
- 支持 MVVM 架构
- 支持 Resources/YooAssets 加载
- 支持节点绑定检查
- 支持层级管理

---

**文档版本**: 1.0.0  
**最后更新**: 2025-01-XX
