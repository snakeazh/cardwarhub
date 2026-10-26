using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent.Attribute;
using Manager;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 月影盾（月影庇护）
     * 伊莱娅对场上所有友军生成1层月影护盾,能够抵挡来自敌人的1次攻击,护盾未消失时，会为英灵提供(5+5%自身魔法强度)魔法强度,该效果可以叠加,冷却时间:50秒
     * 
     * EliaSkillMoonshadowWard
     **/
    public class MoonlightShieldSkill : SkillBase
    {
        
        

        private List<AttributeData> _attributes = new List<AttributeData>();
        
        protected override void OnCast(object obj)
        {
            OnCastStart();
        }
        
        private void OnCastStart()
        {
            _attributes.Clear();
            if (GamePlayManager.Default == null) return;
            
            // 计算血量：恢复(技能攻击力+30%自身魔法强度)生命值
            var spellPower = Parent.BaseAttributes.GetFinal(AttributeType.SoldierSpellPower) * Attributes.GetFinal(AttributeType.SkillSpellInheritance);
            var addVal = Attributes.GetFinal(AttributeType.SkillBasicAttackPower) + spellPower;
            
            GamePlayManager.Default.TraverseVillageSoldier((soldierCard) =>
            {
                soldierCard.AddMoonlightShield(addVal);
            });
        }
    }

    public class MoonlightShield
    {
        public QFramework.GenericObjectPool Pool;
        public GameObject Obj;

        public int ShieldNum;
        
        // 固定加成属性
        private readonly AttributeModifier _modifier = new AttributeModifier
        {
            SourceKey = "MoonlightShieldSkill",
            SourceType = AttributeSourceType.Skill,
        };

        public void AddShield(SoldierCard soldierCard, float addVal)
        {
            if (ShieldNum == 0)
            {
                var pos = soldierCard.transform.position;
                (Pool, Obj) = ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                {
                    PoolName = "EliaSkillMoonshadowWard01",
                    ParentTran = soldierCard.transform,
                    Position = new Vector3(pos.x, pos.y, pos.z),
                    LocalScale = 160,
                    DelayReturn = 0
                });
            }
            
            ShieldNum++;
            AttributeManager.SetAttributeLogic(_modifier, new AttributeData()
            {
                Type = AttributeType.SoldierSpellPower,
                Value = addVal,
                ValueType = ValueType.Percentage,
            }, false, 1, soldierCard.cardId);
        }

        public void ShieldBroken(SoldierCard soldierCard)
        {
            ShieldNum--;
            ReturnPool();

            if (Obj != null)
            {
                // 护盾破裂效果
                ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                {
                    PoolName = "EliaSkillMoonshadowWard02",
                    ParentTran = Obj.transform.parent,
                    Position = Obj.transform.position,
                    LocalScale = 165,
                    DelayReturn = 0.5f
                });
            }
            
            AttributeManager.SetAttributeLogic(_modifier, new AttributeData()
            {
                Type = AttributeType.SoldierSpellPower,
                Value = 0,
                ValueType = ValueType.Percentage,
            }, false, 0, soldierCard.cardId);
        }
        
        private void ReturnPool()
        {
            if (ShieldNum > 0) return;
            ShieldNum = 0;
            
            if (Pool == null) return;
            Pool.Return(Obj);
        }
    }
}