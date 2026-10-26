using Config;
using DG.Tweening;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 风舞疾袭
     * BondAttackSpeedEffect
     **/
    public class WindDanceAttackSkill : SkillBase
    {
        protected override void OnCast(object obj)
        {
            OnCastBlow();
        }
        
        private void OnCastBlow()
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate($"{Config.ProjectileName}01");
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;

            // 子弹设置
            var pos = Parent.transform.position;
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.position = new Vector3(0, pos.y - 1.71f, pos.z);
            bullet.transform.localScale = Vector3.one * (130 + Attributes.GetFinal(AttributeType.SkillRange));
            
            // 技能连射速度
            float continuousAtkSpeed = Mathf.Max(Attributes.GetFinal(AttributeType.SkillContinuousAtkSpeed), 1f);
            var effect = bullet.GetComponent<WindDanceAttackEffect>();
            if (effect == null)
            {
                LogTool.LogError($"[WindDanceAttackSkill] 预制体缺少 WindDanceAttackEffect: {Config.ProjectileName}01");
                pool.Return(bullet);
                return;
            }

            effect.SetStart(this, continuousAtkSpeed);
        }
    }
}