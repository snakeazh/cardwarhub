using Config;
using DG.Tweening;
using UnityEngine;

namespace FusionContinent.Skill
{
    /*
     * 次元斩 XilanSkillDimensionalSlash01
     * 
     * @return 
     * @author Zpp
     * @create 2025/10/24
     **/
    public class DimensionalSlashSkill : SkillBase
    {
        private float _localScale;
        
        protected override void OnCast(object obj)
        {
            // 物体大小
            _localScale = 200 + Attributes.GetFinal(AttributeType.SkillRange);
            
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
            bullet.transform.localScale = Vector3.one * _localScale;
            bullet.GetComponent<BoxCollider2D>().enabled = true;
            bullet.GetComponent<DrawSlashBullet>().SetStartAttack(this, 0.5f);
            
            DOVirtual.DelayedCall(3f, () => { bullet.GetComponent<BoxCollider2D>().enabled = false; });
            DOVirtual.DelayedCall(4.2f, () => { pool.Return(bullet); });
        }
        
    }
}