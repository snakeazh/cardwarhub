using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using UIFramework;
using UIFramework.Services;

namespace Game.Soldier
{
    public class GeneralCardResolvePanelViewModel : ViewModel, Game.UserTool.IUserToolConfigurable
    {
        /// <summary>
        /// 获取 UserTool 配置
        /// </summary>
        public override Game.UserTool.UserToolData GetUserToolConfig()
        {
            return GlobalFunc.GetUserResourceConfig(PageType.ArcCardPage);
        }
        #region 属性
        /// <summary>
        /// Dropdown index: 0=全部, 1..N=对应 QualityType 枚举值
        /// </summary>
        public BindableProperty<int> FilterIndex { get; } = new BindableProperty<int>(0);

        public BindableProperty<List<PlayerCard>> FilteredCards { get; } =
            new BindableProperty<List<PlayerCard>>(new List<PlayerCard>());

        /// <summary>
        /// 当前选择的卡牌数量变化后，对应可获得的道具列表（已聚合）
        /// </summary>
        public BindableProperty<List<RewardItem>> ConvertedItems { get; } =
            new BindableProperty<List<RewardItem>>(new List<RewardItem>());
        #endregion

        #region 命令
        public Command<int> ChangeFilterCommand { get; private set; }
        public Command ResolveCommand { get; private set; }
        #endregion

        #region 事件
        public Action OnResolveSucceeded;
        #endregion

        #region 状态
        private readonly Dictionary<int, int> mSelectedCardCounts = new Dictionary<int, int>();
        private TbSpecialEffectsConfig mSpecialEffectsConfig;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            mSpecialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;

            ChangeFilterCommand = new Command<int>(OnChangeFilter);
            ResolveCommand = new Command(OnResolve);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            mSelectedCardCounts.Clear();
            ConvertedItems.Value = new List<RewardItem>();
            OnResolveSucceeded = null;
        }

        public void ResetOnOpen()
        {
            FilterIndex.Value = 0;
            mSelectedCardCounts.Clear();
            RefreshCardList();
            RecalculateConvertedItems();
        }

        public void RefreshCardList()
        {
            var allCards = Global.PlayerCards.Values
                .Where(card => card.Config.Type == CardType.GeneralSkill)
                .ToList();

            if (FilterIndex.Value == 0)
            {
                FilteredCards.Value = allCards;
                return;
            }

            var qualityFilter = (QualityType)FilterIndex.Value;
            var filtered = new List<PlayerCard>();
            foreach (var card in allCards)
            {
                var skillCfg = mSpecialEffectsConfig.GetOrDefault(card.CardId);
                if (skillCfg == null || skillCfg.LevelPropertyList == null || skillCfg.LevelPropertyList.Count == 0)
                {
                    continue;
                }

                var quality = skillCfg.LevelPropertyList[0].Quality;
                if (quality == qualityFilter)
                {
                    filtered.Add(card);
                }
            }

            FilteredCards.Value = filtered;
        }

        public PlayerCard GetCardAt(int index)
        {
            var list = FilteredCards.Value;
            if (list == null || index < 0 || index >= list.Count) return null;
            return list[index];
        }

        public bool IsSelected(int cardId)
        {
            return mSelectedCardCounts.TryGetValue(cardId, out var count) && count > 0;
        }

        public int GetSelectedCount(int cardId)
        {
            return mSelectedCardCounts.TryGetValue(cardId, out var count) ? count : 0;
        }

        public void SetSelected(int cardId, int count)
        {
            if (count > 0)
            {
                mSelectedCardCounts[cardId] = count;
            }
            else
            {
                mSelectedCardCounts.Remove(cardId);
            }

            RecalculateConvertedItems();
        }

        private void RecalculateConvertedItems()
        {
            if (mSelectedCardCounts.Count == 0)
            {
                ConvertedItems.Value = new List<RewardItem>();
                return;
            }

            var merged = new Dictionary<int, long>(); // itemId -> totalNum
            foreach (var kv in mSelectedCardCounts)
            {
                var cardId = kv.Key;
                var selectedCount = kv.Value;
                if (selectedCount <= 0) continue;

                // 先拿到卡牌品质，再根据 ArcCardProConfig.ArcQualityType 找到分解配置
                var skillCfg = mSpecialEffectsConfig?.GetOrDefault(cardId);
                if (skillCfg == null || skillCfg.LevelPropertyList == null || skillCfg.LevelPropertyList.Count == 0) continue;

                var quality = skillCfg.LevelPropertyList[0].Quality;
                var arcCfg = DataConfigManager.Tables.TbArcCardProConfig.GetOrDefault(quality);
                if (arcCfg == null || arcCfg.DecomposeRewards == null || arcCfg.DecomposeRewards.Count == 0) continue;

                var rewardItems = RewardModel.ToRewardItems(arcCfg.DecomposeRewards, selectedCount);
                foreach (var item in rewardItems)
                {
                    if (item == null) continue;
                    if (item.Num <= 0) continue;

                    merged.TryGetValue(item.Id, out var oldNum);
                    merged[item.Id] = oldNum + item.Num;
                }
            }

            var list = merged
                .Where(p => p.Value > 0)
                .Select(p => new RewardItem(p.Key, p.Value))
                .ToList();

            ConvertedItems.Value = list;
        }

        private void OnChangeFilter(int index)
        {
            FilterIndex.Value = index;
            RefreshCardList();
        }

        private async void OnResolve()
        {
            // 客户端检查：未选中任何卡牌时拦截，避免发空请求到服务端
            if (mSelectedCardCounts.Count == 0)
            {
                Toast.Show("请选择要分解的卡牌");
                return;
            }

            // TODO: 分解/奖励展示逻辑待接入对应 Service（本次迁移先保证筛选、列表与选择链路稳定）
            LogTool.Log("[GeneralCardResolvePanelViewModel] ResolveCommand 执行（TODO: 接入分解逻辑）");
            List<int> cardIds = new List<int>();
            foreach (var card in mSelectedCardCounts)
            {
                for (int i = 0; i < card.Value; i++)
                {
                    cardIds.Add(card.Key);
                }
            }

            var result = await GameServiceManager.Get<SoldierService>().DecompositionGeneralSkillCard(cardIds);
            if (result.Success)
            {
                GlobalFunc.ShowRewardTips(result.Rewards);
                Toast.Show("分解成功");
                ResolveSucceeded();
            }
        }

        private void ResolveSucceeded()
        {
            mSelectedCardCounts.Clear();
            RecalculateConvertedItems();
            RefreshCardList();
            OnResolveSucceeded?.Invoke();
        }
    }
}

