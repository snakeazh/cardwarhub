using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.Skill;
using UnityEngine;
using QFramework;
using static UnityEngine.GraphicsBuffer;

namespace FusionContinent
{
    public partial class RaiseHand : ViewController
    {
        private float speed;
        private Tween pTween;
        private Vector3 direction;
        private int baseAtk;
        private bool IsDOMove = false;
        private Sequence damageSequence;

        // 新增逻辑
        private ISkill _skillCard;
        private HashSet<MonsterCard> hitTargets = new HashSet<MonsterCard>(); // 用于防止对同一个目标造成多次伤害
        private bool isDestroyed = false;
        private bool isPendingDestroy = false;

        private void Awake()
        {
        }

        public void Raise(ISkill skillCard)
        {
            this._skillCard = skillCard;
        }

        private void Update()
        {

        }

        /**
         * 碰撞检测（每帧）
         */
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.name != "HitBox" || _skillCard == null)
            {
                return;
            }

            MonsterCard hitTarget = collision.transform.parent.GetComponent<MonsterCard>();
            if (hitTarget != null && !hitTargets.Contains(hitTarget) && hitTarget.enabled != false)
            {
                hitTargets.Add(hitTarget);
                hitTarget.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(
                    hitTarget, _skillCard, collision.bounds.ClosestPoint(transform.position)));

                // 连续伤害逻辑
                var continuousDamageNum = (int)_skillCard.Attributes.GetFinal(AttributeType.SkillContinuousDamageNum);
                if (continuousDamageNum > 0)
                {
                    ApplyContinuousDamage(hitTarget, continuousDamageNum);
                }
            }
        }
        
        private void ApplyContinuousDamage(MonsterCard target, int continuousDamageNum)
        {
            // 使用 DOTween 的 Sequence 代替协程处理连续伤害
            damageSequence = DOTween.Sequence();

            for (int i = 0; i < continuousDamageNum; i++)
            {
                damageSequence.AppendCallback(() =>
                {
                    if (target != null)
                    {
                        target.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(target, _skillCard, this.transform.position));
                    }
                });
                damageSequence.AppendInterval(0.1f); // 每次伤害间隔
            }
        }

        private void OnDestroy()
        {
            // 确保子弹销毁时，停止所有与其相关的 DOTween 动画
            DOTween.Kill(transform, true);
        }
    }

}
