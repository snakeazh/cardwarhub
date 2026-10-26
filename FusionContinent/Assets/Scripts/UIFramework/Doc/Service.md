# GameService 模块使用文档

## 概述

GameService 是 UIFramework 的服务层架构，用于管理游戏中的全局业务逻辑和数据。所有 Service 都是单例模式，通过 `GameServiceManager` 统一管理。

## 核心概念

### 1. Service 生命周期

```
Register（注册） → InitializeAsync（初始化） → Update（每帧更新） → OnDestroy（销毁）
```

### 2. 三种使用模式

#### 模式A：注册后统一初始化（推荐用于游戏启动）

```csharp
// 1. 注册所有 Service
GameServiceManager.Register(new LoginService());
GameServiceManager.Register(new PlayerService());
GameServiceManager.Register(new ShopService());

// 2. 统一初始化（并行）
await GameServiceManager.InitializeAllAsync();

// 3. 获取使用
var loginService = GameServiceManager.Get<LoginService>();
```

#### 模式B：注册并立即初始化

```csharp
// 注册并立即初始化
GameServiceManager.RegisterAndInitialize(new LoginService());

// 直接使用
var service = GameServiceManager.Get<LoginService>();
```

#### 模式C：自动创建（懒加载）

```csharp
// 如果不存在会自动创建并初始化
var service = GameServiceManager.GetOrCreate<LoginService>();
```

## 快速开始

### 1. 创建自定义 Service

```csharp
using UIFramework.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Services
{
    /// <summary>
    /// 玩家服务示例
    /// </summary>
    public class PlayerService : GameServiceBase
    {
        // 数据属性
        public string PlayerName { get; set; }
        public int Level { get; set; }
        public int Exp { get; set; }
        
        // 事件
        public event System.Action OnLevelUp;
        public event System.Action<int> OnExpChanged;
        
        public override async UniTask InitializeAsync()
        {
            // 异步加载玩家数据
            await LoadPlayerData();
            
            // 必须调用基类方法设置 IsInitialized = true
            await base.InitializeAsync();
        }
        
        private async UniTask LoadPlayerData()
        {
            // 从服务器或本地加载数据
            await UniTask.Delay(100); // 模拟网络延迟
            
            PlayerName = "Player" + Random.Range(1000, 9999);
            Level = 1;
            Exp = 0;
            
            Debug.Log($"[PlayerService] 玩家数据加载完成: {PlayerName}");
        }
        
        /// <summary>
        /// 添加经验值
        /// </summary>
        public void AddExp(int amount)
        {
            Exp += amount;
            OnExpChanged?.Invoke(Exp);
            
            // 检查升级
            if (Exp >= GetLevelUpExp())
            {
                LevelUp();
            }
        }
        
        private void LevelUp()
        {
            Level++;
            Exp = 0;
            OnLevelUp?.Invoke();
            Debug.Log($"[PlayerService] 升级了！当前等级: {Level}");
        }
        
        private int GetLevelUpExp()
        {
            return Level * 100;
        }
        
        public override void Reset()
        {
            // 切换账号时重置数据
            PlayerName = "";
            Level = 1;
            Exp = 0;
            Debug.Log("[PlayerService] 数据已重置");
        }
    }
}
```

### 2. 在游戏启动时初始化

```csharp
using Game.Services;
using UIFramework.Services;

public class GameMainController : MonoBehaviour
{
    async void Start()
    {
        // 1. 初始化 Service 管理器
        await GameServiceManager.InitializeAsync();
        
        // 2. 注册所有 Service
        RegisterAllServices();
        
        // 3. 统一初始化（并行）
        await GameServiceManager.InitializeAllAsync();
        
        Debug.Log("所有 Service 初始化完成！");
    }
    
    void RegisterAllServices()
    {
        // 核心 Service
        GameServiceManager.Register(new LoginService());
        GameServiceManager.Register(new PlayerService());
        GameServiceManager.Register(new InventoryService());
        GameServiceManager.Register(new ShopService());
        GameServiceManager.Register(new TaskService());
        
        // UI Service
        GameServiceManager.Register(new ToastService());
        GameServiceManager.Register(new LoadingService());
    }
}
```

### 3. 在 ViewModel 中使用

```csharp
using UIFramework;
using Game.Services;

public class PlayerViewModel : ViewModel
{
    public BindableProperty<string> PlayerName { get; } = new BindableProperty<string>("");
    public BindableProperty<int> Level { get; } = new BindableProperty<int>(1);
    public BindableProperty<int> Exp { get; } = new BindableProperty<int>(0);
    
    private PlayerService mPlayerService;
    
    public override void OnCreate()
    {
        // 获取 Service
        mPlayerService = GameServiceManager.Get<PlayerService>();
        
        // 订阅 Service 事件
        mPlayerService.OnLevelUp += OnLevelUp;
        mPlayerService.OnExpChanged += OnExpChanged;
        
        // 初始化数据
        RefreshData();
    }
    
    public override void OnDestroy()
    {
        // 取消订阅（重要！防止内存泄漏）
        mPlayerService.OnLevelUp -= OnLevelUp;
        mPlayerService.OnExpChanged -= OnExpChanged;
    }
    
    private void RefreshData()
    {
        PlayerName.Value = mPlayerService.PlayerName;
        Level.Value = mPlayerService.Level;
        Exp.Value = mPlayerService.Exp;
    }
    
    private void OnLevelUp()
    {
        Level.Value = mPlayerService.Level;
        Exp.Value = 0;
        
        // 显示升级提示
        GameServiceManager.Get<ToastService>()?.ShowToast($"升级了！当前等级: {Level.Value}");
    }
    
    private void OnExpChanged(int exp)
    {
        Exp.Value = exp;
    }
    
    // 命令
    public void AddExpCommand(int amount)
    {
        mPlayerService?.AddExp(amount);
    }
}
```

## API 参考

### GameServiceManager

#### 注册方法

```csharp
// 注册 Service（不立即初始化）
void Register<T>(T service) where T : class, IGameService

// 注册并立即初始化
void RegisterAndInitialize<T>(T service) where T : class, IGameService
```

#### 初始化方法

```csharp
// 初始化管理器（游戏启动时调用一次）
UniTask InitializeAsync()

// 统一初始化所有已注册的 Service（并行）
UniTask InitializeAllAsync()
```

#### 获取方法

```csharp
// 获取已存在的 Service（不存在返回 null）
T Get<T>() where T : class, IGameService

// 获取或创建 Service（不存在则自动创建）
T GetOrCreate<T>() where T : class, IGameService, new()

// 检查 Service 是否存在
bool Has<T>() where T : class, IGameService
```

#### 等待方法

```csharp
// 等待指定 Service 初始化完成
UniTask WaitForInitialized<T>() where T : class, IGameService

// 等待所有 Service 初始化完成
UniTask WaitForAllInitialized()
```

#### 释放方法

```csharp
// 释放指定 Service
void Release<T>() where T : class, IGameService

// 重置所有 Service 数据（切换账号）
void ResetAll()

// 销毁所有 Service（退出游戏）
void ReleaseAll()
```

#### 事件

```csharp
// 所有 Service 初始化完成事件
Action OnAllServicesInitialized
```

### IGameService 接口

```csharp
public interface IGameService
{
    string ServiceName { get; }                    // Service 名称
    bool IsInitialized { get; }                    // 是否已初始化
    
    UniTask InitializeAsync();                     // 异步初始化
    void OnUpdate(float deltaTime);                // 每帧更新
    void OnDestroy();                              // 销毁清理
    void Reset();                                  // 重置数据
}
```

### GameServiceBase 基类

```csharp
public abstract class GameServiceBase : IGameService
{
    public virtual string ServiceName => GetType().Name;
    public bool IsInitialized { get; protected set; }
    
    public virtual UniTask InitializeAsync();      // 初始化
    public virtual void OnUpdate(float deltaTime); // 更新
    public virtual void OnDestroy();               // 销毁
    public virtual void Reset();                   // 重置
    
    protected void NotifyDataChanged();            // 触发数据变更事件
    public event Action OnDataChanged;
}
```

## 最佳实践

### 1. Service 职责划分

- **单一职责**：每个 Service 只负责一个业务领域
- **数据持久化**：Service 负责数据的加载、保存、缓存
- **全局状态**：Service 保存全局状态，ViewModel 只是代理

### 2. 事件使用

```csharp
// 在 Service 中定义事件
public event Action OnDataChanged;
public event Action<int> OnExpChanged;

// 触发事件
protected void NotifyDataChanged()
{
    OnDataChanged?.Invoke();
}
```

### 3. 异步处理

```csharp
public override async UniTask InitializeAsync()
{
    // 异步加载数据
    var data = await LoadDataFromServer();
    
    // 处理数据
    ProcessData(data);
    
    // 必须调用基类方法
    await base.InitializeAsync();
}
```

### 4. 错误处理

```csharp
public override async UniTask InitializeAsync()
{
    try
    {
        await LoadData();
        await base.InitializeAsync();
    }
    catch (Exception e)
    {
        Debug.LogError($"[{ServiceName}] 初始化失败: {e.Message}");
        // 可以设置默认值或重试
    }
}
```

### 5. 内存管理

```csharp
public override void OnDestroy()
{
    // 取消所有事件订阅
    OnDataChanged = null;
    OnExpChanged = null;
    
    // 清理缓存
    cache.Clear();
    
    base.OnDestroy();
}
```

## 常见问题

### Q: Service 和 ViewModel 的区别？

**A:** 
- **Service**: 长期存在，管理业务逻辑和数据，全局单例
- **ViewModel**: 短期存在，管理 UI 逻辑，与 Panel 生命周期绑定

### Q: 什么时候使用 Service？

**A:**
- 数据需要跨界面共享
- 业务逻辑复杂，需要独立管理
- 需要全局状态（如玩家信息、背包数据）
- 需要后台运行（如计时器、网络监听）

### Q: 如何避免 Service 过于臃肿？

**A:**
- 按功能拆分（PlayerService, InventoryService, ShopService）
- 使用事件解耦（Service 触发事件，ViewModel 监听）
- 复杂逻辑使用子系统（Service 内部再分层）

### Q: Service 之间如何通信？

**A:**
```csharp
// 方式1: 直接调用（简单依赖）
public class TaskService : GameServiceBase
{
    private PlayerService mPlayerService;
    
    public override async UniTask InitializeAsync()
    {
        mPlayerService = GameServiceManager.Get<PlayerService>();
        await base.InitializeAsync();
    }
}

// 方式2: 事件总线（解耦）
public class EventBus : GameServiceBase
{
    public void Publish(string eventName, object data);
    public void Subscribe(string eventName, Action<object> callback);
}
```

## 示例 Service 列表

| Service | 职责 | 数据 |
|---------|------|------|
| LoginService | 登录逻辑 | Token, 用户信息 |
| PlayerService | 玩家数据 | 等级, 经验, 属性 |
| InventoryService | 背包管理 | 物品列表 |
| ShopService | 商店逻辑 | 商品列表, 购买记录 |
| TaskService | 任务系统 | 任务列表, 进度 |
| ToastService | 提示管理 | 提示队列 |
| LoadingService | 加载管理 | 加载进度 |

---

更多示例请参考 `/Assets/Scripts/Game/Services/` 目录。
