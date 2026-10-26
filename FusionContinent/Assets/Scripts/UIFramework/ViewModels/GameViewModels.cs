using UIFramework;
using Game.Services;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UIFramework.Services;
using UnityEngine;

namespace Game.ViewModels
{
    #region PlayerViewModel

    /// <summary>
    /// 玩家ViewModel
    /// 作为PlayerService的代理，不存储数据
    /// </summary>
    public class PlayerViewModel : ViewModel
    {
        #region 属性（从Service同步）
        public BindableProperty<string> PlayerName { get; } = new BindableProperty<string>("");
        public BindableProperty<int> Level { get; } = new BindableProperty<int>(1);
        public BindableProperty<int> Exp { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> MaxExp { get; } = new BindableProperty<int>(100);
        public BindableProperty<int> Gold { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> HP { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> MaxHP { get; } = new BindableProperty<int>(100);
        public BindableProperty<float> HpPercent { get; } = new BindableProperty<float>(1f);
        public BindableProperty<int> Attack { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> Defense { get; } = new BindableProperty<int>(0);
        #endregion

        #region 命令
        public Command UsePotionCommand { get; private set; }
        public Command TestDamageCommand { get; private set; }
        public Command TestExpCommand { get; private set; }
        #endregion

        #region 依赖
        private PlayerService mPlayerService;
        private InventoryService mInventoryService;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            // 获取Service（单例）
            mPlayerService = GameServiceManager.Get<PlayerService>();
            mInventoryService = GameServiceManager.Get<InventoryService>();

            // 订阅Service事件
            mPlayerService.OnPlayerDataChanged += OnPlayerDataChanged;
            mPlayerService.OnGoldChanged += OnGoldChanged;
            mPlayerService.OnLevelUp += OnLevelUp;

            // 初始化命令
            UsePotionCommand = new Command(OnUsePotion, () => mInventoryService.GetItem(1) != null);
            TestDamageCommand = new Command(OnTestDamage);
            TestExpCommand = new Command(OnTestExp);

            // 初始同步
            SyncFromService();

            LogTool.Log("[PlayerViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            // 取消订阅（重要！）
            mPlayerService.OnPlayerDataChanged -= OnPlayerDataChanged;
            mPlayerService.OnGoldChanged -= OnGoldChanged;
            mPlayerService.OnLevelUp -= OnLevelUp;

            LogTool.Log("[PlayerViewModel] 销毁");
        }
        
        private void OnPlayerDataChanged(PlayerData data)
        {
            SyncFromService();
        }

        #region Service事件处理

        private void SyncFromService()
        {
            if (!mPlayerService.IsInitialized) return;

            var data = mPlayerService.Data;
            
            PlayerName.Value = data.Name;
            Level.Value = data.Level;
            Exp.Value = data.Exp;
            MaxExp.Value = data.MaxExp;
            Gold.Value = data.Gold;
            HP.Value = data.HP;
            MaxHP.Value = data.MaxHP;
            HpPercent.Value = (float)data.HP / data.MaxHP;
            Attack.Value = data.Attack;
            Defense.Value = data.Defense;
        }

        private void OnGoldChanged(int changeAmount)
        {
            Gold.Value = mPlayerService.Data.Gold;
            
            if (changeAmount > 0)
            {
                LogTool.Log($"[PlayerViewModel] 获得金币: +{changeAmount}");
            }
        }

        private void OnLevelUp(int newLevel, int oldLevel)
        {
            Level.Value = newLevel;
            MaxExp.Value = mPlayerService.Data.MaxExp;
            
            // 升级后属性会变化，重新同步
            SyncFromService();
            
            LogTool.Log($"[PlayerViewModel] 升级！{oldLevel} -> {newLevel}");
        }

        #endregion

        #region 命令实现

        private void OnUsePotion()
        {
            // 使用Service的方法
            mInventoryService.UseItem(1);
        }

        private void OnTestDamage()
        {
            mPlayerService.TakeDamage(20);
        }

        private void OnTestExp()
        {
            mPlayerService.AddExp(50);
        }

        #endregion
    }

    #endregion

    #region InventoryViewModel

    /// <summary>
    /// 背包ViewModel
    /// </summary>
    public class InventoryViewModel : ViewModel
    {
        public BindableProperty<List<ItemData>> Items { get; } = new BindableProperty<List<ItemData>>(new List<ItemData>());
        public BindableProperty<int> UsedSlot { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> MaxSlot { get; } = new BindableProperty<int>(100);
        public BindableProperty<bool> IsFull { get; } = new BindableProperty<bool>(false);
        public BindableProperty<ItemType> FilterType { get; } = new BindableProperty<ItemType>();

        public Command<ItemData> UseItemCommand { get; private set; }
        public Command<ItemData> DropItemCommand { get; private set; }
        public Command SortCommand { get; private set; }
        public Command AddTestItemCommand { get; private set; }

        private InventoryService mService;

        public override void OnCreate()
        {
            base.OnCreate();

            mService = GameServiceManager.Get<InventoryService>();

            // 订阅事件
            mService.OnInventoryChanged += SyncFromService;
            mService.OnItemAdded += OnItemAdded;

            // 命令
            UseItemCommand = new Command<ItemData>(OnUseItem);
            DropItemCommand = new Command<ItemData>(OnDropItem);
            SortCommand = new Command(OnSort);
            AddTestItemCommand = new Command(OnAddTestItem);

            // 筛选类型变化时刷新
            FilterType.OnValueChanged += (oldVal, newVal) => RefreshFilteredItems();

            SyncFromService();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            
            mService.OnInventoryChanged -= SyncFromService;
            mService.OnItemAdded -= OnItemAdded;
        }

        private void SyncFromService()
        {
            if (!mService.IsInitialized) return;

            UsedSlot.Value = mService.UsedSlot;
            MaxSlot.Value = mService.MaxSlot;
            IsFull.Value = mService.IsFull;

            RefreshFilteredItems();
        }

        private void RefreshFilteredItems()
        {
            // 根据筛选条件显示物品
            var filtered = mService.GetItemsByType(FilterType.Value);
            Items.Value = new List<ItemData>(filtered.Count > 0 ? filtered : mService.Items);
        }

        private void OnItemAdded(ItemData item)
        {
            LogTool.Log($"[InventoryViewModel] 获得物品: {item.Name} x{item.Count}");
        }

        private void OnUseItem(ItemData item)
        {
            mService.UseItem(item.Id);
        }

        private void OnDropItem(ItemData item)
        {
            mService.RemoveItem(item.Id, item.Count);
        }

        private void OnSort()
        {
            mService.SortItems();
        }

        private void OnAddTestItem()
        {
            // 测试：添加随机物品
            var randomItem = new ItemData
            {
                Id = Random.Range(100, 999),
                Name = "测试物品" + Random.Range(1, 100),
                Type = (ItemType)Random.Range(0, 4),
                Count = Random.Range(1, 10),
                IconPath = "Items/Default"
            };
            
            mService.AddItem(randomItem);
        }
    }

    #endregion

    #region TaskViewModel

    /// <summary>
    /// 任务ViewModel
    /// </summary>
    public class TaskViewModel : ViewModel
    {
        public BindableProperty<List<TaskData>> ActiveTasks { get; } = new BindableProperty<List<TaskData>>(new List<TaskData>());
        public BindableProperty<int> CompletedCount { get; } = new BindableProperty<int>(0);

        public Command<TaskData> GetRewardCommand { get; private set; }
        public Command TestProgressCommand { get; private set; }

        private TaskService mService;

        public override void OnCreate()
        {
            base.OnCreate();

            mService = GameServiceManager.Get<TaskService>();

            mService.OnTaskListChanged += SyncFromService;
            mService.OnTaskCompleted += OnTaskCompleted;

            GetRewardCommand = new Command<TaskData>(OnGetReward, CanGetReward);
            TestProgressCommand = new Command(OnTestProgress);

            SyncFromService();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            
            mService.OnTaskListChanged -= SyncFromService;
            mService.OnTaskCompleted -= OnTaskCompleted;
        }

        private void SyncFromService()
        {
            if (!mService.IsInitialized) return;

            ActiveTasks.Value = new List<TaskData>(mService.GetActiveTasks());
            
            int completed = mService.GetAllTasks().Count - ActiveTasks.Value.Count;
            CompletedCount.Value = completed;
        }

        private void OnTaskCompleted(TaskData task)
        {
            LogTool.Log($"[TaskViewModel] 任务完成: {task.Title}");
            GetRewardCommand.RaiseCanExecuteChanged();
        }

        private bool CanGetReward(TaskData task)
        {
            return task != null && task.IsCompleted;
        }

        private void OnGetReward(TaskData task)
        {
            // 领取奖励逻辑
            LogTool.Log($"[TaskViewModel] 领取奖励: {task.Title}");
        }

        private void OnTestProgress()
        {
            // 测试：增加任务进度
            var activeTasks = mService.GetActiveTasks();
            if (activeTasks.Count > 0)
            {
                mService.UpdateTaskProgress(activeTasks[0].Id, 1);
            }
        }
    }

    #endregion

    #region ShopViewModel

    /// <summary>
    /// 商店ViewModel（整合多个Service）
    /// </summary>
    public class ShopViewModel : ViewModel
    {
        public BindableProperty<int> PlayerGold { get; } = new BindableProperty<int>(0);
        public BindableProperty<List<ShopItem>> ShopItems { get; } = new BindableProperty<List<ShopItem>>(new List<ShopItem>());
        public BindableProperty<bool> IsLoading { get; } = new BindableProperty<bool>(false);

        public Command<ShopItem> BuyCommand { get; private set; }
        public Command RefreshCommand { get; private set; }

        private PlayerService mPlayerService;

        public override void OnCreate()
        {
            base.OnCreate();

            mPlayerService = GameServiceManager.Get<PlayerService>();

            // 监听玩家金币变化
            mPlayerService.OnGoldChanged += OnGoldChanged;

            BuyCommand = new Command<ShopItem>(OnBuy, CanBuy);
            RefreshCommand = new Command(OnRefresh);

            // 初始同步金币
            if (mPlayerService.IsInitialized)
            {
                PlayerGold.Value = mPlayerService.Data.Gold;
            }

            // 加载商品
            LoadShopItems();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            mPlayerService.OnGoldChanged -= OnGoldChanged;
        }

        private void OnGoldChanged(int change)
        {
            PlayerGold.Value = mPlayerService.Data.Gold;
        }

        private bool CanBuy(ShopItem item)
        {
            return PlayerGold.Value >= item.Price;
        }

        private void OnBuy(ShopItem item)
        {
            if (mPlayerService.ConsumeGold(item.Price))
            {
                // 购买成功，添加到背包
                var inventory = GameServiceManager.Get<InventoryService>();
                inventory.AddItem(new ItemData 
                { 
                    Id = item.Id, 
                    Name = item.Name, 
                    Type = item.Type,
                    Count = 1,
                    IconPath = item.IconPath 
                });

                LogTool.Log($"[ShopViewModel] 购买成功: {item.Name}");
            }
        }

        private async void OnRefresh()
        {
            IsLoading.Value = true;
            await LoadShopItems();
            IsLoading.Value = false;
        }

        private async UniTask LoadShopItems()
        {
            // 模拟加载商品
            await UniTask.Delay(500);
            
            ShopItems.Value = new List<ShopItem>
            {
                new ShopItem { Id = 101, Name = "铁剑", Type = ItemType.Weapon, Price = 100, IconPath = "Items/Sword_Iron" },
                new ShopItem { Id = 102, Name = "皮甲", Type = ItemType.Armor, Price = 150, IconPath = "Items/Armor_Leather" },
                new ShopItem { Id = 103, Name = "高级血瓶", Type = ItemType.Consumable, Price = 50, IconPath = "Items/Potion_Big" },
            };
        }
    }

    public class ShopItem
    {
        public int Id;
        public string Name;
        public ItemType Type;
        public int Price;
        public string IconPath;
    }

    #endregion
}
