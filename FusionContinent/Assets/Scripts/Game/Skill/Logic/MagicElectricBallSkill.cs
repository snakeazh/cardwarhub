using Config;
using DG.Tweening;
using Game.Skill.Logic;
using UnityEngine;

namespace FusionContinent.Skill
{
    /*
     * 电幻法球
     * MagicianElectricBall
     * 
     * @return 
     * @author Zpp
     * @create 2025/10/24
     **/
    public class MagicElectricBallSkill : SkillBase
    {
        private float _duration;
        
        protected override void OnCast(object obj)
        {
            // 技能持续时间
            _duration = Attributes.GetFinal(AttributeType.SkillDuration);
            
            var directionPos = (Vector3)obj;
            OnElectricBall(directionPos);
        }

        private void OnElectricBall(Vector3 direction)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(Config.ProjectileName);
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;

            // 子弹设置
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.localScale = Vector3.one * (100f + Attributes.GetFinal(AttributeType.SkillRange));
            
            // 计算子弹的旋转角度，使其朝向目标方向
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            bullet.transform.position = Parent.transform.position;

            var missileSpeed = Attributes.GetFinal(AttributeType.SkillMissileSpeed);
            bullet.GetComponent<WallReboundBullet>().SetElectricBall(this, direction, missileSpeed);
            
            DOVirtual.DelayedCall(_duration, () =>
            {
                pool.Return(bullet); 
            });
        }
        
    }
}