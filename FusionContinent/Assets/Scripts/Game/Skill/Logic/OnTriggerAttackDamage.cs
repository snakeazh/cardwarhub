using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using UnityEngine;

namespace Game.Skill.Logic
{
    public class OnTriggerAttackDamage : MonoBehaviour
    {
        private ISkill _skillCard;
        
        // 记录已经命中过的怪
        private readonly HashSet<MonsterCard> _hitTargets = new HashSet<MonsterCard>();
        private float _hitTargetDuration = 0f;

        public void SetStart(ISkill skillCard, float hitTargetDuration = 0f)
        {
            this._skillCard = skillCard;
            this._hitTargetDuration = hitTargetDuration;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // 如果已经触发过，直接返回
            if (collision.name != "HitBox")
            {
                return;
            }

            MonsterCard hitTarget = collision.transform.parent.GetComponent<MonsterCard>();
            if (hitTarget != null && hitTarget.enabled != false)
            {
                // 检查是否已经命中过
                if (!_hitTargets.Add(hitTarget)) return;
            
                // 命中过的列表延时重置
                if (_hitTargetDuration > 0f) DOVirtual.DelayedCall(_hitTargetDuration, () => { _hitTargets.Clear(); });
                
                // LogTool.Log(">>>> 攻击 - _cardId: "+hitTarget._cardId+", damage: "+damage+", baseAtk: "+ baseAtk);

                // 受伤坐标位置
                var damageHitPos = collision.bounds.ClosestPoint(transform.position);
                hitTarget.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(hitTarget, _skillCard, damageHitPos));
            
                // 特效
                // SpawnHitEffect(hitTarget.transform.position);
            }
        }
    
        /**
         * 受击表现
         */
        private void SpawnHitEffect(Vector3 position)
        {
            string hitEffect = _skillCard.Config.SkillHitEffect ?? "youxia_jn1_06";
            QFramework.GenericObjectPool pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(hitEffect);
            if (pool == null) return;

            var effect = pool.Get();
            if (effect == null) return;

            effect.transform.SetParent(GameLand.Default.DamageParent.transform);
            effect.transform.position = position;
            effect.transform.localScale = Vector3.one * 200;

            StartCoroutine(DisableAfterDelay(pool, effect, 0.5f));
        }

        private IEnumerator DisableAfterDelay(QFramework.GenericObjectPool pool, GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (effect != null)
            {
                pool.Return(effect);
            }
        }
        
        private void OnDisable()
        {
            // 清理集合，避免对象池复用后旧数据残留
            _hitTargets.Clear();
        }
        
    }
}

