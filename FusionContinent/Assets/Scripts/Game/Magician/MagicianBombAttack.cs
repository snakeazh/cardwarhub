using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using FusionContinent.Skill;
using UnityEngine;
using QFramework;

namespace FusionContinent
{
    public partial class MagicianBombAttack : MonoBehaviour
    {

        private ISkill _skillCard;

        private Vector3 splitDirection;
        
        public ItemGenericObjectPool pool;

        private SoldierCard parentCard;
        private GameObject splitProjectile;

        private HashSet<MonsterCard> hitTargets = new HashSet<MonsterCard>(); // 用于防止对同一个目标造成多次伤害

        private void Awake()
        {

        }


        public void StartAttack(ItemGenericObjectPool p, ISkill skillCard)
        {
            pool = p;
            _skillCard = skillCard;

            // 延时关闭
            StartCoroutine(DisableAfterDelay(gameObject, 0.5f));
        }
        
        private IEnumerator DisableAfterDelay(GameObject damageObject, float delay)
        {
            // 等待指定的时间
            yield return new WaitForSeconds(delay);

            // 检查对象是否仍然有效
            if (damageObject != null)
            {
                pool.Return(damageObject);
            }
        }
        
        private void StopAttack()
        {
            
            GameObject.Destroy(splitProjectile); // 销毁对象
            hitTargets = new HashSet<MonsterCard>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.name != "HitBox")
            {
                return;
            }

            MonsterCard hitTarget = collision.transform.parent.GetComponent<MonsterCard>();
            if (hitTarget != null && !hitTargets.Contains(hitTarget) && hitTarget.enabled != false)
            {
                hitTargets.Add(hitTarget);
                hitTarget.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(
                    hitTarget, _skillCard, collision.bounds.ClosestPoint(transform.position)));
            }
        }

        private void OnDestroy()
        {
            // 确保子弹销毁时，停止所有与其相关的 DOTween 动画
            DOTween.Kill(transform, true);
        }

    }
}
