using System;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Model;
using UIFramework;

namespace Game.Challenge
{
    /// <summary>
    /// 怪物预览面板 ViewModel（UIFramework MVVM）
    /// 数据源：TbMonsterGenConfig（按关卡 ID 取）→ 遍历 20 天 StageList → 每天的 WaveList →
    /// 每波的 GenCards，按 CardId 去重收集本关出现的所有怪物种类，再关联
    /// TbMonsterBattleConfig（品质）与 TbCardBaseConfig（名字/图标）。
    /// </summary>
    public class MonsterPreViewPanelViewModel : ViewModel
    {
        #region 属性
        /// <summary>本关出现的怪物种类列表（已去重），供 View 的 ScrollView 填充</summary>
        public BindableProperty<List<MonsterAtlasItemData>> Items { get; } =
            new BindableProperty<List<MonsterAtlasItemData>>(new List<MonsterAtlasItemData>());

        /// <summary>本关通奖励列表（已展开奖励组），供 RewardSCView 填充</summary>
        public BindableProperty<List<RewardItem>> RewardItems { get; } =
            new BindableProperty<List<RewardItem>>(new List<RewardItem>());
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        #endregion

        #region 事件
        /// <summary>列表数据就绪后通知 View 重绘 ScrollView</summary>
        public Action OnItemsChanged;
        /// <summary>奖励列表数据就绪后通知 View 重绘 RewardSCView</summary>
        public Action OnRewardItemsChanged;
        /// <summary>请求关闭面板</summary>
        public Action OnClosePanel;
        #endregion

        private TbMonsterBattleConfig _monsterBattleConfig;
        private TbCardBaseConfig _cardConfig;
        private TbMonsterGenConfig _monsterGenConfig;
        private TbStageBattleConfig _stageBattleConfig;
        private TbRewardGroupConfig _rewardGroupConfig;

        public override void OnCreate()
        {
            base.OnCreate();

            _monsterBattleConfig = DataConfigManager.Tables.TbMonsterBattleConfig;
            _cardConfig = DataConfigManager.Tables.TbCardBaseConfig;
            _monsterGenConfig = DataConfigManager.Tables.TbMonsterGenConfig;
            _stageBattleConfig = DataConfigManager.Tables.TbStageBattleConfig;
            _rewardGroupConfig = DataConfigManager.Tables.TbRewardGroupConfig;

            OnCloseCommand = new Command(OnClose);
        }

        /// <summary>
        /// 带参初始化：按关卡 ID 收集 20 天内出现的所有怪物种类（去重），写入 Items。
        /// 遍历顺序保持 StageList 的 Day 顺序，先出现的怪物排在前面。
        /// </summary>
        public void Setup(int stageId)
        {
            var genCfg = _monsterGenConfig.GetOrDefault(stageId);

            var items = new List<MonsterAtlasItemData>();
            if (genCfg != null && genCfg.StageList != null)
            {
                // 记录已收集的 CardId，保证每种怪物只展示一个
                var seen = new HashSet<int>();
                foreach (var dayCfg in genCfg.StageList)
                {
                    if (dayCfg.WaveList == null) continue;
                    foreach (var wave in dayCfg.WaveList)
                    {
                        if (wave.GenCards == null) continue;
                        foreach (var card in wave.GenCards)
                        {
                            if (!seen.Add(card.CardId)) continue;

                            var monsterCfg = _monsterBattleConfig.GetOrDefault(card.CardId);
                            var cardCfg = _cardConfig.GetOrDefault(card.CardId);
                            items.Add(new MonsterAtlasItemData
                            {
                                MonsterId = card.CardId,
                                Quality = monsterCfg != null ? monsterCfg.MonsterQuality : MonsterQuality.Ordinary,
                                Name = cardCfg != null ? cardCfg.Name : null,
                                Icon = cardCfg != null ? cardCfg.Icon : null,
                                Label = monsterCfg != null ? monsterCfg.Label : null,
                            });
                        }
                    }
                }
            }

            Items.Value = items;
            OnItemsChanged?.Invoke();

            CollectRewards(stageId);
        }

        /// <summary>
        /// 收集当前关卡的通关奖励：Rewards 字段直接是物品列表，
        /// RewardGroups 字段是奖励组 ID，需通过 TbRewardGroupConfig 展开为组内具体物品。
        /// </summary>
        private void CollectRewards(int stageId)
        {
            var stageCfg = _stageBattleConfig.GetOrDefault(stageId);
            var rewardItems = new List<RewardItem>();
            if (stageCfg != null)
            {
                if (stageCfg.Rewards != null)
                {
                    foreach (var rew in stageCfg.Rewards)
                        rewardItems.Add(new RewardItem(rew.Id, rew.Num));
                }

                if (stageCfg.RewardGroups != null)
                {
                    foreach (var group in stageCfg.RewardGroups)
                    {
                        var groupCfg = _rewardGroupConfig.GetOrDefault(group.Id);
                        if (groupCfg == null) continue;
                        foreach (var rew in groupCfg.RewardGroupList)
                            rewardItems.Add(new RewardItem(rew.ItemId, rew.ItemNum));
                    }
                }
            }

            RewardItems.Value = rewardItems;
            OnRewardItemsChanged?.Invoke();
        }

        private void OnClose()
        {
            OnClosePanel?.Invoke();
        }
    }
}
