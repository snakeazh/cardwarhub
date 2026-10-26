using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.Skill;
using UnityEngine;
using QFramework;
using Random = UnityEngine.Random;

namespace FusionContinent
{
    /**
     * 魔法师激光炮
     */
    public partial class MagicianLaserCannonAttack : MonoBehaviour
    {
        private bool isAttacking;
        private int baseAtk;

        private Dictionary<string, DateTime> _hitTargets = new Dictionary<string, DateTime>();
        readonly float _hitInterval = 0.5f;// 受击间隔

        private SoldierCard parentCard;
        private ISkill _skillCard;
        private GameObject raiseHand;

        public void StartAttack(ISkill skillCard, Vector3 direction)
        {
            parentCard = skillCard.Parent;
            _skillCard = skillCard;
            isAttacking = true;

            if (raiseHand == null)
            {
                // 抬手
                raiseHand = GamePlayManager.Default._resLoader.Get<GameObject>("MagicianWeapen01").Instantiate();
                raiseHand.transform.position = new Vector3(parentCard.transform.position.x, parentCard.transform.position.y + 1, 0);
                raiseHand.transform.SetParent(GameLand.Default.Bullet);
                raiseHand.LocalScale(Vector3.one);
            }

            // 计算子弹的旋转角度，使其朝向目标方向
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // 随机左右扫描
            AboutScan(50f, Random.Range(0, 1));

            // 使用方向向量计算远处目标位置
            transform.DOScale(new Vector3(1.5f, 100, 1), 0.3f);
            
            // 停止角色攻击
            parentCard.LockAttackState();
        }

        // 左右扫描
        private void AboutScan(float rotation, int about = 0)
        {
            if (!isAttacking) return;
            
            var duration = 5f;
            var rotationZ = transform.rotation.z * 100;
            
            if (about == 1)
            {
                // 1.右
                DOVirtual.Float(rotationZ, -rotation, duration, (v) =>
                {
                    transform.rotation = Quaternion.Euler(0, 0, v);
                }).OnComplete(() => {
                    AboutScan(rotation, 0);
                });
            }
            else
            {
                // 0.左
                DOVirtual.Float(rotationZ, rotation, duration, (v) =>
                {
                    transform.rotation = Quaternion.Euler(0, 0, v);
                }).OnComplete(() => {
                    AboutScan(rotation, 1);
                });
            }
        }

        public void StopAttack()
        {
            isAttacking = false;
            parentCard.UnlockAttackState();
            
            Destroy(gameObject);
            if (raiseHand) Destroy(raiseHand); // 销毁抬手对象
            _hitTargets.Clear();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.name != "HitBox")
            {
                return;
            }

            MonsterCard hitTarget = other.transform.parent.GetComponent<MonsterCard>();
            if (hitTarget != null && hitTarget.enabled)
            {
                if (CheckHitTargetDamage(hitTarget.uniqueId))
                {
                    _hitTargets[hitTarget.uniqueId] = DateTime.Now;
                    hitTarget.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(
                        hitTarget, _skillCard, other.bounds.ClosestPoint(transform.position)));
                }
            }

        }

        private bool CheckHitTargetDamage(string uniqueId)
        {
            if (!_hitTargets.ContainsKey(uniqueId)) return true;
            return ((DateTime.Now - _hitTargets[uniqueId]).TotalSeconds >= _hitInterval);
        }

        private IEnumerator DisableAfterDelay(ItemGenericObjectPool pool, GameObject damageObject, float delay)
        {
            // 等待指定的时间
            yield return new WaitForSeconds(delay);

            // 检查对象是否仍然有效
            if (damageObject != null)
            {
                pool.Return(damageObject);
            }
        }

        private void OnDestroy()
        {
            // 确保子弹销毁时，停止所有与其相关的 DOTween 动画
            DOTween.Kill(transform, true);
        }

    }
}
