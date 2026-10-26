using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using FusionContinent.Model;

namespace Game.Battle
{
    /// <summary>
    /// 战斗被动卡牌 ViewModel
    /// 管理被动技能卡牌的显示和详情
    /// 使用与 GameLandUI 相同的数据结构
    /// </summary>
    public class BattlePassiveCardViewModel : ViewModel
    {
        #region 属性
        /// <summary>
        /// 被动卡牌ID列表
        /// </summary>
        public BindableProperty<List<int>> PassiveCardIds { get; } = 
            new BindableProperty<List<int>>(new List<int>());

        /// <summary>
        /// 当前选中的卡牌ID
        /// </summary>
        public BindableProperty<int> SelectedCardId { get; } = new BindableProperty<int>(0);

        /// <summary>
        /// 是否显示详情面板
        /// </summary>
        public BindableProperty<bool> IsDetailVisible { get; } = new BindableProperty<bool>(false);
        #endregion

        #region 命令
        public Command<int> OnSelectCardCommand { get; private set; }
        public Command OnCloseDetailCommand { get; private set; }
        #endregion

        #region 事件
        public System.Action<int> OnCardSelected;
        public System.Action OnDetailClosed;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            // 初始化命令
            OnSelectCardCommand = new Command<int>(OnSelectCard);
            OnCloseDetailCommand = new Command(OnCloseDetail);

            // 初始化被动卡牌数据
            InitializePassiveCards();

            LogTool.Log("[BattlePassiveCardViewModel] 创建完成");
        }

        /// <summary>
        /// 初始化被动卡牌 - 使用与 GameLandUI 相同的逻辑
        /// </summary>
        private void InitializePassiveCards()
        {
            var team = FusionContinent.Model.Team.GetTeamByTeamId(Talent.GetUseTalent().TalentId);
            if (team == null) return;

            // 获取被动技能卡牌（GeneralSkill）
            var cardIds = team.Cards
                .Where(cardId => {
                    var cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(cardId);
                    return cardCfg?.Type == CardType.GeneralSkill;
                })
                .ToList();

            PassiveCardIds.Value = cardIds;
            LogTool.Log($"[BattlePassiveCardViewModel] 初始化 {cardIds.Count} 张被动卡牌");
        }

        /// <summary>
        /// 获取 PlayerCard 数据 - 与 GameLandUI 一致
        /// </summary>
        public PlayerCard GetPlayerCard(int cardId)
        {
            return Global.GetCardById(cardId);
        }

        /// <summary>
        /// 获取卡牌配置 - 与 GameLandUI 一致
        /// </summary>
        public CardBaseConfig GetCardConfig(int cardId)
        {
            return DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(cardId);
        }

        /// <summary>
        /// 获取技能配置 - 与 GameLandUI 一致
        /// </summary>
        public SpecialEffectsConfig GetSkillConfig(int cardId)
        {
            return DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(cardId);
        }

        /// <summary>
        /// 获取技能等级配置 - 与 GameLandUI 一致
        /// </summary>
        public SpecialLevelPropertyList GetSkillLevelConfig(int cardId, int level)
        {
            var skillCfg = GetSkillConfig(cardId);
            if (skillCfg == null || skillCfg.LevelPropertyList == null) return null;
            
            int index = level - 1;
            if (index < 0 || index >= skillCfg.LevelPropertyList.Count) return null;
            
            return skillCfg.LevelPropertyList[index];
        }

        #region 命令实现

        private void OnSelectCard(int cardId)
        {
            SelectedCardId.Value = cardId;
            IsDetailVisible.Value = true;
            OnCardSelected?.Invoke(cardId);
            
            // 触发引导
            Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS, GuideType.PressArcaneShowDetails);
            
            LogTool.Log($"[BattlePassiveCardViewModel] 选中卡牌: {cardId}");
        }

        private void OnCloseDetail()
        {
            IsDetailVisible.Value = false;
            OnDetailClosed?.Invoke();
            LogTool.Log("[BattlePassiveCardViewModel] 关闭详情");
        }

        #endregion
    }
}
