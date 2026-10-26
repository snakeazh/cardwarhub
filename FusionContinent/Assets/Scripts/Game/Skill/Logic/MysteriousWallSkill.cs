using Config;
using DG.Tweening;
using UnityEngine;

namespace FusionContinent.Skill
{
    /*
     * 幽邃之墙 LujiaSkillMysteriousWall01
     * 
     * @return 
     * @author Zpp
     * @create 2025/10/24
     **/
    public class MysteriousWallSkill : SkillBase
    {
        protected override void OnCast(object obj)
        {
            var directionPos = (Vector3)obj;
            OnCastMysteriousWall(directionPos);
        }

        private void OnCastMysteriousWall(Vector3 directionPos)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(Config.ProjectileName);
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;

            // 子弹设置
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.position = directionPos;
            bullet.transform.localScale = Vector3.one * (100 + Attributes.GetFinal(AttributeType.SkillRange));
            bullet.GetComponent<DrawSlashBullet>().SetStartAttack(this, 0.5f);
            
            // 技能持续时间
            var duration = Attributes.GetFinal(AttributeType.SkillDuration);
            DOVirtual.DelayedCall(duration, () => { pool.Return(bullet); });
        }
        
    }
}