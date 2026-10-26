using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Buff;
using FusionContinent.Skill;
using QFramework;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 圣光坠落
     * EliaSkillLightFall01
     * 
     * @return 
     * @author Zpp
     * @create 2026/2/10
     **/
    public class TheHolyLightFallsSkill : SkillBase
    {
        protected override void OnCast(object finalDirection)
        {
            CreateDraw((Vector3)finalDirection);
        }

        private void CreateDraw(Vector3 pos)
        {
            var localScale = Attributes.GetFinal(AttributeType.SkillRange);
            var delayReturn = Mathf.Min(Attributes.GetFinal(AttributeType.SkillDuration), 6f);

            var (_, bullet) = ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
            {
                PoolName = Config.ProjectileName,
                ParentTran = GameLand.Default.Bullet,
                Position = pos,
                LocalScale = 100f + localScale,
                DelayReturn = delayReturn
            });
            
            var bulletScripts = bullet.GetComponent<DrawSlashBullet>();
            if (bulletScripts != null)
            {
                foreach (var buff in Config.LevelPropertyList[Level - 1].Buffs)
                {
                    // Buffs.AddBuff(new BuffBase(buff));
                }
                
                bulletScripts.SetStartAttack(this, 1f);
            }

        }
    }
}