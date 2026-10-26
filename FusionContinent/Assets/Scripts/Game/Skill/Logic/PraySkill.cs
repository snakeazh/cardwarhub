using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Attribute;
using FusionContinent.Skill;
using FusionContinent.Utils;
using Manager;
using QFramework;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 祈祷
     * 
     **/
    public class PraySkill
    {
        public bool OnEnable;

        private readonly SpecialEffectType _skillType = SpecialEffectType.Pray;

        // 固定加成属性
        private readonly AttributeModifier _modifier = new AttributeModifier
        {
            SourceKey = "PraySkill",
            SourceType = AttributeSourceType.Skill,
        };

        private List<AttributeData> _attributes = new List<AttributeData>();
        
        /**
         * 祈祷 - 随机强化场上的一名英灵
         * 
         * @return 
         * @author Zpp
         * @create 2026/2/10
         **/
        public void DoRandomSoldierFortify()
        {
            if (!OnEnable || GamePlayManager.Default == null) return;

            var soldierCount = GamePlayManager.Default.villageGameCards.Count;
            SoldierCard soldierCard = GamePlayManager.Default.villageGameCards[Random.Range(0, soldierCount)];
            if (soldierCard == null) return;
            
            var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.DataList.FirstOrDefault(v => v.SpecialEffectType == _skillType);
            if (skillCfg == null) return;
            
            // 读取技能属性
            if (_attributes.Count == 0)
            {
                var playerCard = Global.GetCardById(skillCfg.Id);
                var lvCfg = skillCfg.LevelPropertyList[playerCard?.Lv ?? 1 - 1];
                var prayAttr = lvCfg.Attributes.First(v => v.Type == AttributeType.PrayRandomSoldierFortify);
                var prayAttrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(prayAttr.Type);
                
                foreach (var attr in prayAttrCfg.AdditionAttributes)
                {
                    var attrData = new AttributeData(attr);
                    attrData.Value *= prayAttr.Value;
                    _attributes.Add(attrData);
                }
            }
            
            // 添加特效
            var pos = soldierCard.transform.position;
            ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
            {
                PoolName = skillCfg.ProjectileName,
                ParentTran = soldierCard.transform,
                Position = new Vector3(pos.x, pos.y - 0.8f, 0),
                LocalScale = 160f,
                DelayReturn = 1.5f
            });

            AttributeManager.SetAttributesLogic(_modifier, _attributes, true, 1, soldierCard.cardId);
        }



    }
}