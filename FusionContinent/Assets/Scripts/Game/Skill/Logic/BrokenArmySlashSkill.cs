using Config;
using DG.Tweening;
using Manager;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 破军一击
     * BondAttackEffect01
     **/
    public class BrokenArmySlashSkill : SkillBase
    {
        protected override void OnCast(object obj)
        {
            if (Config.SoundId > 0) SoundManager.Instance.PlayVoiceInGame(Config.SoundId);
            
            var direction = (Vector3)obj;
            OnCastBlow(direction);
        }
        
        private void OnCastBlow(Vector3 direction)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(Config.ProjectileName);
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;

            // 子弹设置
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.position = Parent.transform.position;
            bullet.transform.localScale = Vector3.one * (100 + Attributes.GetFinal(AttributeType.SkillRange));

            var bulletScripts = bullet.transform.Find("ani_tianfu_gongji_01/jian/box");
            if (bulletScripts != null)
            {
                float repelMonster = (int)Attributes.GetFinal(AttributeType.SkillRepelMonster);
                bulletScripts.GetComponent<DrawSlashBullet>().repelMonsterVal = repelMonster;
                bulletScripts.GetComponent<DrawSlashBullet>().SetStartAttack(this, 1f);
            }
            else
            {
                LogTool.LogError($"BrokenArmySlashSkill.OnCastBlow: {Config.ProjectileName} 找不到碰撞盒");
            }

            // 让子弹朝向正确方向（与传入的 direction 一致）
            bullet.transform.up = direction;
            
            DOVirtual.DelayedCall(2.8f, () => pool.Return(bullet));
        }
    }
}