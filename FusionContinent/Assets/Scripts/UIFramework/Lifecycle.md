# UIFramework 生命周期详解

本文详细讲解 Model、ViewModel、View 三层的生命周期管理，帮助你更好地理解和使用 UIFramework。

---

## 架构概览

```
┌─────────────────────────────────────────────────────────────┐
│                         View 层                              │
│  - Unity MonoBehaviour                                       │
│  - 负责显示和输入                                            │
│  - 生命周期：创建 → 初始化 → 打开 → 显示/隐藏 → 关闭 → 销毁   │
└─────────────────────────────────────────────────────────────┘
                              ↑↓ 数据绑定
┌─────────────────────────────────────────────────────────────┐
│                      ViewModel 层                            │
│  - 纯 C# 类，不依赖 Unity                                     │
│  - 业务逻辑和状态管理                                         │
│  - 生命周期：创建 → 初始化 → 活跃 → 销毁                      │
└─────────────────────────────────────────────────────────────┘
                              ↑↓ 数据操作
┌─────────────────────────────────────────────────────────────┐
│                        Model 层                              │
│  - 数据结构（DTO/Entity）                                     │
│  - 原始数据，无逻辑                                           │
│  - 生命周期：由业务决定                                        │
└─────────────────────────────────────────────────────────────┘
```

---

## 一、Model 生命周期

### 1.1 什么是 Model？

Model 是**纯数据结构**，代表业务实体：

```csharp
// Model 示例 - 只是数据容器
public class PlayerModel
{
    public int Id;
    public string Name;
    public int Level;
    public int Gold;
    public List<int> Items;
}

public class ItemModel
{
    public int Id;
    public string Name;
    public int Price;
    public string IconPath;
}
```

### 1.2 Model 的生命周期特点

| 特性 | 说明 |
|------|------|
| **创建时机** | 从服务器获取、从本地加载、程序生成 |
| **存储位置** | 内存、本地存储、服务器数据库 |
| **生命周期** | 随业务逻辑存在，与界面无关 |
| **销毁时机** | 不再需要时（如退出游戏、切换账号） |

### 1.3 Model 的生命周期阶段

```csharp
// 1. 创建（从服务器获取）
PlayerModel player = await GameServer.GetPlayerData(playerId);

// 2. 使用（传递给 ViewModel）
var vm = new PlayerViewModel();
vm.Initialize(player);

// 3. 修改（业务操作）
player.Gold += 100;
await GameServer.SavePlayerData(player);

// 4. 销毁（不再需要）
player = null;  // 等待 GC
```

### 1.4 Model 管理建议

```csharp
// 单例数据管理器
public class GameDataManager
{
    public static GameDataManager Instance { get; } = new GameDataManager();
    
    // 缓存的 Model 数据
    public PlayerModel CurrentPlayer { get; private set; }
    public List<ItemModel> AllItems { get; private set; }
    
    // 加载数据（游戏启动时）
    public async Task LoadData()
    {
        CurrentPlayer = await LoadPlayerFromServer();
        AllItems = await LoadItemsFromConfig();
    }
    
    // 保存数据
    public async Task SaveData()
    {
        if (CurrentPlayer != null)
        {
            await SavePlayerToServer(CurrentPlayer);
        }
    }
    
    // 清理数据（切换账号、退出游戏）
    public void ClearData()
    {
        CurrentPlayer = null;
        AllItems = null;
    }
}
```

---

## 二、ViewModel 生命周期

### 2.1 ViewModel 的生命周期阶段

```
创建 (new)
    ↓
OnCreate() - 初始化命令、订阅事件
    ↓
活跃期 - 数据变更、命令执行
    ↓
OnDestroy() - 清理资源、取消订阅
    ↓
销毁 (GC)
```

### 2.2 完整生命周期示例

```csharp
public class ShopViewModel : ViewModel
{
    // ========== 阶段 1: 属性定义 ==========
    public BindableProperty<int> Gold { get; } = new BindableProperty<int>(0);
    public BindableProperty<List<ShopItem>> Items { get; } = new BindableProperty<List<ShopItem>>(new List<ShopItem>());
    
    public Command RefreshCommand { get; private set; }
    public Command<ShopItem> BuyCommand { get; private set; }
    
    // 内部数据
    private PlayerModel mPlayerModel;
    private List<ItemModel> mAllItems;
    
    // ========== 阶段 2: 创建时初始化 ==========
    public override void OnCreate()
    {
        base.OnCreate();
        
        // 1. 初始化命令
        RefreshCommand = new Command(OnRefresh, CanRefresh);
        BuyCommand = new Command<ShopItem>(OnBuy, CanBuy);
        
        // 2. 订阅全局事件
        GameEvents.OnPlayerDataChanged += OnPlayerDataChanged;
        
        Debug.Log("[ShopViewModel] OnCreate - ViewModel 已创建");
    }
    
    // ========== 阶段 3: 数据初始化（手动调用）==========
    public void Initialize(PlayerModel player, List<ItemModel> items)
    {
        mPlayerModel = player;
        mAllItems = items;
        
        // 设置初始值
        Gold.Value = player.Gold;
        Items.Value = ConvertToShopItems(items);
        
        Debug.Log($"[ShopViewModel] Initialize - 数据已加载，金币: {Gold.Value}");
    }
    
    // ========== 阶段 4: 活跃期（业务逻辑）==========
    private bool CanRefresh() => !IsLoading;
    private bool IsLoading = false;
    
    private async void OnRefresh()
    {
        IsLoading = true;
        RefreshCommand.RaiseCanExecuteChanged();
        
        // 从服务器刷新数据
        mAllItems = await GameServer.GetShopItems();
        Items.Value = ConvertToShopItems(mAllItems);
        
        IsLoading = false;
        RefreshCommand.RaiseCanExecuteChanged();
    }
    
    private bool CanBuy(ShopItem item) => Gold.Value >= item.Price;
    
    private async void OnBuy(ShopItem item)
    {
        if (Gold.Value < item.Price) return;
        
        // 发送购买请求
        var result = await GameServer.BuyItem(item.Id);
        if (result.Success)
        {
            // 更新 Model
            mPlayerModel.Gold -= item.Price;
            mPlayerModel.Items.Add(item.Id);
            
            // 更新 ViewModel（自动通知 View）
            Gold.Value = mPlayerModel.Gold;
            item.IsSold = true;
            Items.Value = new List<ShopItem>(Items.Value); // 触发更新
            
            // 保存 Model
            await GameDataManager.Instance.SaveData();
        }
    }
    
    private void OnPlayerDataChanged(PlayerModel player)
    {
        // 响应全局数据变化
        if (player.Id == mPlayerModel?.Id)
        {
            Gold.Value = player.Gold;
        }
    }
    
    // ========== 阶段 5: 销毁前清理 ==========
    public override void OnDestroy()
    {
        base.OnDestroy();
        
        // 1. 取消事件订阅（防止内存泄漏）
        GameEvents.OnPlayerDataChanged -= OnPlayerDataChanged;
        
        // 2. 清理数据
        mPlayerModel = null;
        mAllItems = null;
        
        Debug.Log("[ShopViewModel] OnDestroy - ViewModel 已销毁");
    }
}
```

### 2.3 ViewModel 生命周期管理建议

```csharp
public class ViewModelManager
{
    private static readonly Dictionary<Type, ViewModel> mActiveViewModels = new Dictionary<Type, ViewModel>();
    
    // 创建或获取已存在的 ViewModel
    public static T GetOrCreateViewModel<T>() where T : ViewModel, new()
    {
        if (!mActiveViewModels.TryGetValue(typeof(T), out var vm))
        {
            vm = new T();
            vm.OnCreate();
            mActiveViewModels[typeof(T)] = vm;
        }
        return (T)vm;
    }
    
    // 销毁 ViewModel
    public static void DestroyViewModel<T>() where T : ViewModel
    {
        if (mActiveViewModels.TryGetValue(typeof(T), out var vm))
        {
            vm.OnDestroy();
            mActiveViewModels.Remove(typeof(T));
        }
    }
    
    // 清理所有 ViewModel（退出游戏、切换场景）
    public static void ClearAll()
    {
        foreach (var vm in mActiveViewModels.Values)
        {
            vm.OnDestroy();
        }
        mActiveViewModels.Clear();
    }
}
```

---

## 三、View 生命周期

### 3.1 View 的生命周期阶段

```
Prefab 加载
    ↓
Instantiate 实例化
    ↓
Init() - 初始化（绑定 ViewModel）
    ↓
Open() - 打开（显示、播放动画）
    ↓
Show/Hide - 显示/隐藏（多次）
    ↓
Close() - 关闭（隐藏、播放动画）
    ↓
Destroy - 销毁
```

### 3.2 View 完整生命周期示例

```csharp
public class ShopPanel : UIPanel<ShopViewModel>
{
    // UI 组件
    public Text TitleText;
    public Text GoldText;
    public Button CloseBtn;
    public Transform ItemContainer;
    public GameObject ItemPrefab;
    public Animator PanelAnimator;
    
    // 运行时数据
    private List<GameObject> mItemObjects = new List<GameObject>();
    
    // ========== 阶段 1: 初始化（绑定 ViewModel）==========
    protected override void OnInit(IUIData uiData)
    {
        base.OnInit(uiData);
        
        // 1. 绑定属性（建立数据驱动）
        BindProperties();
        
        // 2. 绑定命令
        BindCommands();
        
        // 3. 订阅 ViewModel 事件
        VM.Items.OnValueChanged += OnItemsChanged;
        
        Debug.Log("[ShopPanel] OnInit - Panel 已初始化");
    }
    
    private void BindProperties()
    {
        // 金币文本
        UIBind.Text(GoldText, VM.Gold, "金币: {0}");
        
        // 标题
        UIBind.Text(TitleText, VM.ShopTitle);
        
        // 加载状态
        UIBind.Active(LoadingObj, VM.IsLoading);
    }
    
    private void BindCommands()
    {
        CloseBtn.onClick.AddListener(() => VM.CloseCommand.Execute());
    }
    
    // ========== 阶段 2: 打开（显示）==========
    protected override void OnOpen(IUIData uiData)
    {
        base.OnOpen(uiData);
        
        // 1. 播放打开动画
        if (PanelAnimator != null)
        {
            PanelAnimator.SetTrigger("Open");
        }
        
        // 2. 刷新列表
        RefreshItemList();
        
        // 3. 请求刷新数据
        VM.RefreshCommand.Execute();
        
        Debug.Log("[ShopPanel] OnOpen - Panel 已打开");
    }
    
    // ========== 阶段 3: 显示/隐藏（可多次）==========
    protected override void OnShow()
    {
        base.OnShow();
        
        // 暂停游戏（如果是全屏界面）
        Time.timeScale = 0;
        
        Debug.Log("[ShopPanel] OnShow - Panel 显示");
    }
    
    protected override void OnHide()
    {
        base.OnHide();
        
        // 恢复游戏
        Time.timeScale = 1;
        
        Debug.Log("[ShopPanel] OnHide - Panel 隐藏");
    }
    
    // ========== 阶段 4: 数据变更处理 ==========
    private void OnItemsChanged(List<ShopItem> oldItems, List<ShopItem> newItems)
    {
        RefreshItemList();
    }
    
    private void RefreshItemList()
    {
        // 清除旧项
        foreach (var obj in mItemObjects)
        {
            Destroy(obj);
        }
        mItemObjects.Clear();
        
        // 创建新项
        foreach (var item in VM.Items.Value)
        {
            var obj = Instantiate(ItemPrefab, ItemContainer);
            var slot = obj.GetComponent<ItemSlot>();
            slot.Setup(item, VM.BuyCommand);
            mItemObjects.Add(obj);
        }
    }
    
    // ========== 阶段 5: 关闭 ==========
    protected override void OnClose()
    {
        base.OnClose();
        
        // 1. 取消事件订阅
        VM.Items.OnValueChanged -= OnItemsChanged;
        
        // 2. 清理按钮监听
        CloseBtn.onClick.RemoveAllListeners();
        
        // 3. 播放关闭动画（如果有）
        if (PanelAnimator != null)
        {
            PanelAnimator.SetTrigger("Close");
        }
        
        Debug.Log("[ShopPanel] OnClose - Panel 已关闭");
    }
}
```

### 3.3 View 与 ViewModel 的绑定关系

```csharp
// 情况 1: 一个 View 对应一个 ViewModel（最常见）
public class ShopPanel : UIPanel<ShopViewModel>
{
    protected override void OnBindViewModel(ShopViewModel vm)
    {
        // 直接绑定 VM 的属性
        UIBind.Text(GoldText, vm.Gold);
    }
}

// 情况 2: 多个 View 共享一个 ViewModel
public class PlayerInfoPanel : UIPanel<PlayerViewModel>
{
    // 显示玩家信息
}

public class PlayerEquipPanel : UIPanel<PlayerViewModel>
{
    // 显示玩家装备，使用同一个 PlayerViewModel
}

// 打开时传入同一个 ViewModel
var playerVM = ViewModelManager.GetOrCreateViewModel<PlayerViewModel>();

UIManager.OpenPanel<PlayerInfoPanel, PlayerViewModel>(playerVM).Open();
UIManager.OpenPanel<PlayerEquipPanel, PlayerViewModel>(playerVM).Open();
```

---

## 四、三层协作流程

### 4.1 完整流程示例：购买商品

```
用户点击"购买"按钮
        ↓
┌──────────────┐
│   View 层    │  BuyButton.onClick → VM.BuyCommand.Execute(item)
│  ShopPanel   │
└──────────────┘
        ↓
┌──────────────┐
│ ViewModel 层 │  1. 检查金币（VM.Gold.Value >= item.Price）
│ ShopViewModel│  2. 发送购买请求到服务器
│              │  3. 更新 Model（PlayerModel.Gold -= price）
│              │  4. 更新绑定属性（Gold.Value = newValue）
└──────────────┘
        ↓
┌──────────────┐
│   Model 层   │  数据被修改，等待保存
│ PlayerModel  │
└──────────────┘
        ↓
┌──────────────┐
│ ViewModel 层 │  Gold.Value 变更触发 OnValueChanged
│              │  自动通知所有绑定的 View
└──────────────┘
        ↓
┌──────────────┐
│   View 层    │  GoldText.text 自动更新为新的金币数
│  ShopPanel   │
└──────────────┘
        ↓
用户看到金币减少，购买成功
```

### 4.2 代码实现

```csharp
// ========== View 层 ==========
public class ShopItemSlot : MonoBehaviour
{
    public Button BuyBtn;
    public Text PriceText;
    private ShopItem mItem;
    
    public void Setup(ShopItem item, Command<ShopItem> buyCommand)
    {
        mItem = item;
        PriceText.text = $"{item.Price}G";
        
        // 点击按钮执行命令
        BuyBtn.onClick.AddListener(() => {
            buyCommand.Execute(mItem);
        });
    }
}

// ========== ViewModel 层 ==========
public class ShopViewModel : ViewModel
{
    public BindableProperty<int> Gold { get; } = new BindableProperty<int>(0);
    public Command<ShopItem> BuyCommand { get; private set; }
    
    private PlayerModel mPlayer;
    
    public override void OnCreate()
    {
        BuyCommand = new Command<ShopItem>(OnBuy, CanBuy);
    }
    
    private bool CanBuy(ShopItem item) => Gold.Value >= item.Price;
    
    private async void OnBuy(ShopItem item)
    {
        // 调用服务器 API
        var result = await GameServer.BuyItem(item.Id);
        
        if (result.Success)
        {
            // 修改 Model
            mPlayer.Gold -= item.Price;
            
            // 更新 ViewModel（自动通知 View）
            Gold.Value = mPlayer.Gold;
            
            // 保存 Model
            await GameDataManager.Instance.SaveData();
        }
    }
}

// ========== Model 层 ==========
public class PlayerModel
{
    public int Id;
    public string Name;
    public int Gold;  // 被 ViewModel 修改
    public List<int> Items;
}
```

---

## 五、生命周期最佳实践

### 5.1 ViewModel 不应该做什么

```csharp
public class BadViewModel : ViewModel
{
    // ❌ 错误：ViewModel 不应该直接操作 GameObject
    public GameObject EffectPrefab;
    
    // ❌ 错误：ViewModel 不应该调用 Unity 的 Instantiate
    public void CreateEffect()
    {
        Object.Instantiate(EffectPrefab);  // 错误！
    }
    
    // ❌ 错误：ViewModel 不应该知道 View 的存在
    public ShopPanel Panel;
    
    public void UpdatePanel()
    {
        Panel.UpdateUI();  // 错误！
    }
}

public class GoodViewModel : ViewModel
{
    // ✅ 正确：ViewModel 只管理数据和业务逻辑
    public BindableProperty<bool> ShouldShowEffect { get; } = new BindableProperty<bool>(false);
    
    public void TriggerEffect()
    {
        // 通过属性通知 View，让 View 自己处理特效
        ShouldShowEffect.Value = true;
    }
}
```

### 5.2 View 不应该做什么

```csharp
public class BadView : UIPanel<ShopViewModel>
{
    // ❌ 错误：View 不应该直接修改 Model
    protected override void OnBindViewModel(ShopViewModel vm)
    {
        BuyBtn.onClick.AddListener(() => {
            // 直接修改 Model，绕过 ViewModel
            GameDataManager.Instance.CurrentPlayer.Gold -= 100;  // 错误！
        });
    }
    
    // ❌ 错误：View 不应该包含业务逻辑
    private void CalculateDiscount()
    {
        // 折扣计算应该放在 ViewModel
        float discount = 0.8f;  // 错误！
    }
}

public class GoodView : UIPanel<ShopViewModel>
{
    // ✅ 正确：View 只负责显示和输入转发
    protected override void OnBindViewModel(ShopViewModel vm)
    {
        // 绑定命令，让 ViewModel 处理业务逻辑
        UIBind.Click(BuyBtn, vm.BuyCommand);
    }
}
```

### 5.3 资源释放检查清单

```csharp
public class MyPanel : UIPanel<MyViewModel>
{
    protected override void OnClose()
    {
        // ✅ 取消所有事件订阅
        VM.SomeProperty.OnValueChanged -= OnValueChanged;
        GameEvents.OnSomething -= OnSomething;
        
        // ✅ 取消所有协程
        StopAllCoroutines();
        
        // ✅ 清理 DOTween/Tween 动画
        transform.DOKill();
        
        // ✅ 移除所有按钮监听
        MyBtn.onClick.RemoveAllListeners();
        
        // ✅ 释放 Texture/Sprite（如果是动态加载的）
        if (DynamicSprite != null)
        {
            Destroy(DynamicSprite);
        }
        
        base.OnClose();
    }
}
```

### 5.4 避免内存泄漏

```csharp
public class MemorySafeViewModel : ViewModel
{
    // ❌ 错误：闭包捕获可能导致内存泄漏
    public void BadPractice()
    {
        GameServer.OnDataReceived += (data) => {
            // 这个闭包会持有对 ViewModel 的引用
            ProcessData(data);
        };
    }
    
    // ✅ 正确：保存委托引用，便于取消订阅
    private Action<Data> mOnDataReceivedHandler;
    
    public override void OnCreate()
    {
        mOnDataReceivedHandler = OnDataReceived;
        GameServer.OnDataReceived += mOnDataReceivedHandler;
    }
    
    public override void OnDestroy()
    {
        GameServer.OnDataReceived -= mOnDataReceivedHandler;
    }
    
    private void OnDataReceived(Data data)
    {
        ProcessData(data);
    }
}
```

---

## 六、常见问题

### Q: ViewModel 何时销毁？

**A:** 
- **方案 1**（推荐）：View 关闭时销毁对应的 ViewModel
- **方案 2**：ViewModel 独立于 View，手动管理生命周期
- **方案 3**：使用 ViewModelManager 缓存和复用

```csharp
// 方案 1：View 关闭时销毁 ViewModel
public class MyPanel : UIPanel<MyViewModel>
{
    protected override void OnClose()
    {
        VM.OnDestroy();  // 清理 ViewModel
        base.OnClose();
    }
}
```

### Q: 多个界面如何共享数据？

**A:** 使用单例的 Model 或全局 ViewModel

```csharp
// 共享 PlayerModel
public class PlayerDataManager
{
    public static PlayerModel CurrentPlayer { get; private set; }
}

// 两个界面使用同一个 Model
public class PlayerInfoPanel : UIPanel<PlayerInfoViewModel> { }
public class PlayerEquipPanel : UIPanel<PlayerEquipViewModel> { }

// 各自的 ViewModel 都引用同一个 Model
public class PlayerInfoViewModel : ViewModel
{
    public void Initialize()
    {
        Player = PlayerDataManager.CurrentPlayer;
    }
}
```

### Q: 界面打开时数据还没准备好怎么办？

**A:** 使用异步初始化和加载状态

```csharp
public class ShopViewModel : ViewModel
{
    public BindableProperty<bool> IsLoading { get; } = new BindableProperty<bool>(true);
    public BindableProperty<List<ShopItem>> Items { get; } = new BindableProperty<List<ShopItem>>();
    
    public async Task InitializeAsync()
    {
        IsLoading.Value = true;
        
        var items = await GameServer.GetShopItems();
        Items.Value = items;
        
        IsLoading.Value = false;
    }
}

// View 中显示加载状态
protected override void OnBindViewModel(ShopViewModel vm)
{
    UIBind.Active(LoadingSpinner, vm.IsLoading);
    UIBind.Active(ItemContainer, vm.IsLoading, isLoading => !isLoading);  // 反向绑定
}
```

---

**总结**：
- **Model**：纯数据，长生命周期，全局管理
- **ViewModel**：业务逻辑，中生命周期，随 View 或独立管理
- **View**：UI 显示，短生命周期，随开随关
