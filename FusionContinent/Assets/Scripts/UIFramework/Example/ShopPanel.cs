using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace UIFramework.Example
{
    /// <summary>
    /// 商店View - 继承UIPanel
    /// 在Prefab上挂载此脚本
    /// 组件引用由UIKit Designer自动生成，或手动添加
    /// </summary>
    public class ShopPanel : UIPanel<ShopViewModel>
    {
        // 手动添加组件引用（实际项目中由Designer.cs自动生成）
        [SerializeField] private Text ShopTitleText;
        [SerializeField] private Text GoldText;
        [SerializeField] private Transform ItemContainer;
        [SerializeField] private Button CloseButton;
        [SerializeField] private Button RefreshButton;
        [SerializeField] private GameObject LoadingObj;
        [SerializeField] private GameObject ItemPrefab;
        
        private ShopItemViewPool mItemPool;
        
        /// <summary>
        /// 绑定ViewModel - 在OnInit时调用，此时VM已准备好
        /// 可以立即进行属性和命令的绑定
        /// </summary>
        protected override void OnBindViewModel(ShopViewModel vm)
        {
            // 使用UIBind工具类进行便捷绑定
            
            // 1. 文本绑定
            UIBind.Text(ShopTitleText, vm.ShopTitle);
            UIBind.Text(GoldText, vm.PlayerGold, "金币: {0}");
            
            // 2. 按钮命令绑定
            UIBind.Click(CloseButton, vm.CloseCommand);
            UIBind.Click(RefreshButton, vm.RefreshCommand);
            
            // 3. Loading状态绑定
            UIBind.Active(LoadingObj, vm.IsLoading);
            
            // 4. 列表数据绑定 - 需要自定义处理
            vm.Items.OnValueChanged += OnItemsChanged;
            RefreshItems(vm.Items.Value);
            
            // 5. 金币变化时刷新购买按钮状态
            vm.PlayerGold.OnValueChanged += (oldVal, newVal) =>
            {
                vm.BuyCommand.RaiseCanExecuteChanged();
            };
        }
        
        /// <summary>
        /// 解绑ViewModel - 清理所有监听
        /// </summary>
        protected override void OnUnbindViewModel(ShopViewModel vm)
        {
            // 移除事件监听
            vm.Items.OnValueChanged -= OnItemsChanged;
            
            // 清理按钮监听（UIBind已自动处理，这里只是示例）
            CloseButton?.onClick.RemoveAllListeners();
            RefreshButton?.onClick.RemoveAllListeners();
        }
        
        /// <summary>
        /// 商品列表变化时刷新
        /// </summary>
        private void OnItemsChanged(List<ShopItemModel> oldItems, 
            List<ShopItemModel> newItems)
        {
            RefreshItems(newItems);
        }
        
        /// <summary>
        /// 刷新商品列表UI
        /// </summary>
        private void RefreshItems(List<ShopItemModel> items)
        {
            // 清除旧项
            foreach (Transform child in ItemContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 创建新项
            foreach (var item in items)
            {
                CreateItemView(item);
            }
        }
        
        /// <summary>
        /// 创建单个商品项的View
        /// </summary>
        private void CreateItemView(ShopItemModel item)
        {
            var itemObj = Instantiate(ItemPrefab, ItemContainer);
            var itemView = itemObj.GetComponent<ShopItemView>();
            
            // 初始化项View
            itemView.Setup(item, VM.PlayerGold, VM.BuyCommand);
        }
        
        /// <summary>
        /// 关闭按钮点击（如果需要额外处理）
        /// </summary>
        public void OnCloseButtonClick()
        {
            // 命令已绑定到VM.CloseCommand，这里可以直接关闭
            CloseSelf();
        }
    }
    
    /// <summary>
    /// 单个商品项的View（简单示例）
    /// </summary>
    public class ShopItemView : MonoBehaviour
    {
        public Text ItemNameText;
        public Text PriceText;
        public Button BuyButton;
        public Image IconImage;
        
        public void Setup(ShopItemModel item, BindableProperty<int> playerGold, Command<ShopItemModel> buyCommand)
        {
            ItemNameText.text = item.Name;
            PriceText.text = item.Price.ToString();
            // IconImage.sprite = Resources.Load<Sprite>(item.IconPath);
            
            // 绑定购买命令
            BuyButton.onClick.AddListener(() => buyCommand.Execute(item));
            
            // 根据金币和是否已售更新按钮状态
            UpdateButtonState(item, playerGold.Value);
            
            // 监听金币变化
            playerGold.OnValueChanged += (oldVal, newVal) =>
            {
                UpdateButtonState(item, newVal);
            };
        }
        
        private void UpdateButtonState(ShopItemModel item, int gold)
        {
            BuyButton.interactable = !item.IsSold && gold >= item.Price;
            BuyButton.GetComponentInChildren<Text>().text = item.IsSold ? "已售" : "购买";
        }
    }
    
    // 占位类，实际项目中应该有对象池实现
    public class ShopItemViewPool { }
}
