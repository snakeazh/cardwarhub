using Config;
using UnityEngine;

namespace FusionContinent.Skill
{
    /*
     * 雷霆领域
     * MagicianElectricBall
     * 
     * @return 
     * @author Zpp
     * @create 2025/10/24
     **/
    public class ThunderFieldFullScreenSkill : SkillBase
    {
        private float _duration;
        
        protected override void OnCast(object obj)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(Config.ProjectileName);
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;
            
            // 技能连射速度
            float continuousAtkSpeed = Mathf.Max(Attributes.GetFinal(AttributeType.SkillContinuousAtkSpeed), 1f);
            
            // 范围
            float localScale = Mathf.Max(Attributes.GetFinal(AttributeType.SkillRange), 140f);
            
            // 子弹设置
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.localScale = Vector3.one * localScale;
            bullet.transform.position = Vector3.zero;
            var effect = bullet.GetComponent<ThunderFieldFullScreenEffect>();
            if (effect == null)
            {
                LogTool.LogError($"[ThunderFieldFullScreenSkill] 预制体缺少 ThunderFieldFullScreenEffect: {Config.ProjectileName}");
                pool.Return(bullet);
                return;
            }

            effect.SetStart(this, continuousAtkSpeed);
        }
        
    }
}