using Config;
using DG.Tweening;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 旋风斩
     * GusSkillWhirlwind01
     * 
     * @return 
     * @author Zpp
     * @create 2025/10/24
     **/
    public class TornadoAttackSkill : SkillBase
    {
        [SerializeField] private readonly float scale = 1f;// 物体缩放
        
        protected override void OnCast(object obj)
        {
            var directionPos = (Vector3)obj;
            OnCastDimensionalSlash(directionPos);
        }
        
        private void OnCastDimensionalSlash(Vector3 directionPos)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(Config.ProjectileName);
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;

            // 子弹设置
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.position = directionPos;
            bullet.transform.localScale = Vector3.one * (scale + Attributes.GetFinal(AttributeType.SkillRange));
            bullet.GetComponent<TornadoProjectile>().Launch(this, directionPos, 0.5f);
            
            // DOVirtual.DelayedCall(4.2f, () => { pool.Return(bullet); });
        }
    }
}