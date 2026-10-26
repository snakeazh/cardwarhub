using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using UnityEngine;
using QFramework;

namespace FusionContinent
{
    /**
     * 战斗卡组配置结构
     */
    public class BattleCardGroup
    {
        public int CardId;
        public int CardLevel;
        public int CardSlotIndex;
        public CardType CardType;
        public BindableProperty<bool> IsUnlock = new BindableProperty<bool>();
        public CardBaseConfig CardBaseCfg;
        public MainAttributeType MainAttrType; 
        
        public BattleCardGroup(int id, int level, CardType type, int slotIndex, bool isUnlock = false)
        {
            CardId = id;
            CardType = type;
            CardLevel = level;
            IsUnlock.Value = isUnlock;
            CardSlotIndex = slotIndex;
        }

        public void ResetData()
        {
            CardBaseCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(CardId);
            switch (CardBaseCfg.Type)
            {
                case CardType.Skill:
                    var specialCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(CardId);
                    // 组合生效
                    if (specialCfg.CombinationEffect.Count > 0)
                    {
                        foreach (var combinationId in specialCfg.CombinationEffect)
                        {
                            if (Global.SoldierSkillTeams.Any(v => v.CardId == combinationId) == false)
                            {
                                IsUnlock.Value = false;// 不满足条件，未解锁状态
                                break;
                            }
                        }
                    }
                    break;
            }
        }

        public QualityType GetQualityType()
        {
            return  DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(CardId)
                .LevelPropertyList[CardLevel-1].Quality;
        }

        /**
         * 解锁卡
         */
        public void UnlockCard()
        {
            if (IsUnlock.Value) return;

            IsUnlock.Value = true;
            switch (CardType)
            {
                case CardType.Skill:// 解锁技能
                    var specialCardCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(CardId);
                    GamePlayManager.Default.AddSkillCard(specialCardCfg.Id, specialCardCfg.ActsOnCard, 1, true);
                    break;
            }
        }
        
    }
    
}