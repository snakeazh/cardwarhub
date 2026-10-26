using UIFramework.Services;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using FusionContinent;
using FusionContinent.Model;
using UnityEngine;
using Game.UI;

namespace Game.UI
{
    /// <summary>
    /// 奖励提示服务 - 全局管理奖励提示面板
    /// </summary>
    public class RewardTipsService : GameServiceBase
    {
        #region 属性
        private bool mIsPanelOpened = false;
        private RewardTipsViewModel mCurrentViewModel;
        private UniTaskCompletionSource mCloseTcs;
        #endregion

        #region 事件
        /// <summary>
        /// 显示奖励提示事件
        /// </summary>
        public event Action<List<RewardItem>, RewardTipsViewData> OnShowRewardTips;

        /// <summary>
        /// 奖励提示界面关闭后触发（含关闭动画结束）
        /// </summary>
        public event Action OnRewardTipsClosed;
        #endregion

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            LogTool.Log("[RewardTipsService] 初始化完成");
        }

        #region 公共方法

        /// <summary>
        /// 显示奖励提示（List&lt;RewardItem&gt;版本）
        /// </summary>
        public async UniTask ShowRewardTips(List<RewardItem> rewardItems, RewardTipsViewData data = null)
        {
            if (rewardItems == null || rewardItems.Count == 0)
            {
                LogTool.LogWarning("[RewardTipsService] 奖励列表为空，不显示提示");
                return;
            }

            // 确保面板已打开
            await EnsurePanelOpen();

            mCurrentViewModel?.SetData(rewardItems, data);

            // 触发显示事件
            OnShowRewardTips?.Invoke(rewardItems, data);

            if (mCloseTcs == null)
            {
                mCloseTcs = new UniTaskCompletionSource();
            }

            await mCloseTcs.Task;
        }

        /// <summary>
        /// 显示奖励提示（RewardAndNum列表版本）
        /// </summary>
        public async UniTask ShowRewardTips(List<Config.RewardAndNum> rewardItems, RewardTipsViewData data = null)
        {
            if (rewardItems == null || rewardItems.Count == 0)
            {
                LogTool.LogWarning("[RewardTipsService] 奖励列表为空，不显示提示");
                return;
            }

            // 转换为RewardItem列表
            var rewards = new List<RewardItem>();
            foreach (var v in rewardItems)
            {
                var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(v.Id);
                rewards.Add(new RewardItem(v.Id, v.Num, itemCfg.Type));
            }

            await ShowRewardTips(rewards, data);
        }

        /// <summary>
        /// 显示奖励提示（Dictionary版本）
        /// </summary>
        public async UniTask ShowRewardTips(Dictionary<string, int> rewardItems, RewardTipsViewData data = null)
        {
            if (rewardItems == null || rewardItems.Count == 0)
            {
                LogTool.LogWarning("[RewardTipsService] 奖励列表为空，不显示提示");
                return;
            }

            // 转换为RewardItem列表
            var rewards = new List<RewardItem>();
            foreach (var v in rewardItems)
            {
                var itemId = int.Parse(v.Key);
                var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(itemId);
                rewards.Add(new RewardItem(itemId, v.Value, itemCfg.Type));
            }

            await ShowRewardTips(rewards, data);
        }
        public async UniTask ShowRewardTips(Dictionary<int, long> rewardItems, RewardTipsViewData data = null)
        {
            if (rewardItems == null || rewardItems.Count == 0)
            {
                LogTool.LogWarning("[RewardTipsService] 奖励列表为空，不显示提示");
                return;
            }

            // 转换为RewardItem列表
            var rewards = new List<RewardItem>();
            foreach (var v in rewardItems)
            {
                var itemId = v.Key;
                var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(itemId);
                rewards.Add(new RewardItem(itemId, v.Value, itemCfg.Type));
            }

            await ShowRewardTips(rewards, data);
        }
        /// <summary>
        /// 关闭奖励提示
        /// </summary>
        public void CloseRewardTips()
        {
            if (mIsPanelOpened)
            {
                UIFramework.UIManager.ClosePanel<RewardTipsNew>();
                mCurrentViewModel = null;
                mIsPanelOpened = false;
            }

            OnRewardTipsClosed?.Invoke();
            mCloseTcs?.TrySetResult();
            mCloseTcs = null;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 确保 RewardTips 面板已打开
        /// </summary>
        private async UniTask EnsurePanelOpen()
        {
            if (!mIsPanelOpened)
            {
                var vm = new RewardTipsViewModel();
                vm.OnCreate();
                await UIFramework.UIManager.OpenPanelAsync<RewardTipsNew, RewardTipsViewModel>(
                    vm,
                    UIFramework.UILevel.Tips);

                mCurrentViewModel = vm;
                mIsPanelOpened = true;
                LogTool.Log("[RewardTipsService] RewardTips面板已打开");
            }
        }

        #endregion
    }
}
