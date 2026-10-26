using Config;
using DG.Tweening;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 重劈斩
     * SpearmenHeavySplittingChop
     * 
     * @return 
     * @author Zpp
     * @create 2025/10/24
     **/
    public class HeavySplittingChopSkill : SkillBase
    {
        protected override void OnCast(object obj)
        {
            
            var direction = (Vector3)obj;
            OnCastHeavySplittingChop(direction);
        }
        
        private void OnCastHeavySplittingChop(Vector3 direction)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(Config.ProjectileName);
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;

            // 子弹设置
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.position = Parent.transform.position;
            bullet.transform.localScale = Vector3.one * (100f + Attributes.GetFinal(AttributeType.SkillRange));
            
            var bulletScripts = bullet.transform.Find("ani_xl_jn5_02/box");
            if (bulletScripts != null)
                bulletScripts.GetComponent<DrawSlashBullet>().SetStartAttack(this);
            
            // 让子弹朝向正确方向（与传入的 direction 一致）
            bullet.transform.up = direction;
            
            DOVirtual.DelayedCall(1.2f, () => { pool.Return(bullet); });
        }
    }
}