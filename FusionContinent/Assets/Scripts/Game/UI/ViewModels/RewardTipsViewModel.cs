using UIFramework;
using System.Collections.Generic;
using FusionContinent;
using FusionContinent.Model;

namespace Game.UI
{
    /// <summary>
    /// 奖励提示数据
    /// </summary>
    public class RewardTipsViewData
    {
        public string Title = "恭喜获得";
        public string TipText = "";
    }


    /// <summary>
    /// 奖励提示 ViewModel
    /// </summary>
    public class RewardTipsViewModel : ViewModel
    {
        #region 属性
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; private set; } = "恭喜获得";

        /// <summary>
        /// 提示文本
        /// </summary>
        public string TipText { get; private set; } = "";

        /// <summary>
        /// 提示文本是否显示
        /// </summary>
        public bool IsTipTextVisible { get; private set; } = false;

        /// <summary>
        /// 奖励列表
        /// </summary>
        public BindableProperty<List<FusionContinent.Model.RewardItem>> RewardItems { get; } = new BindableProperty<List<FusionContinent.Model.RewardItem>>(new List<FusionContinent.Model.RewardItem>());

        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
        }

        /// <summary>
        /// 设置数据（主方法）
        /// </summary>
        public void SetData(List<FusionContinent.Model.RewardItem> rewardItems, RewardTipsViewData data = null)
        {
            var viewData = data ?? new RewardTipsViewData();

            if (rewardItems == null)
            {
                LogTool.LogError("[RewardTipsViewModel] 奖励列表为 null");
                return;
            }

            if (rewardItems.Count == 0)
            {
                LogTool.LogError("[RewardTipsViewModel] 奖励列表为空");
                return;
            }
            
            // 设置标题
            Title = string.IsNullOrEmpty(viewData.Title) ? "恭喜获得" : viewData.Title;

            // 设置提示文本
            TipText = viewData.TipText ?? "";
            IsTipTextVisible = !string.IsNullOrEmpty(TipText);

            // 设置奖励列表
            RewardItems.Value = rewardItems;
        }
    }
}
