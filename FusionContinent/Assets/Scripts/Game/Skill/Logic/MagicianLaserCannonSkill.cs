using Config;
using DG.Tweening;
using UnityEngine;

namespace FusionContinent.Skill
{
    /*
     * 魔法射线
     * MagicianLaserCannonProjectile
     * 
     * @return 
     * @author Zpp
     * @create 2025/10/24
     **/
    public class MagicianLaserCannonSkill : SkillBase
    {
        private float _scale = 100f;// 物体缩放
        private float _duration;
        
        protected override void OnCast(object obj)
        {
            // 物体大小
            _scale = Attributes.GetFinal(AttributeType.SkillRange);
            
            // 技能持续时间
            _duration = Attributes.GetFinal(AttributeType.SkillDuration);
            
            var directionPos = (Vector3)obj;
            OnShadowFormation(directionPos);
        }

        private void OnShadowFormation(Vector3 directionPos)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(Config.ProjectileName);
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;

            // 子弹设置
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.position = new Vector3(Parent.transform.position.x, Parent.transform.position.y + 1f, 0);
            bullet.transform.localScale = Vector3.one;
            bullet.GetComponent<MagicianLaserCannonAttack>().StartAttack(this, directionPos);
            
            DOVirtual.DelayedCall(_duration, () =>
            {
                bullet.GetComponent<MagicianLaserCannonAttack>().StopAttack();
                pool.Return(bullet); 
            });
        }
        
    }
}