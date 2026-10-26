# UIFramework（独立版）

完全独立的 MVVM UI 框架，支持可插拔的资源加载系统。

## 特性

- ✅ **完全独立** - 不依赖任何外部框架
- ✅ **可插拔资源加载** - 支持 Resources、YooAssets、Addressables
- ✅ **MVVM 模式** - 数据驱动视图
- ✅ **异步加载** - 支持异步资源加载
- ✅ **节点绑定检查** - 关键节点销毁时自动取消打开界面
- ✅ **层级管理** - 内置 Bg/Common/PopUI/Guide/Loading 层级
- ✅ **栈管理** - 支持返回（Back）功能

## 快速开始

### 1. 初始化

在游戏启动时调用一次：

```csharp
void Start()
{
    // 可选：配置资源加载器（默认使用 Resources）
    // UIFramework.UIManager.AssetLoader = new YooAssetsLoader("DefaultPackage");
    
    UIFramework.UIManager.Initialize();
}
```

### 2. 配置 YooAssets（可选）

如果使用 YooAssets，在初始化前设置：

```csharp
using UIFramework;

void Start()
{
    // 设置使用 YooAssets
    UIManager.AssetLoader = new YooAssetsLoader("DefaultPackage");
    
    // 可选：设置自定义路径规则
    UIManager.PathProvider = new DefaultPrefabPathProvider("Assets/HotUpdateAssets/UI");
    
    UIManager.Initialize();
}
```

**注意**：使用 YooAssets 需要：
1. 安装 YooAssets 包
2. 在项目设置中添加 `YOO_ASSETS` 宏
3. 确保 YooAssets 已完成初始化

### 3. 创建 ViewModel

```csharp
public class ShopViewModel : ViewModel
{
    public BindableProperty<int> Gold { get; } = new BindableProperty<int>(0);
    public BindableProperty<string> Title { get; } = new BindableProperty<string>("商店");
    
    public override void OnCreate()
    {
        // 初始化
    }
}
```

### 4. 创建 View

```csharp
public class ShopPanel : UIPanel<ShopViewModel>
{
    public Text GoldText;
    
    protected override void OnBindViewModel(ShopViewModel vm)
    {
        UIBind.Text(GoldText, vm.Gold, "金币: {0}");
    }
    
    protected override void OnUnbindViewModel(ShopViewModel vm) { }
}
```

### 5. 打开界面

```csharp
// 方式1：使用构建器（推荐）
var vm = new ShopViewModel();
vm.Gold.Value = 1000;

UIManager.OpenPanel<ShopPanel, ShopViewModel>(vm)
    .SetLevel(UILevel.PopUI)
    .OnComplete(panel => Debug.Log("打开成功"))
    .Open();

// 方式2：await
var panel = await UIManager.OpenPanel<ShopPanel, ShopViewModel>(vm)
    .SetLevel(UILevel.PopUI);

// 方式3：绑定节点（如果节点销毁，界面不打开）
UIManager.OpenPanel<ShopPanel, ShopViewModel>(vm)
    .BindNode(playerGameObject)  // 如果玩家被销毁，不打开商店
    .Open();
```

### 6. 关闭界面

```csharp
UIManager.ClosePanel<ShopPanel>();
```

## 目录结构

```
Assets/Scripts/UIFramework/
├── Core/
│   ├── UIPanel.cs           # Panel 基类
│   ├── UIManager.cs         # 界面管理器
│   ├── AssetLoader.cs       # 资源加载接口和实现
│   ├── ViewModelData.cs     # ViewModel 数据包装
│   ├── BindableProperty.cs  # 可绑定属性
│   └── UIBind.cs           # 绑定工具
└── Example/                # 示例代码
```

## 资源加载系统

### 内置加载器

#### 1. ResourcesAssetLoader（默认）

```csharp
// 使用 Resources 加载
UIManager.AssetLoader = new ResourcesAssetLoader();
```

资源路径：`Resources/UI/Prefabs/ShopPanel.prefab`

#### 2. YooAssetsLoader

```csharp
// 使用 YooAssets 加载
UIManager.AssetLoader = new YooAssetsLoader("DefaultPackage");
```

资源路径：`Assets/HotUpdateAssets/UI/ShopPanel.prefab`（或其他你配置的路径）

### 自定义路径规则

```csharp
// 自定义路径提供器
public class MyPathProvider : IPrefabPathProvider
{
    public string GetPrefabPath(Type panelType)
    {
        return $"Assets/MyUI/{panelType.Name}";
    }
}

// 使用
UIManager.PathProvider = new MyPathProvider();
```

### 自定义资源加载器

实现 `IAssetLoader` 接口：

```csharp
public class AddressablesLoader : IAssetLoader
{
    public void Initialize() { }
    
    public void LoadAssetAsync<T>(string path, Action<T> onComplete) where T : Object
    {
        Addressables.LoadAssetAsync<T>(path).Completed += (handle) =>
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
        Addressables.LoadSceneAsync(sceneName).Completed += (handle) =>
        {
            onComplete?.Invoke();
        };
    }
}
```

然后使用：

```csharp
UIManager.AssetLoader = new AddressablesLoader();
```

## 注意事项

1. 确保在场景启动时调用 `UIManager.Initialize()`
2. 如果使用 YooAssets，确保 YooAssets 已初始化完成再设置 AssetLoader
3. Prefab 路径根据使用的加载器不同而不同
4. 使用 `BindNode` 可以防止在关键对象销毁时打开无关界面
