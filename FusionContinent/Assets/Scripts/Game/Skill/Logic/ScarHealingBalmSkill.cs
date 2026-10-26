using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using Game.Battle;
using QFramework;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 抚痕灵愈
     * 
     **/
    public class ScarHealingBalmSkill : SkillBase
    {
        protected override void OnCast(object finalDirection)
        {
            ScarHealingBalm();
        }

        void ScarHealingBalm()
        {
            if (GamePlayManager.Default == null) return;

            // 计算血量：恢复(技能攻击力+30%自身魔法强度)生命值
            var spellPower = Parent.BaseAttributes.GetFinal(AttributeType.SoldierSpellPower) * Attributes.GetFinal(AttributeType.SkillSpellInheritance);
            var addHpVal = Mathf.CeilToInt(Attributes.GetFinal(AttributeType.SkillBasicAttackPower) + spellPower);
            if (addHpVal <= 0) return;
            
            GamePlayManager.Default.TraverseVillageSoldier((soldierCard) =>
            {
                var pos = soldierCard.transform.position;
                soldierCard.SetSoldierHp(addHpVal);
                BattleRuleTreatmentTrigger.Trigger(Parent, soldierCard, TakeDamageType.Treatment);
                ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                {
                    PoolName = "EliaSkillSoothmark01",
                    ParentTran = soldierCard.transform,
                    Position = new Vector3(pos.x, pos.y - 0.8f, 0),
                    LocalScale = 40f,
                    DelayReturn = 2.1f
                });
            });
            
        }

    }
}