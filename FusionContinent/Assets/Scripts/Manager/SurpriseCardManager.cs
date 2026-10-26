using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Attribute;
using UnityEngine;

namespace Manager
{
    public class SurpriseCardManager : MonoBehaviour
    {
        public static SurpriseCardManager Instance;
        
        private readonly Dictionary<int, SurpriseCard> _surpriseCards = new Dictionary<int, SurpriseCard>(); // 惊喜卡
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public void Init()
        {
            _surpriseCards.Clear();
        }

        /**
         * 添加惊喜卡
         */
        public SurpriseCard AddSurpriseCard(int surpriseId, int deBuffId)
        {
            // 获取惊喜卡信息
            if (!_surpriseCards.TryGetValue(surpriseId, out var surpriseCard))
            {
                surpriseCard = new SurpriseCard(surpriseId, deBuffId, 0);
                _surpriseCards.Add(surpriseId, surpriseCard);
                
                // 通知战斗Ui添加
                GameLandUI.Default.AddSurpriseCardView(surpriseId, deBuffId);
            }

            surpriseCard.Level++;
            
            // 属性生效
            var attrModifier = new AttributeModifier
            {
                SourceKey = surpriseCard.Id.ToString(),
                SourceType = AttributeSourceType.SurpriseCard,
            };
            
            foreach (var v in surpriseCard.Config.Attributes)
            {
                var attr = new AttributeData(v);
                switch (surpriseCard.Config.Type)
                {
                    case BattleSurpriseType.CoronationGift:
                        // 多个英灵，则加成效果会平分
                        var soldierCount = GamePlayManager.Default.villageGameCards.Count;
                        if (attr.Type == AttributeType.RedoubleDamage && soldierCount > 1)
                        {
                            attr.Value /= soldierCount;
                        }
                        
                        break;
                }
                
                AttributeManager.SetAttributeLogic(attrModifier, attr, true);
            }

            // 负收益属性
            var deBuffCfg = DataConfigManager.Tables.TbDeBuffConfig.GetOrDefault(deBuffId);
            if (deBuffCfg != null)
            {
                foreach (var v in deBuffCfg.Attributes)
                {
                    var attr = new AttributeData(v);
                    AttributeManager.SetAttributeLogic(attrModifier, attr, true);
                }
            }

            return surpriseCard;
        }
        
    }

    /**
     * 惊喜卡类
     */
    public class SurpriseCard
    {
        public readonly int Id;
        public readonly int DeBuffId;
        public int Level;
        public BattleSurpriseType Type;

        public readonly BattleSurpriseConfig Config;
        
        public SurpriseCard(int cardId, int deBuffId, int cardLevel = 1)
        {
            Config = DataConfigManager.Tables.TbBattleSurpriseConfig.GetOrDefault(cardId);
            if (Config == null)
            {
                LogTool.LogError("Get TbBattleSurpriseConfig does not exist Id: " + cardId);
                return;
            }

            Id = cardId;
            Level = cardLevel;
            DeBuffId = deBuffId;
        }
    }
}