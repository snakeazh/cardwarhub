using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Model;
using Cysharp.Threading.Tasks;
using FusionContinent;
using UIFramework.Services;

namespace Game.Soldier
{
    /// <summary>
    /// 养成奖励界面 ViewModel
    /// </summary>
    public class DevelopRewardsViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<int> CardId { get; } = new BindableProperty<int>(0);
        public BindableProperty<List<DevelopLevelPropertyList>> DevelopRewardList { get; } = new BindableProperty<List<DevelopLevelPropertyList>>();
        public BindableProperty<PlayerCard> CardInfo { get; } = new BindableProperty<PlayerCard>();
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command<int> OnReceiveRewardCommand { get; private set; }
        #endregion

        #region 依赖
        private SoldierService mSoldierService;
        #endregion

        #region 配置数据
        private TbCardDevelopConfig mCardDevelopConfig;
        private PrioritySorter<DevelopLevelPropertyList> mPrioritySorter;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mSoldierService = GameServiceManager.Get<SoldierService>();
            
            // 加载配置
            mCardDevelopConfig = DataConfigManager.Tables.TbCardDevelopConfig;
            mPrioritySorter = new PrioritySorter<DevelopLevelPropertyList>();
            
            // 初始化命令
            OnCloseCommand = new Command(OnClose);
            OnReceiveRewardCommand = new Command<int>(OnReceiveReward);
            
            LogTool.Log("[DevelopRewardsViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogTool.Log("[DevelopRewardsViewModel] 销毁");
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        public void Initialize(int cardId)
        {
            CardId.Value = cardId;
            
            // 获取卡牌信息
            var cardInfo = Global.GetCardById(cardId);
            CardInfo.Value = cardInfo;
            
            // 加载养成奖励配置
            LoadDevelopRewards();
        }

        #region 命令实现

        private void OnClose()
        {
            OnCloseRequested?.Invoke();
        }

        private async void OnReceiveReward(int level)
        {
            var rewardCfg = DevelopRewardList.Value?.FirstOrDefault(r => r.Level == level);
            if (rewardCfg == null) return;
            
            var result = await mSoldierService.ReceiveDevelopRewardAsync(CardId.Value, level);
            if (result.Success)
            {
                // 更新卡牌信息
                CardInfo.Value = Global.GetCardById(CardId.Value);
                
                // 触发领取成功事件
                OnRewardReceived?.Invoke(level, rewardCfg.TakeRewards);
                
                // 刷新列表
                RefreshRewardList();
                
                LogTool.Log($"[DevelopRewardsViewModel] 领取奖励成功，Level={level}");
            }
            else
            {
                OnReceiveFailed?.Invoke(result.ErrorMessage);
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 加载养成奖励
        /// </summary>
        public void LoadDevelopRewards()
        {
            var developCfg = mCardDevelopConfig.GetOrDefault(CardId.Value);
            if (developCfg == null) return;
            
            var rewardList = developCfg.LevelPropertyList
                .Where(t => t.TakeRewards.Count > 0)
                .ToList();
            
            DevelopRewardList.Value = rewardList;
            
            // 排序
            SortRewardList();
        }

        /// <summary>
        /// 刷新奖励列表
        /// </summary>
        public void RefreshRewardList()
        {
            SortRewardList();
        }

        /// <summary>
        /// 排序奖励列表
        /// </summary>
        private void SortRewardList()
        {
            if (DevelopRewardList.Value == null || CardInfo.Value == null) return;
            
            mPrioritySorter.SetData(DevelopRewardList.Value);
            mPrioritySorter.SetScoreCalculator(Calculator);
            DevelopRewardList.Value = mPrioritySorter.GetSortedList();
        }

        /// <summary>
        /// 计算优先级分数
        /// </summary>
        private int Calculator(DevelopLevelPropertyList arg)
        {
            var cardInfo = CardInfo.Value;
            var stateScore = -arg.Level;
            
            if (cardInfo.ObjData.ReceiveDevelops != null && cardInfo.ObjData.ReceiveDevelops.Contains(arg.Level))
            {
                // 已领取
                stateScore += 100;
            }
            else if (cardInfo.Lv < arg.Level)
            {
                // 待升级
                stateScore += 1000;
            }
            else
            {
                // 未领取（可领取）
                stateScore += 10000;
            }
            
            return stateScore;
        }

        /// <summary>
        /// 检查奖励状态
        /// </summary>
        public Config.RewardStatus CheckRewardStatus(int level)
        {
            var cardInfo = CardInfo.Value;
            if (cardInfo == null) return Config.RewardStatus.Default;
            
            // 已领取
            if (cardInfo.ObjData.ReceiveDevelops != null && cardInfo.ObjData.ReceiveDevelops.Contains(level))
            {
                return Config.RewardStatus.Received;
            }
            
            // 待升级
            if (cardInfo.Lv < level)
            {
                return Config.RewardStatus.Default;
            }
            
            // 可领取
            return Config.RewardStatus.CanReceive;
        }

        #endregion

        #region 事件

        public System.Action OnCloseRequested;
        public System.Action<int, List<RewardAndNum>> OnRewardReceived;
        public System.Action<string> OnReceiveFailed;

        #endregion
    }


}
