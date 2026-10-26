using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent.Skill.Handler;
using Manager;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 重击斩
     * SpearmenHeavySplittingChop
     **/
    public class HeavyBlowSkill : SkillBase
    {
        private Tween _continuousCastTween;
        
        protected override void OnCast(object obj)
        {
            if (Config.SoundId > 0) SoundManager.Instance.PlayVoiceInGame(Config.SoundId);
            
            var direction = (Vector3)obj;
            StartContinuousCast(direction);
        }

        private void StartContinuousCast(Vector3 direction)
        {
            // 连续释放次数，至少1次
            // var continuousAtkNum = Mathf.Max(Mathf.CeilToInt(Attributes.GetFinal(AttributeType.SkillContinuousAtkNum)), 1);
            // 每次释放间隔（秒），最小0.05防止极端值
            var continuousAtkInterval = Mathf.Max(Attributes.GetFinal(AttributeType.SkillContinuousAtkSpeed), 0.05f);

            StopContinuousCast();

            // 第一次立即释放
            OnCastBlow(direction);

            // 只有1次时无需计时器
          
            _continuousCastTween = DOVirtual.DelayedCall(continuousAtkInterval, () =>
                {
                    OnCastBlow(direction);
                })
                .SetLoops(-1)
                .SetUpdate(false);
        }

        public override void StopLegacyRuntime()
        {
            StopContinuousCast();
        }

        private void StopContinuousCast()
        {
            _continuousCastTween?.Kill();
            _continuousCastTween = null;
        }
        
        private void OnCastBlow(Vector3 direction)
        {
            var bullet = EffectManager.Instance.PlayEffect(Config.ProjectileName,Vector3.zero,parent:GameLand.Default.HeavyBlowPos);
            if (bullet == null) return;
            bullet.transform.localPosition = Vector3.zero;
            // 子弹设置
            // var bulletScripts = bullet.transform.Find("eff_js_ts05/ani_js_ts05/box");
            // if (bulletScripts != null)
            //     bulletScripts.GetComponent<DrawSlashBullet>().SetStartAttack(this);
            //
            // // 让子弹朝向正确方向（与传入的 direction 一致）
            // bullet.transform.up = direction;
            //
            DOVirtual.DelayedCall(1f, () =>
            {
                List<HitResult> hits = null;
                hits = HitSystem2D.Circle(bullet.transform.position, 7, LayerMask.GetMask("MonsterBox"));
                
                for (int i = 0; i < hits.Count; i++)
                {
                    MonsterCard hitTarget = hits[i].target.transform.parent.GetComponent<MonsterCard>();
                    if (hitTarget == null || hitTarget.enabled == false) continue;
                    

                    var damageHitPos = hits[i].hitPoint;

                    // 播放命中特效
                    if (!string.IsNullOrEmpty(Config.SkillHitEffect))
                    {
                        EffectManager.Instance.PlayEffect(Config.SkillHitEffect, damageHitPos);
                    }

                    try
                    {
                        // 伤害
                        hitTarget.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(hitTarget, this, damageHitPos));
                        // EventBus.Publish(new ProjectileHitEvent()
                        // {
                        //     skillContext = context,
                        //     hitpoint = damageHitPos,
                        //     monsterCard = hitTarget
                        // });
                    }
                    catch { }
                }
            });
        }
    }
}