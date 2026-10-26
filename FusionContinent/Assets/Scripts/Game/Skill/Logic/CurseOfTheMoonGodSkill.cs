using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using QFramework;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 月神诅咒
     * EliaSkillLunarCurse01
     * 
     * @return 
     * @author Zpp
     * @create 2026/2/10
     **/
    public class CurseOfTheMoonGod : SkillBase
    {
        protected override void OnCast(object finalDirection)
        {
            CreateDraw((Vector3)finalDirection);
        }

        private void CreateDraw(Vector3 pos)
        {
            var localScale = Attributes.GetFinal(AttributeType.SkillRange);
            var delayReturn = Mathf.Min(Attributes.GetFinal(AttributeType.SkillDuration), 6f);
            if (delayReturn <= 0f)
                delayReturn = 1f;

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
                bulletScripts.SetStartAttack(this, 1f);
        }
    }
}