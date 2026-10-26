# GameService 框架使用指南

基于 **Service 层架构** 的完整游戏框架，Service 长生命周期管理数据，ViewModel 作为代理层，View 纯显示。

---

## 架构图

```
┌────────────────────────────────────────────────────────────────┐
│                           View 层                               │
│  - MonoBehaviour，负责显示和输入                                │
│  - 通过 BindableProperty 绑定 ViewModel                         │
│  - 生命周期：随界面打开创建，关闭销毁                            │
└────────────────────────────────────────────────────────────────┘
                              ↑↓ 数据绑定
┌────────────────────────────────────────────────────────────────┐
│                        ViewModel 层                             │
│  - 纯 C#，不存储数据                                            │
│  - 持有 Service 引用，调用 Service 方法                          │
│  - 将 Service 数据转换为 BindableProperty                       │
│  - 生命周期：随 View 创建销毁                                    │
└────────────────────────────────────────────────────────────────┘
                              ↑↓ 业务调用
┌────────────────────────────────────────────────────────────────┐
│                        Service 层                               │
│  - 业务逻辑 + 数据管理                                           │
│  - 单例，长生命周期（不随界面销毁）                              │
│  - 与服务器通信、数据缓存、业务计算                              │
│  - 提供事件供 ViewModel 订阅                                     │
└────────────────────────────────────────────────────────────────┘
```

---

## 核心优势

| 特性 | 说明 |
|------|------|
| **数据持久** | Service 单例，关闭界面数据不丢失 |
| **自动同步** | Service 数据变化，所有绑定界面自动更新 |
| **职责清晰** | Service 管业务，ViewModel 管绑定，View 管显示 |
| **易于测试** | 可单独测试 Service，无需创建 UI |
| **灵活复用** | 多个界面共用同一个 Service |

---

## 快速开始

### 1. 游戏入口初始化

```csharp
public class GameMainController : MonoBehaviour
{
    async void Start()
    {
        // 1. 初始化UIFramework
        UIManager.Initialize();
        
        // 2. 初始化Service系统
        await GameServiceManager.InitializeAsync();
        
        // 3. 加载核心Service
        var playerService = GameServiceManager.Get<PlayerService>();
        await WaitForService(playerService);
        
        // 4. 打开游戏界面
        OpenGameUI();
    }
}
```

### 2. 创建 Service

```csharp
public class MyService : GameServiceBase
{
    public override string ServiceName => "MyService";
    
    // 数据存储在Service
    private MyData mData;
    
    // 事件供ViewModel订阅
    public event Action OnDataChanged;
    
    public override async Task InitializeAsync()
    {
        // 加载数据
        mData = await LoadDataFromServer();
        await base.InitializeAsync();
    }
    
    // 业务方法
    public void DoSomething()
    {
        // 修改数据
        mData.SomeValue++;
        
        // 通知订阅者
        OnDataChanged?.Invoke();
    }
}
```

### 3. 创建 ViewModel

```csharp
public class MyViewModel : ViewModel
{
    // 属性（从Service同步）
    public BindableProperty<int> SomeValue { get; } = new BindableProperty<int>(0);
    
    // 命令
    public Command DoSomethingCommand { get; private set; }
    
    private MyService mService;
    
    public override void OnCreate()
    {
        // 获取Service
        mService = GameServiceManager.Get<MyService>();
        
        // 订阅Service事件
        mService.OnDataChanged += SyncFromService;
        
        // 创建命令
        DoSomethingCommand = new Command(OnDoSomething);
        
        // 初始同步
        SyncFromService();
    }
    
    public override void OnDestroy()
    {
        // 取消订阅（必须！）
        mService.OnDataChanged -= SyncFromService;
    }
    
    private void SyncFromService()
    {
        // 从Service同步数据到BindableProperty
        SomeValue.Value = mService.GetData().SomeValue;
    }
    
    private void OnDoSomething()
    {
        // 调用Service方法
        mService.DoSomething();
    }
}
```

### 4. 创建 View

```csharp
public class MyPanel : UIPanel<MyViewModel>
{
    public Text ValueText;
    public Button ActionBtn;
    
    protected override void OnBindViewModel(MyViewModel vm)
    {
        // 绑定属性
        UIBind.Text(ValueText, vm.SomeValue);
        
        // 绑定命令
        UIBind.Click(ActionBtn, vm.DoSomethingCommand);
    }
    
    protected override void OnUnbindViewModel(MyViewModel vm) { }
}
```

### 5. 使用

```csharp
// 打开界面
var vm = new MyViewModel();
UIManager.OpenPanel<MyPanel, MyViewModel>(vm).Open();

// 关闭界面（ViewModel销毁，但Service数据保留）
UIManager.ClosePanel<MyPanel>();

// 重新打开（数据还在）
var vm2 = new MyViewModel();  // 新的ViewModel
UIManager.OpenPanel<MyPanel, MyViewModel>(vm2).Open();
// vm2.SomeValue.Value 会自动显示之前修改的值
```

---

## 关键概念

### Service 单例

```csharp
// 任意地方获取同一个实例
var service1 = GameServiceManager.Get<PlayerService>();
var service2 = GameServiceManager.Get<PlayerService>();
// service1 == service2
```

### 数据自动同步

```csharp
// 修改Service数据
playerService.AddGold(100);

// 所有使用PlayerViewModel的界面自动更新：
// - HUD上的金币显示 +100
// - 角色面板上的金币显示 +100
// - 商店界面显示最新金币数
```

### 事件订阅与取消

```csharp
public override void OnCreate()
{
    // 订阅
    mService.OnDataChanged += OnDataChanged;
}

public override void OnDestroy()
{
    // 必须取消订阅，防止内存泄漏！
    mService.OnDataChanged -= OnDataChanged;
}
```

---

## 文件结构

```
Assets/Scripts/
├── UIFramework/
│   ├── Core/
│   │   ├── UIPanel.cs
│   │   ├── UIManager.cs
│   │   ├── BindableProperty.cs
│   │   └── UIBind.cs
│   ├── Services/
│   │   ├── GameServiceManager.cs    # Service管理器
│   │   └── GameServices.cs          # 具体Service实现
│   ├── ViewModels/
│   │   └── GameViewModels.cs        # 使用Service的ViewModel
│   └── Panels/                      # View层（自行创建）
│       ├── HUDPanel.cs
│       ├── InventoryPanel.cs
│       └── ...
└── GameMainController.cs            # 游戏入口
```

---

## 生命周期对比

| 层级 | 存储数据 | 生命周期 | 场景切换 | 切换账号 |
|------|---------|----------|----------|----------|
| **View** | 否 | 随界面开关 | 销毁 | 销毁 |
| **ViewModel** | 否 | 随View创建销毁 | 销毁 | 销毁 |
| **Service** | **是** | **游戏期间常驻** | **保留** | **重置** |

---

## 注意事项

### 1. 必须取消事件订阅

```csharp
public override void OnDestroy()
{
    // ❌ 错误：不取消订阅会导致内存泄漏
    
    // ✅ 正确：必须取消所有订阅
    mService.OnDataChanged -= OnDataChanged;
    mService.OnSomeEvent -= OnSomeEvent;
    
    base.OnDestroy();
}
```

### 2. Service初始化是异步的

```csharp
// ❌ 错误：立即访问可能数据还未加载
var service = GameServiceManager.Get<PlayerService>();
int gold = service.Data.Gold;  // Data可能为null

// ✅ 正确：等待初始化完成
await GameServiceManager.WaitForInitialized<PlayerService>();
// 或
while (!service.IsInitialized) await Task.Delay(100);
int gold = service.Data.Gold;
```

### 3. Service数据修改必须通过Service方法

```csharp
// ❌ 错误：直接修改Model
playerService.Data.Gold += 100;

// ✅ 正确：通过Service方法
playerService.AddGold(100);  // 内部会触发事件
```

### 4. ViewModel只做代理，不存储数据

```csharp
// ❌ 错误：ViewModel自己存储数据
public class BadViewModel : ViewModel
{
    public BindableProperty<int> Gold { get; } = new BindableProperty<int>(0);
}

// ✅ 正确：ViewModel从Service读取
public class GoodViewModel : ViewModel
{
    public BindableProperty<int> Gold { get; } = new BindableProperty<int>(0);
    
    void Sync()
    {
        Gold.Value = service.Data.Gold;  // 从Service读取
    }
}
```

---

## 进阶用法

### 多Service协作

```csharp
public class ShopViewModel : ViewModel
{
    private PlayerService mPlayerService;
    private InventoryService mInventoryService;
    
    void BuyItem(ItemData item)
    {
        // 扣除金币（PlayerService）
        if (mPlayerService.ConsumeGold(item.Price))
        {
            // 添加到背包（InventoryService）
            mInventoryService.AddItem(item);
        }
    }
}
```

### Service之间的事件通信

```csharp
public class TaskService : GameServiceBase
{
    public void CompleteTask(int taskId)
    {
        // 发送奖励
        var playerService = GameServiceManager.Get<PlayerService>();
        playerService.AddGold(100);
        
        // 添加物品
        var inventoryService = GameServiceManager.Get<InventoryService>();
        inventoryService.AddItem(rewardItem);
    }
}
```

### 切换账号重置

```csharp
void OnSwitchAccount()
{
    // 关闭所有界面
    UIManager.CloseAllPanels();
    
    // 重置所有Service
    GameServiceManager.ResetAll();
    
    // 重新加载
    GameServiceManager.Get<PlayerService>().InitializeAsync();
}
```

---

## 调试技巧

```csharp
// 查看所有Service状态
var infos = GameServiceManager.GetAllServiceInfo();
foreach (var info in infos)
{
    Debug.Log($"Service: {info.Name}, 初始化: {info.IsInitialized}");
}

// 在Inspector中查看（可扩展）
```

---

**总结**：Service 层架构让数据与界面彻底解耦，数据存在 Service，界面只是数据的「显示器」，关闭再打开数据依然在！
