# UIFramework 设计文档

## 架构概述

UIFramework 是一个基于 MVVM（Model-View-ViewModel）架构的 Unity UI 框架，旨在解决传统 UI 开发中的以下问题：

1. **代码耦合**：UI 逻辑和业务逻辑混杂
2. **难以测试**：UI 代码依赖 Unity 环境，无法单元测试
3. **数据同步**：手动更新 UI，容易出错
4. **资源管理**：界面打开关闭逻辑复杂
5. **性能问题**：频繁的 GC Alloc

## 核心架构

### 1. 整体架构图

```
┌─────────────────────────────────────────────────────────────┐
│                        数据层 (Data)                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │ 配置表数据  │  │ 服务器数据  │  │ 本地存储    │         │
│  │ (TbConfig)  │  │ (API)       │  │ (PlayerPrefs│         │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘         │
└─────────┼────────────────┼────────────────┼────────────────┘
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────┐
│                      Service 层                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │LoginService │  │PlayerService│  │ ShopService │         │
│  │             │  │             │  │             │         │
│  │- 业务逻辑   │  │- 数据缓存   │  │- 商品管理   │         │
│  │- 数据持久化 │  │- 状态管理   │  │- 购买逻辑   │         │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘         │
└─────────┼────────────────┼────────────────┼────────────────┘
          │                │                │
          │ 通过事件通信    │                │
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────┐
│                    ViewModel 层                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │LoginPanel   │  │PlayerPanel  │  │ ShopPanel   │         │
│  │ViewModel    │  │ViewModel    │  │ViewModel    │         │
│  │             │  │             │  │             │         │
│  │- Bindable   │  │- Bindable   │  │- Bindable   │         │
│  │  Property   │  │  Property   │  │  Property   │         │
│  │- Command    │  │- Command    │  │- Command    │         │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘         │
└─────────┼────────────────┼────────────────┼────────────────┘
          │                │                │
          │ 数据绑定        │                │
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────┐
│                       View 层                                │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │ LoginPanel  │  │ PlayerPanel │  │ ShopPanel   │         │
│  │ (MonoBehaviour)            │  │             │         │
│  │             │  │             │  │             │         │
│  │- UI组件绑定 │  │- UI组件绑定 │  │- UI组件绑定 │         │
│  │- 事件监听   │  │- 事件监听   │  │- 事件监听   │         │
│  │- 生命周期   │  │- 生命周期   │  │- 生命周期   │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
└─────────────────────────────────────────────────────────────┘
          │                │                │
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────┐
│                    UIManager                                 │
│  - 界面打开/关闭管理                                         │
│  - 层级管理 (UILevel)                                        │
│  - 资源加载 (YooAssets)                                      │
│  - 生命周期管理                                              │
└─────────────────────────────────────────────────────────────┘
```

### 2. 数据流向

```
用户操作 → View (UI事件) → ViewModel (Command) → Service (业务逻辑) → Data (数据)
                                              ↓
数据更新 ← View (自动更新) ← ViewModel (BindableProperty) ← Service (事件通知)
```

## 核心组件设计

### 1. UIPanel<TViewModel>（视图层）

```csharp
public abstract class UIPanel<TViewModel> : UIPanel, IView 
    where TViewModel : ViewModel
{
    public TViewModel VM { get; private set; }
    
    // 生命周期
    protected override void OnInit(IUIData uiData)
    {
        // 从 uiData 中提取 ViewModel
        // 调用 OnBindViewModel 进行绑定
    }
    
    // 必须实现：绑定 ViewModel
    protected abstract void OnBindViewModel(TViewModel vm);
    
    // 必须实现：解绑 ViewModel
    protected abstract void OnUnbindViewModel(TViewModel vm);
    
    // 关闭自己
    protected void CloseSelf()
    {
        UIManager.ClosePanel(this);
    }
}
```

**设计理由：**
- **泛型约束**：编译时类型安全，避免运行时类型错误
- **抽象方法**：强制子类实现绑定和解绑，防止内存泄漏
- **生命周期**：与 Unity 生命周期结合，便于资源管理

### 2. ViewModel（视图模型层）

```csharp
public abstract class ViewModel
{
    // 创建时调用（替代构造函数）
    public virtual void OnCreate()
    {
        // 初始化命令
        // 获取 Service
        // 加载数据
    }
    
    // 销毁时调用（替代析构函数）
    public virtual void OnDestroy()
    {
        // 取消事件订阅
        // 清理资源
    }
}
```

**设计理由：**
- **OnCreate/OnDestroy**：替代构造函数，支持异步初始化
- **无 Unity 依赖**：可以单元测试
- **数据绑定**：通过 BindableProperty 实现响应式

### 3. BindableProperty<T>（数据绑定）

```csharp
public class BindableProperty<T>
{
    private T mValue;
    
    public T Value
    {
        get => mValue;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(mValue, value))
            {
                var oldValue = mValue;
                mValue = value;
                OnValueChanged?.Invoke(oldValue, value);
            }
        }
    }
    
    public event Action<T, T> OnValueChanged;
}
```

**设计理由：**
- **值比较**：避免重复赋值触发事件
- **新旧值**：通知时提供上下文
- **泛型**：支持任意数据类型

### 4. Command（命令模式）

```csharp
public class Command
{
    private readonly Action mExecute;
    private readonly Func<bool> mCanExecute;
    
    public Command(Action execute, Func<bool> canExecute = null)
    {
        mExecute = execute;
        mCanExecute = canExecute;
    }
    
    public bool CanExecute => mCanExecute?.Invoke() ?? true;
    
    public void Execute()
    {
        if (CanExecute)
        {
            mExecute();
        }
    }
    
    public void RaiseCanExecuteChanged()
    {
        OnCanExecuteChanged?.Invoke();
    }
    
    public event Action OnCanExecuteChanged;
}
```

**设计理由：**
- **CanExecute**：控制命令可用状态，自动禁用按钮
- **解耦**：View 只调用 Execute，不关心具体实现
- **可测试**：命令逻辑可独立测试

### 5. UIManager（界面管理器）

```csharp
public static class UIManager
{
    // 已打开的界面缓存
    private static readonly Dictionary<Type, UIPanel> mPanels = 
        new Dictionary<Type, UIPanel>();
    
    // 打开界面（构建器模式）
    public static PanelOpenBuilder<TView, TViewModel> OpenPanel<TView, TViewModel>(
        TViewModel viewModel)
        where TView : UIPanel<TViewModel>
        where TViewModel : ViewModel
    {
        return new PanelOpenBuilder<TView, TViewModel>(viewModel);
    }
    
    // 关闭界面
    public static void ClosePanel<T>() where T : UIPanel
    {
        if (mPanels.TryGetValue(typeof(T), out var panel))
        {
            panel.Close();
        }
    }
}
```

**设计理由：**
- **构建器模式**：链式调用，代码可读性强
- **泛型约束**：类型安全
- **缓存管理**：避免重复打开，便于统一管理

### 6. GameServiceManager（服务管理器）

见 Service 模块设计文档。

## 关键设计决策

### 1. 为什么使用 MVVM？

**对比传统 MVC：**

| 特性 | MVC | MVVM |
|------|-----|------|
| 数据绑定 | 手动 | 自动 |
| 测试性 | 难（依赖 UI） | 易（ViewModel 可测试） |
| 代码复用 | 低 | 高（ViewModel 可复用） |
| 维护性 | 低 | 高（职责清晰） |

**设计：**
- View：只负责显示，不包含业务逻辑
- ViewModel：UI 逻辑，数据转换，可单元测试
- Service：业务逻辑，数据持久化

### 2. 为什么使用 UniTask？

**对比 Task：**

| 特性 | Task | UniTask |
|------|------|---------|
| GC Alloc | 有 | 无 |
| 性能 | 一般 | 高 |
| Unity 兼容 | 需要配置 | 原生支持 |

**设计：**
- 所有异步操作使用 UniTask
- 避免在热路径分配内存

### 3. 为什么使用 YooAssets？

**对比 Resources：**

| 特性 | Resources | YooAssets |
|------|-----------|-----------|
| 热更新 | 不支持 | 支持 |
| 内存管理 | 手动 | 自动 |
| 加载速度 | 慢 | 快（可缓存） |

**设计：**
- 资源加载统一使用 YooAssets
- 支持热更新和缓存

### 4. 为什么使用 Inspector 配置层级？

**对比代码配置：**

| 特性 | 代码配置 | Inspector 配置 |
|------|----------|----------------|
| 灵活性 | 低（需改代码） | 高（运行时调整） |
| 可视化 | 差 | 好 |
| 团队协作 | 易冲突 | 易协作 |

**设计：**
- UIRootConfig 组件可视化配置
- 层级使用枚举值作为 SortingOrder

## 性能优化

### 1. 对象池

```csharp
// 界面关闭时不销毁，只是隐藏
public virtual void Hide()
{
    gameObject.SetActive(false);
    OnHide();
}

// 下次打开时复用
public virtual void Show()
{
    gameObject.SetActive(true);
    OnShow();
}
```

### 2. 延迟加载

```csharp
public override void OnCreate()
{
    // 延迟加载数据，不阻塞主线程
    LoadDataAsync().Forget();
}

private async UniTaskVoid LoadDataAsync()
{
    await UniTask.Delay(100); // 延迟一帧
    var data = await LoadFromServer();
    // 更新 UI
}
```

### 3. 批量更新

```csharp
// 避免频繁更新 UI
private List<Action> mPendingUpdates = new List<Action>();

public void UpdateProperty(string name, object value)
{
    mPendingUpdates.Add(() => 
    {
        // 实际更新
    });
}

void LateUpdate()
{
    // 每帧只更新一次
    foreach (var update in mPendingUpdates)
    {
        update();
    }
    mPendingUpdates.Clear();
}
```

## 线程安全

### 设计决策

**UIFramework 不是线程安全的**，理由：
1. Unity 主线程限制：所有 Unity API 必须在主线程调用
2. UniTask 在主线程执行
3. 简化设计

### 使用规范

```csharp
// ✅ 正确：UniTask 在主线程执行
async void SomeMethod()
{
    await LoadDataAsync();
    UpdateUI(); // 在主线程更新 UI
}

// ❌ 错误：在后台线程操作 UI
void WrongMethod()
{
    Task.Run(() =>
    {
        text.text = "Hello"; // 崩溃！
    });
}
```

## 扩展性

### 1. 添加新的 ViewModel

```csharp
// 1. 继承 ViewModel
public class MyViewModel : ViewModel
{
    public BindableProperty<string> Data { get; } = new BindableProperty<string>("");
    
    public override void OnCreate()
    {
        // 初始化
    }
}

// 2. 创建对应的 Panel
[PrefabPath("UI/MyPanel")]
public class MyPanel : UIPanel<MyViewModel>
{
    protected override void OnBindViewModel(MyViewModel vm)
    {
        vm.Data.OnValueChanged += (o, n) => text.text = n;
    }
}
```

### 2. 自定义资源加载器

```csharp
public class CustomAssetLoader : IAssetLoader
{
    public void LoadAssetAsync<T>(string path, Action<T> onComplete) where T : Object
    {
        // 自定义加载逻辑
    }
}

// 使用
UIManager.AssetLoader = new CustomAssetLoader();
```

### 3. 自定义路径提供器

```csharp
public class CustomPathProvider : IPrefabPathProvider
{
    public string GetPrefabPath(Type panelType)
    {
        // 自定义路径规则
        return $"Custom/UI/{panelType.Name}";
    }
}

// 使用
UIManager.PathProvider = new CustomPathProvider();
```

## 错误处理

### 1. 初始化错误

```csharp
private static async UniTask InitializeServiceWithErrorHandling(IGameService service)
{
    try
    {
        await service.InitializeAsync();
    }
    catch (Exception e)
    {
        Debug.LogError($"[GameServiceManager] Service初始化失败: {service.ServiceName}, 错误: {e}");
        // 继续初始化其他 Service
    }
}
```

### 2. 运行时错误

```csharp
public class MyViewModel : ViewModel
{
    private async void OnConfirm()
    {
        try
        {
            IsLoading.Value = true;
            var result = await mService.DoSomethingAsync();
            // 处理结果
        }
        catch (Exception e)
        {
            ErrorMessage.Value = $"操作失败: {e.Message}";
        }
        finally
        {
            IsLoading.Value = false;
        }
    }
}
```

## 与旧框架共存

### 迁移策略

```
阶段1：新旧共存
- 新界面使用 UIFramework
- 旧界面继续使用 QFramework
- 可以互相打开

阶段2：逐步替换
- 按优先级迁移界面
- 先迁移简单界面
- 最后迁移复杂界面

阶段3：完全替换
- 删除旧框架代码
- 清理无用资源
```

### 共存实现

```csharp
// 新界面打开旧界面
UIManager.OpenPanel<NewPanel, NewViewModel>(vm).Open();
// ...
UIKit.OpenPanel<OldPanel>(); // 旧界面正常打开

// 旧界面打开新界面
UIKit.OpenPanel<OldPanel>();
// ...
var vm = new NewViewModel();
vm.OnCreate();
UIManager.OpenPanel<NewPanel, NewViewModel>(vm).Open();
```

## 总结

UIFramework 设计目标：
1. **解耦**：UI、逻辑、数据分离
2. **可测试**：ViewModel 和 Service 可单元测试
3. **高性能**：UniTask + YooAssets + 对象池
4. **易维护**：清晰的职责划分
5. **可扩展**：易于添加新功能

---

设计文档版本：1.0
最后更新：2026-03-19
作者：UIFramework Team
