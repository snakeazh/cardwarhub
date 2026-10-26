using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using Config;
using FusionContinent;
using Manager;

namespace Game.Battle
{
    /// <summary>
    /// 战斗惊喜卡牌 ViewModel
    /// 管理惊喜卡牌的显示和更新
    /// </summary>
    public class BattleSurpriseCardViewModel : ViewModel
    {
        #region 属性
        /// <summary>
        /// 惊喜卡牌ID列表
        /// </summary>
        public BindableProperty<List<int>> SurpriseCardIds { get; } =
            new BindableProperty<List<int>>(new List<int>());

        /// <summary>
        /// 惊喜卡牌DeBuffID字典（Key: surpriseId, Value: deBuffId）
        /// </summary>
        public BindableProperty<Dictionary<int, int>> SurpriseDeBuffIds { get; } =
            new BindableProperty<Dictionary<int, int>>(new Dictionary<int, int>());
        #endregion

        #region 依赖
        private TbBattleSurpriseConfig _surpriseConfig;
        #endregion

        #region 事件
        public System.Action<int> OnSurpriseCardAdded;     // 添加惊喜卡牌
        public System.Action<int> OnSurpriseCardUpdated;   // 更新惊喜卡牌
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            // 加载配置
            _surpriseConfig = DataConfigManager.Tables.TbBattleSurpriseConfig;

     

            LogTool.Log("[BattleSurpriseCardViewModel] 创建完成");
        }

        /// <summary>
        /// 添加惊喜卡牌
        /// </summary>
        public void AddSurpriseCard(int surpriseId, int deBuffId)
        {
            // 更新ID列表
            if (!SurpriseCardIds.Value.Contains(surpriseId))
            {
                var newList = new List<int>(SurpriseCardIds.Value);
                newList.Add(surpriseId);
                SurpriseCardIds.Value = newList;

                OnSurpriseCardAdded?.Invoke(surpriseId);
                LogTool.Log($"[BattleSurpriseCardViewModel] 添加惊喜卡牌: {surpriseId}");
            }
            else
            {
                // 已存在，更新DeBuffId
                var newDict = new Dictionary<int, int>(SurpriseDeBuffIds.Value);
                newDict[surpriseId] = deBuffId;
                SurpriseDeBuffIds.Value = newDict;

                OnSurpriseCardUpdated?.Invoke(surpriseId);
                LogTool.Log($"[BattleSurpriseCardViewModel] 更新惊喜卡牌: {surpriseId}");
            }
        }

        /// <summary>
        /// 获取惊喜卡牌配置
        /// </summary>
        public BattleSurpriseConfig GetSurpriseConfig(int surpriseId)
        {
            return _surpriseConfig?.GetOrDefault(surpriseId);
        }

        /// <summary>
        /// 获取惊喜卡牌的DeBuffId
        /// </summary>
        public int GetDeBuffId(int surpriseId)
        {
            if (SurpriseDeBuffIds.Value.TryGetValue(surpriseId, out var deBuffId))
            {
                return deBuffId;
            }
            return 0;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
