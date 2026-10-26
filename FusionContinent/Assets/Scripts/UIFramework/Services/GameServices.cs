using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UIFramework.Services;
using UnityEngine;

namespace Game.Services
{
    #region 数据模型
    
    /// <summary>
    /// 玩家数据模型
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public int Id;
        public string Name;
        public int Level;
        public int Exp;
        public int MaxExp;
        public int Gold;
        public int Diamond;
        public int HP;
        public int MaxHP;
        public int MP;
        public int MaxMP;
        public int Attack;
        public int Defense;
    }

    /// <summary>
    /// 物品数据
    /// </summary>
    [Serializable]
    public class ItemData
    {
        public int Id;
        public string Name;
        public ItemType Type;
        public int Count;
        public string IconPath;
    }

    public enum ItemType
    {
        Weapon,
        Armor,
        Consumable,
        Material
    }

    #endregion

    #region PlayerService - 玩家核心数据

    /// <summary>
    /// 玩家Service - 管理玩家核心数据
    /// 生命周期：登录后创建，切换账号前一直存在
    /// </summary>
    public class PlayerService : GameServiceBase
    {
        public override string ServiceName => "PlayerService";
        
        // 核心数据
        private PlayerData mData;
        
        // 事件
        public event Action<PlayerData> OnPlayerDataChanged;
        public event Action<int, int> OnLevelUp;  // (新等级, 旧等级)
        public event Action<int> OnGoldChanged;   // 变化量
        public event Action OnDeath;
        
        // 对外属性
        public PlayerData Data => mData;
        public bool IsAlive => mData != null && mData.HP > 0;
        
        public override async UniTask InitializeAsync()
        {
            // 模拟从服务器加载数据
            await UniTask.Delay(500);
            
            mData = new PlayerData
            {
                Id = 10001,
                Name = "勇者",
                Level = 1,
                Exp = 0,
                MaxExp = 100,
                Gold = 1000,
                Diamond = 100,
                HP = 100,
                MaxHP = 100,
                MP = 50,
                MaxMP = 50,
                Attack = 10,
                Defense = 5
            };
            
            await base.InitializeAsync();
        }

        #region 业务方法

        /// <summary>
        /// 增加金币
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            
            mData.Gold += amount;
            OnGoldChanged?.Invoke(amount);
            NotifyDataChanged();
            
            LogTool.Log($"[PlayerService] 获得金币: +{amount}, 当前: {mData.Gold}");
        }

        /// <summary>
        /// 消耗金币
        /// </summary>
        public bool ConsumeGold(int amount)
        {
            if (amount <= 0) return true;
            if (mData.Gold < amount) return false;
            
            mData.Gold -= amount;
            OnGoldChanged?.Invoke(-amount);
            NotifyDataChanged();
            
            LogTool.Log($"[PlayerService] 消耗金币: -{amount}, 当前: {mData.Gold}");
            return true;
        }

        /// <summary>
        /// 增加经验
        /// </summary>
        public void AddExp(int amount)
        {
            if (amount <= 0) return;
            
            mData.Exp += amount;
            
            // 检查升级
            while (mData.Exp >= mData.MaxExp)
            {
                LevelUp();
            }
            
            NotifyDataChanged();
        }

        /// <summary>
        /// 升级
        /// </summary>
        private void LevelUp()
        {
            int oldLevel = mData.Level;
            
            mData.Exp -= mData.MaxExp;
            mData.Level++;
            mData.MaxExp = mData.Level * 100;
            
            // 提升属性
            mData.MaxHP += 20;
            mData.MaxMP += 10;
            mData.Attack += 5;
            mData.Defense += 3;
            
            // 回满状态
            mData.HP = mData.MaxHP;
            mData.MP = mData.MaxMP;
            
            OnLevelUp?.Invoke(mData.Level, oldLevel);
            
            LogTool.Log($"[PlayerService] 升级！Lv.{oldLevel} -> Lv.{mData.Level}");
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (!IsAlive) return;
            
            int actualDamage = Mathf.Max(1, damage - mData.Defense);
            mData.HP = Mathf.Max(0, mData.HP - actualDamage);
            
            NotifyDataChanged();
            
            if (mData.HP <= 0)
            {
                OnDeath?.Invoke();
                LogTool.Log("[PlayerService] 玩家死亡");
            }
        }

        /// <summary>
        /// 恢复HP
        /// </summary>
        public void Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            
            mData.HP = Mathf.Min(mData.MaxHP, mData.HP + amount);
            NotifyDataChanged();
        }

        /// <summary>
        /// 保存数据（到服务器或本地）
        /// </summary>
        public async UniTask SaveData()
        {
            // 模拟保存
            await UniTask.Delay(200);
            LogTool.Log("[PlayerService] 数据已保存");
        }

        #endregion

        public override void Reset()
        {
            mData = null;
            base.Reset();
        }
    }

    #endregion

    #region InventoryService - 背包系统

    /// <summary>
    /// 背包Service - 管理物品
    /// </summary>
    public class InventoryService : GameServiceBase
    {
        public override string ServiceName => "InventoryService";
        
        private List<ItemData> mItems = new List<ItemData>();
        private const int MAX_SLOT = 100;
        
        public event Action OnInventoryChanged;
        public event Action<ItemData> OnItemAdded;
        public event Action<ItemData> OnItemRemoved;
        
        public IReadOnlyList<ItemData> Items => mItems;
        public int MaxSlot => MAX_SLOT;
        public int UsedSlot => mItems.Count;
        public int RemainingSlot => MAX_SLOT - mItems.Count;
        public bool IsFull => mItems.Count >= MAX_SLOT;

        public override async UniTask InitializeAsync()
        {
            await UniTask.Delay(300);
            
            // 模拟初始物品
            AddItem(new ItemData { Id = 1, Name = "血瓶", Type = ItemType.Consumable, Count = 5, IconPath = "Items/Potion_Red" });
            AddItem(new ItemData { Id = 2, Name = "木剑", Type = ItemType.Weapon, Count = 1, IconPath = "Items/Sword_Wood" });
            
            await base.InitializeAsync();
        }

        #region 业务方法

        public bool AddItem(ItemData item)
        {
            if (item == null || IsFull) return false;
            
            // 检查是否可堆叠
            if (item.Type == ItemType.Consumable || item.Type == ItemType.Material)
            {
                var existing = mItems.Find(i => i.Id == item.Id);
                if (existing != null)
                {
                    existing.Count += item.Count;
                    OnInventoryChanged?.Invoke();
                    OnItemAdded?.Invoke(existing);
                    return true;
                }
            }
            
            mItems.Add(item);
            OnInventoryChanged?.Invoke();
            OnItemAdded?.Invoke(item);
            
            return true;
        }

        public bool RemoveItem(int itemId, int count = 1)
        {
            var item = mItems.Find(i => i.Id == itemId);
            if (item == null || item.Count < count) return false;
            
            item.Count -= count;
            
            if (item.Count <= 0)
            {
                mItems.Remove(item);
                OnItemRemoved?.Invoke(item);
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }

        public ItemData GetItem(int itemId)
        {
            return mItems.Find(i => i.Id == itemId);
        }

        public List<ItemData> GetItemsByType(ItemType type)
        {
            return mItems.FindAll(i => i.Type == type);
        }

        public void UseItem(int itemId)
        {
            var item = GetItem(itemId);
            if (item == null) return;
            
            // 使用逻辑
            switch (item.Type)
            {
                case ItemType.Consumable:
                    var playerService = GameServiceManager.Get<PlayerService>();
                    playerService.Heal(50);  // 血瓶恢复50HP
                    RemoveItem(itemId, 1);
                    LogTool.Log($"[InventoryService] 使用物品: {item.Name}");
                    break;
                    
                default:
                    LogTool.Log($"[InventoryService] {item.Name} 无法直接使用");
                    break;
            }
        }

        public void SortItems()
        {
            mItems.Sort((a, b) => 
            {
                if (a.Type != b.Type)
                    return a.Type.CompareTo(b.Type);
                return a.Id.CompareTo(b.Id);
            });
            
            OnInventoryChanged?.Invoke();
        }

        #endregion

        public override void Reset()
        {
            mItems.Clear();
            base.Reset();
        }
    }

    #endregion

    #region TaskService - 任务系统

    /// <summary>
    /// 任务Service
    /// </summary>
    public class TaskService : GameServiceBase
    {
        public override string ServiceName => "TaskService";
        
        private List<TaskData> mTasks = new List<TaskData>();
        
        public event Action OnTaskListChanged;
        public event Action<TaskData> OnTaskUpdated;
        public event Action<TaskData> OnTaskCompleted;

        public override async UniTask InitializeAsync()
        {
            await UniTask.Delay(200);
            
            // 初始化任务
            mTasks.Add(new TaskData 
            { 
                Id = 1, 
                Title = "初出茅庐", 
                Description = "击败5只史莱姆",
                TargetCount = 5,
                CurrentCount = 0,
                RewardGold = 100,
                IsCompleted = false
            });
            
            await base.InitializeAsync();
        }

        public void UpdateTaskProgress(int taskId, int addCount)
        {
            var task = mTasks.Find(t => t.Id == taskId);
            if (task == null || task.IsCompleted) return;
            
            task.CurrentCount += addCount;
            
            if (task.CurrentCount >= task.TargetCount)
            {
                CompleteTask(task);
            }
            else
            {
                OnTaskUpdated?.Invoke(task);
            }
            
            OnTaskListChanged?.Invoke();
        }

        private void CompleteTask(TaskData task)
        {
            task.IsCompleted = true;
            task.CurrentCount = task.TargetCount;
            
            // 发放奖励
            var playerService = GameServiceManager.Get<PlayerService>();
            playerService.AddGold(task.RewardGold);
            
            OnTaskCompleted?.Invoke(task);
            LogTool.Log($"[TaskService] 任务完成: {task.Title}");
        }

        public void AddTask(TaskData task)
        {
            mTasks.Add(task);
            OnTaskListChanged?.Invoke();
        }

        public IReadOnlyList<TaskData> GetAllTasks()
        {
            return mTasks;
        }

        public IReadOnlyList<TaskData> GetActiveTasks()
        {
            return mTasks.FindAll(t => !t.IsCompleted);
        }

        public override void Reset()
        {
            mTasks.Clear();
            base.Reset();
        }
    }

    [Serializable]
    public class TaskData
    {
        public int Id;
        public string Title;
        public string Description;
        public int TargetCount;
        public int CurrentCount;
        public int RewardGold;
        public bool IsCompleted;
        
        public float Progress => TargetCount > 0 ? (float)CurrentCount / TargetCount : 0;
    }

    #endregion
}
