using UnityEngine;
using System.Collections;
using System.Linq;

namespace UIFramework.Example
{
    /// <summary>
    /// 使用示例：展示如何打开UIFramework界面
    /// </summary>
    public class ShopExample : MonoBehaviour
    {
        [ContextMenu("打开商店")]
        public void OpenShop()
        {
            // ========================================
            // 步骤1: 创建并准备ViewModel（纯C#，不依赖Unity）
            // ========================================
            
            var viewModel = new ShopViewModel();
            
            // 准备数据 - 这里可以来自服务器、本地配置等
            var player = new PlayerModel
            {
                Gold = 5000,
                OwnedItems = new System.Collections.Generic.List<int>()
            };
            
            var items = new System.Collections.Generic.List<ShopItemModel>
            {
                new ShopItemModel { Id = 1, Name = "铁剑", Price = 100, IconPath = "Icons/Sword1" },
                new ShopItemModel { Id = 2, Name = "银剑", Price = 500, IconPath = "Icons/Sword2" },
                new ShopItemModel { Id = 3, Name = "金剑", Price = 2000, IconPath = "Icons/Sword3" },
            };
            
            // 初始化ViewModel数据
            viewModel.Initialize(player, items);
            viewModel.ShopTitle.Value = "武器商店";
            
            // ========================================
            // 步骤2: 异步打开界面（使用构建器模式）
            // ========================================
            
            // 方式1: 直接使用异步接口
            UIFramework.UIManager.OpenPanelAsync<ShopPanel, ShopViewModel>(
                viewModel,
                UIFramework.UILevel.PopUI,
                onComplete: panel =>
                {
                    LogTool.Log($"商店界面已打开");
                });
            
            // 方式2: await方式（需要在async方法中使用）
            // await OpenShopAsync(viewModel);
            
            // ========================================
            // 步骤3: 数据驱动更新（界面打开后）
            // ========================================
            
            // 3秒后增加金币（演示数据驱动）
            StartCoroutine(IncreaseGoldCoroutine(viewModel));
        }
        

        
        [ContextMenu("打开商店（自动管理栈）")]
        public void OpenShopWithStack()
        {
            var viewModel = new ShopViewModel();
            viewModel.Initialize(
                new PlayerModel { Gold = 5000, OwnedItems = new System.Collections.Generic.List<int>() },
                new System.Collections.Generic.List<ShopItemModel>()
            );
            
            UIFramework.UIManager.OpenPanelAsync<ShopPanel, ShopViewModel>(
                viewModel,
                onComplete: panel =>
                {
                    LogTool.Log("商店已打开，点击返回可以关闭");
                });
        }
        
        [ContextMenu("返回")]
        public void Back()
        {
            // 返回上一个界面（弹出栈顶）
            UIFramework.UIManager.Back();
        }
        
        [ContextMenu("关闭商店")]
        public void CloseShop()
        {
            // 关闭指定界面
            UIFramework.UIManager.ClosePanel<ShopPanel>();
        }
        
        [ContextMenu("关闭所有弹出层")]
        public void CloseAllPopUI()
        {
            // 关闭指定层级的所有界面
            UIFramework.UIManager.ClosePanelsByLevel(UIFramework.UILevel.PopUI);
        }
        
        [ContextMenu("显示层级信息")]
        public void ShowLevelInfo()
        {
            LogTool.Log($"PopUI层界面数: {UIFramework.UIManager.GetPanelsByLevel(UIFramework.UILevel.PopUI).Count()}");
            LogTool.Log($"Common层界面数: {UIFramework.UIManager.GetPanelsByLevel(UIFramework.UILevel.Common).Count()}");
        }
        
        [ContextMenu("获取ViewModel")]
        public void GetViewModel()
        {
            // 获取已打开的ViewModel
            var vm = UIFramework.UIManager.GetViewModel<ShopViewModel>();
            if (vm != null)
            {
                LogTool.Log($"当前金币: {vm.PlayerGold.Value}");
                vm.PlayerGold.Value += 100;  // 修改会自动更新UI
            }
            else
            {
                LogTool.Log("商店界面未打开");
            }
        }
        
        private IEnumerator IncreaseGoldCoroutine(ShopViewModel vm)
        {
            yield return new WaitForSeconds(3);
            
            // 修改数据，UI会自动更新
            vm.PlayerGold.Value += 1000;
            
            LogTool.Log($"金币已更新为: {vm.PlayerGold.Value}");
        }
    }
}