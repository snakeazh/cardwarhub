using Config;
using DG.Tweening;
using QFramework;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 天流斩
     */
    public class SkyFlowAttackSkill : SkillBase
    {
        protected override void OnCast(object finalDirection)
        {
            
            Vector3 targetPosition = (Vector3)finalDirection;
            CreateSkyFlow(targetPosition);
            
        }

        private void CreateSkyFlow(Vector3 targetPosition)
        {
            var skillSkyPool = ObjectPoolManager.Instance.GetGameObjectPool(Config.ProjectileName);
            GameObject skillSkyObj = skillSkyPool.Get();
            if(skillSkyObj ==null) return;
            
            skillSkyObj.transform.SetParent(GameLand.Default.Bullet);
            skillSkyObj.transform.position = targetPosition;
            skillSkyObj.transform.localScale = Vector3.one*400f;
            DOTween.To(v => { }, 0, 0, 2f).onComplete += () =>
            {
                ReturnPool(skillSkyObj);
            };

            for (int i = 0; i < Config.DamageType.Count; i++)
            {
                DamageHit(skillSkyObj, this);
            }
        }

        private void ReturnPool(GameObject skyFlowEffectObj)
        {
            ObjectPoolManager.Instance.GetGameObjectPool(Config.ProjectileName).Return(skyFlowEffectObj);
        }

        private void DamageHit(GameObject skyFlowEffectObj, ISkill skillCard)
        {
            skyFlowEffectObj.GetComponent<SkyFlowBullet>().StartAttackSet(skillCard);
        }
    }
}