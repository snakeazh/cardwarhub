using System;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using QFramework;
using UnityEngine;

namespace FusionContinent.Assassin
{
    public class AssassinDance : MonoBehaviour
    {
        private ISkill _skillCard;
        
        // 受击
        private int _hitNum;
        private float _hitTime;
        readonly float _hitInterval = 0.5f;// 一秒受击一次
        
        public void SetData(ISkill skillCard)
        {
            this._skillCard = skillCard;
            if(_skillCard == null) {Destroy(this.transform.parent.parent.gameObject); return;}
        }

        private void Update()
        {
            _hitTime += Time.deltaTime;
            if (_hitTime >= _hitInterval)
            {
                _hitTime = 0f;
                _hitNum = 0;
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            // 如果已经触发过，直接返回
            if (collision.name != "HitBox")
            {
                return;
            }

            MonsterCard hitTarget = collision.transform.parent.GetComponent<MonsterCard>();
            if (hitTarget != null && hitTarget.enabled)
            {
                if(_skillCard == null) {Destroy(this.transform.parent.parent.gameObject); return;}
                if (_hitNum >= _skillCard.Attributes.GetFinal(AttributeType.SkillTrajectoryNum))
                {
                    return;
                }

                // 修改后的子弹发射部分
                GameObject assassinProjectile = GamePlayManager.Default._resLoader.Get<GameObject>("KarenWeapon01");
                ShootProjectile(hitTarget, collision, assassinProjectile);
            }
        }
        
        private void ShootProjectile(MonsterCard target, Collider2D collision,GameObject projectilePrefab)
        {
            _hitNum++;
            
            Vector3 startPos = transform.position;
            Vector3 targetPos = target.transform.position;
            Vector3 direction = (targetPos - startPos).normalized;

            GameObject bullet = Instantiate(projectilePrefab, startPos, Quaternion.identity);
            bullet.transform.SetParent(GameLand.Default.Bullet, true);
            bullet.transform.position = this.transform.position;
            bullet.transform.localScale = Vector3.one;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            float moveDuration = Vector3.Distance(startPos, targetPos) / 15f; // 调整速度
            bullet.transform.DOMove(targetPos, moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                Destroy(bullet);
                target.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(
                    target, _skillCard, collision.bounds.ClosestPoint(startPos)));
                // 受击Effect
                var archerDust = GamePlayManager.Default._resLoader
                    .Get<GameObject>("KarenSkillAssassinDance02").Instantiate();
                archerDust.transform.position = bullet.transform.position;
                archerDust.transform.SetParent(GameLand.Default.Bullet);
                Destroy(archerDust, 0.2f);
            });
        }
        
    }
}