using Config;
using DG.Tweening;
using UnityEngine;

namespace FusionContinent.Skill
{
    /*
     * 暗影法阵 LujiaSkillShadowCircle01
     * 
     * @return 
     * @author Zpp
     * @create 2025/10/24
     **/
    public class MagicShadowFormationSkill : SkillBase
    {
        protected override void OnCast(object obj)
        {
            
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
            bullet.transform.position = directionPos;
            bullet.transform.localScale = Vector3.one * (100 + Attributes.GetFinal(AttributeType.SkillRange));
            bullet.GetComponent<MagicianShadowFormation>().SetData(this);
            
            DOVirtual.DelayedCall(4.2f, () => { pool.Return(bullet); });
        }
        
    }
}