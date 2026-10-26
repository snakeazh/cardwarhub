using System.Collections.Generic;

namespace UIFramework.Example
{
    #region Model
    
    /// <summary>
    /// 商品数据模型
    /// </summary>
    public class ShopItemModel
    {
        public int Id;
        public string Name;
        public int Price;
        public string IconPath;
        public bool IsSold;
    }
    
    /// <summary>
    /// 玩家数据模型
    /// </summary>
    public class PlayerModel
    {
        public int Gold;
        public List<int> OwnedItems;
    }
    
    #endregion
    
    #region ViewModel
    
    /// <summary>
    /// 商店ViewModel（纯C#）
    /// 可以在任何时机创建并设置数据，不依赖Unity
    /// </summary>
    public class ShopViewModel : ViewModel
    {
        // 可绑定属性 - 数据变更时自动更新UI
        public BindableProperty<int> PlayerGold { get; } = new BindableProperty<int>(0);
        public BindableProperty<string> ShopTitle { get; } = new BindableProperty<string>("商店");
        public BindableProperty<bool> IsLoading { get; } = new BindableProperty<bool>(false);
        public BindableProperty<List<ShopItemModel>> Items { get; } = new BindableProperty<List<ShopItemModel>>(new List<ShopItemModel>());
        
        // 命令
        public Command CloseCommand { get; private set; }
        public Command<ShopItemModel> BuyCommand { get; private set; }
        public Command RefreshCommand { get; private set; }
        
        // 内部数据
        private PlayerModel mPlayer;
        private List<ShopItemModel> mAllItems;
        
        public override void OnCreate()
        {
            base.OnCreate();
            
            // 初始化命令
            CloseCommand = new Command(OnClose);
            BuyCommand = new Command<ShopItemModel>(OnBuy, CanBuy);
            RefreshCommand = new Command(OnRefresh);
        }
        
        /// <summary>
        /// 初始化数据（可以在任何时机调用，不依赖UI）
        /// </summary>
        public void Initialize(PlayerModel player, List<ShopItemModel> items)
        {
            mPlayer = player;
            mAllItems = items;
            
            // 设置可绑定属性的值
            PlayerGold.Value = player.Gold;
            Items.Value = items;
            ShopTitle.Value = $"商店 (拥有金币: {player.Gold})";
        }
        
        /// <summary>
        /// 购买商品
        /// </summary>
        private void OnBuy(ShopItemModel item)
        {
            if (PlayerGold.Value < item.Price)
            {
                // 金币不足
                return;
            }
            
            // 扣除金币
            PlayerGold.Value -= item.Price;
            item.IsSold = true;
            
            // 更新标题
            ShopTitle.Value = $"商店 (拥有金币: {PlayerGold.Value})";
            
            // 刷新购买按钮状态
            BuyCommand.RaiseCanExecuteChanged();
            
            // 这里可以发送购买请求到服务器
            // GameServer.BuyItem(item.Id);
        }
        
        /// <summary>
        /// 判断是否可购买
        /// </summary>
        private bool CanBuy(ShopItemModel item)
        {
            return !item.IsSold && PlayerGold.Value >= item.Price;
        }
        
        /// <summary>
        /// 刷新商品列表
        /// </summary>
        private void OnRefresh()
        {
            IsLoading.Value = true;
            
            // 模拟异步加载
            // await Task.Delay(1000);
            
            // 重新加载数据
            Items.Value = new List<ShopItemModel>(mAllItems);
            IsLoading.Value = false;
        }
        
        /// <summary>
        /// 关闭界面
        /// </summary>
        private void OnClose()
        {
            // 关闭命令，实际关闭操作在View中处理
            // 这里可以发送关闭前的事件
        }
        
        public override void OnDestroy()
        {
            base.OnDestroy();
            // 清理资源
        }
    }
    
    #endregion
}
