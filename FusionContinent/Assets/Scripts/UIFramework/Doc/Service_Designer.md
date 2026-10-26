# GameService 模块设计文档

## 架构设计

### 1. 整体架构图

```
┌─────────────────────────────────────────────────────────────┐
│                      GameMainController                      │
│  (游戏主控制器，负责初始化 GameServiceManager)               │
└────────────────────┬────────────────────────────────────────┘
                     │ InitializeAsync()
                     │
┌────────────────────▼────────────────────────────────────────┐
│                 GameServiceManager                           │
│  (Service 管理器，统一管理所有 Service)                      │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  - mServices: Dictionary<Type, IGameService>        │   │
│  │  - mPendingServices: List<IGameService>             │   │
│  │  - Register()                                       │   │
│  │  - InitializeAllAsync()                             │   │
│  │  - Get<T>()                                         │   │
│  └─────────────────────────────────────────────────────┘   │
└────────────────────┬────────────────────────────────────────┘
                     │ 管理多个 Service
                     │
    ┌────────────────┼────────────────┐
    │                │                │
┌───▼───┐      ┌────▼────┐      ┌───▼────┐
│Login  │      │Player   │      │Shop    │
│Service│      │Service  │      │Service │
│       │      │         │      │        │
│-登录  │      │-玩家数据│      │-商品   │
│-Token │      │-等级    │      │-购买   │
└───┬───┘      └────┬────┘      └────┬───┘
    │               │                │
    │ 通过事件通信   │                │
    │               │                │
┌───▼───────────────▼────────────────▼───┐
│              ViewModel 层              │
│  - LoginPanel01ViewModel               │
│  - ShopPanel01ViewModel                │
│  - ...                                 │
└────────────────────────────────────────┘
```

### 2. 类关系图

```
                    IGameService (接口)
                         │
                         │ 实现
                         ▼
               GameServiceBase (抽象基类)
                         │
           ┌─────────────┼─────────────┐
           │             │             │
           ▼             ▼             ▼
    LoginService   PlayerService   ShopService
    (具体实现)      (具体实现)      (具体实现)
```

### 3. 生命周期图

```
Service 生命周期：

GameMainController
       │
       │ GameServiceManager.InitializeAsync()
       ▼
GameServiceManager 创建 GameServiceUpdater (MonoBehaviour)
       │
       │ Register(Service)
       ▼
Service 被添加到 mServices 和 mPendingServices
       │
       │ InitializeAllAsync()
       ▼
并行初始化所有 Service：
  Service1.InitializeAsync() ──┐
  Service2.InitializeAsync() ──┼──→ UniTask.WhenAll()
  Service3.InitializeAsync() ──┘
       │
       │ 初始化完成
       ▼
Service.IsInitialized = true
       │
       │ 每帧 Update
       ▼
GameServiceUpdater.Update() 
  └─> Service.OnUpdate(deltaTime)
       │
       │ Release<T>() 或 ReleaseAll()
       ▼
Service.OnDestroy()
  └─> 清理资源、取消事件订阅
```

## 核心设计

### 1. 单例管理

```csharp
// Service 本身不是单例，通过 GameServiceManager 管理
public static class GameServiceManager
{
    private static readonly Dictionary<Type, IGameService> mServices = 
        new Dictionary<Type, IGameService>();
    
    // 通过类型获取，保证全局唯一
    public static T Get<T>() where T : class, IGameService
    {
        if (mServices.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }
        return null;
    }
}
```

**设计理由：**
- 解耦：Service 不依赖单例模式，便于测试
- 统一管理：GameServiceManager 控制所有 Service 生命周期
- 类型安全：泛型 API 保证编译时类型检查

### 2. 延迟初始化

```csharp
// 模式1：注册后统一初始化（推荐）
GameServiceManager.Register(new LoginService());
GameServiceManager.Register(new PlayerService());
await GameServiceManager.InitializeAllAsync(); // 并行初始化

// 模式2：懒加载（自动创建）
var service = GameServiceManager.GetOrCreate<LoginService>();
```

**设计理由：**
- 性能：并行初始化减少启动时间
- 控制：可以精确控制初始化时机
- 依赖：Service A 可以在初始化时使用 Service B

### 3. 异步初始化

```csharp
public interface IGameService
{
    UniTask InitializeAsync(); // 异步初始化
}

// 实现
public override async UniTask InitializeAsync()
{
    // 异步加载配置
    await LoadConfigAsync();
    
    // 异步加载数据
    await LoadDataAsync();
    
    // 标记初始化完成
    IsInitialized = true;
}
```

**设计理由：**
- 非阻塞：不阻塞主线程
- 可等待：可以等待特定 Service 初始化完成
- 异常处理：可以捕获和处理初始化错误

### 4. 事件驱动

```csharp
// Service 定义事件
public event Action OnLevelUp;
public event Action<int> OnExpChanged;

// ViewModel 订阅事件
public override void OnCreate()
{
    mPlayerService.OnLevelUp += OnLevelUp;
}

public override void OnDestroy()
{
    mPlayerService.OnLevelUp -= OnLevelUp; // 必须取消订阅！
}
```

**设计理由：**
- 解耦：Service 和 ViewModel 解耦
- 响应式：数据变化自动通知 UI
- 安全：必须在 OnDestroy 中取消订阅，防止内存泄漏

### 5. 数据持久化

```csharp
public class PlayerService : GameServiceBase
{
    // Service 层管理数据
    public PlayerData Data { get; private set; }
    
    public override async UniTask InitializeAsync()
    {
        // 加载数据
        Data = await LoadFromServerOrLocal();
        
        await base.InitializeAsync();
    }
    
    public async void SaveData()
    {
        // 保存数据
        await SaveToServerOrLocal(Data);
    }
}
```

**设计理由：**
- 数据归属：数据属于 Service，ViewModel 只是代理
- 持久化：Service 负责数据的加载和保存
- 缓存：Service 缓存数据，减少重复请求

## 详细设计

### 1. GameServiceManager 设计

```csharp
public static class GameServiceManager
{
    // 已注册的 Service 字典
    private static readonly Dictionary<Type, IGameService> mServices;
    
    // 待初始化的 Service 列表
    private static readonly List<IGameService> mPendingServices;
    
    // 更新器（用于每帧调用 Service.OnUpdate）
    private static GameServiceUpdater mUpdater;
    
    // ========== 注册方法 ==========
    
    /// <summary>
    /// 注册 Service 到待初始化列表
    /// </summary>
    public static void Register<T>(T service) where T : class, IGameService
    {
        mServices[typeof(T)] = service;
        mPendingServices.Add(service);
    }
    
    /// <summary>
    /// 统一初始化所有待初始化的 Service（并行）
    /// </summary>
    public static async UniTask InitializeAllAsync()
    {
        var tasks = new List<UniTask>();
        
        foreach (var service in mPendingServices)
        {
            tasks.Add(service.InitializeAsync());
        }
        
        await UniTask.WhenAll(tasks);
        mPendingServices.Clear();
    }
    
    // ========== 获取方法 ==========
    
    /// <summary>
    /// 获取已存在的 Service
    /// </summary>
    public static T Get<T>() where T : class, IGameService
    {
        return mServices.TryGetValue(typeof(T), out var s) ? (T)s : null;
    }
}
```

### 2. IGameService 接口设计

```csharp
public interface IGameService
{
    // ========== 属性 ==========
    string ServiceName { get; }           // Service 名称（用于日志）
    bool IsInitialized { get; }           // 初始化状态
    
    // ========== 生命周期 ==========
    UniTask InitializeAsync();            // 异步初始化
    void OnUpdate(float deltaTime);       // 每帧更新（可选）
    void OnDestroy();                     // 销毁清理
    void Reset();                         // 重置数据（切换账号）
}
```

**设计理由：**
- **InitializeAsync**: 支持异步加载（配置、数据）
- **OnUpdate**: 需要每帧更新的 Service（如计时器）
- **OnDestroy**: 清理资源，防止内存泄漏
- **Reset**: 切换账号时重置数据，不需要重新初始化

### 3. GameServiceBase 基类设计

```csharp
public abstract class GameServiceBase : IGameService
{
    // 默认使用类名作为 ServiceName
    public virtual string ServiceName => GetType().Name;
    
    // 初始化状态（protected set，子类可以修改）
    public bool IsInitialized { get; protected set; }
    
    /// <summary>
    /// 异步初始化（子类必须调用 base.InitializeAsync()）
    /// </summary>
    public virtual async UniTask InitializeAsync()
    {
        IsInitialized = true;
        Debug.Log($"[GameService] {ServiceName} 初始化完成");
        await UniTask.CompletedTask;
    }
    
    /// <summary>
    /// 每帧更新（可选重写）
    /// </summary>
    public virtual void OnUpdate(float deltaTime) { }
    
    /// <summary>
    /// 销毁清理（可选重写）
    /// </summary>
    public virtual void OnDestroy()
    {
        IsInitialized = false;
    }
    
    /// <summary>
    /// 重置数据（可选重写）
    /// </summary>
    public virtual void Reset()
    {
        // 默认实现：什么都不做
    }
}
```

**设计理由：**
- **默认实现**: 提供默认实现，减少子类代码
- **虚方法**: 允许子类重写特定方法
- **IsInitialized**: 自动管理初始化状态

### 4. GameServiceUpdater 设计

```csharp
/// <summary>
/// 用于每帧更新的 MonoBehaviour
/// </summary>
public class GameServiceUpdater : MonoBehaviour
{
    public Action<float> OnUpdateAction;
    
    void Update()
    {
        OnUpdateAction?.Invoke(Time.deltaTime);
    }
    
    void OnDestroy()
    {
        OnUpdateAction = null;
    }
}
```

**设计理由：**
- **MonoBehaviour**: 需要 Update 方法，但 GameServiceManager 是静态类
- **Action**: 委托给 GameServiceManager.OnUpdate
- **DontDestroyOnLoad**: 在 GameServiceManager.InitializeAsync 中设置

## 线程安全

### 设计决策

**GameServiceManager 不是线程安全的**，理由：
1. Unity 主线程：所有 Unity API 必须在主线程调用
2. UniTask：异步操作在 Unity 主线程执行
3. 简化设计：游戏逻辑通常在主线程

### 使用规范

```csharp
// 所有 Service 操作必须在主线程
async void SomeMethod()
{
    // ✅ 正确：UniTask 在 Unity 主线程执行
    await GameServiceManager.InitializeAllAsync();
    
    // ✅ 正确：获取 Service 在主线程
    var service = GameServiceManager.Get<LoginService>();
}

// ❌ 错误：在后台线程操作 Service
void WrongMethod()
{
    Task.Run(() => {
        var service = GameServiceManager.Get<LoginService>(); // 危险！
    });
}
```

## 性能优化

### 1. 初始化优化

```csharp
// 并行初始化（同时启动多个 Service）
public static async UniTask InitializeAllAsync()
{
    var tasks = new List<UniTask>();
    
    foreach (var service in mPendingServices)
    {
        tasks.Add(service.InitializeAsync()); // 不等待，直接加入列表
    }
    
    await UniTask.WhenAll(tasks); // 同时等待所有完成
}
```

**优化效果：**
- 串行：Service1(1s) → Service2(1s) → Service3(1s) = 3s
- 并行：Service1(1s) + Service2(1s) + Service3(1s) = 1s

### 2. Update 优化

```csharp
// GameServiceUpdater.Update
void OnUpdate(float deltaTime)
{
    foreach (var service in mServices.Values)
    {
        if (service.IsInitialized) // 只更新已初始化的
        {
            service.OnUpdate(deltaTime);
        }
    }
}
```

**优化：**
- 只更新已初始化的 Service
- 未初始化的 Service 不调用 OnUpdate

### 3. 缓存优化

```csharp
public class ShopService : GameServiceBase
{
    private List<ShopItemConfig> mCache; // 缓存商品配置
    
    public List<ShopItemConfig> GetShopItems(int shopId)
    {
        if (mCache == null)
        {
            mCache = LoadFromConfig(); // 只加载一次
        }
        return mCache;
    }
}
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
        // 不抛出异常，继续初始化其他 Service
    }
}
```

**设计：**
- 单个 Service 初始化失败不影响其他 Service
- 记录错误日志，便于调试

### 2. 运行时错误

```csharp
public class PlayerService : GameServiceBase
{
    public void AddExp(int amount)
    {
        try
        {
            Exp += amount;
            SaveData();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{ServiceName}] AddExp失败: {e}");
            // 可以通知用户或重试
        }
    }
}
```

## 扩展性

### 1. 添加新 Service

```csharp
// 1. 继承 GameServiceBase
public class MyService : GameServiceBase
{
    public override async UniTask InitializeAsync()
    {
        // 自定义初始化逻辑
        await base.InitializeAsync();
    }
}

// 2. 在 GameStartMono 中注册
GameServiceManager.Register(new MyService());

// 3. 在 ViewModel 中使用
var service = GameServiceManager.Get<MyService>();
```

### 2. Service 装饰器

```csharp
// 装饰器模式：为 Service 添加日志功能
public class LoggedService<T> : IGameService where T : IGameService
{
    private readonly T mInnerService;
    
    public async UniTask InitializeAsync()
    {
        Debug.Log($"[Log] {mInnerService.ServiceName} 开始初始化");
        await mInnerService.InitializeAsync();
        Debug.Log($"[Log] {mInnerService.ServiceName} 初始化完成");
    }
}
```

## 与 MVVM 的关系

```
View (UI) ────── ViewModel ────── Service ────── Data
   │                │               │             │
   │                │               │             │
Button ────── Command ─────── Business Logic ─── DB/Server
   │                │               │             │
Text ────── BindableProperty ─── Cache ─────── PlayerPrefs
```

**职责划分：**
- **View**: 显示 UI，用户交互
- **ViewModel**: UI 逻辑，数据绑定
- **Service**: 业务逻辑，数据管理
- **Data**: 数据来源（配置、服务器、本地存储）

## 总结

GameService 模块设计目标：
1. **解耦**：Service 和 View 解耦，便于独立开发和测试
2. **可复用**：Service 可被多个 ViewModel 使用
3. **可维护**：统一的生命周期管理，便于维护
4. **可扩展**：易于添加新的 Service
5. **高性能**：并行初始化，缓存优化

---

设计文档版本：1.0
最后更新：2026-03-19
作者：UIFramework Team
