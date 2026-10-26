using System;
using System.Collections.Generic;
using System.Linq;
using UIFramework;
using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.UserTool;

namespace Game.Soldier
{
    /// <summary>
    /// 通用技能面板 ViewModel。
    /// 去佩戴改造：奥术卡不再装备到槽位，已拥有即生效，装备相关状态与保存逻辑已移除。
    /// </summary>
    public class GeneralSkillPanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<List<PlayerCard>> GeneralSkillList { get; } = new BindableProperty<List<PlayerCard>>(new List<PlayerCard>());
        public BindableProperty<List<QualityType>> QualityFilters { get; } = new BindableProperty<List<QualityType>>(new List<QualityType>());
        public BindableProperty<string> CardCountText { get; } = new BindableProperty<string>("0/0");
        #endregion

        #region 命令
        public Command ClearFiltersCommand { get; private set; }
        public Command<QualityType> ToggleQualityFilterCommand { get; private set; }
        public Command<int> OnSkillCardClickCommand { get; private set; }
        #endregion

        #region 配置数据
        private TbCardBaseConfig mCardBaseConfig;
        private TbCardDevelopConfig mCardDevelopConfig;
        private TbSpecialEffectsConfig mSpecialEffectsConfig;
        #endregion

        public override UserToolData GetUserToolConfig()
        {
            return GlobalFunc.GetUserResourceConfig(PageType.ArcCardPage);
        }

        public override void OnCreate()
        {
            base.OnCreate();

            mCardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            mCardDevelopConfig = DataConfigManager.Tables.TbCardDevelopConfig;
            mSpecialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;

            ClearFiltersCommand = new Command(ClearFilters);
            ToggleQualityFilterCommand = new Command<QualityType>(ToggleQualityFilter);
            OnSkillCardClickCommand = new Command<int>(OnSkillCardClick);

            LogTool.Log("[GeneralSkillPanelViewModel] 创建完成");
        }

        /// <summary>
        /// 初始化通用技能列表
        /// </summary>
        public void InitGeneralSkillList()
        {
            var skillList = new List<PlayerCard>();

            foreach (var cfg in mCardBaseConfig.DataList)
            {
                if (cfg.Type != CardType.GeneralSkill)
                    continue;

                var skillCfg = mSpecialEffectsConfig.GetOrDefault(cfg.Id);
                if (skillCfg == null)
                    continue;

                var cardInfo = Global.GetCardById(cfg.Id, new PlayerCard()
                {
                    Lv = 1,
                    IsHave = false,
                    Config = cfg,
                    CardId = cfg.Id,
                    ObjData = new CardDataStruct(),
                });

                skillList.Add(cardInfo);
            }

            foreach (var card in skillList)
            {
                var q = mSpecialEffectsConfig.GetOrDefault(card.CardId)?.LevelPropertyList.First().Quality;
                LogTool.Log($"[GeneralSkillPanelVM] CardId={card.CardId}, Quality={q}");
            }

            GeneralSkillList.Value = skillList;
            UpdateCardCount();
        }

        /// <summary>
        /// 获取筛选后的技能列表
        /// </summary>
        public List<PlayerCard> GetFilteredSkillList()
        {
            var result = new List<PlayerCard>();
            var filters = QualityFilters.Value;

            foreach (var playerSkill in GeneralSkillList.Value)
            {
                var skillCfg = mSpecialEffectsConfig.GetOrDefault(playerSkill.CardId);
                if (skillCfg == null)
                    continue;

                if (filters.Count > 0 && !filters.Contains(skillCfg.LevelPropertyList[0].Quality))
                    continue;

                result.Add(playerSkill);
            }

            // 排序规则：
            // 1) 已拥有 > 未拥有
            // 2) 同组内：品质高 > 品质低
            // 3) 同品质：数量多 > 数量少
            // 4) 同数量：id(这里用 CardId) 小 > 大
            return result
                .OrderByDescending(v => v.IsHave)
                .ThenByDescending(v => (int)((mSpecialEffectsConfig.GetOrDefault(v.CardId).LevelPropertyList[0]).Quality))
                .ThenByDescending(v => v.Num)
                .ThenBy(v => v.CardId)
                .ToList();
        }

        /// <summary>
        /// 更新卡牌数量显示
        /// </summary>
        private void UpdateCardCount()
        {
            var filteredList = GetFilteredSkillList();
            int filteredCount = 0;
            int filteredTotalCount = filteredList.Count;

            foreach (var playerSkill in filteredList)
            {
                if (playerSkill.IsHave)
                    filteredCount++;
            }

            CardCountText.Value = $"{filteredCount}/{filteredTotalCount}";
        }

        /// <summary>
        /// 品质筛选按钮点击
        /// </summary>
        public void ClearFilters()
        {
            QualityFilters.Value = new List<QualityType>();
            UpdateCardCount();
        }

        public void ToggleQualityFilter(QualityType qualityType)
        {
            QualityFilters.Value = new List<QualityType> { qualityType };
            UpdateCardCount();
        }

        /// <summary>
        /// 技能卡片点击
        /// </summary>
        private void OnSkillCardClick(int cardId)
        {
            LogTool.Log($"[GeneralSkillPanelViewModel] 点击技能卡片: {cardId}");
        }

        /// <summary>
        /// 检查卡牌消耗红点
        /// </summary>
        public int CheckCardConsumeRedDot(PlayerCard playerSkill)
        {
            if (playerSkill == null)
            {
                return 0;
            }

            var devCfg = mCardDevelopConfig.GetOrDefault(playerSkill.CardId);
            if (devCfg == null || !playerSkill.IsHave || playerSkill.Lv >= devCfg.LevelPropertyList.Count)
            {
                return 0;
            }

            var devLvCfg = devCfg.LevelPropertyList[playerSkill.Lv - 1];
            return Items.CheckEnough(devLvCfg.ConsumeItems) ? 1 : 0;
        }

        /// <summary>
        /// 获取卡牌进度数据
        /// </summary>
        public List<long> GetCardProgressData(PlayerCard playerCard)
        {
            var devCfg = mCardDevelopConfig.GetOrDefault(playerCard.CardId);
            if (devCfg == null || playerCard.Lv > devCfg.LevelPropertyList.Count)
                return new List<long> { -1 };

            var devLvCfg = devCfg.LevelPropertyList[playerCard.Lv - 1];

            var cardProgress = new List<long>();
            if (devLvCfg.ConsumeItems.Count > 0)
            {
                foreach (var v in devLvCfg.ConsumeItems)
                {
                    var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(v.Id);
                    if (itemCfg.Type is ItemType.AttributeCardFragment or ItemType.SkillCardFragment)
                    {
                        cardProgress.Add(Global.GetItemNumById(v.Id));
                        cardProgress.Add(v.Num);
                        break;
                    }
                }
            }

            if (cardProgress.Count == 0)
            {
                cardProgress.Add(-1);
            }

            return cardProgress;
        }

        /// <summary>
        /// 获取技能配置
        /// </summary>
        public SpecialEffectsConfig GetSkillConfig(int cardId)
        {
            return mSpecialEffectsConfig.GetOrDefault(cardId);
        }

        #region 事件
        public System.Action<int> OnOpenSkillUpgradePanel;
        public System.Action OnOpenResolvePanel;
        public System.Action OnOpenConbinePanel;
        #endregion

        public void NotifyOpenSkillUpgradePanel(int skillId)
        {
            OnOpenSkillUpgradePanel?.Invoke(skillId);
        }

        public void NotifyOpenResolvePanel()
        {
            OnOpenResolvePanel?.Invoke();
        }

        public void NotifyOpenConbinePanel()
        {
            OnOpenConbinePanel?.Invoke();
        }
    }
}
