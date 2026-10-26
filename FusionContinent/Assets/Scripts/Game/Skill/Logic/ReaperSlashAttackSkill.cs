using Config;
using DG.Tweening;
using Game.Skill.Logic;
using Manager;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 死神挥砍
     * GusSkillDeathSlash
     * 
     * @return 
     * @author Zpp
     * @create 2025/10/24
     **/
    public class ReaperSlashAttackSkill : SkillBase
    {
        [SerializeField] private float _localScale;// 物体缩放
        
        protected override void OnCast(object obj)
        {
            // 物体大小
            _localScale = 100f + Attributes.GetFinal(AttributeType.SkillRange);
            
            var direction = (Vector3)obj;
            ActionReaperSlash01(direction);
            
            DOVirtual.DelayedCall(0.5f, ActionReaperSlash02);
        }
        
        /**
         * 钩子
         * 
         * @return 
         * @author Zpp
         * @create 2025/10/24
         **/
        private void ActionReaperSlash01(Vector3 direction)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate($"{Config.ProjectileName}01");
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;

            // 子弹设置
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.position = Parent.transform.position;
            bullet.transform.localScale = Vector3.one * _localScale;
            
            var bulletScripts = bullet.transform.Find("ani_js_jn3_01/eff_js_jn3_liandao03/box");
            if (bulletScripts != null)
                bulletScripts.GetComponent<SpearmenReaperSlash>().SetData(this);

            // 让子弹朝向正确方向（与传入的 direction 一致）
            bullet.transform.up = direction;
            
            DOVirtual.DelayedCall(2f, () => { pool.Return(bullet); });
        }
        
        /**
         * 挥动镰刀
         * 
         * @return 
         * @author Zpp
         * @create 2025/10/24
         **/
        private void ActionReaperSlash02()
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate($"{Config.ProjectileName}02");
            if (pool == null) return;
        
            GameObject bullet = pool.Get();
            if (bullet == null) return;
            SoundManager.Instance.PlayVoiceInGame(this.Config.SoundId);
            // 子弹设置
            Vector3 dancePos = Parent.transform.position;
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.position = new Vector3(dancePos.x, dancePos.y - 2f, dancePos.z);
            bullet.transform.localScale = Vector3.one * _localScale;
            
            var bulletScripts = bullet.transform.Find("ani_js_jn3_02/eff_js_jn3_liandao02/box");
            if (bulletScripts != null)
                bulletScripts.GetComponent<OnTriggerAttackDamage>().SetStart(this);

            // DOVirtual.DelayedCall(4f, () => { pool.Return(bullet); });
        }
        
    }
}